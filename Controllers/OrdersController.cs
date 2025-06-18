using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using food_takeout.Models;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.html.simpleparser;
using System.Text;

namespace food_takeout.Controllers
{
    // 创建ChartDataItem类，用于图表数据
    public class ChartDataItem
    {
        public string name { get; set; }
        public int value { get; set; }
    }

    public class OrdersController : Controller
    {
        private FoodContext db = new FoodContext();

        // GET: Orders
        // 订单列表页面 - 仅管理员可访问
        [AdminOnly]
        public ActionResult Index()
        {
            var orders = db.Orders
                .Include(o => o.Customer)
                .Include(o => o.Restaurant)
                .Include(o => o.Rider);
            return View(orders.ToList());
        }

        // GET: Orders/MerchantOrders
        // 商家订单管理页面
        public ActionResult MerchantOrders(string keyword = "", string status = "all")
        {
            // 检查用户是否登录且是商家
            if (Session["UserId"] == null || (string)Session["UserType"] != UserTypes.Merchant)
            {
                return RedirectToAction("Login", "Account");
            }

            int merchantId;
            if (!int.TryParse(Session["UserId"].ToString(), out merchantId))
            {
                return RedirectToAction("Login", "Account");
            }
            
            System.Diagnostics.Debug.WriteLine($"商家ID: {merchantId}");
            
            try
            {
                // 获取商家所拥有的所有餐厅IDs
                var restaurantIds = db.Restaurants
                    .Where(r => r.MerchantId == merchantId)
                    .Select(r => r.RestaurantId)
                    .ToList();
                    
                System.Diagnostics.Debug.WriteLine($"商家拥有的餐厅数量: {restaurantIds.Count}, 餐厅IDs: {string.Join(", ", restaurantIds)}");
                    
                if (restaurantIds.Count == 0)
                {
                    ViewBag.NoRestaurants = true;
                    System.Diagnostics.Debug.WriteLine("商家没有餐厅，返回空列表");
                    return View(new List<Order>());
                }

                // 获取这些餐厅的所有订单
                var query = db.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.Rider)
                    .Include(o => o.Restaurant)
                    .Include(o => o.OrderDetails.Select(od => od.Dish))
                    .Where(o => restaurantIds.Contains(o.RestaurantId));
                
                // 根据状态筛选
                switch (status.ToLower())
                {
                    case "pending":
                        query = query.Where(o => o.Status == OrderStatus.Pending);
                        break;
                    case "preparing":
                        query = query.Where(o => o.Status == OrderStatus.Preparing);
                        break;
                    case "ready":
                        query = query.Where(o => o.Status == OrderStatus.ReadyForDelivery);
                        break;
                    case "delivery":
                        query = query.Where(o => o.Status == OrderStatus.InDelivery);
                        break;
                    case "completed":
                        query = query.Where(o => o.Status == OrderStatus.Delivered);
                        break;
                    case "cancelled":
                        query = query.Where(o => o.Status == OrderStatus.Cancelled);
                        break;
                    default:
                        // 所有订单
                        break;
                }
                
                // 如果有搜索关键字
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    keyword = keyword.ToLower();
                    query = query.Where(o => 
                        o.OrderNumber.ToLower().Contains(keyword) ||
                        (o.Customer != null && o.Customer.Username != null && o.Customer.Username.ToLower().Contains(keyword)) ||
                        (o.Customer != null && o.Customer.PhoneNumber != null && o.Customer.PhoneNumber.Contains(keyword)) ||
                        (o.DeliveryAddress != null && o.DeliveryAddress.ToLower().Contains(keyword))
                    );
                }
                
                var orders = query.OrderByDescending(o => o.CreatedAt).ToList();
                
                System.Diagnostics.Debug.WriteLine($"获取到的订单数量: {orders.Count}");
                foreach (var order in orders)
                {
                    System.Diagnostics.Debug.WriteLine($"订单ID: {order.OrderId}, 订单号: {order.OrderNumber}, 餐厅ID: {order.RestaurantId}, 状态: {order.Status}");
                }

                // 准备待处理订单数据供视图使用
                var pendingOrders = orders
                    .Where(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Accepted)
                    .ToList();
                    
                ViewBag.PendingOrders = pendingOrders;
                ViewBag.PendingOrdersCount = pendingOrders.Count;
                ViewBag.Keyword = keyword;
                ViewBag.Status = status;
                System.Diagnostics.Debug.WriteLine($"待处理订单数量: {pendingOrders.Count}");

