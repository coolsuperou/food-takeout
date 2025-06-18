using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using food_takeout.Models;
using System.IO;
using food_takeout.Helpers;
using System.Drawing;

namespace food_takeout.Controllers
{
    public class MerchantController : Controller
    {
        private FoodContext db = new FoodContext();

        // 获取当前登录商家的ID
        private int? GetMerchantId()
        {
            if (Session["UserId"] == null)
            {
                return null;
            }

            int merchantId;
            if (int.TryParse(Session["UserId"].ToString(), out merchantId))
            {
                return merchantId;
            }
            return null;
        }

        // GET: Merchant/Dashboard
        public ActionResult Dashboard()
        {
            try
            {
                int merchantId;
                if (!IsAuthenticated(out merchantId))
                {
                    return RedirectToAction("Login", "Account");
                }

                using (var db = new FoodContext())
                {
                    // 获取商家
                    var merchant = db.Merchants.Find(merchantId);
                    if (merchant == null)
                    {
                        return RedirectToAction("Login", "Account", new { returnUrl = "/Merchant/Dashboard" });
                    }

                    // 获取餐厅ID列表
                    var restaurantIds = db.Restaurants
                        .Where(r => r.MerchantId == merchant.MerchantId)
                        .Select(r => r.RestaurantId)
                        .ToList();

                    if (restaurantIds.Count == 0)
                    {
                        // 商家未关联餐厅，重定向到创建餐厅页面
                        return RedirectToAction("Create", "Restaurants");
                    }

                    // 获取所有餐厅
                    var restaurants = db.Restaurants
                        .Where(r => restaurantIds.Contains(r.RestaurantId))
                        .ToList();

                    try
                    {
                        // 获取今日所有订单（用于其他统计）
                        var todayOrders = db.Orders
                            .Where(o => restaurantIds.Contains(o.RestaurantId) && 
                                   o.CreatedAt >= DateTime.Today && o.CreatedAt < DateTime.Today.AddDays(1))
                            .ToList();
                        
                        // 获取待处理订单 - 参考MerchantOrders方法
                        var pendingOrders = db.Orders
                            .Include(o => o.Customer)
                            .Include(o => o.Rider)
                            .Include(o => o.Restaurant)
                            .Include(o => o.OrderDetails.Select(od => od.Dish))
                            .Where(o => restaurantIds.Contains(o.RestaurantId) && 
                                   (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Accepted))
                            .OrderByDescending(o => o.CreatedAt)
                            .Take(5)
                            .ToList();
                        
                        // 详细记录订单数据
                        System.Diagnostics.Debug.WriteLine($"Dashboard: 尝试获取所有订单");
                        var allOrders = db.Orders
                            .Include(o => o.Customer)
                            .Include(o => o.Rider)
                            .Include(o => o.Restaurant)
                            .Where(o => restaurantIds.Contains(o.RestaurantId))
                            .OrderByDescending(o => o.CreatedAt)
                            .Take(10)
                            .ToList();
                            
                        System.Diagnostics.Debug.WriteLine($"Dashboard: 餐厅IDs: {string.Join(", ", restaurantIds)}");
                        System.Diagnostics.Debug.WriteLine($"Dashboard: 找到所有订单数量: {allOrders.Count}");
                        foreach (var order in allOrders)
                        {
                            System.Diagnostics.Debug.WriteLine($"所有订单 - ID: {order.OrderId}, 订单号: {order.OrderNumber}, 餐厅ID: {order.RestaurantId}, 状态: {order.Status}, 顾客ID: {order.CustomerId}");
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"Dashboard: 找到待处理订单数量: {pendingOrders.Count}");
                        foreach (var order in pendingOrders)
                        {
                            System.Diagnostics.Debug.WriteLine($"待处理订单 - ID: {order.OrderId}, 订单号: {order.OrderNumber}, 餐厅ID: {order.RestaurantId}, 状态: {order.Status}, 顾客ID: {order.CustomerId}");
                        }
                        
                        // 如果没有找到任何待处理订单，则尝试获取最近的5个订单作为替代
                        if (pendingOrders.Count == 0 && allOrders.Count > 0)
                        {
                            System.Diagnostics.Debug.WriteLine("Dashboard: 没有待处理订单，使用最近的订单");
                            pendingOrders = allOrders.Take(5).ToList();
                        }
                        
                        // 为视图准备数据
                        ViewBag.PendingOrders = pendingOrders;
                        ViewBag.PendingOrdersCount = pendingOrders.Count;
                        
                        // 直接将订单传递给视图模型
                        TempData["DisplayOrders"] = pendingOrders;
                        
                        // 检查ViewBag和TempData的值
                        System.Diagnostics.Debug.WriteLine($"Dashboard: ViewBag.PendingOrders设置为{(ViewBag.PendingOrders != null ? "非空值" : "null")}");
                        System.Diagnostics.Debug.WriteLine($"Dashboard: TempData[\"DisplayOrders\"]设置为{(TempData["DisplayOrders"] != null ? "非空值" : "null")}");
                    
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"加载订单数据出错: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                        ViewBag.PendingOrders = new List<Order>();
                        ViewBag.PendingOrdersCount = 0;
                    }
                    
                    try
                    {
                        // 热销菜品数据
                        var hotDishes = db.Dishes
                            .Where(d => restaurantIds.Contains(d.RestaurantId) && d.IsAvailable)
                            .OrderBy(d => Guid.NewGuid())
                            .Take(8)
                            .ToList();

                        ViewBag.HotDishes = hotDishes;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"加载菜品数据出错: {ex.Message}");
                        ViewBag.HotDishes = new List<Dish>();
                    }
                    
                    try
                    {
                        // 计算今日营收总额（排除已取消的订单，不包含配送费）
                        decimal todayRevenue = 0;
                        System.Diagnostics.Debug.WriteLine("========== 开始计算今日营收 ==========");
                        
                        // 直接查询已完成配送的订单（已送达的订单）
                        var completedOrders = db.Orders
                            .Where(o => restaurantIds.Contains(o.RestaurantId) && 
                                   o.Status == OrderStatus.Delivered &&
                                   o.ActualDeliveryTime >= DateTime.Today && o.ActualDeliveryTime < DateTime.Today.AddDays(1))
                            .ToList();
                        
                        foreach (var order in completedOrders)
                        {
                            decimal orderRevenue = order.TotalAmount - order.DeliveryFee;
                            todayRevenue += orderRevenue;
                            System.Diagnostics.Debug.WriteLine($"已完成订单 {order.OrderId}: 总额={order.TotalAmount}, 配送费={order.DeliveryFee}, 商家收入={orderRevenue}");
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"今日完成订单数: {completedOrders.Count}, 总营收: {todayRevenue}");
                        System.Diagnostics.Debug.WriteLine("========== 结束计算今日营收 ==========");
                        
                        // 计算总营收（所有已完成的订单，不包含配送费）
                        var allCompletedOrders = db.Orders
                            .Where(o => restaurantIds.Contains(o.RestaurantId) && 
                                   o.Status == OrderStatus.Delivered)
                            .ToList();
                        
                        decimal totalRevenue = 0;
                        foreach (var order in allCompletedOrders)
                        {
                            totalRevenue += (order.TotalAmount - order.DeliveryFee);
                        }
                        System.Diagnostics.Debug.WriteLine($"总完成订单数: {allCompletedOrders.Count}, 总营收: {totalRevenue}");
                        
                        // 获取昨日数据用于比较
                        var yesterday = DateTime.Today.AddDays(-1);
                        var dayBeforeYesterday = yesterday.AddDays(-1);
                        
                        // 获取昨日已完成订单
                        var yesterdayCompletedOrders = db.Orders
                            .Where(o => restaurantIds.Contains(o.RestaurantId) && 
                                   o.Status == OrderStatus.Delivered &&
                                   o.ActualDeliveryTime >= yesterday && o.ActualDeliveryTime < DateTime.Today)
                            .ToList();
                        
                        int yesterdayOrdersCount = yesterdayCompletedOrders.Count;
                        decimal yesterdayRevenue = 0;
                        foreach (var order in yesterdayCompletedOrders)
                        {
                            decimal orderRevenue = order.TotalAmount - order.DeliveryFee;
                            yesterdayRevenue += orderRevenue;
                            System.Diagnostics.Debug.WriteLine($"昨日已完成订单 {order.OrderId}: 总额={order.TotalAmount}, 配送费={order.DeliveryFee}, 商家收入={orderRevenue}");
                        }
                        System.Diagnostics.Debug.WriteLine($"昨日完成订单数: {yesterdayOrdersCount}, 总营收: {yesterdayRevenue}");
                        
                        // 计算增长百分比
                        double ordersPercentage = 0;
                        if (yesterdayOrdersCount > 0)
                        {
                            ordersPercentage = Math.Round(((double)completedOrders.Count - yesterdayOrdersCount) / yesterdayOrdersCount * 100, 1);
                        }
                        
                        double revenuePercentage = 0;
                        if (yesterdayRevenue > 0)
                        {
                            revenuePercentage = Math.Round(((double)todayRevenue - (double)yesterdayRevenue) / (double)yesterdayRevenue * 100, 1);
                        }
                        
                        // 设置ViewBag数据
                        ViewBag.TodayOrdersCount = completedOrders.Count;
                        ViewBag.YesterdayOrdersCount = yesterdayOrdersCount;
                        ViewBag.TodayRevenue = todayRevenue;
                        ViewBag.YesterdayRevenue = yesterdayRevenue;
                        ViewBag.OrdersPercentage = ordersPercentage;
                        ViewBag.RevenuePercentage = revenuePercentage;
                        ViewBag.TotalRevenue = totalRevenue;
                        ViewBag.TotalOrdersCount = allCompletedOrders.Count;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"计算营收数据出错: {ex.Message}");
                        // 设置默认值
                        ViewBag.TodayOrdersCount = 0;
                        ViewBag.YesterdayOrdersCount = 0;
                        ViewBag.TodayRevenue = 0m;
                        ViewBag.YesterdayRevenue = 0m;
                        ViewBag.OrdersPercentage = 0;
                        ViewBag.RevenuePercentage = 0;
                        ViewBag.TotalRevenue = 0m;
                        ViewBag.TotalOrdersCount = 0;
                    }
                    
                    // 如果TempData中有刷新的数据，优先使用TempData中的数据
                    if (TempData["TodayOrdersCount"] != null)
                    {
                        ViewBag.TodayOrdersCount = TempData["TodayOrdersCount"];
                    }
                    if (TempData["TodayRevenue"] != null)
                    {
                        ViewBag.TodayRevenue = TempData["TodayRevenue"];
                    }
                    if (TempData["OrdersPercentage"] != null)
                    {
                        ViewBag.OrdersPercentage = TempData["OrdersPercentage"];
                    }
                    if (TempData["RevenuePercentage"] != null)
                    {
                        ViewBag.RevenuePercentage = TempData["RevenuePercentage"];
                    }
                    if (TempData["TotalRevenue"] != null)
                    {
                        ViewBag.TotalRevenue = TempData["TotalRevenue"];
                    }
                    if (TempData["TotalOrdersCount"] != null)
                    {
                        ViewBag.TotalOrdersCount = TempData["TotalOrdersCount"];
                    }
                    
                    ViewBag.TotalRestaurants = restaurants.Count;
                    ViewBag.ActiveOrders = ViewBag.PendingOrdersCount;

                    try
                    {
                        // 获取所有评论
                        var recentReviews = db.Reviews
                            .Include(r => r.Customer)
                            .Include(r => r.Restaurant)
                            .Where(r => restaurantIds.Contains(r.RestaurantId))
                            .OrderByDescending(r => r.CreatedAt)
                            .Take(5)
                            .ToList()
                            .Select(r => new
                            {
                                ReviewId = r.ReviewId,
                                CustomerName = r.Customer?.Name ?? r.Customer?.Username ?? "匿名用户",
                                RestaurantName = r.Restaurant?.Name ?? "未知餐厅",
                                Content = r.Content ?? "无评价内容",
                                Rating = r.Rating,
                                CreatedAt = r.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                            })
                            .ToList();

                        ViewBag.RecentReviews = recentReviews;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"加载评论数据出错: {ex.Message}");
                        ViewBag.RecentReviews = new List<object>();
                    }
                    
