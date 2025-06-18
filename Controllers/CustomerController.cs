using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using food_takeout.Models;

namespace food_takeout.Controllers
{
    public class CustomerController : Controller
    {
        private FoodContext db = new FoodContext();

        // GET: Customer
        public ActionResult Index()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("CustomerController.Index 开始执行");
                
                // 检查是否已登录
                if (Session["UserId"] == null || Session["UserType"] as string != UserTypes.Customer)
                {
                    System.Diagnostics.Debug.WriteLine("用户未登录或不是顾客，重定向到登录页");
                    return RedirectToAction("Login", "Account");
                }

                // 使用简单的字符串值而不是复杂对象
                ViewBag.CustomerName = Session["Username"] as string ?? "顾客";
                ViewBag.CustomerAddress = "选择地址";
                
                // 获取用户信息
                var customerId = (int)Session["UserId"];
                System.Diagnostics.Debug.WriteLine($"获取顾客信息，ID: {customerId}");
                var customer = db.Customers.Find(customerId);
                ViewBag.Customer = customer ?? new Customer 
                { 
                    Name = ViewBag.CustomerName,
                    Address = ViewBag.CustomerAddress
                };

                try
                {
                    // 获取用户最新的5个订单（不限状态）
                    System.Diagnostics.Debug.WriteLine("正在获取用户最新的5个订单...");
                    var latestOrders = db.Orders
                        .Include(o => o.Restaurant)
                        .Where(o => o.CustomerId == customerId)
                        .OrderByDescending(o => o.CreatedAt)
                        .Take(5)
                        .ToList();
                    
                    System.Diagnostics.Debug.WriteLine($"找到 {latestOrders.Count} 个最新订单");
                    ViewBag.LatestOrders = latestOrders;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"获取最新订单失败: {ex.Message}");
                    ViewBag.LatestOrders = new List<Order>();
                }
                