                return View(orders);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MerchantOrders 方法发生异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                ViewBag.ErrorMessage = "加载订单数据时发生错误，请稍后再试。";
                return View(new List<Order>());
            }
        }

        // GET: Orders/RiderOrders
        // 骑手待配送订单页面
        public ActionResult RiderOrders()
        {
            // 检查用户是否登录且是骑手
            if (Session["UserId"] == null || (string)Session["UserType"] != UserTypes.Rider)
            {
                return RedirectToAction("Login", "Account");
            }

            int riderId;
            if (!int.TryParse(Session["UserId"].ToString(), out riderId))
            {
                return RedirectToAction("Login", "Account");
            }

            // 获取所有可接单的订单（状态为ReadyForDelivery且没有分配给骑手）
            // 以及当前骑手正在配送的订单（状态为InDelivery且分配给当前骑手）
            var orders = db.Orders
                .Include(o => o.Customer)
                .Include(o => o.Restaurant)
                .Where(o => (o.Status == OrderStatus.ReadyForDelivery && o.RiderId == null) || 
                            (o.Status == OrderStatus.InDelivery && o.RiderId == riderId))
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            return View(orders);
        }

        // GET: Orders/DeliveryHistory
        // 骑手配送历史页面
        public ActionResult DeliveryHistory(string period = "all")
        {
            // 检查用户是否登录且是骑手
            if (Session["UserId"] == null || (string)Session["UserType"] != UserTypes.Rider)
            {
                return RedirectToAction("Login", "Account");
            }

            int riderId;
            if (!int.TryParse(Session["UserId"].ToString(), out riderId))
            {
                return RedirectToAction("Login", "Account");
            }

            // 根据选择的时间段筛选订单
            var query = db.Orders
                .Include(o => o.Customer)
                .Include(o => o.Restaurant)
                .Where(o => o.RiderId == riderId && o.Status == OrderStatus.Delivered);

            // 根据时间段筛选
            DateTime? startDate = null;
            switch (period.ToLower())
            {
                case "day":
                    startDate = DateTime.Today;
                    break;
                case "week":
                    startDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                    break;
                case "month":
                    startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    break;
                default:
                    // "all" - 不进行时间筛选
                    break;
            }

            if (startDate.HasValue)
            {
                query = query.Where(o => o.ActualDeliveryTime.HasValue && o.ActualDeliveryTime.Value >= startDate.Value);
            }

            var orders = query.OrderByDescending(o => o.ActualDeliveryTime ?? o.CreatedAt).ToList();

            // 传递时间段参数到视图
            ViewBag.Period = period;

            return View(orders);
        }

        // GET: Orders/SalesStatistics
        // 商家销售统计页面
        public ActionResult SalesStatistics()
        {
            // 检查用户是否登录且是商家
            if (Session["UserId"] == null || (string)Session["UserType"] != UserTypes.Merchant)
            {
                return RedirectToAction("Login", "Account");
            }

            int merchantId;
            if (!int.TryParse(Session["UserId"].ToString(), out merchantId))
            {
                return RedirectToAction("Login", "Account");
            }
            
            try
            {
                // 获取商家所拥有的所有餐厅IDs
                var restaurantIds = db.Restaurants
                    .Where(r => r.MerchantId == merchantId)
                    .Select(r => r.RestaurantId)
                    .ToList();
                    
                if (restaurantIds.Count == 0)
                {
                    ViewBag.NoRestaurants = true;
                    ViewBag.ErrorMessage = "您还没有创建餐厅，请先创建餐厅";
                    return View(new List<Order>());
                }

                // 获取默认统计数据（近7天）
                var endDate = DateTime.Today;
                var startDate = endDate.AddDays(-6);
                
                // 获取这些餐厅的所有订单
                var orders = db.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.Rider)
                    .Include(o => o.Restaurant)
                    .Include(o => o.OrderDetails.Select(od => od.Dish))
                    .Where(o => restaurantIds.Contains(o.RestaurantId) &&
                           o.CreatedAt >= startDate && 
                           o.CreatedAt <= endDate)
                    .OrderByDescending(o => o.CreatedAt)
                    .ToList();
                
                // 计算总营业额和总订单数
                decimal totalRevenue = orders
                    .Where(o => o.Status == OrderStatus.Delivered)
                    .Sum(o => o.TotalAmount - o.DeliveryFee);
                
                int totalOrders = orders
                    .Where(o => o.Status == OrderStatus.Delivered)
                    .Count();
                
                // 计算客单价
                decimal averageOrder = totalOrders > 0 ? Math.Round(totalRevenue / totalOrders, 2) : 0;
                
                // 获取热销菜品数据
                var hotDishesData = db.OrderDetails
                    .Where(od => od.Order.RestaurantId != null &&
                           restaurantIds.Contains((int)od.Order.RestaurantId) &&
                           od.Order.CreatedAt >= startDate &&
                           od.Order.CreatedAt <= endDate &&
                           od.Order.Status == OrderStatus.Delivered)
                    .GroupBy(od => od.Dish.Name)
                    .Select(g => new
                    {
                        name = g.Key,
                        value = g.Sum(od => od.Quantity)
                    })
                    .OrderByDescending(x => x.value)
                    .Take(5)
                    .ToList();
                
                // 获取所有菜品数量
                var totalDishesCount = db.Dishes
                    .Where(d => d.RestaurantId != null && restaurantIds.Contains((int)d.RestaurantId))
                    .Count();
                    
                // 计算热销菜品占比
                int dishesPercentage = 0;
                if (totalDishesCount > 0)
                {
                    dishesPercentage = (int)Math.Round((double)hotDishesData.Count / totalDishesCount * 100);
                }
                
                // 设置ViewBag数据
                ViewBag.TotalRevenue = totalRevenue;
                ViewBag.TotalOrders = totalOrders;
                ViewBag.AverageOrder = averageOrder;
                ViewBag.HotDishesCount = hotDishesData.Count;
                ViewBag.DishesPercentage = dishesPercentage;
                
                return View(orders);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SalesStatistics 方法发生异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                ViewBag.ErrorMessage = "加载销售统计数据时发生错误，请稍后再试。";
                return View(new List<Order>());
            }
        }
        
        // GET: Orders/GetSalesData
        // 获取销售统计数据的API
        public ActionResult GetSalesData(string period = "week")
        {
            // 检查用户是否登录且是商家
            if (Session["UserId"] == null || (string)Session["UserType"] != UserTypes.Merchant)
            {
                return Json(new { error = "未授权访问" }, JsonRequestBehavior.AllowGet);
            }

            int merchantId;
            if (!int.TryParse(Session["UserId"].ToString(), out merchantId))
            {
                return Json(new { error = "未授权访问" }, JsonRequestBehavior.AllowGet);
            }
            
            try
            {
                // 获取商家所拥有的所有餐厅IDs
                var restaurantIds = db.Restaurants
                    .Where(r => r.MerchantId == merchantId)
                    .Select(r => r.RestaurantId)
                    .ToList();
                    
                if (restaurantIds.Count == 0)
                {
                    return Json(new { error = "没有找到餐厅" }, JsonRequestBehavior.AllowGet);
                }

                // 根据选择的时间段确定起始日期
                DateTime startDate;
                DateTime endDate = DateTime.Today;
                
                switch (period.ToLower())
                {
                    case "month":
                        startDate = DateTime.Today.AddDays(-29);
                        break;
                    case "year":
                        startDate = DateTime.Today.AddDays(-364);
                        break;
                    default: // week
                        startDate = DateTime.Today.AddDays(-6);
                        break;
                }
                
                // 获取所有订单
                var orders = db.Orders
                    .Where(o => restaurantIds.Contains(o.RestaurantId) && 
                           o.CreatedAt >= startDate && 
                           o.CreatedAt <= endDate)
                    .ToList();
                
                // 准备日期和数据数组
                var dates = new List<string>();
                var revenues = new List<decimal>();
                var orderCounts = new List<int>();
                
                // 如果是年视图，按月聚合数据
                if (period.ToLower() == "year")
                {
                    // 按月聚合数据
                    for (int i = 0; i < 12; i++)
                    {
                        var currentMonth = DateTime.Today.AddMonths(-11 + i);
                        var monthStart = new DateTime(currentMonth.Year, currentMonth.Month, 1);
                        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                        
                        var monthOrders = orders.Where(o => 
                            o.CreatedAt >= monthStart && 
                            o.CreatedAt <= monthEnd).ToList();
                        
                        dates.Add(currentMonth.ToString("yyyy年MM月"));
                        revenues.Add(monthOrders.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount - o.DeliveryFee));
                        orderCounts.Add(monthOrders.Where(o => o.Status == OrderStatus.Delivered).Count());
                    }
                }
                else
                {
                    // 按天聚合数据
                    for (var date = startDate; date <= endDate; date = date.AddDays(1))
                    {
                        var dailyOrders = orders
                            .Where(o => o.CreatedAt.Year == date.Year && 
                                   o.CreatedAt.Month == date.Month && 
                                   o.CreatedAt.Day == date.Day)
                            .ToList();
                            
                        dates.Add(date.ToString("MM/dd"));
                        revenues.Add(dailyOrders.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.TotalAmount - o.DeliveryFee));
                        orderCounts.Add(dailyOrders.Where(o => o.Status == OrderStatus.Delivered).Count());
                    }
                }
                
                // 计算总营业额和总订单数
                var totalRevenue = revenues.Sum();
                var totalOrders = orderCounts.Sum();
                
                // 计算客单价
                decimal averageOrder = totalOrders > 0 ? Math.Round(totalRevenue / totalOrders, 2) : 0;
                
                // 计算环比增长率
                var previousPeriodStartDate = startDate.AddDays(-(endDate - startDate).Days - 1);
                var previousPeriodEndDate = startDate.AddDays(-1);
                
                var previousOrders = db.Orders
                    .Where(o => restaurantIds.Contains(o.RestaurantId) && 
                           o.CreatedAt >= previousPeriodStartDate && 
                           o.CreatedAt <= previousPeriodEndDate &&
                           o.Status == OrderStatus.Delivered)
                    .ToList();
                    
                var previousRevenue = previousOrders.Sum(o => o.TotalAmount - o.DeliveryFee);
                var previousOrdersCount = previousOrders.Count;
                
                double revenueCompare = 0;
                if (previousRevenue > 0)
                {
                    revenueCompare = Math.Round(((double)totalRevenue - (double)previousRevenue) / (double)previousRevenue * 100, 1);
                }
                
                double ordersCompare = 0;
                if (previousOrdersCount > 0)
                {
                    ordersCompare = Math.Round(((double)totalOrders - previousOrdersCount) / previousOrdersCount * 100, 1);
                }
                
                double averageCompare = 0;
                decimal previousAverage = previousOrdersCount > 0 ? previousRevenue / previousOrdersCount : 0;
                if (previousAverage > 0)
                {
                    averageCompare = Math.Round(((double)averageOrder - (double)previousAverage) / (double)previousAverage * 100, 1);
                }
                
                // 获取订单状态分布
                var orderStatusData = new[]
                {
                    new { value = orders.Count(o => o.Status == OrderStatus.Delivered), name = "已完成" },
                    new { value = orders.Count(o => o.Status == OrderStatus.InDelivery), name = "配送中" },
                    new { value = orders.Count(o => o.Status == OrderStatus.ReadyForDelivery), name = "已出餐" },
                    new { value = orders.Count(o => o.Status == OrderStatus.Accepted || o.Status == OrderStatus.Preparing), name = "准备中" },
                    new { value = orders.Count(o => o.Status == OrderStatus.Pending), name = "待处理" },
                    new { value = orders.Count(o => o.Status == OrderStatus.Cancelled), name = "已取消" }
                };
                
                // 获取热销菜品数据
                var hotDishesData = db.OrderDetails
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
                    .Take(10)
                    .ToList();
                    
                var hotDishesCount = hotDishesData.Count;
                
                // 获取所有菜品数量
                var totalDishesCount = db.Dishes
                    .Where(d => d.RestaurantId != null && restaurantIds.Contains((int)d.RestaurantId))
                    .Count();
                    
                var dishesPercentage = 0;
                if (totalDishesCount > 0)
                {
                    dishesPercentage = (int)Math.Round((double)hotDishesCount / totalDishesCount * 100);
                }
                
                // 获取订单时段分布
                var orderTimeData = new List<object>();
                var timeSlots = new[] 
                {
                    new { start = 6, end = 8, name = "6-8点" },
                    new { start = 8, end = 10, name = "8-10点" },
                    new { start = 10, end = 12, name = "10-12点" },
                    new { start = 12, end = 14, name = "12-14点" },
                    new { start = 14, end = 16, name = "14-16点" },
                    new { start = 16, end = 18, name = "16-18点" },
                    new { start = 18, end = 20, name = "18-20点" },
                    new { start = 20, end = 22, name = "20-22点" },
                    new { start = 22, end = 24, name = "22-24点" }
                };
                
                foreach (var slot in timeSlots)
                {
                    var count = orders.Count(o => 
                        o.CreatedAt.Hour >= slot.start && 
                        o.CreatedAt.Hour < slot.end);
                        
                    if (count > 0)
                    {
                        orderTimeData.Add(new { name = slot.name, value = count });
                    }
                }
                
                // 返回JSON数据
                return Json(new
                {
                    dates = dates,
                    revenues = revenues,
                    orders = orderCounts,
                    totalRevenue = totalRevenue,
                    totalOrders = totalOrders,
                    averageOrder = averageOrder,
                    revenueCompare = revenueCompare,
                    ordersCompare = ordersCompare,
                    averageCompare = averageCompare,
                    hotDishesCount = hotDishesCount,
                    dishesPercentage = dishesPercentage,
                    orderStatusData = orderStatusData,
                    hotDishesData = hotDishesData,
                    orderTimeData = orderTimeData
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetSalesData 方法发生异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                return Json(new { error = "获取销售数据时发生错误" }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: Orders/AcceptOrder/5
        // 商家接单
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AcceptOrder(int id)
        {
            // 检查用户是否登录且是商家
            if (Session["UserId"] == null || (string)Session["UserType"] != UserTypes.Merchant)
            {
                return RedirectToAction("Login", "Account");
            }

            int merchantId;
            if (!int.TryParse(Session["UserId"].ToString(), out merchantId))
            {
                return RedirectToAction("Login", "Account");
            }

            var order = db.Orders
                .Include(o => o.Restaurant)
                .FirstOrDefault(o => o.OrderId == id);
                
            if (order == null)
            {
                return HttpNotFound();
            }

            // 验证订单是否属于该商家的餐厅
            var restaurant = db.Restaurants.Find(order.RestaurantId);
            if (restaurant == null || restaurant.MerchantId != merchantId)
            {
                TempData["ErrorMessage"] = "无权操作此订单";
                return RedirectToAction("MerchantOrders");
            }

            // 检查订单状态是否为待处理
            if (order.Status != OrderStatus.Pending)
            {
                TempData["ErrorMessage"] = "只能接受待处理状态的订单";
                return RedirectToAction("MerchantOrders");
            }

            // 更新订单状态
            order.Status = OrderStatus.Preparing;
            order.UpdatedAt = DateTime.Now;
            db.SaveChanges();

            TempData["SuccessMessage"] = "已接受订单";
            
            // 如果是从商家仪表盘操作的，返回到仪表盘
            if (Request.UrlReferrer != null && Request.UrlReferrer.AbsolutePath.Contains("Merchant/Dashboard"))
            {
                return RedirectToAction("Dashboard", "Merchant");
            }
            
            return RedirectToAction("MerchantOrders");
        }
        
        // GET: Orders/AcceptOrder/5
        // 商家接单 (GET方法)
        [HttpGet]
        [ActionName("AcceptOrderGet")]
        public ActionResult AcceptOrderByGet(int id)
        {
            // 检查用户是否登录且是商家
            if (Session["UserId"] == null || Session["UserType"] as string != UserTypes.Merchant)
            {
                return RedirectToAction("Login", "Account");
            }

            int merchantId;
            if (!int.TryParse(Session["UserId"].ToString(), out merchantId))
            {
                return RedirectToAction("Login", "Account");
            }

            var order = db.Orders.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }

            // 获取商家所拥有的所有餐厅IDs
            var restaurantIds = db.Restaurants
                .Where(r => r.MerchantId == merchantId)
                .Select(r => r.RestaurantId)
                .ToList();

            // 验证订单是否属于该商家的餐厅
            if (!restaurantIds.Contains(order.RestaurantId))
            {
                TempData["ErrorMessage"] = "无权操作此订单";
                return RedirectToAction("MerchantOrders");
            }

            // 检查订单状态是否为待处理
            if (order.Status != OrderStatus.Pending)
            {
                TempData["ErrorMessage"] = "只能接受待处理状态的订单";
                return RedirectToAction("MerchantOrders");
            }

            // 更新订单状态
            order.Status = OrderStatus.Preparing;
            order.UpdatedAt = DateTime.Now;
            db.SaveChanges();

            TempData["SuccessMessage"] = "已接受订单";
            
            // 如果是从商家仪表盘操作的，返回到仪表盘
            if (Request.UrlReferrer != null && Request.UrlReferrer.AbsolutePath.Contains("Merchant/Dashboard"))
            {
                return RedirectToAction("Dashboard", "Merchant");
            }
            
            return RedirectToAction("MerchantOrders");
        }

        // POST: Orders/RejectOrder/5
        // 商家拒单
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RejectOrder(int id)
        {
            // 检查用户是否登录且是商家
            if (Session["UserId"] == null || (string)Session["UserType"] != UserTypes.Merchant)
            {
                return RedirectToAction("Login", "Account");
            }

            int merchantId;
            if (!int.TryParse(Session["UserId"].ToString(), out merchantId))
            {
                return RedirectToAction("Login", "Account");
            }

            var order = db.Orders
                .Include(o => o.Restaurant)
                .FirstOrDefault(o => o.OrderId == id);
                
            if (order == null)
            {
                return HttpNotFound();
            }

            // 验证订单是否属于该商家的餐厅
            var restaurant = db.Restaurants.Find(order.RestaurantId);
            if (restaurant == null || restaurant.MerchantId != merchantId)
            {
                TempData["ErrorMessage"] = "无权操作此订单";
                return RedirectToAction("MerchantOrders");
            }

            // 检查订单状态是否为待处理
            if (order.Status != OrderStatus.Pending)
            {
                TempData["ErrorMessage"] = "只能拒绝待处理状态的订单";
                return RedirectToAction("MerchantOrders");
            }

            // 更新订单状态
            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.Now;
            order.CancelReason = "商家拒单";
            db.SaveChanges();

            TempData["SuccessMessage"] = "已拒绝订单";
            
            // 如果是从商家仪表盘操作的，返回到仪表盘
            if (Request.UrlReferrer != null && Request.UrlReferrer.AbsolutePath.Contains("Merchant/Dashboard"))
            {
                return RedirectToAction("Dashboard", "Merchant");
            }
            
            return RedirectToAction("MerchantOrders");
        }
        
        // GET: Orders/RejectOrder/5
        // 商家拒单 (GET方法)
        [HttpGet]
        [ActionName("RejectOrderGet")]
        public ActionResult RejectOrderByGet(int id)
        {
            // 检查用户是否登录且是商家
            if (Session["UserId"] == null || Session["UserType"] as string != UserTypes.Merchant)
            {
                return RedirectToAction("Login", "Account");
            }

            int merchantId;
            if (!int.TryParse(Session["UserId"].ToString(), out merchantId))
            {
                return RedirectToAction("Login", "Account");
            }

            var order = db.Orders.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }

            // 获取商家所拥有的所有餐厅IDs
            var restaurantIds = db.Restaurants
                .Where(r => r.MerchantId == merchantId)
                .Select(r => r.RestaurantId)
                .ToList();

            // 验证订单是否属于该商家的餐厅
            if (!restaurantIds.Contains(order.RestaurantId))
            {
                TempData["ErrorMessage"] = "无权操作此订单";
                return RedirectToAction("MerchantOrders");
            }

            // 检查订单状态是否为待处理
            if (order.Status != OrderStatus.Pending)
            {
                TempData["ErrorMessage"] = "只能拒绝待处理状态的订单";
                return RedirectToAction("MerchantOrders");
            }

            // 更新订单状态
            order.Status = OrderStatus.Cancelled;
            order.CancelReason = "商家拒绝订单";
            order.UpdatedAt = DateTime.Now;
            db.SaveChanges();

            TempData["SuccessMessage"] = "已拒绝订单";
            
            // 如果是从商家仪表盘操作的，返回到仪表盘
            if (Request.UrlReferrer != null && Request.UrlReferrer.AbsolutePath.Contains("Merchant/Dashboard"))
            {
                return RedirectToAction("Dashboard", "Merchant");
            }
            
            return RedirectToAction("MerchantOrders");
        }

        // POST: Orders/MarkAsReady/5
        // 商家标记订单已出餐
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarkAsReady(int id)
        {
            // 检查用户是否登录且是商家
            if (Session["UserId"] == null || (string)Session["UserType"] != UserTypes.Merchant)
            {
                return RedirectToAction("Login", "Account");
            }

            int merchantId;
            if (!int.TryParse(Session["UserId"].ToString(), out merchantId))
            {
                return RedirectToAction("Login", "Account");
            }

            var order = db.Orders
                .Include(o => o.Restaurant)
                .FirstOrDefault(o => o.OrderId == id);
                
            if (order == null)
            {
                return HttpNotFound();
            }

            // 验证订单是否属于该商家的餐厅
            var restaurant = db.Restaurants.Find(order.RestaurantId);
            if (restaurant == null || restaurant.MerchantId != merchantId)
            {
                TempData["ErrorMessage"] = "无权操作此订单";
                return RedirectToAction("MerchantOrders");
            }

            // 检查订单状态是否为准备中
            if (order.Status != OrderStatus.Preparing)
            {
                TempData["ErrorMessage"] = "只能标记准备中状态的订单";
                return RedirectToAction("MerchantOrders");
            }

            // 更新订单状态
            order.Status = OrderStatus.ReadyForDelivery;
            order.UpdatedAt = DateTime.Now;
            db.SaveChanges();

            TempData["SuccessMessage"] = "订单已标记为已出餐";
            
            // 如果是从商家仪表盘操作的，返回到仪表盘
            if (Request.UrlReferrer != null && Request.UrlReferrer.AbsolutePath.Contains("Merchant/Dashboard"))
            {
                return RedirectToAction("Dashboard", "Merchant");
            }
            
            return RedirectToAction("MerchantOrders");
        }
        
        // GET: Orders/MarkAsReady/5
        // 商家标记订单已出餐 (GET方法)
        [HttpGet]
        [ActionName("MarkAsReadyGet")]
        public ActionResult MarkAsReadyByGet(int id)
        {
            // 检查用户是否登录且是商家
            if (Session["UserId"] == null || Session["UserType"] as string != UserTypes.Merchant)
            {
                return RedirectToAction("Login", "Account");
            }

            int merchantId;
            if (!int.TryParse(Session["UserId"].ToString(), out merchantId))
            {
                return RedirectToAction("Login", "Account");
            }

            var order = db.Orders.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }

            // 获取商家所拥有的所有餐厅IDs
            var restaurantIds = db.Restaurants
                .Where(r => r.MerchantId == merchantId)
                .Select(r => r.RestaurantId)
                .ToList();

            // 验证订单是否属于该商家的餐厅
            if (!restaurantIds.Contains(order.RestaurantId))
            {
                TempData["ErrorMessage"] = "无权操作此订单";
                return RedirectToAction("MerchantOrders");
            }

            // 检查订单状态是否为准备中
            if (order.Status != OrderStatus.Preparing)
            {
                // 提供更详细的错误消息
                if (order.Status == OrderStatus.Pending)
                {
                    TempData["ErrorMessage"] = "请先接受订单后再标记为已出餐";
                }
                else if (order.Status == OrderStatus.ReadyForDelivery)
                {
                    TempData["ErrorMessage"] = "订单已经标记为已出餐";
                }
                else
                {
                    TempData["ErrorMessage"] = "只能标记准备中状态的订单为已出餐";
                }
                return RedirectToAction("MerchantOrders");
            }

            // 更新订单状态
            order.Status = OrderStatus.ReadyForDelivery;
            order.UpdatedAt = DateTime.Now;
            db.SaveChanges();

            TempData["SuccessMessage"] = "订单已标记为已出餐";
            
            // 如果是从商家仪表盘操作的，返回到仪表盘
            if (Request.UrlReferrer != null && Request.UrlReferrer.AbsolutePath.Contains("Merchant/Dashboard"))
            {
                return RedirectToAction("Dashboard", "Merchant");
            }
            
            return RedirectToAction("MerchantOrders");
        }

        // GET: Orders/CancelOrder/5
        // 商家取消订单 (GET方法)
        [HttpGet]
        [ActionName("CancelOrderGet")]
        public ActionResult CancelOrderByGet(int id)
        {
            // 检查用户是否登录且是商家
            if (Session["UserId"] == null || Session["UserType"] as string != UserTypes.Merchant)
            {
                return RedirectToAction("Login", "Account");
            }

            int merchantId;
            if (!int.TryParse(Session["UserId"].ToString(), out merchantId))
            {
                return RedirectToAction("Login", "Account");
            }

            var order = db.Orders.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }

            // 获取商家所拥有的所有餐厅IDs
            var restaurantIds = db.Restaurants
                .Where(r => r.MerchantId == merchantId)
                .Select(r => r.RestaurantId)
                .ToList();

            // 验证订单是否属于该商家的餐厅
            if (!restaurantIds.Contains(order.RestaurantId))
            {
                TempData["ErrorMessage"] = "无权操作此订单";
                return RedirectToAction("MerchantOrders");
            }

            // 检查订单状态是否为可取消状态（Preparing、ReadyForDelivery）
            // 注意：Pending状态应通过RejectOrder处理，而不是Cancel
            if (order.Status != OrderStatus.Preparing && order.Status != OrderStatus.ReadyForDelivery)
            {
                TempData["ErrorMessage"] = "只有制作中或待配送的订单可以取消";
                return RedirectToAction("MerchantOrders");
            }

            // 更新订单状态
            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.Now;
            order.CancelReason = "商家取消订单";
            db.SaveChanges();

            TempData["SuccessMessage"] = "订单已取消";
            
            // 如果是从商家仪表盘操作的，返回到仪表盘
            if (Request.UrlReferrer != null && Request.UrlReferrer.AbsolutePath.Contains("Merchant/Dashboard"))
            {
                return RedirectToAction("Dashboard", "Merchant");
            }
            
            return RedirectToAction("MerchantOrders");
        }

        // POST: Orders/TakeOrder/5
        // 骑手接单
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TakeOrder(int id)
        {
            // 检查用户是否登录且是骑手
            if (Session["UserId"] == null || (string)Session["UserType"] != UserTypes.Rider)
            {
                return RedirectToAction("Login", "Account");
            }

            int riderId;
            if (!int.TryParse(Session["UserId"].ToString(), out riderId))
            {
                return RedirectToAction("Login", "Account");
            }
            
            var order = db.Orders.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }

            // 检查订单状态是否为待配送
            if (order.Status != OrderStatus.ReadyForDelivery)
            {
                TempData["ErrorMessage"] = "只能接受待配送状态的订单";
                return RedirectToAction("Index", "Rider");
            }

            // 检查订单是否已被分配骑手
            if (order.RiderId != null)
            {
                TempData["ErrorMessage"] = "该订单已被其他骑手接单";
                return RedirectToAction("Index", "Rider");
            }

            // 检查骑手是否可用
            var rider = db.Riders.Find(riderId);
            if (rider == null || !rider.IsOnline)
            {
                TempData["ErrorMessage"] = "请先上线";
                return RedirectToAction("Index", "Rider");
            }
            
            // 检查骑手是否正在配送其他订单
            var activeDeliveries = db.Orders.Count(o => o.RiderId == riderId && o.Status == OrderStatus.InDelivery);
            if (activeDeliveries > 0)
            {
                TempData["ErrorMessage"] = "您当前正在配送其他订单，请先完成该订单";
                return RedirectToAction("Index", "Rider");
            }

            // 更新订单状态
            order.Status = OrderStatus.InDelivery;
            order.RiderId = riderId;
            order.DeliveryStartTime = DateTime.Now;
            order.UpdatedAt = DateTime.Now;
            
            // 设置预计送达时间（当前时间加30分钟）
            order.EstimatedDeliveryTime = DateTime.Now.AddMinutes(30);
            
            // 更新骑手状态
            rider.IsDelivering = true;
            
            db.SaveChanges();

            TempData["SuccessMessage"] = "已成功接单";
            return RedirectToAction("Index", "Rider");
        }

        // POST: Orders/CompleteDelivery/5
        // 骑手完成配送
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CompleteDelivery(int id)
        {
            // 检查用户是否登录且是骑手
            if (Session["UserId"] == null || (string)Session["UserType"] != UserTypes.Rider)
            {
                return RedirectToAction("Login", "Account");
            }

            int riderId;
            if (!int.TryParse(Session["UserId"].ToString(), out riderId))
            {
                return RedirectToAction("Login", "Account");
            }
            
            var order = db.Orders
                .Include(o => o.Restaurant) // 加载关联的餐厅信息
                .FirstOrDefault(o => o.OrderId == id);
                
            if (order == null)
            {
                return HttpNotFound();
            }

            // 检查订单是否属于当前骑手
            if (order.RiderId != riderId)
            {
                TempData["ErrorMessage"] = "只能完成自己接的订单";
                return RedirectToAction("Index", "Rider");
            }

            // 检查订单状态是否为配送中
            if (order.Status != OrderStatus.InDelivery)
            {
                TempData["ErrorMessage"] = "只能完成配送中状态的订单";
                return RedirectToAction("Index", "Rider");
            }

            // 更新订单状态
            order.Status = OrderStatus.Delivered;
            order.ActualDeliveryTime = DateTime.Now;
            order.UpdatedAt = DateTime.Now;
            
            // 更新骑手状态和收入
            var rider = db.Riders.Find(riderId);
            if (rider != null)
            {
                rider.IsDelivering = false;
                rider.TotalCompletedOrders += 1;
                rider.TodayEarning += order.DeliveryFee;
                rider.TotalEarning += order.DeliveryFee;
                
                // 记录调试信息
                System.Diagnostics.Debug.WriteLine($"骑手 {riderId} 完成订单 #{order.OrderNumber}，获得配送费 ¥{order.DeliveryFee}");
            }
            
            db.SaveChanges();

            // 清除缓存，确保商家仪表盘能获取最新数据
            HttpContext.Cache.Remove("TodayOrders_" + order.RestaurantId);
            HttpContext.Cache.Remove("TodayRevenue_" + order.RestaurantId);
            System.Diagnostics.Debug.WriteLine($"已清除餐厅 {order.RestaurantId} 的今日订单和营收缓存");
            
            // 直接调用MerchantController的RefreshRevenue方法
            try
            {
                var merchantController = new MerchantController();
                merchantController.ControllerContext = new ControllerContext(this.Request.RequestContext, merchantController);
                merchantController.RefreshRevenue();
                System.Diagnostics.Debug.WriteLine("已调用MerchantController.RefreshRevenue方法");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"调用RefreshRevenue失败: {ex.Message}");
            }

            TempData["SuccessMessage"] = "配送已完成，可以继续接单";
            return RedirectToAction("Index", "Rider");
        }

        // GET: Orders/CustomerOrders
        // 顾客订单管理页面
        public ActionResult MyOrders()
        {
            // 检查用户是否登录且是顾客
            if (Session["UserId"] == null || (string)Session["UserType"] != UserTypes.Customer)
            {
                return RedirectToAction("Login", "Account");
            }

            int customerId = (int)Session["UserId"];
            var orders = db.Orders
                .Include(o => o.Restaurant)
                .Include(o => o.Rider)
                .Where(o => o.CustomerId == customerId)
                .OrderByDescending(o => o.CreatedAt)
                .ToList();

            // 获取该顾客已评价的订单ID
            var reviewedOrderIds = db.Reviews
                .Where(r => r.CustomerId == customerId)
                .Select(r => r.OrderId)
                .ToList();

            ViewBag.ReviewedOrders = reviewedOrderIds;

            return View(orders);
        }

        // GET: Orders/Details/5
        // 订单详情页面
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Order order = db.Orders
                .Include(o => o.Customer)
                .Include(o => o.Restaurant)
                .Include(o => o.Rider)
                .Include(o => o.OrderDetails.Select(od => od.Dish))
                .FirstOrDefault(o => o.OrderId == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            // 检查当前用户是否有权限查看此订单
            if (Session["UserId"] != null)
            {
                string userType = Session["UserType"] as string;
                int userId = (int)Session["UserId"];
                
                if (userType == UserTypes.Customer && order.CustomerId != userId)
                {
                    return RedirectToAction("MyOrders");
                }
                else if (userType == UserTypes.Merchant && order.RestaurantId != userId)
                {
                    return RedirectToAction("MerchantOrders");
                }
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }

            // 获取订单评价（如果有）
            var review = db.Reviews.FirstOrDefault(r => r.OrderId == id);
            if (review != null)
            {
                ViewBag.Review = review;
                ViewBag.HasReview = true;
            }
            else
            {
                ViewBag.HasReview = false;
            }

            return View(order);
        }

        // GET: Orders/SelectRestaurant
        // 选择餐厅页面
        public ActionResult SelectRestaurant()
        {
            // 检查用户是否登录且是顾客
            if (Session["UserId"] == null || (string)Session["UserType"] != UserTypes.Customer)
            {
                return RedirectToAction("Login", "Account");
            }

            var restaurants = db.Restaurants.ToList();
            return View(restaurants);
        }

        // GET: Orders/SelectDishes/5
        // 选择菜品页面 - 参数是餐厅ID
        public ActionResult SelectDishes(int? id)
        {
            // 检查用户是否登录且是顾客
            if (Session["UserId"] == null || (string)Session["UserType"] != UserTypes.Customer)
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return RedirectToAction("SelectRestaurant");
            }

            // 获取餐厅信息
            var restaurant = db.Restaurants.Find(id);
            if (restaurant == null)
            {
                return HttpNotFound();
            }

            // 获取该餐厅的可用菜品
            var dishes = db.Dishes
                .Where(d => d.RestaurantId == id && d.Status == "Active")
                .ToList();

            ViewBag.Restaurant = restaurant;
            return View(dishes);
        }

        // POST: Orders/PlaceOrder
        // 提交订单
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PlaceOrder(int restaurantId, FormCollection form)
        {
            // 检查用户是否登录且是顾客
            if (Session["UserId"] == null || (string)Session["UserType"] != UserTypes.Customer)
            {
                return RedirectToAction("Login", "Account");
            }

            int customerId = (int)Session["UserId"];

            // 获取所有被选中的菜品和数量
            var selectedDishes = new List<Tuple<int, int>>(); // DishId, Quantity
            foreach (var key in form.AllKeys)
            {
                if (key.StartsWith("quantity_"))
                {
                    string dishIdStr = key.Substring(9); // 去掉"quantity_"前缀
                    int dishId = int.Parse(dishIdStr);
                    int quantity = int.Parse(form[key]);

                    if (quantity > 0)
                    {
                        selectedDishes.Add(new Tuple<int, int>(dishId, quantity));
                    }
                }
            }

            // 检查是否有选中的菜品
            if (selectedDishes.Count == 0)
            {
                TempData["ErrorMessage"] = "请至少选择一项菜品";
                return RedirectToAction("SelectDishes", new { id = restaurantId });
            }

            // 计算订单总额
            decimal totalAmount = 0;
            foreach (var item in selectedDishes)
            {
                int dishId = item.Item1;
                int quantity = item.Item2;
                var dish = db.Dishes.Find(dishId);
                if (dish != null)
                {
                    totalAmount += dish.Price * quantity;
                }
            }
            
            // 添加配送费
            decimal deliveryFee = 5.0M; // 默认骑手配送费为5元
            totalAmount += deliveryFee;

            // 获取顾客送餐地址
            var customer = db.Customers.Find(customerId);
            string deliveryAddress = customer?.CurrentAddress ?? customer?.Address ?? "";
            
            // 创建订单
            var order = new Order
            {
                CustomerId = customerId,
                RestaurantId = restaurantId,
                Status = OrderStatus.Pending,
                TotalAmount = totalAmount,
                DeliveryFee = deliveryFee,
                DeliveryAddress = deliveryAddress,
                OrderNumber = "ORD" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            db.Orders.Add(order);
            db.SaveChanges();

            // 创建订单明细
            foreach (var item in selectedDishes)
            {
                int dishId = item.Item1;
                int quantity = item.Item2;

                // 获取菜品信息
                var dish = db.Dishes.Find(dishId);
                if (dish != null)
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.OrderId,
                        DishId = dishId,
                        Quantity = quantity,
                        Price = dish.Price,
                        Subtotal = dish.Price * quantity
                    };
                    db.OrderDetails.Add(orderDetail);
                }
            }

            db.SaveChanges();

            TempData["SuccessMessage"] = "订单提交成功！";
            return RedirectToAction("Details", new { id = order.OrderId });
        }

        // POST: Orders/Cancel/5
        // 取消订单
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Cancel(int id)
        {
            // 检查用户是否登录且是顾客
            if (Session["UserId"] == null || (string)Session["UserType"] != UserTypes.Customer)
            {
                return RedirectToAction("Login", "Account");
            }

            int customerId = (int)Session["UserId"];

            // 获取订单
            var order = db.Orders.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }

            // 检查订单是否属于当前用户
            if (order.CustomerId != customerId)
            {
                return RedirectToAction("MyOrders");
            }

            // 检查订单状态是否为待处理（只有待处理的订单才能取消）
            if (order.Status != OrderStatus.Pending)
            {
                TempData["ErrorMessage"] = "只能取消待处理状态的订单";
                return RedirectToAction("Details", new { id = id });
            }

            // 取消订单
            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.Now;
            db.SaveChanges();

            TempData["SuccessMessage"] = "订单已取消";
            return RedirectToAction("MyOrders");
        }

        // GET: Orders/Create
        // 创建订单页面 - 仅管理员可访问
        [AdminOnly]
        public ActionResult Create()
        {
            ViewBag.CustomerId = new SelectList(db.Customers, "CustomerId", "Username");
            ViewBag.RestaurantId = new SelectList(db.Restaurants, "RestaurantId", "Name");
            ViewBag.RiderId = new SelectList(db.Riders, "RiderId", "Name");
            return View();
        }

        // POST: Orders/Create
        // 处理创建订单请求 - 仅管理员可访问
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnly]
        public ActionResult Create([Bind(Include = "OrderId,OrderNumber,Status,TotalAmount,Remark,DeliveryAddress,CustomerId,RestaurantId,RiderId")] Order order)
        {
            if (ModelState.IsValid)
            {
                // 获取顾客的默认地址，如果没有提供配送地址
                if (string.IsNullOrEmpty(order.DeliveryAddress))
                {
                    var customer = db.Customers.Find(order.CustomerId);
                    if (customer != null && !string.IsNullOrEmpty(customer.Address))
                    {
                        order.DeliveryAddress = customer.Address;
                    }
                }
                
                order.CreatedAt = DateTime.Now;
                db.Orders.Add(order);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.CustomerId = new SelectList(db.Customers, "CustomerId", "Username", order.CustomerId);
            ViewBag.RestaurantId = new SelectList(db.Restaurants, "RestaurantId", "Name", order.RestaurantId);
            ViewBag.RiderId = new SelectList(db.Riders, "RiderId", "Name", order.RiderId);
            return View(order);
        }

        // GET: Orders/Edit/5
        // 编辑订单页面 - 仅管理员可访问
        [AdminOnly]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Order order = db.Orders.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }
            ViewBag.CustomerId = new SelectList(db.Customers, "CustomerId", "Username", order.CustomerId);
            ViewBag.RestaurantId = new SelectList(db.Restaurants, "RestaurantId", "Name", order.RestaurantId);
            ViewBag.RiderId = new SelectList(db.Riders, "RiderId", "Name", order.RiderId);
            return View(order);
        }

        // POST: Orders/Edit/5
        // 处理编辑订单请求 - 仅管理员可访问
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnly]
        public ActionResult Edit([Bind(Include = "OrderId,OrderNumber,Status,TotalAmount,Remark,DeliveryAddress,CreatedAt,CustomerId,RestaurantId,RiderId")] Order order)
        {
            if (ModelState.IsValid)
            {
                order.UpdatedAt = DateTime.Now;
                db.Entry(order).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.CustomerId = new SelectList(db.Customers, "CustomerId", "Username", order.CustomerId);
            ViewBag.RestaurantId = new SelectList(db.Restaurants, "RestaurantId", "Name", order.RestaurantId);
            ViewBag.RiderId = new SelectList(db.Riders, "RiderId", "Name", order.RiderId);
            return View(order);
        }

        // GET: Orders/Delete/5
        // 删除订单页面 - 仅管理员可访问
        [AdminOnly]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Order order = db.Orders
                .Include(o => o.Customer)
                .Include(o => o.Restaurant)
                .Include(o => o.Rider)
                .FirstOrDefault(o => o.OrderId == id);
            if (order == null)
            {
                return HttpNotFound();
            }
            return View(order);
        }

        // POST: Orders/Delete/5
        // 处理删除订单请求 - 仅管理员可访问
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AdminOnly]
        public ActionResult DeleteConfirmed(int id)
        {
            Order order = db.Orders.Find(id);
            db.Orders.Remove(order);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // POST: Orders/ChangeStatus/5
        // 修改订单状态 - 仅管理员可访问
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnly]
        public ActionResult ChangeStatus(int id, OrderStatus status)
        {
            Order order = db.Orders.Find(id);
            if (order != null)
            {
                order.Status = status;
                order.UpdatedAt = DateTime.Now;
                db.SaveChanges();
            }
            return RedirectToAction("Details", new { id = id });
        }

        // GET: Orders/Review/5
        // 评价订单页面
        public ActionResult Review(int? id)
        {
            // 检查用户是否登录
            if (Session["CustomerId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int customerId = (int)Session["CustomerId"];

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // 获取订单
            var order = db.Orders
                .Include(o => o.Restaurant)
                .FirstOrDefault(o => o.OrderId == id);

            if (order == null)
            {
                return HttpNotFound();
            }

            // 检查订单是否属于当前用户
            if (order.CustomerId != customerId)
            {
                return RedirectToAction("MyOrders");
            }

            // 检查订单是否已完成
            if (order.Status != OrderStatus.Delivered)
            {
                TempData["ErrorMessage"] = "只能评价已送达的订单";
                return RedirectToAction("Details", new { id = id });
            }

            // 检查是否已经评价过
            var existingReview = db.Reviews.FirstOrDefault(r => r.OrderId == id);
            if (existingReview != null)
            {
                TempData["ErrorMessage"] = "此订单已评价";
                return RedirectToAction("Details", new { id = id });
            }

            var model = new Review
            {
                OrderId = order.OrderId,
                CustomerId = customerId,
                RestaurantId = order.RestaurantId
            };

            ViewBag.Order = order;
            return View(model);
        }

        // POST: Orders/SubmitReview
        // 提交评价
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitReview(Review review)
        {
            // 检查用户是否登录
            if (Session["CustomerId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int customerId = (int)Session["CustomerId"];

            // 确保当前用户是评价的创建者
            if (review.CustomerId != customerId)
            {
                return RedirectToAction("MyOrders");
            }

            if (ModelState.IsValid)
            {
                review.CreatedAt = DateTime.Now;
                db.Reviews.Add(review);
                db.SaveChanges();

                TempData["SuccessMessage"] = "评价提交成功，感谢您的反馈！";
                return RedirectToAction("Details", new { id = review.OrderId });
            }

            // 获取订单信息用于重新显示视图
            var order = db.Orders
                .Include(o => o.Restaurant)
                .FirstOrDefault(o => o.OrderId == review.OrderId);

            ViewBag.Order = order;
            return View("Review", review);
        }

        // 导出订单报表为PDF
        public ActionResult ExportOrdersReport(string filter = "all", string keyword = "")
        {
            try
            {
                // 检查用户是否登录且是商家
                if (Session["UserId"] == null || (string)Session["UserType"] != UserTypes.Merchant)
                {
                    return RedirectToAction("Login", "Account");
                }

                int merchantId;
                if (!int.TryParse(Session["UserId"].ToString(), out merchantId))
                {
                    return RedirectToAction("Login", "Account");
                }
                
                // 获取商家所拥有的所有餐厅IDs
                var restaurantIds = db.Restaurants
                    .Where(r => r.MerchantId == merchantId)
                    .Select(r => r.RestaurantId)
                    .ToList();
                    
                if (restaurantIds.Count == 0)
                {
                    TempData["ErrorMessage"] = "您还没有创建餐厅";
                    return RedirectToAction("Dashboard", "Merchant");
                }
                
                // 获取所有符合条件的订单
                var query = db.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.Rider)
                    .Include(o => o.Restaurant)
                    .Where(o => restaurantIds.Contains(o.RestaurantId));
                
                // 根据筛选条件获取订单
                switch (filter.ToLower())
                {
                    case "pending":
                        query = query.Where(o => o.Status == OrderStatus.Pending);
                        break;
                    case "preparing":
                        query = query.Where(o => o.Status == OrderStatus.Preparing);
                        break;
                    case "ready":
                        query = query.Where(o => o.Status == OrderStatus.ReadyForDelivery);
                        break;
                    case "delivery":
                        query = query.Where(o => o.Status == OrderStatus.InDelivery);
                        break;
                    case "completed":
                        query = query.Where(o => o.Status == OrderStatus.Delivered);
                        break;
                    case "cancelled":
                        query = query.Where(o => o.Status == OrderStatus.Cancelled);
                        break;
                    default:
                        // 所有订单
                        break;
                }
                
                // 如果有搜索关键字
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    keyword = keyword.ToLower();
                    query = query.Where(o => 
                        o.OrderNumber.ToLower().Contains(keyword) ||
                        (o.Customer != null && o.Customer.Username != null && o.Customer.Username.ToLower().Contains(keyword)) ||
                        (o.Customer != null && o.Customer.PhoneNumber != null && o.Customer.PhoneNumber.Contains(keyword)) ||
                        (o.DeliveryAddress != null && o.DeliveryAddress.ToLower().Contains(keyword))
                    );
                }
                
                var orders = query.OrderByDescending(o => o.CreatedAt).ToList();
                
                // 创建PDF文档
                using (MemoryStream ms = new MemoryStream())
                {
                    // 设置文档属性
                    Document document = new Document(PageSize.A4, 25, 25, 30, 30);
                    PdfWriter writer = PdfWriter.GetInstance(document, ms);
                    
                    // 添加元数据
                    document.AddAuthor("食物外卖系统");
                    document.AddCreator("食物外卖系统");
                    document.AddKeywords("订单,报表,PDF");
                    document.AddSubject("订单列表报表");
                    document.AddTitle("订单列表报表");
                    
                    // 打开文档
                    document.Open();
                    
                    // 设置中文字体
                    string fontPath = Path.Combine(HttpRuntime.AppDomainAppPath, "Content/fonts/msyh.ttc");
                    BaseFont baseFont;
                    try
                    {
                        baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);
                    }
                    catch
                    {
                        // 如果找不到字体文件，尝试使用替代字体
                        baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                    }
                    
                    Font titleFont = new Font(baseFont, 18, Font.BOLD);
                    Font headerFont = new Font(baseFont, 12, Font.BOLD);
                    Font normalFont = new Font(baseFont, 10, Font.NORMAL);
                    Font smallFont = new Font(baseFont, 8, Font.NORMAL);
                    
                    // 获取餐厅名称列表
                    var restaurants = db.Restaurants
                        .Where(r => restaurantIds.Contains(r.RestaurantId))
                        .Select(r => r.Name)
                        .ToList();
                    
                    string restaurantNames = string.Join("、", restaurants);
                    
                    // 添加系统名称
                    Paragraph systemName = new Paragraph("食物外卖系统", headerFont);
                    systemName.Alignment = Element.ALIGN_CENTER;
                    document.Add(systemName);
                    
                    // 添加餐厅名称
                    Paragraph restaurantInfo = new Paragraph(restaurantNames, normalFont);
                    restaurantInfo.Alignment = Element.ALIGN_CENTER;
                    document.Add(restaurantInfo);
                    
                    document.Add(new Paragraph(" ")); // 空行
                    
                    // 添加报表标题
                    Paragraph title = new Paragraph("订单列表报表", titleFont);
                    title.Alignment = Element.ALIGN_CENTER;
                    document.Add(title);
                    
                    // 添加报表筛选条件
                    string filterText = "全部订单";
                    switch (filter.ToLower())
                    {
                        case "pending": filterText = "待处理订单"; break;
                        case "preparing": filterText = "制作中订单"; break;
                        case "ready": filterText = "待配送订单"; break;
                        case "delivery": filterText = "配送中订单"; break;
                        case "completed": filterText = "已完成订单"; break;
                        case "cancelled": filterText = "已取消订单"; break;
                    }
                    
                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        filterText += " (搜索: " + keyword + ")";
                    }
                    
                    Paragraph filterInfo = new Paragraph("筛选条件: " + filterText, normalFont);
                    filterInfo.Alignment = Element.ALIGN_CENTER;
                    document.Add(filterInfo);
                    
                    // 添加报表时间
                    Paragraph dateTime = new Paragraph("生成时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), normalFont);
                    dateTime.Alignment = Element.ALIGN_CENTER;
                    document.Add(dateTime);
                    
                    document.Add(new Paragraph(" ")); // 空行
                    
                    // 添加统计信息
                    PdfPTable statsTable = new PdfPTable(3);
                    statsTable.WidthPercentage = 100;
                    statsTable.SetWidths(new float[] { 1, 1, 1 });
                    
                    statsTable.AddCell(new PdfPCell(new Phrase("待处理订单: " + orders.Count(o => o.Status == OrderStatus.Pending) + "单", normalFont)));
                    statsTable.AddCell(new PdfPCell(new Phrase("处理中订单: " + (orders.Count(o => o.Status == OrderStatus.Preparing || o.Status == OrderStatus.ReadyForDelivery)) + "单", normalFont)));
                    statsTable.AddCell(new PdfPCell(new Phrase("已完成订单: " + orders.Count(o => o.Status == OrderStatus.Delivered) + "单", normalFont)));
                    
                    document.Add(statsTable);
                    
                    document.Add(new Paragraph(" ")); // 空行
                    
                    // 创建订单表格
                    PdfPTable table = new PdfPTable(7);
                    table.WidthPercentage = 100;
                    table.SetWidths(new float[] { 1.5f, 1f, 1f, 1f, 1.5f, 1.2f, 1f });
                    
                    // 添加表头
                    PdfPCell cell = new PdfPCell(new Phrase("订单编号", headerFont));
                    cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                    table.AddCell(cell);
                    
                    cell = new PdfPCell(new Phrase("顾客", headerFont));
                    cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                    table.AddCell(cell);
                    
                    cell = new PdfPCell(new Phrase("金额", headerFont));
                    cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                    table.AddCell(cell);
                    
                    cell = new PdfPCell(new Phrase("商家收入", headerFont));
                    cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                    table.AddCell(cell);
                    
                    cell = new PdfPCell(new Phrase("订单时间", headerFont));
                    cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                    table.AddCell(cell);
                    
                    cell = new PdfPCell(new Phrase("状态", headerFont));
                    cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                    table.AddCell(cell);
                    
                    cell = new PdfPCell(new Phrase("骑手", headerFont));
                    cell.BackgroundColor = BaseColor.LIGHT_GRAY;
                    table.AddCell(cell);
                    
                    // 添加订单数据
                    foreach (var order in orders)
                    {
                        table.AddCell(new PdfPCell(new Phrase(order.OrderNumber, normalFont)));
                        table.AddCell(new PdfPCell(new Phrase(order.Customer?.Username ?? "未知", normalFont)));
                        table.AddCell(new PdfPCell(new Phrase("¥" + order.TotalAmount.ToString("F2"), normalFont)));
                        table.AddCell(new PdfPCell(new Phrase("¥" + order.MerchantRevenue.ToString("F2"), normalFont)));
                        table.AddCell(new PdfPCell(new Phrase(order.CreatedAt.ToString("yyyy-MM-dd HH:mm"), normalFont)));
                        table.AddCell(new PdfPCell(new Phrase(order.Status.GetDisplayName(), normalFont)));
                        table.AddCell(new PdfPCell(new Phrase(order.Rider?.Name ?? "未分配", normalFont)));
                    }
                    
                    document.Add(table);
                    
                    document.Add(new Paragraph(" ")); // 空行
                    
                    // 添加总结信息
                    Paragraph summary = new Paragraph("总计订单数: " + orders.Count + "单", normalFont);
                    document.Add(summary);
                    
                    // 添加页脚
                    Paragraph footer = new Paragraph("此报表由系统自动生成，如有疑问请联系管理员。", smallFont);
                    footer.Alignment = Element.ALIGN_CENTER;
                    document.Add(footer);
                    
                    // 关闭文档
                    document.Close();
                    writer.Close();
                    
                    // 返回PDF文件
                    string fileName = "订单列表报表_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".pdf";
                    return File(ms.ToArray(), "application/pdf", fileName);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "生成PDF报表失败: " + ex.Message;
                return RedirectToAction("MerchantOrders");
            }
        }

        // 导出单个订单详情为PDF
        public ActionResult ExportOrderDetail(int id)
        {
            try
            {
                // 检查用户是否登录
                if (Session["UserId"] == null)
                {
                    return RedirectToAction("Login", "Account");
                }
                
                // 获取订单信息
                var order = db.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.Restaurant)
                    .Include(o => o.Rider)
                    .Include(o => o.OrderDetails.Select(od => od.Dish))
                    .FirstOrDefault(o => o.OrderId == id);
                
                if (order == null)
                {
                    return HttpNotFound();
                }
                
                // 验证访问权限
                bool hasAccess = false;
                string userType = Session["UserType"] as string;
                int userId = int.Parse(Session["UserId"].ToString());
                
                if (userType == UserTypes.Customer && order.CustomerId == userId)
                {
                    hasAccess = true; // 顾客本人
                }
                else if (userType == UserTypes.Merchant)
                {
                    var restaurantIds = db.Restaurants
                        .Where(r => r.MerchantId == userId)
                        .Select(r => r.RestaurantId)
                        .ToList();
                    
                    if (restaurantIds.Contains(order.RestaurantId))
                    {
                        hasAccess = true; // 订单所属餐厅的商家
                    }
                }
                else if (userType == UserTypes.Rider && order.RiderId == userId)
                {
                    hasAccess = true; // 订单骑手
                }
                else if (userType == UserTypes.Admin)
                {
                    hasAccess = true; // 管理员
                }
                
                if (!hasAccess)
                {
                    TempData["ErrorMessage"] = "您无权查看此订单";
                    return RedirectToAction("Index", "Home");
                }

                // 创建PDF文档
                using (MemoryStream ms = new MemoryStream())
                {
                    // 设置文档属性
                    Document document = new Document(PageSize.A4, 25, 25, 30, 30);
                    PdfWriter writer = PdfWriter.GetInstance(document, ms);
                    
                    // 添加元数据
                    document.AddAuthor("食物外卖系统");
                    document.AddCreator("食物外卖系统");
                    document.AddKeywords("订单,详情,PDF");
                    document.AddSubject("订单详情");
                    document.AddTitle("订单详情 #" + order.OrderNumber);
                    
                    // 打开文档
                    document.Open();
                    
                    // 设置中文字体
                    string fontPath = Path.Combine(HttpRuntime.AppDomainAppPath, "Content/fonts/msyh.ttc");
                    BaseFont baseFont;
                    try
                    {
                        baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.NOT_EMBEDDED);
                    }
                    catch
                    {
                        // 如果找不到字体文件，尝试使用替代字体
                        baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                    }
                    
                    Font titleFont = new Font(baseFont, 18, Font.BOLD);
                    Font headerFont = new Font(baseFont, 12, Font.BOLD);
                    Font normalFont = new Font(baseFont, 10, Font.NORMAL);
                    Font smallFont = new Font(baseFont, 8, Font.NORMAL);
                    
                    // 添加订单编号
                    PdfPTable headerTable = new PdfPTable(2);
                    headerTable.WidthPercentage = 100;
                    headerTable.SetWidths(new float[] { 1f, 1f });
                    
                    PdfPCell cell = new PdfPCell(new Phrase(": " + order.OrderNumber, headerFont));
                    cell.Border = Rectangle.NO_BORDER;
                    headerTable.AddCell(cell);
                    
                    cell = new PdfPCell(new Phrase("", headerFont));
                    cell.Border = Rectangle.NO_BORDER;
                    headerTable.AddCell(cell);
                    
                    document.Add(headerTable);
                    
                    document.Add(new Paragraph(" ")); // 空行
                    
                    // 添加订单基本信息
                    PdfPTable infoTable = new PdfPTable(2);
                    infoTable.WidthPercentage = 100;
                    infoTable.SetWidths(new float[] { 1f, 1f });
                    
                    // 第一行
                    cell = new PdfPCell(new Phrase(": " + order.OrderNumber, normalFont));
                    cell.Border = Rectangle.NO_BORDER;
                    infoTable.AddCell(cell);
                    
                    cell = new PdfPCell(new Phrase("", normalFont));
                    cell.Border = Rectangle.NO_BORDER;
                    infoTable.AddCell(cell);
                    
                    // 第二行 - 下单时间
                    cell = new PdfPCell(new Phrase(": " + order.CreatedAt.ToString("yyyy-MM-dd HH:mm"), normalFont));
                    cell.Border = Rectangle.NO_BORDER;
                    infoTable.AddCell(cell);
                    
                    cell = new PdfPCell(new Phrase(": " + (order.EstimatedDeliveryTime.HasValue ? order.EstimatedDeliveryTime.Value.ToString("yyyy-MM-dd HH:mm") : ""), normalFont));
                    cell.Border = Rectangle.NO_BORDER;
                    infoTable.AddCell(cell);
                    
                    // 第三行 - 更新时间
                    cell = new PdfPCell(new Phrase(": " + (order.UpdatedAt.HasValue ? order.UpdatedAt.Value.ToString("yyyy-MM-dd HH:mm") : ""), normalFont));
                    cell.Border = Rectangle.NO_BORDER;
                    infoTable.AddCell(cell);
                    
                    cell = new PdfPCell(new Phrase(": " + (order.ActualDeliveryTime.HasValue ? order.ActualDeliveryTime.Value.ToString("yyyy-MM-dd HH:mm") : ""), normalFont));
                    cell.Border = Rectangle.NO_BORDER;
                    infoTable.AddCell(cell);
                    
                    // 第四行 - 订单金额
                    cell = new PdfPCell(new Phrase("", normalFont));
                    cell.Border = Rectangle.NO_BORDER;
                    infoTable.AddCell(cell);
                    
                    cell = new PdfPCell(new Phrase(": " + (order.Rider != null ? order.Rider.Name : ""), normalFont));
                    cell.Border = Rectangle.NO_BORDER;
                    infoTable.AddCell(cell);
                    
                    // 第五行 - 顾客信息
                    cell = new PdfPCell(new Phrase(": ¥" + order.TotalAmount.ToString("F2"), normalFont));
                    cell.Border = Rectangle.NO_BORDER;
                    infoTable.AddCell(cell);
                    
                    cell = new PdfPCell(new Phrase(": " + (order.Rider != null && !string.IsNullOrEmpty(order.Rider.PhoneNumber) ? order.Rider.PhoneNumber : ""), normalFont));
                    cell.Border = Rectangle.NO_BORDER;
                    infoTable.AddCell(cell);
                    
                    document.Add(infoTable);
                    
                    // 添加分隔线
                    PdfPTable separatorTable = new PdfPTable(1);
                    separatorTable.WidthPercentage = 100;
                    
                    cell = new PdfPCell();
                    cell.Border = Rectangle.TOP_BORDER;
                    cell.BorderWidth = 1;
                    cell.FixedHeight = 2;
                    separatorTable.AddCell(cell);
                    
                    document.Add(separatorTable);
                    
                    // 添加顾客和餐厅信息
                    PdfPTable customerTable = new PdfPTable(2);
                    customerTable.WidthPercentage = 100;
                    customerTable.SetWidths(new float[] { 1f, 1f });
                    
                    // 顾客信息行
                    cell = new PdfPCell(new Phrase(": " + order.Customer.Username, normalFont));
                    cell.Border = Rectangle.NO_BORDER;
                    customerTable.AddCell(cell);
                    
                    cell = new PdfPCell(new Phrase(": " + order.Restaurant.Name, normalFont));
                    cell.Border = Rectangle.NO_BORDER;
                    customerTable.AddCell(cell);
                    
                    // 顾客信息行
                    cell = new PdfPCell(new Phrase(": " + (order.Customer.Name ?? ""), normalFont));
                    cell.Border = Rectangle.NO_BORDER;
                    customerTable.AddCell(cell);
                    
                    cell = new PdfPCell(new Phrase(": " + (order.Restaurant.PhoneNumber ?? ""), normalFont));
                    cell.Border = Rectangle.NO_BORDER;
                    customerTable.AddCell(cell);
                    
                    // 顾客电话行
                    cell = new PdfPCell(new Phrase(": " + (order.Customer.PhoneNumber ?? ""), normalFont));
                    cell.Border = Rectangle.NO_BORDER;
                    customerTable.AddCell(cell);
                    
                    cell = new PdfPCell(new Phrase("", normalFont));
                    cell.Border = Rectangle.NO_BORDER;
                    customerTable.AddCell(cell);
                    
                    document.Add(customerTable);
                    
                    // 添加分隔线
                    document.Add(separatorTable);
                    
                    document.Add(new Paragraph(" ")); // 空行
                    
                    // 添加订单明细表格标题
                    Paragraph detailTitle = new Paragraph("订单明细", headerFont);
                    detailTitle.SpacingAfter = 10f;
                    detailTitle.Alignment = Element.ALIGN_LEFT;
                    document.Add(detailTitle);
                    
                    PdfPTable detailTable = new PdfPTable(4);
                    detailTable.WidthPercentage = 100;
                    detailTable.SetWidths(new float[] { 2f, 1f, 1f, 1f });
                    
                    // 添加表头
                    cell = new PdfPCell(new Phrase("菜品", headerFont));
                    cell.Border = Rectangle.BOTTOM_BORDER;
                    cell.PaddingBottom = 5;
                    cell.BackgroundColor = new BaseColor(240, 240, 240);  // 浅灰色背景
                    detailTable.AddCell(cell);
                    
                    cell = new PdfPCell(new Phrase("单价", headerFont));
                    cell.Border = Rectangle.BOTTOM_BORDER;
                    cell.PaddingBottom = 5;
                    cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    cell.BackgroundColor = new BaseColor(240, 240, 240);  // 浅灰色背景
                    detailTable.AddCell(cell);
                    
                    cell = new PdfPCell(new Phrase("数量", headerFont));
                    cell.Border = Rectangle.BOTTOM_BORDER;
                    cell.PaddingBottom = 5;
                    cell.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell.BackgroundColor = new BaseColor(240, 240, 240);  // 浅灰色背景
                    detailTable.AddCell(cell);
                    
                    cell = new PdfPCell(new Phrase("小计", headerFont));
                    cell.Border = Rectangle.BOTTOM_BORDER;
                    cell.PaddingBottom = 5;
                    cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    cell.BackgroundColor = new BaseColor(240, 240, 240);  // 浅灰色背景
                    detailTable.AddCell(cell);
                    
                    // 添加明细行
                    decimal total = 0;
                    foreach (var detail in order.OrderDetails)
                    {
                        decimal subtotal = detail.Price * detail.Quantity;
                        total += subtotal;
                        
                        // 菜品名称
                        cell = new PdfPCell(new Phrase(detail.Dish != null ? detail.Dish.Name : "未知菜品", normalFont));
                        cell.Border = Rectangle.NO_BORDER;
                        cell.PaddingTop = 5;
                        cell.PaddingBottom = 5;
                        detailTable.AddCell(cell);
                        
                        // 单价
                        cell = new PdfPCell(new Phrase("¥" + detail.Price.ToString("F2"), normalFont));
                        cell.Border = Rectangle.NO_BORDER;
                        cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                        cell.PaddingTop = 5;
                        cell.PaddingBottom = 5;
                        detailTable.AddCell(cell);
                        
                        // 数量
                        cell = new PdfPCell(new Phrase(detail.Quantity.ToString(), normalFont));
                        cell.Border = Rectangle.NO_BORDER;
                        cell.HorizontalAlignment = Element.ALIGN_CENTER;
                        cell.PaddingTop = 5;
                        cell.PaddingBottom = 5;
                        detailTable.AddCell(cell);
                        
                        // 小计
                        cell = new PdfPCell(new Phrase("¥" + subtotal.ToString("F2"), normalFont));
                        cell.Border = Rectangle.NO_BORDER;
                        cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                        cell.PaddingTop = 5;
                        cell.PaddingBottom = 5;
                        detailTable.AddCell(cell);
                    }
                    
                    document.Add(detailTable);
                    
                    // 添加小计、配送费和总计
                    PdfPTable summaryTable = new PdfPTable(2);
                    summaryTable.WidthPercentage = 100;
                    summaryTable.SetWidths(new float[] { 3f, 1f });
                    
                    // 小计行
                    cell = new PdfPCell(new Phrase("小计：", normalFont));
                    cell.Border = Rectangle.NO_BORDER;
                    cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    cell.PaddingTop = 5;
                    summaryTable.AddCell(cell);
                    
                    cell = new PdfPCell(new Phrase("¥" + total.ToString("F2"), normalFont));
                    cell.Border = Rectangle.NO_BORDER;
                    cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    cell.PaddingTop = 5;
                    summaryTable.AddCell(cell);
                    
                    // 配送费行
                    cell = new PdfPCell(new Phrase("配送费：", normalFont));
                    cell.Border = Rectangle.NO_BORDER;
                    cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    cell.PaddingTop = 5;
                    summaryTable.AddCell(cell);
                    
                    cell = new PdfPCell(new Phrase("¥" + order.DeliveryFee.ToString("F2"), normalFont));
                    cell.Border = Rectangle.NO_BORDER;
                    cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    cell.PaddingTop = 5;
                    summaryTable.AddCell(cell);
                    
                    // 总计行
                    Font boldFont = new Font(baseFont, 11, Font.BOLD);
                    cell = new PdfPCell(new Phrase("总计：", boldFont));
                    cell.Border = Rectangle.NO_BORDER;
                    cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    cell.PaddingTop = 5;
                    summaryTable.AddCell(cell);
                    
                    cell = new PdfPCell(new Phrase("¥" + order.TotalAmount.ToString("F2"), boldFont));
                    cell.Border = Rectangle.NO_BORDER;
                    cell.HorizontalAlignment = Element.ALIGN_RIGHT;
                    cell.PaddingTop = 5;
                    summaryTable.AddCell(cell);
                    
                    document.Add(summaryTable);
                    
                    // 添加页脚
                    document.Add(new Paragraph(" ")); // 空行
                    document.Add(new Paragraph(" ")); // 空行
                    
                    // 添加支付和签名区域
                    PdfPTable signatureTable = new PdfPTable(2);
                    signatureTable.WidthPercentage = 100;
                    signatureTable.SetWidths(new float[] { 1f, 1f });
                    
                    document.Add(new Paragraph(" ")); // 空行
                    
                    document.Add(new Paragraph(" ")); // 空行
                    
                    Paragraph footer = new Paragraph("打印时间: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), smallFont);
                    footer.Alignment = Element.ALIGN_CENTER;
                    document.Add(footer);
                    
                    // 关闭文档
                    document.Close();
                    writer.Close();
                    
                    // 返回PDF文件
                    string fileName = "订单详情_" + order.OrderNumber + ".pdf";
                    return File(ms.ToArray(), "application/pdf", fileName);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "生成PDF订单详情失败: " + ex.Message;
                return RedirectToAction("Details", new { id = id });
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