                    // 注意：这里不再使用模拟数据，使用真实计算的百分比
                    // ViewBag.OrdersPercentage和ViewBag.RevenuePercentage已在上面设置
                    ViewBag.OldestPendingOrderTime = ViewBag.PendingOrders != null && ((List<Order>)ViewBag.PendingOrders).Any() ? 
                        ((List<Order>)ViewBag.PendingOrders).OrderBy(o => o.CreatedAt).First().CreatedAt.ToString("HH:mm") : 
                        DateTime.Now.ToString("HH:mm");
                    
                    // 主餐厅信息 - 始终使用第一个餐厅
                    var primaryRestaurant = restaurants.FirstOrDefault();
                    
                    // 检查Session中是否已有餐厅状态
                    bool? restaurantStatus = null;
                    if (Session["RestaurantStatus"] != null)
                    {
                        restaurantStatus = (bool)Session["RestaurantStatus"];
                        System.Diagnostics.Debug.WriteLine($"Dashboard: 使用Session中的餐厅状态 - {(restaurantStatus.Value ? "营业中" : "休息中")}");
                    }
                    
                    // 确保这里传递的是正确的ID属性
                    if (primaryRestaurant != null)
                    {
                        // 如果Session中没有状态，从数据库获取
                        if (!restaurantStatus.HasValue)
                        {
                            restaurantStatus = primaryRestaurant.IsActive;
                            Session["RestaurantStatus"] = restaurantStatus;
                            Session["RestaurantId"] = primaryRestaurant.RestaurantId;
                            System.Diagnostics.Debug.WriteLine($"Dashboard: 从数据库获取餐厅状态 - {(restaurantStatus.Value ? "营业中" : "休息中")}");
                        }
                        else
                        {
                            // 如果数据库状态与Session状态不一致，更新数据库
                            if (primaryRestaurant.IsActive != restaurantStatus.Value)
                            {
                                System.Diagnostics.Debug.WriteLine($"Dashboard: 数据库状态({primaryRestaurant.IsActive})与Session状态({restaurantStatus.Value})不一致，将更新数据库");
                                primaryRestaurant.IsActive = restaurantStatus.Value;
                                primaryRestaurant.UpdateTime = DateTime.Now;
                                db.SaveChanges();
                            }
                        }
                        
                        ViewBag.Restaurant = new
                        {
                            Id = primaryRestaurant.RestaurantId, // 保留Id以支持旧代码
                            RestaurantId = primaryRestaurant.RestaurantId, 
                            Name = primaryRestaurant.Name,
                            IsActive = restaurantStatus.Value, // 使用Session中的状态
                            Logo = primaryRestaurant.LogoUrl ?? "/Content/Images/restaurant-placeholder.jpg", // 使用LogoUrl
                            Address = primaryRestaurant.Address,
                            PhoneNumber = primaryRestaurant.PhoneNumber, // 使用PhoneNumber代替Phone
                            Description = primaryRestaurant.Description,
                            OpeningHours = primaryRestaurant.BusinessHours ?? "10:00-22:00" // 添加营业时间
                        };
                    }
                    else
                    {
                        // 如果没有找到餐厅，提供默认值
                        ViewBag.Restaurant = new
                        {
                            Id = 0,
                            RestaurantId = 0,
                            Name = "请创建餐厅",
                            IsActive = false,
                            Logo = "/Content/Images/placeholders/100x100/restaurant.png",
                            Address = "",
                            PhoneNumber = "",
                            Description = "",
                            OpeningHours = "未设置"
                        };
                    }

                    // 模拟一些菜品品类和热销菜品数据
                    ViewBag.TopCategories = new List<object> {
                        new { Name = "主食", Sales = 120, Percentage = 35 },
                        new { Name = "小吃", Sales = 85, Percentage = 25 },
                        new { Name = "饮品", Sales = 68, Percentage = 20 },
                        new { Name = "其他", Sales = 68, Percentage = 20 }
                    };

                    // 销售数据
                    ViewBag.SalesData = GenerateMockSalesData();
                    
                    // 提取销售图表数据
                    var salesData = ViewBag.SalesData as dynamic;
                    if (salesData != null)
                    {
                        try
                        {
                            ViewBag.SalesChartDates = salesData.Dates;
                            ViewBag.SalesChartValues = salesData.Revenue;
                            
                            // 生成饼图数据
                            List<object> categoryData = new List<object>();
                            var categories = ViewBag.TopCategories as List<object>;
                            if (categories != null)
                            {
                                foreach (dynamic category in categories)
                                {
                                    categoryData.Add(new { value = category.Percentage, name = category.Name });
                                }
                                ViewBag.CategoryChartData = categoryData;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"提取销售图表数据出错: {ex.Message}");
                        }
                    }
                    
                    ViewBag.DataInitialized = true;  // 标记数据已初始化