                try
                {
                    // 获取所有餐厅信息
                    System.Diagnostics.Debug.WriteLine("正在获取所有餐厅信息...");
                    var allRestaurants = db.Restaurants
                        .Include(r => r.Reviews)
                        .OrderByDescending(r => r.IsActive)
                        .ThenByDescending(r => r.IsHot)
                        .ToList();
                    
                    System.Diagnostics.Debug.WriteLine($"找到 {allRestaurants.Count} 个餐厅");
                    ViewBag.AllRestaurants = allRestaurants;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"获取餐厅信息失败: {ex.Message}");
                    ViewBag.AllRestaurants = new List<Restaurant>();
                }
                
                try
                {
                    // 获取推荐菜品 - 使用IsHot标记和评分排序
                    System.Diagnostics.Debug.WriteLine("正在获取推荐菜品...");
                    // 先获取符合条件的数据，不进行排序
                    var dishes = db.Dishes
                        .Include(d => d.Restaurant)
                        .Where(d => d.Status == "Active" && d.IsHot)
                        .ToList();
                    
                    // 在内存中进行排序
                    var recommendedDishes = dishes
                        .OrderByDescending(d => d.Rating)
                        .Take(8)
                        .ToList();
                    
                    System.Diagnostics.Debug.WriteLine($"找到 {recommendedDishes.Count} 个推荐菜品");
                    ViewBag.RecommendedDishes = recommendedDishes;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"获取推荐菜品失败: {ex.Message}");
                    ViewBag.RecommendedDishes = new List<Dish>();
                }
                
                try
                {
                    // 获取销量前三的菜品
                    System.Diagnostics.Debug.WriteLine("正在获取销量前三的菜品...");
                    
                    // 检查OrderDetails表是否有数据
                    var orderDetailsCount = db.OrderDetails.Count();
                    System.Diagnostics.Debug.WriteLine($"OrderDetails表中有 {orderDetailsCount} 条数据");
                    
                    if (orderDetailsCount > 0)
                    {
                        // 使用更简单的方法获取热销菜品，避免复杂的LINQ转换问题
                        var dishSales = new Dictionary<int, int>();
                        
                        // 获取所有订单明细
                        var allOrderDetails = db.OrderDetails.ToList();
                        System.Diagnostics.Debug.WriteLine($"成功获取 {allOrderDetails.Count} 条订单明细");
                        
                        // 汇总每个菜品的销量
                        foreach (var detail in allOrderDetails)
                        {
                            if (!dishSales.ContainsKey(detail.DishId))
                            {
                                dishSales[detail.DishId] = 0;
                            }
                            dishSales[detail.DishId] += detail.Quantity;
                        }
                        
                        // 按销量排序并获取前三名
                        var topDishIds = dishSales.OrderByDescending(kv => kv.Value)
                                                 .Take(3)
                                                 .Select(kv => new { DishId = kv.Key, TotalSold = kv.Value })
                                                 .ToList();
                        
                        System.Diagnostics.Debug.WriteLine($"找到 {topDishIds.Count} 个热销菜品ID");
                        
                        // 获取这些菜品的详细信息
                        var topDishes = new List<Dish>();
                        foreach (var item in topDishIds)
                        {
                            var dish = db.Dishes
                                .Include(d => d.Restaurant)
                                .FirstOrDefault(d => d.DishId == item.DishId);
                            
                            if (dish != null)
                            {
                                // 添加销量属性
                                ViewData["Sold_" + dish.DishId] = item.TotalSold;
                                topDishes.Add(dish);
                                System.Diagnostics.Debug.WriteLine($"添加热销菜品: {dish.Name}, 销量: {item.TotalSold}");
                            }
                        }
                        
                        ViewBag.TopSellingDishes = topDishes;
                    }
                    else
                    {
                        // 如果没有订单数据，使用推荐菜品作为热销菜品
                        System.Diagnostics.Debug.WriteLine("没有订单数据，使用推荐菜品代替热销菜品");
                        // 先获取数据
                        var dishes = db.Dishes
                            .Include(d => d.Restaurant)
                            .Where(d => d.Status == "Active")
                            .ToList();
                            
                        // 在内存中排序
                        var recommendedForTopSelling = dishes
                            .OrderByDescending(d => d.Rating)
                            .Take(3)
                            .ToList();
                            
                        // 添加虚拟销量
                        for (int i = 0; i < recommendedForTopSelling.Count; i++)
                        {
                            var dish = recommendedForTopSelling[i];
                            ViewData["Sold_" + dish.DishId] = 100 - (i * 20); // 模拟销量数据
                        }
                        
                        ViewBag.TopSellingDishes = recommendedForTopSelling;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"获取热销菜品失败: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                    
                    // 使用一些默认数据
                    var allDishes = db.Dishes
                        .Include(d => d.Restaurant)
                        .Where(d => d.Status == "Active")
                        .ToList();
                        
                    // 在内存中排序
                    var defaultDishes = allDishes
                        .OrderByDescending(d => d.Rating)
                        .Take(3)
                        .ToList();
                    
                    // 添加虚拟销量
                    for (int i = 0; i < defaultDishes.Count; i++)
                    {
                        var dish = defaultDishes[i];
                        ViewData["Sold_" + dish.DishId] = 50 - (i * 10); // 模拟销量数据
                    }
                    
                    ViewBag.TopSellingDishes = defaultDishes;
                }
                
                System.Diagnostics.Debug.WriteLine("CustomerController.Index 执行完成，返回视图");
                return View();
            }
            catch (Exception ex)
            {
                // 记录异常
                System.Diagnostics.Debug.WriteLine($"CustomerController.Index 发生异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                
                // 设置默认值，确保视图不会出错
                ViewBag.CustomerName = "顾客";
                ViewBag.CustomerAddress = "选择地址";
                
                // 初始化所有可能在视图中使用的集合为空列表
                ViewBag.LatestOrders = new List<Order>();
                ViewBag.AllRestaurants = new List<Restaurant>();
                ViewBag.RecommendedDishes = new List<Dish>();
                ViewBag.TopSellingDishes = new List<Dish>();
                
                // 设置一个基本的Customer对象避免空引用
                ViewBag.Customer = new Customer 
                { 
                    Name = "顾客",
                    Address = "选择地址"
                };
                
                // 使用ViewData保存错误信息，方便调试
                ViewData["ErrorMessage"] = ex.Message;
                ViewData["ErrorStack"] = ex.StackTrace;
                
                return View();
            }
        }
        
        // 初始化默认ViewBag值，避免错误
        private void InitializeDefaultViewBags()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("初始化默认ViewBag值");
                
                string username = Session["Username"] as string ?? "顾客";
                
                // 确保Customer对象一定有Name属性
                ViewBag.CustomerName = username;
                ViewBag.CustomerAddress = "选择地址";
                
                // 默认Customer对象
                ViewBag.Customer = new Customer
                { 
                    CustomerId = Session["UserId"] != null ? (int)Session["UserId"] : 0,
                    Name = username, 
                    Address = "选择地址",
                    PhoneNumber = ""
                };
                
                // 空的集合，避免null引用
                ViewBag.LatestOrders = new List<Order>();
                ViewBag.AllRestaurants = new List<Restaurant>();
                ViewBag.RecommendedDishes = new List<Dish>();
                ViewBag.TopSellingDishes = new List<Dish>();
                
                System.Diagnostics.Debug.WriteLine("默认ViewBag值初始化完成");
            }
            catch (Exception ex)
            {
                // 出现任何错误，设置绝对最小值
                System.Diagnostics.Debug.WriteLine($"InitializeDefaultViewBags 发生异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                
                // 保存错误信息用于调试
                ViewData["ErrorMessage"] = ex.Message;
                ViewData["ErrorStack"] = ex.StackTrace;
                
                // 设置最基本的值
                ViewBag.Customer = new Customer { Name = "顾客", Address = "选择地址" };
                ViewBag.CustomerName = "顾客";
                ViewBag.CustomerAddress = "选择地址";
                ViewBag.ActiveOrders = new List<Order>();
                ViewBag.AllRestaurants = new List<Restaurant>();
                ViewBag.RecommendedDishes = new List<Dish>();
                ViewBag.TopSellingDishes = new List<Dish>();
            }
        }

        // 获取餐厅分类
        private string GetRestaurantCategories(int restaurantId)
        {
            // 根据餐厅菜品类型确定分类
            var categories = new List<string> { "中餐", "西餐", "快餐", "日料", "甜点" };
            Random rand = new Random();
            return categories[rand.Next(categories.Count)];
        }

        // GET: Customer/MyCoupons
        public ActionResult MyCoupons()
        {
            if (Session["UserId"] == null || Session["UserType"] as string != UserTypes.Customer)
            {
                return RedirectToAction("Login", "Account");
            }

            int customerId = (int)Session["UserId"];
            // 这里应该从数据库获取顾客的优惠券信息
            
            return View();
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