                    return View();
                }
            }
            catch (Exception ex)
            {
                // 记录错误
                System.Diagnostics.Debug.WriteLine($"Dashboard错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                
                // 设置错误信息
                ViewBag.Error = "加载商家仪表盘时出错，请稍后再试";
                ViewBag.DataInitialized = false;  // 标记数据初始化失败
                ViewBag.ErrorDetails = ex.ToString();  // 调试时使用，生产环境应移除
                
                // 确保ViewBag中有必要的默认数据，避免视图中的空引用异常
                ViewBag.PendingOrders = new List<Order>();
                ViewBag.PendingOrdersCount = 0;
                ViewBag.HotDishes = new List<Dish>();
                ViewBag.TodayOrdersCount = 0;
                ViewBag.YesterdayOrdersCount = 0;
                ViewBag.TodayRevenue = 0m;
                ViewBag.YesterdayRevenue = 0m;
                ViewBag.OrdersPercentage = 0;
                ViewBag.RevenuePercentage = 0;
                ViewBag.TotalRevenue = 0m;
                ViewBag.TotalOrdersCount = 0;
                ViewBag.OldestPendingOrderTime = DateTime.Now.ToString("HH:mm");
                ViewBag.RecentReviews = new List<object>();
                ViewBag.TotalRestaurants = 0;
                ViewBag.ActiveOrders = 0;
                
                // 设置默认餐厅信息
                ViewBag.Restaurant = new
                {
                    Id = 0,
                    RestaurantId = 0,
                    Name = "请创建餐厅",
                    IsActive = false,
                    Logo = "/Content/Images/placeholders/100x100/restaurant.png",
                    Address = "",
                    PhoneNumber = "",
                    Description = "",
                    OpeningHours = "未设置"
                };
                
                // 生成模拟数据以确保图表能够正常显示
                var mockSalesData = GenerateMockSalesData();
                ViewBag.SalesData = mockSalesData;
                
                // 从模拟数据中提取图表所需的数据
                try
                {
                    dynamic salesData = mockSalesData;
                    ViewBag.SalesChartDates = salesData.Dates;
                    ViewBag.SalesChartValues = salesData.Revenue;
                    
                    // 生成饼图数据
                    List<object> categoryData = new List<object>
                    {
                        new { value = 35, name = "主食" },
                        new { value = 25, name = "小吃" },
                        new { value = 20, name = "饮品" },
                        new { value = 20, name = "其他" }
                    };
                    ViewBag.CategoryChartData = categoryData;
                }
                catch
                {
                    // 如果提取失败，提供默认值
                    ViewBag.SalesChartDates = new[] { "今日", "昨日", "前日", "4天前", "5天前", "6天前", "7天前" };
                    ViewBag.SalesChartValues = new[] { 0, 0, 0, 0, 0, 0, 0 };
                    ViewBag.CategoryChartData = new[]
                    {
                        new { value = 35, name = "主食" },
                        new { value = 25, name = "小吃" },
                        new { value = 20, name = "饮品" },
                        new { value = 20, name = "其他" }
                    };
                }
                
                return View();
            }
        }

        // GET: Merchant/MyRestaurants
        public ActionResult MyRestaurants()
        {
            // 检查用户是否登录且是商家
            if (Session["UserId"] == null || Session["UserType"] as string != UserTypes.Merchant)
            {
                return RedirectToAction("Login", "Account");
            }

            int merchantId;
            if (int.TryParse(Session["UserId"].ToString(), out merchantId))
            {
                var restaurants = db.Restaurants.Where(r => r.MerchantId == merchantId).ToList();
                
                // 向视图传递是否已有餐厅的信息
                ViewBag.HasRestaurant = restaurants.Any();
                
                return View(restaurants);
            }

            ViewBag.HasRestaurant = false;
            return View(new List<Restaurant>());
        }

        // GET: Merchant/CreateRestaurant
        public ActionResult CreateRestaurant()
        {
            // 检查用户是否登录且是商家
            if (Session["UserId"] == null || Session["UserType"] as string != UserTypes.Merchant)
            {
                return RedirectToAction("Login", "Account");
            }

            // 检查商家是否已经创建了餐厅
            int merchantId = GetMerchantId() ?? 0;
            var existingRestaurant = db.Restaurants.FirstOrDefault(r => r.MerchantId == merchantId);
            
            if (existingRestaurant != null)
            {
                TempData["ErrorMessage"] = "您已经创建了一个餐厅，每个商家只能创建一个餐厅。";
                return RedirectToAction("MyRestaurants");
            }

            return View();
        }

        // POST: Merchant/CreateRestaurant
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateRestaurant([Bind(Include = "Name,PhoneNumber,Address,Description,BusinessHours,Category")] Restaurant restaurant, HttpPostedFileBase imageFile)
        {
            // 检查用户是否登录且是商家
            if (Session["UserId"] == null || Session["UserType"] as string != UserTypes.Merchant)
            {
                return RedirectToAction("Login", "Account");
            }

            // 检查商家是否已经创建了餐厅
            int merchantId = GetMerchantId() ?? 0;
            var existingRestaurant = db.Restaurants.FirstOrDefault(r => r.MerchantId == merchantId);
            
            if (existingRestaurant != null)
            {
                TempData["ErrorMessage"] = "您已经创建了一个餐厅，每个商家只能创建一个餐厅。";
                return RedirectToAction("MyRestaurants");
            }

            if (ModelState.IsValid)
            {
                // 设置商家ID
                restaurant.MerchantId = merchantId;
                restaurant.CreateTime = DateTime.Now;
                restaurant.IsActive = true;
                restaurant.Username = $"rest_{Guid.NewGuid().ToString().Substring(0, 8)}";
                restaurant.Password = $"{Guid.NewGuid().ToString().Substring(0, 8)}";

                // 处理图片上传
                if (imageFile != null && imageFile.ContentLength > 0)
                {
                    try
                    {
                        restaurant.ImageUrl = FileUploadHelper.UploadFile(imageFile, "Content/Images/Restaurants");
                        restaurant.LogoUrl = restaurant.ImageUrl; // 同时设置LogoUrl，确保一致性
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "图片上传失败：" + ex.Message);
                        return View(restaurant);
                    }
                }
                else
                {
                    // 设置默认图片
                    restaurant.ImageUrl = "/Content/Images/Restaurants/default.jpg";
                    restaurant.LogoUrl = restaurant.ImageUrl;
                }

                db.Restaurants.Add(restaurant);
                db.SaveChanges();

                return RedirectToAction("MyRestaurants");
            }

            return View(restaurant);
        }

        // GET: Merchant/ToggleStatus/5
        public ActionResult ToggleStatus(int id)
        {
            System.Diagnostics.Debug.WriteLine($"====== ToggleStatus开始 - ID: {id} ======");
            
            try
        {
            // 检查用户是否登录且是商家
            if (Session["UserId"] == null || Session["UserType"] as string != UserTypes.Merchant)
            {
                    System.Diagnostics.Debug.WriteLine("用户未登录或不是商家");
                return RedirectToAction("Login", "Account");
            }

            int merchantId;
                if (!int.TryParse(Session["UserId"].ToString(), out merchantId))
                {
                    System.Diagnostics.Debug.WriteLine("用户ID格式无效");
                    TempData["ErrorMessage"] = "用户ID格式无效";
                    return RedirectToAction("Dashboard");
                }
                
                System.Diagnostics.Debug.WriteLine($"商家ID: {merchantId}, 餐厅ID: {id}");
                
                // 尝试使用SQL直接查询餐厅
                try 
                {
                    var query = "SELECT * FROM Restaurant WHERE RestaurantId = @id";
                    var parameters = new System.Data.SqlClient.SqlParameter[] { 
                        new System.Data.SqlClient.SqlParameter("@id", id) 
                    };
                    
                    var sqlResult = db.Database.SqlQuery<RestaurantDebugInfo>(query, parameters).ToList();
                    System.Diagnostics.Debug.WriteLine($"SQL查询结果: {sqlResult.Count} 个记录");
                    foreach (var r in sqlResult)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - ID: {r.RestaurantId}, 名称: {r.Name}, 商家ID: {r.MerchantId}");
                    }
                }
                catch (Exception sqlEx)
                {
                    System.Diagnostics.Debug.WriteLine($"SQL查询异常: {sqlEx.Message}");
                }
                
                var restaurant = db.Restaurants.Find(id);
                
                if (restaurant == null)
                {
                    System.Diagnostics.Debug.WriteLine($"找不到ID为 {id} 的餐厅");
                    TempData["ErrorMessage"] = $"找不到ID为 {id} 的餐厅";
                    return RedirectToAction("Dashboard");
                }
                
                System.Diagnostics.Debug.WriteLine($"找到餐厅 - ID: {restaurant.RestaurantId}, 名称: {restaurant.Name}, 商家ID: {restaurant.MerchantId}");
                
                if (restaurant.MerchantId != merchantId)
                {
                    System.Diagnostics.Debug.WriteLine($"餐厅所有权验证失败，餐厅商家ID: {restaurant.MerchantId}, 当前商家ID: {merchantId}");
                    TempData["ErrorMessage"] = "您没有权限修改此餐厅";
                    return RedirectToAction("Dashboard");
                }
                
                // 切换餐厅状态
                restaurant.IsActive = !restaurant.IsActive;
                restaurant.UpdateTime = DateTime.Now;
                db.Entry(restaurant).State = EntityState.Modified;
                db.SaveChanges();
                
                System.Diagnostics.Debug.WriteLine($"餐厅状态已更新为: {(restaurant.IsActive ? "营业中" : "已关闭")}");
                
                // 如果是AJAX请求，返回JSON结果
                if (Request.IsAjaxRequest())
                {
                    return Json(new { 
                        success = true, 
                        isActive = restaurant.IsActive, 
                        message = restaurant.IsActive ? "餐厅已恢复接单" : "餐厅已暂停接单" 
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ToggleStatus异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                TempData["ErrorMessage"] = $"操作失败: {ex.Message}";
            }
            
            System.Diagnostics.Debug.WriteLine("====== ToggleStatus结束 ======");
            return RedirectToAction("Dashboard");
        }

        // AJAX切换餐厅状态
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleStatusAjax(int id)
        {
            try
            {
                int merchantId;
                if (!IsAuthenticated(out merchantId))
                {
                    return Json(new { success = false, message = "请先登录" });
                }

                using (var db = new FoodContext())
                {
                    // 获取餐厅
                    var restaurant = db.Restaurants.Find(id);
                    if (restaurant == null)
                    {
                        return Json(new { success = false, message = "找不到餐厅信息" });
                    }

                    // 验证餐厅归属
                    if (restaurant.MerchantId != merchantId)
                    {
                        return Json(new { success = false, message = "无权操作此餐厅" });
                    }

                    // 切换状态
                    bool wasActive = restaurant.IsActive;
                    restaurant.IsActive = !restaurant.IsActive;
                    restaurant.UpdateTime = DateTime.Now;
                    db.SaveChanges();

                    // 如果是从关闭变为开启，则自动接受所有待处理订单
                    string additionalMessage = "";
                    if (!wasActive && restaurant.IsActive)
                    {
                        var pendingOrders = db.Orders
                            .Where(o => o.RestaurantId == id && o.Status == OrderStatus.Pending)
                            .ToList();

                        if (pendingOrders.Any())
                        {
                            int acceptedCount = 0;
                            foreach (var order in pendingOrders)
                            {
                                order.Status = OrderStatus.Preparing;
                                order.UpdatedAt = DateTime.Now;
                                order.AcceptedTime = DateTime.Now;
                                db.Entry(order).State = EntityState.Modified;
                                acceptedCount++;
                            }
                            db.SaveChanges();
                            additionalMessage = $"，并自动接受了 {acceptedCount} 个待处理订单";
                        }
                    }

                    // 返回成功结果
                    string statusText = restaurant.IsActive ? "营业中" : "休息中";
                    string buttonText = restaurant.IsActive ? "暂停营业" : "开始营业";
                    
                    return Json(new { 
                        success = true, 
                        message = $"餐厅状态已切换为：{statusText}{additionalMessage}",
                        isActive = restaurant.IsActive,
                        statusText = statusText,
                        buttonText = buttonText
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "操作失败: " + ex.Message });
            }
        }
        
        // 切换营业状态并接单
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleActiveAndAcceptOrders(int id)
        {
            try
            {
                int merchantId;
                if (!IsAuthenticated(out merchantId))
                {
                    return Json(new { success = false, message = "请先登录" });
                }

                using (var db = new FoodContext())
                {
                    // 获取餐厅
                    var restaurant = db.Restaurants.Find(id);
                    if (restaurant == null)
                    {
                        return Json(new { success = false, message = "找不到餐厅信息" });
                    }

                    // 验证餐厅归属
                    if (restaurant.MerchantId != merchantId)
                    {
                        return Json(new { success = false, message = "无权操作此餐厅" });
                    }

                    // 切换状态
                    bool wasActive = restaurant.IsActive;
                    restaurant.IsActive = !restaurant.IsActive;
                    restaurant.UpdateTime = DateTime.Now;
                    db.SaveChanges();

                    // 如果是从关闭变为开启，则自动接受所有待处理订单
                    string additionalMessage = "";
                    if (!wasActive && restaurant.IsActive)
                    {
                        var pendingOrders = db.Orders
                            .Where(o => o.RestaurantId == id && o.Status == OrderStatus.Pending)
                            .ToList();

                        if (pendingOrders.Any())
                        {
                            int acceptedCount = 0;
                            foreach (var order in pendingOrders)
                            {
                                order.Status = OrderStatus.Preparing;
                                order.UpdatedAt = DateTime.Now;
                                order.AcceptedTime = DateTime.Now;
                                db.Entry(order).State = EntityState.Modified;
                                acceptedCount++;
                            }
                            db.SaveChanges();
                            additionalMessage = $"，并自动接受了 {acceptedCount} 个待处理订单";
                        }
                    }

                    // 返回成功结果
                    string statusText = restaurant.IsActive ? "营业中" : "休息中";
                    string buttonText = restaurant.IsActive ? "暂停营业" : "开始营业";
                    
                    return Json(new { 
                        success = true, 
                        message = $"餐厅状态已切换为：{statusText}{additionalMessage}",
                        isActive = restaurant.IsActive,
                        statusText = statusText,
                        buttonText = buttonText
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "操作失败: " + ex.Message });
            }
        }

        // GET方法切换餐厅状态并接受订单
        [HttpGet]
        public ActionResult ToggleActiveAndAcceptOrdersGet()
        {
            try
            {
                // 检查用户是否登录且是商家
                if (Session["UserId"] == null || Session["UserType"] as string != UserTypes.Merchant)
                {
                    TempData["ErrorMessage"] = "请先登录";
                    return RedirectToAction("Login", "Account");
                }

                int merchantId;
                if (!int.TryParse(Session["UserId"].ToString(), out merchantId))
                {
                    TempData["ErrorMessage"] = "无效的用户ID";
                    return RedirectToAction("Dashboard");
                }
                
                // 获取商家的餐厅
                var restaurant = db.Restaurants.FirstOrDefault(r => r.MerchantId == merchantId);
                if (restaurant == null)
                {
                    TempData["ErrorMessage"] = "您尚未创建餐厅";
                    return RedirectToAction("Dashboard");
                }

                // 切换餐厅状态
                restaurant.IsActive = !restaurant.IsActive;
                
                // 保存状态到Session，确保全站同步
                Session["RestaurantStatus"] = restaurant.IsActive;
                
                // 设置状态通知，供_Layout.cshtml使用
                TempData["StatusNotification"] = restaurant.IsActive ? 
                    "已切换为营业中，开始接受订单" : 
                    "已切换为休息中，暂停接单";
                
                // 如果餐厅变为营业中，自动接受所有待处理的订单
                if (restaurant.IsActive)
                {
                    var pendingOrders = db.Orders
                        .Where(o => o.RestaurantId == restaurant.RestaurantId && o.Status == OrderStatus.Pending)
                        .ToList();

                    if (pendingOrders.Any())
                    {
                        foreach (var order in pendingOrders)
                        {
                            order.Status = OrderStatus.Accepted;
                            order.AcceptedTime = DateTime.Now;
                        }
                        
                        TempData["SuccessMessage"] = $"已自动接受 {pendingOrders.Count} 个待处理订单";
                    }
                }

                db.SaveChanges();
                
                System.Diagnostics.Debug.WriteLine($"餐厅状态已切换为: {(restaurant.IsActive ? "营业中" : "休息中")}");
                System.Diagnostics.Debug.WriteLine($"Session状态已更新为: {(bool)Session["RestaurantStatus"]}");

                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "切换餐厅状态失败: " + ex.Message;
                System.Diagnostics.Debug.WriteLine($"切换餐厅状态失败: {ex.Message}");
                return RedirectToAction("Dashboard");
            }
        }
        
        // 用于SQL调试的简单类
        public class RestaurantDebugInfo
        {
            public int RestaurantId { get; set; }
            public string Name { get; set; }
            public int? MerchantId { get; set; }
            public bool IsActive { get; set; }
        }

        // POST: Merchant/UpdateLogo/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateLogo(int id, HttpPostedFileBase imageFile)
        {
            if (imageFile == null || imageFile.ContentLength == 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "请选择要上传的图片");
            }

            try
            {
                var restaurant = db.Restaurants.Find(id);
                if (restaurant == null)
                {
                    return HttpNotFound();
                }

                // 验证餐厅所有权
                var merchantId = GetMerchantId() ?? 0;
                if (restaurant.MerchantId != merchantId)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "您没有权限修改此餐厅信息");
                }

                // 确保目录存在
                string uploadsDir = Path.Combine(Server.MapPath("~/Content/Images/Restaurants"));
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                    System.Diagnostics.Debug.WriteLine($"创建目录: {uploadsDir}");
                }
                
                // 生成文件名并保存
                string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(imageFile.FileName)}";
                string filePath = Path.Combine(uploadsDir, fileName);
                
                System.Diagnostics.Debug.WriteLine($"准备保存文件: {filePath}, 大小: {imageFile.ContentLength} 字节");
                
                // 保存文件
                imageFile.SaveAs(filePath);
                
                // 验证文件是否已保存
                if (System.IO.File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"文件保存成功: {filePath}");
                    
                    // 删除旧图片
                    if (!string.IsNullOrEmpty(restaurant.LogoUrl) && 
                        restaurant.LogoUrl.StartsWith("/Content/Images/") &&
                        !restaurant.LogoUrl.Contains("default.jpg"))
                    {
                        try
                        {
                            string oldFilePath = Server.MapPath(restaurant.LogoUrl);
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                                System.Diagnostics.Debug.WriteLine($"删除旧Logo: {oldFilePath}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"删除旧Logo失败: {ex.Message}");
                        }
                    }
                    
                    // 更新数据库中的Logo路径
                    string logoUrl = $"/Content/Images/Restaurants/{fileName}";
                    restaurant.LogoUrl = logoUrl;
                    restaurant.ImageUrl = logoUrl; // 同时更新ImageUrl字段，确保一致性
                    restaurant.UpdateTime = DateTime.Now;
                    
                    System.Diagnostics.Debug.WriteLine($"设置LogoUrl为: {restaurant.LogoUrl}");
                    System.Diagnostics.Debug.WriteLine($"设置ImageUrl为: {restaurant.ImageUrl}");
                    
                    db.Entry(restaurant).State = EntityState.Modified;
                    db.SaveChanges();
                    
                    // 验证更新
                    db.Entry(restaurant).Reload();
                    System.Diagnostics.Debug.WriteLine($"更新后的LogoUrl: {restaurant.LogoUrl}");
                    System.Diagnostics.Debug.WriteLine($"更新后的ImageUrl: {restaurant.ImageUrl}");
                    
                    TempData["SuccessMessage"] = "Logo更新成功！";
                    
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = true, imageUrl = logoUrl });
                    }
                    
                    // 清除缓存，确保下次访问Dashboard时重新加载数据
                    Response.Cache.SetCacheability(HttpCacheability.NoCache);
                    Response.Cache.SetNoStore();
                    
                    return RedirectToAction("Dashboard");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"文件保存失败: {filePath}");
                    TempData["ErrorMessage"] = "Logo保存失败：文件未能写入磁盘";
                    
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "Logo保存失败：文件未能写入磁盘" });
                    }
                    
                    return RedirectToAction("Dashboard");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logo上传异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                
                TempData["ErrorMessage"] = $"Logo上传失败: {ex.Message}";
                
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = $"Logo上传失败: {ex.Message}" });
                }
                
                return RedirectToAction("Dashboard");
            }
        }

        // GET: Merchant/RestaurantSettings/5
        public ActionResult RestaurantSettings(int? id)
        {
            // 检查用户是否登录且是商家
            if (Session["UserId"] == null || Session["UserType"] as string != UserTypes.Merchant)
            {
                return RedirectToAction("Login", "Account");
            }

            int merchantId = (int)Session["UserId"];
            System.Diagnostics.Debug.WriteLine($"GET RestaurantSettings - 商家ID: {merchantId}, 请求的餐厅ID: {id}");
            
            Restaurant restaurant;
            
            // 如果ID为0或null，尝试加载商家的第一个餐厅
            if (!id.HasValue || id.Value <= 0)
            {
                System.Diagnostics.Debug.WriteLine("ID为0或null，尝试加载商家的第一个餐厅");
                restaurant = db.Restaurants.FirstOrDefault(r => r.MerchantId == merchantId);
                
                if (restaurant == null)
                {
                    System.Diagnostics.Debug.WriteLine($"商家 {merchantId} 没有餐厅");
                    TempData["ErrorMessage"] = "您还没有创建餐厅，请先创建一个餐厅。";
                    return RedirectToAction("CreateRestaurant");
                }
                
                System.Diagnostics.Debug.WriteLine($"找到商家的第一个餐厅，ID: {restaurant.RestaurantId}");
            }
            else
            {
                // 尝试加载指定ID的餐厅
                restaurant = db.Restaurants.Find(id);
                
                if (restaurant == null)
                {
                    System.Diagnostics.Debug.WriteLine($"找不到ID为 {id} 的餐厅");
                    
                    // 如果找不到指定ID的餐厅，尝试加载商家的第一个餐厅
                    restaurant = db.Restaurants.FirstOrDefault(r => r.MerchantId == merchantId);
                    
                    if (restaurant == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"商家 {merchantId} 没有餐厅");
                        TempData["ErrorMessage"] = "找不到指定的餐厅，并且您还没有创建其他餐厅，请先创建一个餐厅。";
                        return RedirectToAction("CreateRestaurant");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"找到商家的第一个餐厅，ID: {restaurant.RestaurantId}");
                }
                else
                {
                    // 验证是否是当前商家的餐厅
                    if (restaurant.MerchantId != merchantId)
                    {
                        System.Diagnostics.Debug.WriteLine($"餐厅所有权验证失败，餐厅商家ID: {restaurant.MerchantId}, 当前商家ID: {merchantId}");
                        TempData["ErrorMessage"] = "您没有权限访问此餐厅的设置。";
                        return RedirectToAction("Dashboard");
                    }
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"加载餐厅成功 - ID: {restaurant.RestaurantId}, 名称: {restaurant.Name}, 地址: {restaurant.Address}");
            return View(restaurant);
        }

        // POST: Merchant/RestaurantSettings/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RestaurantSettings(Restaurant restaurant, HttpPostedFileBase logoFile)
        {
            try
            {
                // 手动验证模型，但忽略Username和Password字段
                ModelState.Remove("Username");
                ModelState.Remove("Password");
                
                if (ModelState.IsValid)
                {
                    var merchantId = (int)Session["UserId"];
                    var merchant = db.Merchants.Find(merchantId);
                    
                    if (merchant == null)
                    {
                        TempData["ErrorMessage"] = "找不到商家信息，请确认您已正确登录。";
                        return View(restaurant);
                    }
                    
                    var existingRestaurant = db.Restaurants.Find(restaurant.RestaurantId);
                    
                    if (existingRestaurant == null)
                    {
                        TempData["ErrorMessage"] = "找不到店铺信息，店铺ID无效。";
                        return View(restaurant);
                    }

                    // 记录更新前的值，用于调试
                    System.Diagnostics.Debug.WriteLine($"更新前: ID={existingRestaurant.RestaurantId}, 名称={existingRestaurant.Name}");
                    
                    // 更新餐厅信息，但保留Username和Password
                    existingRestaurant.Name = restaurant.Name;
                    existingRestaurant.Description = restaurant.Description;
                    existingRestaurant.Address = restaurant.Address;
                    existingRestaurant.PhoneNumber = restaurant.PhoneNumber;
                    existingRestaurant.BusinessHours = restaurant.BusinessHours;
                    existingRestaurant.DeliveryFee = restaurant.DeliveryFee;
                    existingRestaurant.MinimumOrderAmount = restaurant.MinimumOrderAmount;
                    existingRestaurant.EstimatedDeliveryTime = restaurant.EstimatedDeliveryTime;
                    existingRestaurant.Cuisine = restaurant.Cuisine;
                    existingRestaurant.IsActive = restaurant.IsActive;
                    existingRestaurant.IsHot = restaurant.IsHot;
                    existingRestaurant.Location = restaurant.Location;
                    existingRestaurant.Category = restaurant.Category;
                    existingRestaurant.Categories = restaurant.Categories;
                    existingRestaurant.DeliveryTime = restaurant.DeliveryTime;
                    existingRestaurant.UpdateTime = DateTime.Now;
                    // Username和Password不从表单更新
                
                // 处理Logo上传
                if (logoFile != null && logoFile.ContentLength > 0)
                    {
                        try
                        {
                            // 确保目录存在
                            string uploadsDir = Path.Combine(Server.MapPath("~/Content/Images/Restaurants"));
                            if (!Directory.Exists(uploadsDir))
                            {
                                Directory.CreateDirectory(uploadsDir);
                                System.Diagnostics.Debug.WriteLine($"创建目录: {uploadsDir}");
                            }
                            
                            // 生成文件名并保存
                            string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(logoFile.FileName)}";
                            string filePath = Path.Combine(uploadsDir, fileName);
                            
                            System.Diagnostics.Debug.WriteLine($"准备保存文件: {filePath}, 大小: {logoFile.ContentLength} 字节");
                            
                            // 保存文件
                            logoFile.SaveAs(filePath);
                            
                            // 验证文件是否已保存
                            if (System.IO.File.Exists(filePath))
                            {
                                System.Diagnostics.Debug.WriteLine($"文件保存成功: {filePath}");
                                
                                // 更新数据库中的Logo路径
                                existingRestaurant.LogoUrl = $"/Content/Images/Restaurants/{fileName}";
                                existingRestaurant.ImageUrl = $"/Content/Images/Restaurants/{fileName}"; // 同时更新ImageUrl字段
                                
                                System.Diagnostics.Debug.WriteLine($"设置LogoUrl为: {existingRestaurant.LogoUrl}");
                                System.Diagnostics.Debug.WriteLine($"设置ImageUrl为: {existingRestaurant.ImageUrl}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"文件保存失败: {filePath}");
                                ModelState.AddModelError("LogoUrl", "Logo保存失败：文件未能写入磁盘");
                                TempData["ErrorMessage"] = "Logo保存失败：文件未能写入磁盘";
                                return View(restaurant);
                            }
                        }
                        catch (Exception ex)
                        {
                            // 记录Logo上传错误
                            System.Diagnostics.Debug.WriteLine($"Logo上传异常: {ex.Message}");
                            System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                            
                            ModelState.AddModelError("LogoUrl", $"Logo上传失败: {ex.Message}");
                            TempData["ErrorMessage"] = $"Logo上传失败: {ex.Message}";
                            return View(restaurant);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("没有上传Logo文件或文件为空");
                    }
                    
                    try
                    {
                        // 尝试保存更改
                        db.Entry(existingRestaurant).State = EntityState.Modified;
                        
                        // 记录更新前的值
                        System.Diagnostics.Debug.WriteLine($"保存前的值: ID={existingRestaurant.RestaurantId}, 名称={existingRestaurant.Name}, 地址={existingRestaurant.Address}, 电话={existingRestaurant.PhoneNumber}");
                        System.Diagnostics.Debug.WriteLine($"保存前的值: 营业时间={existingRestaurant.BusinessHours}, 配送费={existingRestaurant.DeliveryFee}, 最低订单金额={existingRestaurant.MinimumOrderAmount}");
                        
                        // 记录实体状态
                        System.Diagnostics.Debug.WriteLine($"实体状态: {db.Entry(existingRestaurant).State}");
                        
                        // 尝试保存更改
                        int result = db.SaveChanges();
                        
                        // 记录更新结果
                        System.Diagnostics.Debug.WriteLine($"SaveChanges结果: {result}, 更新后ID={existingRestaurant.RestaurantId}");
                        
                        // 从数据库重新加载实体，验证更改是否已保存
                        db.Entry(existingRestaurant).Reload();
                        System.Diagnostics.Debug.WriteLine($"重新加载后的值: ID={existingRestaurant.RestaurantId}, 名称={existingRestaurant.Name}, 地址={existingRestaurant.Address}");
                        System.Diagnostics.Debug.WriteLine($"重新加载后的值: 营业时间={existingRestaurant.BusinessHours}, 配送费={existingRestaurant.DeliveryFee}, 最低订单金额={existingRestaurant.MinimumOrderAmount}");
                        
                        if (result > 0)
                        {
                            // 直接从数据库重新查询以验证更新
                            var updatedRestaurant = db.Restaurants.Find(existingRestaurant.RestaurantId);
                            System.Diagnostics.Debug.WriteLine($"数据库验证 - ID: {updatedRestaurant.RestaurantId}, 名称: {updatedRestaurant.Name}");
                            System.Diagnostics.Debug.WriteLine($"数据库验证 - LogoUrl: {updatedRestaurant.LogoUrl}, ImageUrl: {updatedRestaurant.ImageUrl}");
                            
                            // 清除缓存，确保下次访问Dashboard时重新加载数据
                            Response.Cache.SetCacheability(HttpCacheability.NoCache);
                            Response.Cache.SetNoStore();
                            
                            TempData["SuccessMessage"] = "店铺设置已成功保存！";
                            return RedirectToAction("RestaurantSettings", new { id = existingRestaurant.RestaurantId });
                        }
                        else
                        {
                            // 如果SaveChanges返回0，尝试使用SQL直接更新
                            System.Diagnostics.Debug.WriteLine("Entity Framework保存失败，尝试使用SQL直接更新");
                            
                            string sql = @"UPDATE Restaurant SET 
                                Name = @Name, 
                                Description = @Description,
                                Address = @Address,
                                PhoneNumber = @PhoneNumber,
                                BusinessHours = @BusinessHours,
                                DeliveryFee = @DeliveryFee,
                                MinimumOrderAmount = @MinimumOrderAmount,
                                EstimatedDeliveryTime = @EstimatedDeliveryTime,
                                Cuisine = @Cuisine,
                                IsActive = @IsActive,
                                IsHot = @IsHot,
                                Location = @Location,
                                Category = @Category,
                                Categories = @Categories,
                                DeliveryTime = @DeliveryTime,
                                UpdateTime = @UpdateTime,
                                LogoUrl = @LogoUrl,
                                ImageUrl = @ImageUrl
                                WHERE RestaurantId = @RestaurantId";
                                
                            var parameters = new System.Data.SqlClient.SqlParameter[] {
                                new System.Data.SqlClient.SqlParameter("@Name", existingRestaurant.Name),
                                new System.Data.SqlClient.SqlParameter("@Description", (object)existingRestaurant.Description ?? DBNull.Value),
                                new System.Data.SqlClient.SqlParameter("@Address", existingRestaurant.Address),
                                new System.Data.SqlClient.SqlParameter("@PhoneNumber", (object)existingRestaurant.PhoneNumber ?? DBNull.Value),
                                new System.Data.SqlClient.SqlParameter("@BusinessHours", (object)existingRestaurant.BusinessHours ?? DBNull.Value),
                                new System.Data.SqlClient.SqlParameter("@DeliveryFee", existingRestaurant.DeliveryFee),
                                new System.Data.SqlClient.SqlParameter("@MinimumOrderAmount", existingRestaurant.MinimumOrderAmount),
                                new System.Data.SqlClient.SqlParameter("@EstimatedDeliveryTime", existingRestaurant.EstimatedDeliveryTime),
                                new System.Data.SqlClient.SqlParameter("@Cuisine", (object)existingRestaurant.Cuisine ?? DBNull.Value),
                                new System.Data.SqlClient.SqlParameter("@IsActive", existingRestaurant.IsActive),
                                new System.Data.SqlClient.SqlParameter("@IsHot", existingRestaurant.IsHot),
                                new System.Data.SqlClient.SqlParameter("@Location", (object)existingRestaurant.Location ?? DBNull.Value),
                                new System.Data.SqlClient.SqlParameter("@Category", (object)existingRestaurant.Category ?? DBNull.Value),
                                new System.Data.SqlClient.SqlParameter("@Categories", (object)existingRestaurant.Categories ?? DBNull.Value),
                                new System.Data.SqlClient.SqlParameter("@DeliveryTime", existingRestaurant.DeliveryTime),
                                new System.Data.SqlClient.SqlParameter("@UpdateTime", DateTime.Now),
                                new System.Data.SqlClient.SqlParameter("@LogoUrl", (object)existingRestaurant.LogoUrl ?? DBNull.Value),
                                new System.Data.SqlClient.SqlParameter("@ImageUrl", (object)existingRestaurant.ImageUrl ?? DBNull.Value),
                                new System.Data.SqlClient.SqlParameter("@RestaurantId", existingRestaurant.RestaurantId)
                            };
                            
                            int sqlResult = db.Database.ExecuteSqlCommand(sql, parameters);
                            System.Diagnostics.Debug.WriteLine($"SQL更新结果: {sqlResult}");
                            
                            if (sqlResult > 0)
                            {
                                // 直接从数据库重新查询以验证SQL更新
                                var updatedRestaurant = db.Restaurants.Find(existingRestaurant.RestaurantId);
                                System.Diagnostics.Debug.WriteLine($"SQL更新验证 - ID: {updatedRestaurant.RestaurantId}, 名称: {updatedRestaurant.Name}");
                                System.Diagnostics.Debug.WriteLine($"SQL更新验证 - LogoUrl: {updatedRestaurant.LogoUrl}, ImageUrl: {updatedRestaurant.ImageUrl}");
                                
                                // 清除缓存，确保下次访问Dashboard时重新加载数据
                                Response.Cache.SetCacheability(HttpCacheability.NoCache);
                                Response.Cache.SetNoStore();
                                
                                TempData["SuccessMessage"] = "店铺设置已成功保存！(SQL方式)";
                                return RedirectToAction("RestaurantSettings", new { id = existingRestaurant.RestaurantId });
                            }
                            else
                            {
                                TempData["ErrorMessage"] = "保存失败，数据库未更新任何记录。";
                                return View(restaurant);
                            }
                        }
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        // 处理并发异常
                        System.Diagnostics.Debug.WriteLine($"并发异常: {ex.Message}");
                        foreach (var entry in ex.Entries)
                        {
                            System.Diagnostics.Debug.WriteLine($"受影响的实体: {entry.Entity.GetType().Name}");
                        }
                        TempData["ErrorMessage"] = $"保存失败，数据并发错误: {ex.Message}";
                        return View(restaurant);
                    }
                    catch (DbUpdateException ex)
                    {
                        // 处理数据库更新异常
                        var innerException = ex.InnerException?.InnerException;
                        string errorMessage = innerException?.Message ?? ex.Message;
                        System.Diagnostics.Debug.WriteLine($"数据库更新异常: {errorMessage}");
                        TempData["ErrorMessage"] = $"保存失败，数据库错误: {errorMessage}";
                        return View(restaurant);
                    }
                    catch (Exception ex)
                    {
                        // 处理其他异常
                        System.Diagnostics.Debug.WriteLine($"未知异常: {ex.Message}");
                        TempData["ErrorMessage"] = $"保存失败，发生未知错误: {ex.Message}";
                        return View(restaurant);
                    }
                }
                else
                {
                    // 记录模型验证错误
                    foreach (var state in ModelState)
                    {
                        foreach (var error in state.Value.Errors)
                        {
                            System.Diagnostics.Debug.WriteLine($"模型验证错误 - {state.Key}: {error.ErrorMessage}");
                        }
                    }
                }
                
                return View(restaurant);
            }
            catch (Exception ex)
            {
                // 处理整体异常
                System.Diagnostics.Debug.WriteLine($"RestaurantSettings操作异常: {ex.Message}");
                TempData["ErrorMessage"] = $"操作失败: {ex.Message}";
                return View(restaurant);
            }
        }

        // GET: Merchant/DefaultLogo
        public ActionResult DefaultLogo()
        {
            // 返回一个简单的默认Logo图片
            byte[] imageBytes = GenerateDefaultLogo();
            return File(imageBytes, "image/png");
        }

        private byte[] GenerateDefaultLogo()
        {
            // 创建一个简单的默认Logo图片
            using (var bitmap = new System.Drawing.Bitmap(200, 200))
            using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
            {
                // 填充背景
                graphics.Clear(System.Drawing.Color.FromArgb(255, 79, 205, 196)); // 使用主题色

                // 绘制文字
                using (var font = new System.Drawing.Font("Arial", 40, System.Drawing.FontStyle.Bold))
                using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.White))
                {
                    var text = "R";
                    var size = graphics.MeasureString(text, font);
                    graphics.DrawString(text, font, brush, 
                        (bitmap.Width - size.Width) / 2, 
                        (bitmap.Height - size.Height) / 2);
                }

                // 转换为字节数组
                using (var stream = new System.IO.MemoryStream())
                {
                    bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    return stream.ToArray();
                }
            }
        }

        // GET: Merchant/TestToggleStatus/5
        public ActionResult TestToggleStatus(int id)
        {
            System.Diagnostics.Debug.WriteLine("\n\n");
            System.Diagnostics.Debug.WriteLine("======= 开始完整测试流程 =======");
            
            try
            {
                // 检查登录状态
                System.Diagnostics.Debug.WriteLine($"Session信息: UserId={Session["UserId"]}, UserType={Session["UserType"]}");
                
                int merchantId;
                if (Session["UserId"] == null || !int.TryParse(Session["UserId"].ToString(), out merchantId))
                {
                    System.Diagnostics.Debug.WriteLine("用户未登录或ID无效");
                    TempData["ErrorMessage"] = "请先登录";
                    return RedirectToAction("Login", "Account");
                }
                
                System.Diagnostics.Debug.WriteLine($"测试餐厅ID: {id}，商家ID: {merchantId}");
                
                // 简化流程：找到所有商家的餐厅
                var merchantRestaurants = db.Restaurants.Where(r => r.MerchantId == merchantId).ToList();
                System.Diagnostics.Debug.WriteLine($"当前商家拥有 {merchantRestaurants.Count} 个餐厅:");
                foreach (var r in merchantRestaurants)
                {
                    System.Diagnostics.Debug.WriteLine($"  - ID: {r.RestaurantId}, 名称: {r.Name}");
                }
                
                // 如果ID为0，尝试找到第一个餐厅
                Restaurant restaurant = null;
                if (id == 0 && merchantRestaurants.Any())
                {
                    restaurant = merchantRestaurants.First();
                    id = restaurant.RestaurantId;
                    System.Diagnostics.Debug.WriteLine($"使用第一个找到的餐厅 - ID: {id}, 名称: {restaurant.Name}");
                }
                else
                {
                    restaurant = db.Restaurants.Find(id);
                }
                
                // 强制切换状态
                if (restaurant != null)
                {
                    System.Diagnostics.Debug.WriteLine($"找到餐厅 - ID: {restaurant.RestaurantId}, 名称: {restaurant.Name}, 当前状态: {(restaurant.IsActive ? "营业中" : "已关闭")}");
                    
                    // 直接切换状态
                    restaurant.IsActive = !restaurant.IsActive;
                    restaurant.UpdateTime = DateTime.Now;
                    
                    System.Diagnostics.Debug.WriteLine($"强制切换状态 - 新状态: {(restaurant.IsActive ? "营业中" : "已关闭")}");
                    
                    try
                    {
                        db.Entry(restaurant).State = EntityState.Modified;
                        db.SaveChanges();
                        System.Diagnostics.Debug.WriteLine("保存成功！");
                        TempData["SuccessMessage"] = $"餐厅状态已切换为: {(restaurant.IsActive ? "营业中" : "已暂停接单")}";
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"保存时出错: {ex.Message}");
                        TempData["ErrorMessage"] = $"保存出错: {ex.Message}";
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("找不到指定的餐厅。");
                    
                    // 如果找不到指定餐厅但有其他餐厅，提供所有餐厅的状态切换链接
                    if (merchantRestaurants.Any())
                    {
                        string restaurants = string.Join(", ", merchantRestaurants.Select(r => 
                            $"<a href='{Url.Action("TestToggleStatus", "Merchant", new { id = r.RestaurantId })}'>切换 {r.Name} (ID:{r.RestaurantId})</a>"));
                        
                        TempData["ErrorMessage"] = $"找不到指定的餐厅，但您有以下餐厅: {restaurants}";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "找不到任何餐厅，请先创建餐厅";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"测试过程中发生异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                TempData["ErrorMessage"] = $"操作失败: {ex.Message}";
            }
            
            System.Diagnostics.Debug.WriteLine("======= 测试流程结束 =======\n");
            return RedirectToAction("Dashboard");
        }

        // 直接切换餐厅状态的操作（不使用AJAX，适用于AJAX不工作时）
        public ActionResult DirectToggleStatus()
        {
            try
            {
                int merchantId;
                if (!IsAuthenticated(out merchantId))
                {
                    return RedirectToAction("Login", "Account");
                }

                using (var db = new FoodContext())
                {
                    // 获取第一个餐厅（简化处理）
                    var restaurant = db.Restaurants
                        .FirstOrDefault(r => r.MerchantId == merchantId);

                    if (restaurant == null)
                    {
                        TempData["ErrorMessage"] = "找不到相关餐厅";
                        return RedirectToAction("Dashboard");
                    }

                    // 切换状态
                    restaurant.IsActive = !restaurant.IsActive;
                    restaurant.UpdateTime = DateTime.Now;
                    db.SaveChanges();

                    string statusText = restaurant.IsActive ? "营业中" : "休息中";
                    TempData["SuccessMessage"] = $"餐厅状态已切换为：{statusText}";
                }
                
                // 重定向回仪表盘
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "切换餐厅状态失败: " + ex.Message;
                return RedirectToAction("Dashboard");
            }
        }

        // 设置餐厅状态方法 - 允许商家手动固定餐厅状态
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SetRestaurantStatus(int id, bool isActive)
        {
            try
            {
                int merchantId;
                if (!IsAuthenticated(out merchantId))
                {
                    return Json(new { success = false, message = "请先登录" });
                }

                var restaurant = db.Restaurants.Find(id);
                if (restaurant == null)
                {
                    return Json(new { success = false, message = "找不到餐厅信息" });
                }

                if (restaurant.MerchantId != merchantId)
                {
                    return Json(new { success = false, message = "无权操作此餐厅" });
                }

                // 直接设置为指定状态，不做切换
                restaurant.IsActive = isActive;
                restaurant.UpdateTime = DateTime.Now;
                db.SaveChanges();

                string statusText = isActive ? "营业中" : "休息中";
                return Json(new { 
                    success = true, 
                    message = $"餐厅状态已设置为：{statusText}",
                    isActive = restaurant.IsActive,
                    statusText = statusText
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "操作失败: " + ex.Message });
            }
        }

        // GET方法设置餐厅状态
        [HttpGet]
        public ActionResult SetRestaurantStatusGet(int id, bool isActive)
        {
            try
            {
                // 检查用户是否登录且是商家
                if (Session["UserId"] == null || Session["UserType"] as string != UserTypes.Merchant)
                {
                    TempData["ErrorMessage"] = "请先登录";
                    return RedirectToAction("Login", "Account");
                }

                int merchantId;
                if (!int.TryParse(Session["UserId"].ToString(), out merchantId))
                {
                    TempData["ErrorMessage"] = "无效的用户ID";
                    return RedirectToAction("Dashboard");
                }
                
                var restaurant = db.Restaurants.Find(id);
                if (restaurant == null)
                {
                    TempData["ErrorMessage"] = "找不到餐厅信息";
                    return RedirectToAction("Dashboard");
                }

                if (restaurant.MerchantId != merchantId)
                {
                    TempData["ErrorMessage"] = "无权操作此餐厅";
                    return RedirectToAction("Dashboard");
                }

                // 直接设置为指定状态，不做切换
                restaurant.IsActive = isActive;
                restaurant.UpdateTime = DateTime.Now;
                db.SaveChanges();

                string statusText = isActive ? "营业中" : "休息中";
                TempData["SuccessMessage"] = $"餐厅状态已设置为：{statusText}";
                
                // 更新Session中的餐厅状态
                Session["RestaurantStatus"] = isActive;
                Session["RestaurantId"] = id;
                
                System.Diagnostics.Debug.WriteLine($"SetRestaurantStatusGet: 已更新Session中的餐厅状态 - 餐厅ID: {id}, 状态: {(isActive ? "营业中" : "休息中")}");
                
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "操作失败: " + ex.Message;
                return RedirectToAction("Dashboard");
            }
        }
        
        // 修复商家登录后餐厅状态
        public ActionResult FixRestaurantStatus()
        {
            try
            {
                // 检查用户是否登录且是商家
                if (Session["UserId"] == null || Session["UserType"] as string != UserTypes.Merchant)
                {
                    TempData["ErrorMessage"] = "请先登录";
                    return RedirectToAction("Login", "Account");
                }

                int merchantId;
                if (!int.TryParse(Session["UserId"].ToString(), out merchantId))
                {
                    TempData["ErrorMessage"] = "无效的用户ID";
                    return RedirectToAction("Dashboard");
                }
                
                // 获取商家的餐厅
                var restaurant = db.Restaurants.FirstOrDefault(r => r.MerchantId == merchantId);
                if (restaurant == null)
                {
                    TempData["ErrorMessage"] = "您尚未创建餐厅";
                    return RedirectToAction("Dashboard");
                }

                // 更新Session中的餐厅状态
                Session["RestaurantStatus"] = restaurant.IsActive;
                
                // 设置状态通知
                TempData["StatusNotification"] = restaurant.IsActive ? 
                    "餐厅状态已刷新：营业中" : 
                    "餐厅状态已刷新：休息中";
                
                System.Diagnostics.Debug.WriteLine($"餐厅状态已刷新: {(restaurant.IsActive ? "营业中" : "休息中")}");
                
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "刷新餐厅状态失败: " + ex.Message;
                System.Diagnostics.Debug.WriteLine($"刷新餐厅状态失败: {ex.Message}");
                return RedirectToAction("Dashboard");
            }
        }

        // 辅助方法：获取简化的订单项文本
        private string GetSimplifiedOrderItems(ICollection<OrderDetail> orderDetails)
        {
            if (orderDetails == null || !orderDetails.Any())
            {
                return "无菜品";
            }
            
            try
            {
                var items = new List<string>();
                foreach (var detail in orderDetails)
                {
                    // 如果Dish为null，尝试从数据库加载
                    if (detail.Dish == null && detail.DishId > 0)
                    {
                        try
                        {
                            detail.Dish = db.Dishes.Find(detail.DishId);
                        }
                        catch
                        {
                            // 忽略加载异常
                        }
                    }
                    
                    string dishName = "未知菜品";
                    if (detail.Dish != null && !string.IsNullOrEmpty(detail.Dish.Name))
                    {
                        dishName = detail.Dish.Name;
                    }
                    else if (detail.DishId > 0)
                    {
                        dishName = $"菜品#{detail.DishId}";
                    }
                    
                    items.Add(dishName + " x " + detail.Quantity);
                }
                
                if (items.Count == 0)
                {
                    return "无菜品信息";
                }
                
                return string.Join("，", items);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取订单项时出错: {ex.Message}");
                return "菜品信息加载错误";
            }
        }

        // 生成模拟销售数据
        private object GenerateMockSalesData()
        {
            var random = new Random();
            var dates = new List<string>();
            var revenue = new List<decimal>();
            var orders = new List<int>();
            var dishes = new List<int>();
            
            // 生成最近7天的数据
            for (int i = 6; i >= 0; i--)
            {
                var date = DateTime.Now.AddDays(-i);
                dates.Add(date.ToString("MM/dd"));
                
                // 模拟营业额数据（不含配送费）
                revenue.Add(Math.Round((decimal)(random.NextDouble() * 2000 + 1500), 2));
                
                // 模拟订单量
                orders.Add(random.Next(10, 60));
                
                // 模拟菜品销量
                dishes.Add(random.Next(30, 120));
            }
            
            return new
            {
                Dates = dates,
                Revenue = revenue,
                Orders = orders,
                Dishes = dishes
            };
        }
        
        // 辅助方法：检查用户是否已认证
        private bool IsAuthenticated(out int merchantId)
        {
            merchantId = 0;
            if (Session["UserId"] == null || Session["UserType"] as string != "Merchant")
            {
                return false;
            }
            
            if (int.TryParse(Session["UserId"].ToString(), out merchantId))
            {
                return true;
            }
            return false;
        }

        // GET: Merchant/RefreshRevenue
        public ActionResult RefreshRevenue()
        {
            try
            {
                // 检查用户是否登录且是商家
                if (Session["UserId"] == null || Session["UserType"] as string != UserTypes.Merchant)
                {
                    TempData["ErrorMessage"] = "请先登录";
                    return RedirectToAction("Login", "Account");
                }

                int merchantId;
                if (!int.TryParse(Session["UserId"].ToString(), out merchantId))
                {
                    TempData["ErrorMessage"] = "无效的用户ID";
                    return RedirectToAction("Dashboard");
                }
                
                // 获取商家所有餐厅
                var restaurantIds = db.Restaurants
                    .Where(r => r.MerchantId == merchantId)
                    .Select(r => r.RestaurantId)
                    .ToList();
                
                if (restaurantIds.Count == 0)
                {
                    TempData["ErrorMessage"] = "您还没有餐厅";
                    return RedirectToAction("Dashboard");
                }

                // 获取当天订单
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                var todayOrders = db.Orders
                    .Where(o => restaurantIds.Contains(o.RestaurantId) && 
                           o.CreatedAt >= today && o.CreatedAt < tomorrow)
                    .ToList();
                
                // 计算今日营收总额（排除已取消的订单，不包含配送费）
                decimal todayRevenue = 0;
                System.Diagnostics.Debug.WriteLine("========== 开始计算今日营收 ==========");
                
                // 直接查询已完成配送的订单（已送达的订单）
                var completedOrders = db.Orders
                    .Where(o => restaurantIds.Contains(o.RestaurantId) && 
                           o.Status == OrderStatus.Delivered &&
                           o.ActualDeliveryTime >= today && o.ActualDeliveryTime < tomorrow)
                    .ToList();
                
                foreach (var order in completedOrders)
                {
                    decimal orderRevenue = order.TotalAmount - order.DeliveryFee;
                    todayRevenue += orderRevenue;
                    System.Diagnostics.Debug.WriteLine($"已完成订单 {order.OrderId}: 总额={order.TotalAmount}, 配送费={order.DeliveryFee}, 商家收入={orderRevenue}");
                }
                
                System.Diagnostics.Debug.WriteLine($"今日完成订单数: {completedOrders.Count}, 总营收: {todayRevenue}");
                System.Diagnostics.Debug.WriteLine("========== 结束计算今日营收 ==========");
                
                // 计算总营收（所有已完成的订单，不包含配送费）
                var allCompletedOrders = db.Orders
                    .Where(o => restaurantIds.Contains(o.RestaurantId) && 
                           o.Status == OrderStatus.Delivered)
                    .ToList();
                
                decimal totalRevenue = 0;
                foreach (var order in allCompletedOrders)
                {
                    totalRevenue += (order.TotalAmount - order.DeliveryFee);
                }
                System.Diagnostics.Debug.WriteLine($"总完成订单数: {allCompletedOrders.Count}, 总营收: {totalRevenue}");
                
                // 获取昨日数据用于比较
                var yesterday = today.AddDays(-1);
                var dayBeforeYesterday = yesterday.AddDays(-1);
                
                // 获取昨日已完成订单
                var yesterdayCompletedOrders = db.Orders
                    .Where(o => restaurantIds.Contains(o.RestaurantId) && 
                           o.Status == OrderStatus.Delivered &&
                           o.ActualDeliveryTime >= yesterday && o.ActualDeliveryTime < today)
                    .ToList();
                
                int yesterdayOrdersCount = yesterdayCompletedOrders.Count;
                decimal yesterdayRevenue = 0;
                foreach (var order in yesterdayCompletedOrders)
                {
                    decimal orderRevenue = order.TotalAmount - order.DeliveryFee;
                    yesterdayRevenue += orderRevenue;
                    System.Diagnostics.Debug.WriteLine($"昨日已完成订单 {order.OrderId}: 总额={order.TotalAmount}, 配送费={order.DeliveryFee}, 商家收入={orderRevenue}");
                }
                System.Diagnostics.Debug.WriteLine($"昨日完成订单数: {yesterdayOrdersCount}, 总营收: {yesterdayRevenue}");
                
                // 计算增长百分比
                double ordersPercentage = 0;
                if (yesterdayOrdersCount > 0)
                {
                    ordersPercentage = Math.Round(((double)completedOrders.Count - yesterdayOrdersCount) / yesterdayOrdersCount * 100, 1);
                }
                
                double revenuePercentage = 0;
                if (yesterdayRevenue > 0)
                {
                    revenuePercentage = Math.Round(((double)todayRevenue - (double)yesterdayRevenue) / (double)yesterdayRevenue * 100, 1);
                }
                
                // 保存到临时数据，用于显示
                TempData["TodayOrdersCount"] = completedOrders.Count;
                TempData["TodayRevenue"] = todayRevenue;
                TempData["OrdersPercentage"] = ordersPercentage;
                TempData["RevenuePercentage"] = revenuePercentage;
                TempData["TotalRevenue"] = totalRevenue;
                TempData["TotalOrdersCount"] = allCompletedOrders.Count;
                
                return RedirectToAction("Dashboard");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "刷新数据失败: " + ex.Message;
                return RedirectToAction("Dashboard");
            }
        }

        // GET: Merchant/TestRevenue
        public ActionResult TestRevenue()
        {
            try
            {
                // 检查用户是否登录且是商家
                if (Session["UserId"] == null || Session["UserType"] as string != UserTypes.Merchant)
                {
                    TempData["ErrorMessage"] = "请先登录";
                    return RedirectToAction("Login", "Account");
                }

                int merchantId;
                if (!int.TryParse(Session["UserId"].ToString(), out merchantId))
                {
                    TempData["ErrorMessage"] = "无效的用户ID";
                    return RedirectToAction("Dashboard");
                }
                
                // 获取商家所有餐厅
                var restaurantIds = db.Restaurants
                    .Where(r => r.MerchantId == merchantId)
                    .Select(r => r.RestaurantId)
                    .ToList();
                
                if (restaurantIds.Count == 0)
                {
                    TempData["ErrorMessage"] = "您还没有餐厅";
                    return RedirectToAction("Dashboard");
                }

                // 获取所有订单
                var allOrders = db.Orders
                    .Where(o => restaurantIds.Contains(o.RestaurantId))
                    .ToList();
                
                // 测试每个订单的营收计算
                var result = new System.Text.StringBuilder();
                result.AppendLine("订单营收计算测试结果:");
                
                foreach (var order in allOrders)
                {
                    decimal revenue = order.TotalAmount - order.DeliveryFee;
                    result.AppendLine($"订单 {order.OrderId} ({order.Status}): 总额={order.TotalAmount}, 配送费={order.DeliveryFee}, 商家收入={revenue}");
                }
                
                // 计算当天营收
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                var todayOrders = allOrders.Where(o => o.CreatedAt >= today && o.CreatedAt < tomorrow && o.Status != OrderStatus.Cancelled).ToList();
                decimal todayRevenue = todayOrders.Sum(o => o.TotalAmount - o.DeliveryFee);
                
                result.AppendLine($"今日订单数: {todayOrders.Count}");
                result.AppendLine($"今日营收: {todayRevenue}");
                
                // 返回结果
                ViewBag.TestResult = result.ToString();
                ViewBag.AllOrders = allOrders;
                ViewBag.TodayOrders = todayOrders;
                ViewBag.TodayRevenue = todayRevenue;
                
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "测试失败: " + ex.Message;
                return RedirectToAction("Dashboard");
            }
        }

        // GET: Merchant/GetChartData
        public ActionResult GetChartData(string period = "week", int? restaurantId = null)
        {
            try
            {
                int merchantId;
                if (!IsAuthenticated(out merchantId))
                {
                    return Json(new { error = "未授权访问" }, JsonRequestBehavior.AllowGet);
                }

                // 获取餐厅ID列表
                List<int> restaurantIds;
                
                if (restaurantId.HasValue && restaurantId.Value > 0)
                {
                    // 如果提供了特定餐厅ID，验证该餐厅属于当前商家
                    var restaurant = db.Restaurants.FirstOrDefault(r => r.RestaurantId == restaurantId.Value && r.MerchantId == merchantId);
                    if (restaurant != null)
                    {
                        restaurantIds = new List<int> { restaurant.RestaurantId };
                    }
                    else
                    {
                        return Json(new { error = "没有找到指定餐厅或无权限访问" }, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    // 否则获取该商家的所有餐厅
                    restaurantIds = db.Restaurants
                        .Where(r => r.MerchantId == merchantId)
                        .Select(r => r.RestaurantId)
                        .ToList();
                }

                if (restaurantIds.Count == 0)
                {
                    return Json(new { error = "没有找到餐厅" }, JsonRequestBehavior.AllowGet);
                }

                // 根据选择的时间段确定起始日期
                DateTime startDate;
                DateTime endDate = DateTime.Today;
                int daysCount;
                
                switch (period.ToLower())
                {
                    case "month":
                        startDate = DateTime.Today.AddDays(-29);
                        daysCount = 30;
                        break;
                    case "year":
                        startDate = DateTime.Today.AddMonths(-11);
                        daysCount = 12;
                        break;
                    case "quarter":
                        startDate = DateTime.Today.AddMonths(-2);
                        daysCount = 3;
                        break;
                    case "halfyear":
                        startDate = DateTime.Today.AddMonths(-5);
                        daysCount = 6;
                        break;
                    case "custom":
                        // 自定义范围，可以通过额外参数传入
                        // 例如customDays参数指定天数
                        int customDays = 7; // 默认7天
                        if (Request.QueryString["customDays"] != null)
                        {
                            int.TryParse(Request.QueryString["customDays"], out customDays);
                            if (customDays <= 0) customDays = 7;
                            if (customDays > 365) customDays = 365; // 限制最大范围
                        }
                        startDate = DateTime.Today.AddDays(-(customDays - 1));
                        daysCount = customDays;
                        break;
                    default: // week
                        startDate = DateTime.Today.AddDays(-6);
                        daysCount = 7;
                        break;
                }

                // 准备返回数据
                var dates = new List<string>();
                var revenues = new List<decimal>();
                var orders = new List<int>();
                var dishes = new List<int>();

                // 如果是年视图，按月聚合数据
                if (period.ToLower() == "year")
                {
                    // 按月聚合数据
                    for (int i = 0; i < daysCount; i++)
                    {
                        var currentMonth = startDate.AddMonths(i);
                        var nextMonth = currentMonth.AddMonths(1);
                        
                        // 获取当月订单
                        var monthOrders = db.Orders
                            .Where(o => restaurantIds.Contains(o.RestaurantId) &&
                                   o.CreatedAt >= currentMonth &&
                                   o.CreatedAt < nextMonth)
                            .ToList();
                        
                        // 计算当月营收（不含配送费）
                        decimal monthRevenue = monthOrders
                            .Where(o => o.Status == OrderStatus.Delivered)
                            .Sum(o => o.TotalAmount - o.DeliveryFee);
                        
                        // 计算当月订单数
                        int monthOrderCount = monthOrders
                            .Where(o => o.Status == OrderStatus.Delivered)
                            .Count();
                        
                        // 计算当月菜品销量
                        int monthDishCount = db.OrderDetails
                            .Where(od => od.Order.RestaurantId != null &&
                                  restaurantIds.Contains((int)od.Order.RestaurantId) &&
                                  od.Order.CreatedAt >= currentMonth &&
                                  od.Order.CreatedAt < nextMonth &&
                                  od.Order.Status == OrderStatus.Delivered)
                            .Sum(od => od.Quantity);
                        
                        dates.Add(currentMonth.ToString("yyyy年MM月"));
                        revenues.Add(monthRevenue);
                        orders.Add(monthOrderCount);
                        dishes.Add(monthDishCount);
                    }
                }
                else
                {
                    // 按天聚合数据
                    for (int i = 0; i < daysCount; i++)
                    {
                        var currentDate = startDate.AddDays(i);
                        var nextDate = currentDate.AddDays(1);
                        
                        // 获取当日订单
                        var dayOrders = db.Orders
                            .Where(o => restaurantIds.Contains(o.RestaurantId) &&
                                   o.CreatedAt >= currentDate &&
                                   o.CreatedAt < nextDate)
                            .ToList();
                        
                        // 计算当日营收（不含配送费）
                        decimal dayRevenue = dayOrders
                            .Where(o => o.Status == OrderStatus.Delivered)
                            .Sum(o => o.TotalAmount - o.DeliveryFee);
                        
                        // 计算当日订单数
                        int dayOrderCount = dayOrders
                            .Where(o => o.Status == OrderStatus.Delivered)
                            .Count();
                        
                        // 计算当日菜品销量
                        int dayDishCount = db.OrderDetails
                            .Where(od => od.Order.RestaurantId != null &&
                                  restaurantIds.Contains((int)od.Order.RestaurantId) &&
                                  od.Order.CreatedAt >= currentDate &&
                                  od.Order.CreatedAt < nextDate &&
                                  od.Order.Status == OrderStatus.Delivered)
                            .Sum(od => od.Quantity);
                        
                        dates.Add(currentDate.ToString("MM/dd"));
                        revenues.Add(dayRevenue);
                        orders.Add(dayOrderCount);
                        dishes.Add(dayDishCount);
                    }
                }

                // 获取品类数据
                var categoryData = db.OrderDetails
                    .Where(od => od.Order.RestaurantId != null &&
                           restaurantIds.Contains((int)od.Order.RestaurantId) &&
                           od.Order.CreatedAt >= startDate &&
                           od.Order.CreatedAt <= endDate &&
                           od.Order.Status == OrderStatus.Delivered &&
                           od.Dish.Category != null)
                    .GroupBy(od => od.Dish.Category)
                    .Select(g => new
                    {
                        name = g.Key,
                        value = g.Sum(od => od.Quantity)
                    })
                    .OrderByDescending(x => x.value)
                    .Take(5)
                    .ToList();
               
                // 获取菜品数据
                var dishData = db.OrderDetails
                    .Where(od => od.Order.RestaurantId != null &&
                           restaurantIds.Contains((int)od.Order.RestaurantId) &&
                           od.Order.CreatedAt >= startDate &&
                           od.Order.CreatedAt <= endDate &&
                           od.Order.Status == OrderStatus.Delivered &&
                           od.Dish.Name != null)
                    .GroupBy(od => od.Dish.Name)
                    .Select(g => new
                    {
                        name = g.Key,
                        value = g.Sum(od => od.Quantity)
                    })
                    .OrderByDescending(x => x.value)
                    .Take(5)
                    .ToList();
                
             

                // 返回数据
                return Json(new
                {
                    dates = dates,
                    revenues = revenues,
                    orders = orders,
                    dishes = dishes,
                    categoryData = categoryData,
                    dishData = dishData
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetChartData 方法发生异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                
                // 发生异常时返回空数据
                return Json(new { 
                    error = ex.Message,
                    dates = new List<string>(),
                    revenues = new List<decimal>(),
                    orders = new List<int>(),
                    dishes = new List<int>(),
                    categoryData = new List<object>(),
                    dishData = new List<object>()
                }, JsonRequestBehavior.AllowGet);
            }
        }
        


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
} 