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
    public class RiderController : Controller
    {
        /// <summary>
        /// 用于在视图中展示订单数据的视图模型
        /// </summary>
        public class OrderViewModel
        {
            /// <summary>
            /// 订单ID
            /// </summary>
            public int OrderId { get; set; }
            
            /// <summary>
            /// 订单编号
            /// </summary>
            public string OrderNumber { get; set; }
            
            /// <summary>
            /// 餐厅名称
            /// </summary>
            public string RestaurantName { get; set; }
            
            /// <summary>
            /// 餐厅地址
            /// </summary>
            public string RestaurantAddress { get; set; }
            
            /// <summary>
            /// 客户名称
            /// </summary>
            public string CustomerName { get; set; }
            
            /// <summary>
            /// 配送地址
            /// </summary>
            public string DeliveryAddress { get; set; }
            
            /// <summary>
            /// 客户电话
            /// </summary>
            public string CustomerPhone { get; set; }
            
            /// <summary>
            /// 配送距离
            /// </summary>
            public double Distance { get; set; }
            
            /// <summary>
            /// 配送费
            /// </summary>
            public decimal DeliveryFee { get; set; }
            
            /// <summary>
            /// 订单时间（格式化后的字符串）
            /// </summary>
            public string OrderTime { get; set; }
            
            /// <summary>
            /// 接单时间
            /// </summary>
            public string AcceptTime { get; set; }
            
            /// <summary>
            /// 取餐时间
            /// </summary>
            public string PickupTime { get; set; }
            
            /// <summary>
            /// 开始配送时间
            /// </summary>
            public string DeliveryStartTime { get; set; }
            
            /// <summary>
            /// 送达时间
            /// </summary>
            public string CompletedTime { get; set; }
            
            /// <summary>
            /// 预计到达时间
            /// </summary>
            public string EstimatedArrival { get; set; }
            
            /// <summary>
            /// 订单总金额
            /// </summary>
            public decimal TotalAmount { get; set; }
            
            /// <summary>
            /// 订单状态
            /// </summary>
            public int Status { get; set; }
            
            /// <summary>
            /// 订单备注
            /// </summary>
            public string Remarks { get; set; }
            
            /// <summary>
            /// 地图URL
            /// </summary>
            public string MapUrl { get; set; }
            
            /// <summary>
            /// 订单项目列表
            /// </summary>
            public List<OrderItemViewModel> Items { get; set; }
            
            /// <summary>
            /// 是否选中（用于UI显示）
            /// </summary>
            public bool IsSelected { get; set; }
            
            /// <summary>
            /// 构造函数
            /// </summary>
            public OrderViewModel()
            {
                Items = new List<OrderItemViewModel>();
            }
        }
        
        /// <summary>
        /// 订单项目视图模型
        /// </summary>
        public class OrderItemViewModel
        {
            /// <summary>
            /// 商品名称
            /// </summary>
            public string Name { get; set; }
            
            /// <summary>
            /// 数量
            /// </summary>
            public int Quantity { get; set; }
            
            /// <summary>
            /// 价格
            /// </summary>
            public decimal Price { get; set; }
        }
        
        private FoodContext db = new FoodContext();

        // GET: Rider
        public ActionResult Index()
        {
            try
            {
                // 检查是否已登录
                if (Session["UserId"] == null || Session["UserType"] as string != UserTypes.Rider)
                {
                    return RedirectToAction("Login", "Account");
                }

                int riderId;
                if (int.TryParse(Session["UserId"].ToString(), out riderId))
                {
                    // 获取骑手信息
                    var rider = db.Riders.Find(riderId);
                    
                    // 如果找不到骑手数据，先创建一个新的骑手记录
                    if (rider == null)
                    {
                        rider = new Rider
                        {
                            RiderId = riderId,
                            Name = Session["Username"] as string ?? "骑手",
                            Username = Session["Username"] as string ?? "rider",
                            Avatar = "/Content/Images/placeholders/32x32/user.png",
                            IsOnline = false,
                            IsAvailable = true,
                            Rating = 4.5,
                            TodayEarning = 0.0M,
                            TotalEarning = 0.0M,
                            TotalCompletedOrders = 0,
                            PhoneNumber = "",
                            Address = "",
                            CreatedAt = DateTime.Now
                        };
                        
                        db.Riders.Add(rider);
                        db.SaveChanges();
                        
                        System.Diagnostics.Debug.WriteLine($"已为用户 {riderId} 创建了骑手记录");
                    }

                    // 使用实际的Rider对象直接赋值给ViewBag
                    ViewBag.Rider = rider;
                    
                    // 重新计算今日完成订单数
                    var today = DateTime.Today;
                    var tomorrow = today.AddDays(1);
                    
                    var todayCompletedOrders = db.Orders
                        .Count(o => o.RiderId == riderId && o.Status == OrderStatus.Delivered 
                               && o.ActualDeliveryTime.HasValue 
                               && o.ActualDeliveryTime.Value >= today 
                               && o.ActualDeliveryTime.Value < tomorrow);
                    
                    ViewBag.TodayCompletedOrders = todayCompletedOrders;
                    
                    // 计算今日收入
                    var todayEarning = db.Orders
                        .Where(o => o.RiderId == riderId && o.Status == OrderStatus.Delivered
                               && o.ActualDeliveryTime.HasValue
                               && o.ActualDeliveryTime.Value >= today
                               && o.ActualDeliveryTime.Value < tomorrow)
                        .Sum(o => (decimal?)o.DeliveryFee) ?? 0M;
                    
                    ViewBag.TodayEarning = todayEarning;
                    
                    // 计算平均配送时间（分钟）
                    var completedOrders = db.Orders
                        .Where(o => o.RiderId == riderId && o.Status == OrderStatus.Delivered
                              && o.DeliveryStartTime.HasValue && o.ActualDeliveryTime.HasValue)
                        .ToList();
                    
                    double avgDeliveryTime = 25; // 默认值
                    if (completedOrders.Any())
                    {
                        var totalMinutes = completedOrders.Sum(o => 
                            (o.ActualDeliveryTime.Value - o.DeliveryStartTime.Value).TotalMinutes);
                        avgDeliveryTime = Math.Round(totalMinutes / completedOrders.Count);
                    }
                    
                    ViewBag.AverageDeliveryTime = avgDeliveryTime;
                    
                    // 骑手评分
                    ViewBag.Rating = rider.Rating;
                    
                    // 待接单的列表 - 只显示ReadyForDelivery状态的订单
                    var availableOrders = db.Orders
                        .Include(o => o.Restaurant)
                        .Include(o => o.Customer)
                        // 只获取待配送且未分配骑手的订单
                        .Where(o => o.Status == OrderStatus.ReadyForDelivery && o.RiderId == null)
                        .OrderByDescending(o => o.CreatedAt)
                        .ToList();
                    
                    // 输出可接订单数量以便调试
                    System.Diagnostics.Debug.WriteLine($"找到 {availableOrders.Count} 个可接订单");
                    foreach (var o in availableOrders)
                    {
                        System.Diagnostics.Debug.WriteLine($"可接订单 #{o.OrderNumber}, 状态: {o.Status}, 骑手ID: {o.RiderId}");
                    }
                    
                    var availableOrderViewModels = availableOrders.Select(o => new RiderController.OrderViewModel
                    {
                        OrderId = o.OrderId,
                        OrderNumber = o.OrderNumber,
                        RestaurantName = o.Restaurant != null ? o.Restaurant.Name : "未知餐厅",
                        RestaurantAddress = o.Restaurant != null ? o.Restaurant.Address : "",
                        DeliveryAddress = o.DeliveryAddress != null ? o.DeliveryAddress : (o.Customer != null ? o.Customer.Address : ""),
                        Distance = o.Distance > 0 ? o.Distance : CalculateDistance(o.Restaurant != null ? o.Restaurant.Address : "", o.DeliveryAddress),
                        DeliveryFee = o.DeliveryFee,
                        OrderTime = o.CreatedAt.ToString("HH:mm"),
                        Status = (int)o.Status,
                        IsSelected = false
                    }).ToList();

                    ViewBag.AvailableOrders = availableOrderViewModels;

                    // 待配送订单列表 - 显示当前骑手已接单但尚未完成的订单
                    var pendingDeliveryOrders = db.Orders
                        .Include(o => o.Customer)
                        .Include(o => o.Restaurant)
                        .Include(o => o.OrderDetails.Select(od => od.Dish))
                        .Where(o => o.RiderId == riderId && o.Status == OrderStatus.InDelivery)
                        .OrderByDescending(o => o.CreatedAt)
                        .ToList();
                    
                    // 输出待配送订单数量以便调试
                    System.Diagnostics.Debug.WriteLine($"找到 {pendingDeliveryOrders.Count} 个待配送订单");
                    foreach (var o in pendingDeliveryOrders)
                    {
                        System.Diagnostics.Debug.WriteLine($"待配送订单 #{o.OrderNumber}, 状态: {o.Status}, 骑手ID: {o.RiderId}");
                    }
                    
                    var pendingDeliveryViewModels = pendingDeliveryOrders.Select(o => new RiderController.OrderViewModel
                    {
                        OrderId = o.OrderId,
                        OrderNumber = o.OrderNumber,
                        RestaurantName = o.Restaurant != null ? o.Restaurant.Name : "未知餐厅",
                        RestaurantAddress = o.Restaurant != null ? o.Restaurant.Address : "",
                        CustomerName = o.Customer != null ? (string.IsNullOrEmpty(o.Customer.Name) ? o.Customer.Username : o.Customer.Name) : "未知顾客",
                        DeliveryAddress = o.DeliveryAddress != null ? o.DeliveryAddress : (o.Customer != null ? o.Customer.Address : ""),
                        CustomerPhone = o.Customer != null ? o.Customer.PhoneNumber : "",
                        Distance = o.Distance > 0 ? o.Distance : CalculateDistance(o.Restaurant != null ? o.Restaurant.Address : "", o.DeliveryAddress),
                        DeliveryFee = o.DeliveryFee,
                        EstimatedArrival = o.EstimatedDeliveryTime.HasValue ? o.EstimatedDeliveryTime.Value.ToString("HH:mm") : DateTime.Now.AddMinutes(30).ToString("HH:mm"),
                        Items = o.OrderDetails.Select(od => new RiderController.OrderItemViewModel 
                        { 
                            Name = od.Dish != null ? od.Dish.Name : "未知商品", 
                            Quantity = od.Quantity,
                            Price = od.Price
                        }).ToList(),
                        TotalAmount = o.TotalAmount,
                        Remarks = o.Remark != null ? o.Remark : "无备注",
                        AcceptTime = o.AcceptedTime.HasValue ? o.AcceptedTime.Value.ToString("HH:mm") : "",
                        PickupTime = o.PickupTime.HasValue ? o.PickupTime.Value.ToString("HH:mm") : "",
                        DeliveryStartTime = o.DeliveryStartTime.HasValue ? o.DeliveryStartTime.Value.ToString("HH:mm") : "",
                        CompletedTime = o.ActualDeliveryTime.HasValue ? o.ActualDeliveryTime.Value.ToString("HH:mm") : "",
                        Status = (int)o.Status,
                        MapUrl = "" // 暂无地图链接
                    }).ToList();

                    ViewBag.PendingDeliveryOrders = pendingDeliveryViewModels;
                    
                    // 移除之前的CurrentOrder - 现在使用PendingDeliveryOrders列表替代
                    ViewBag.CurrentOrder = null;

                    // 历史配送记录
                    var historyOrders = db.Orders
                        .Include(o => o.Restaurant)
                        .Where(o => o.RiderId == riderId && o.Status == OrderStatus.Delivered)
                        .OrderByDescending(o => o.UpdatedAt ?? o.CreatedAt)
                        .Take(10)
                        .ToList()
                        .Select(o => new RiderController.OrderViewModel
                        {
                            OrderId = o.OrderId,
                            OrderNumber = o.OrderNumber,
                            RestaurantName = o.Restaurant != null ? o.Restaurant.Name : "未知餐厅",
                            DeliveryAddress = o.DeliveryAddress != null ? o.DeliveryAddress : (o.Customer != null ? o.Customer.Address : ""),
                            Distance = o.Distance > 0 ? o.Distance : CalculateDistance(o.Restaurant != null ? o.Restaurant.Address : "", o.DeliveryAddress),
                            CompletedTime = o.ActualDeliveryTime.HasValue ? o.ActualDeliveryTime.Value.ToString("HH:mm") : (o.UpdatedAt.HasValue ? o.UpdatedAt.Value.ToString("HH:mm") : ""),
                            DeliveryFee = o.DeliveryFee
                        })
                        .ToList();

                    ViewBag.HistoryOrders = historyOrders;
                }
                else
                {
                    // 设置默认值
                    InitializeDefaultViewBags();
                }

                return View();
            }
            catch (Exception ex)
            {
                // 记录异常
                System.Diagnostics.Debug.WriteLine($"RiderController.Index 发生异常: {ex.Message}");
                
                // 设置默认值，确保视图不会出错
                InitializeDefaultViewBags();
                return View();
            }
        }

        private void InitializeDefaultViewBags()
        {
            // 尝试获取当前登录的骑手ID
            int riderId = 0;
            if (Session["UserId"] != null && Session["UserType"] as string == UserTypes.Rider)
            {
                int.TryParse(Session["UserId"].ToString(), out riderId);
            }
            
            // 尝试从数据库获取骑手信息
            Rider rider = null;
            if (riderId > 0)
            {
                rider = db.Riders.Find(riderId);
            }
            
            // 如果找不到骑手数据，创建一个默认对象
            if (rider == null)
            {
                rider = new Rider
                {
                    RiderId = riderId, // 使用实际的riderId，而不是硬编码为0
                    Name = Session["Username"] as string ?? "骑手",
                    Username = Session["Username"] as string ?? "rider",
                    Avatar = "/Content/Images/placeholders/32x32/user.png",
                    IsOnline = false,
                    IsAvailable = true,
                    Rating = 4.5,
                    TodayEarning = 0.0M,
                    TotalEarning = 0.0M,
                    TotalCompletedOrders = 0,
                    PhoneNumber = "",
                    Address = "",
                    CreatedAt = DateTime.Now
                };
            }
            
            ViewBag.Rider = rider;
            
            // 设置默认的统计数据
            // 如果有骑手ID，尝试计算真实数据
            if (riderId > 0)
            {
                // 重新计算今日完成订单数
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                
                // 使用日期范围比较而不是.Date属性
                var todayCompletedOrders = db.Orders
                    .Count(o => o.RiderId == riderId && o.Status == OrderStatus.Delivered 
                           && o.ActualDeliveryTime.HasValue 
                           && o.ActualDeliveryTime.Value >= today 
                           && o.ActualDeliveryTime.Value < tomorrow);
                
                ViewBag.TodayCompletedOrders = todayCompletedOrders;
                
                // 计算今日收入
                var todayEarning = db.Orders
                    .Where(o => o.RiderId == riderId && o.Status == OrderStatus.Delivered
                           && o.ActualDeliveryTime.HasValue
                           && o.ActualDeliveryTime.Value >= today
                           && o.ActualDeliveryTime.Value < tomorrow)
                    .Sum(o => (decimal?)o.DeliveryFee) ?? 0M;
                
                ViewBag.TodayEarning = todayEarning;
                
                // 计算平均配送时间（分钟）
                var completedOrders = db.Orders
                    .Where(o => o.RiderId == riderId && o.Status == OrderStatus.Delivered
                          && o.DeliveryStartTime.HasValue && o.ActualDeliveryTime.HasValue)
                    .ToList();
                
                double avgDeliveryTime = 25; // 默认值
                if (completedOrders.Any())
                {
                    var totalMinutes = completedOrders.Sum(o => 
                        (o.ActualDeliveryTime.Value - o.DeliveryStartTime.Value).TotalMinutes);
                    avgDeliveryTime = Math.Round(totalMinutes / completedOrders.Count);
                }
                
                ViewBag.AverageDeliveryTime = avgDeliveryTime;
                
                // 骑手评分
                ViewBag.Rating = rider.Rating;
            }
            else
            {
                // 如果没有骑手ID，使用默认值
                ViewBag.TodayCompletedOrders = 0;
                ViewBag.TodayEarning = 0.0M;
                ViewBag.AverageDeliveryTime = 25;
                ViewBag.Rating = rider != null ? rider.Rating : 4.5;
            }
            
            // 初始化空的订单列表
            ViewBag.AvailableOrders = new List<RiderController.OrderViewModel>();
            ViewBag.PendingDeliveryOrders = new List<RiderController.OrderViewModel>();
            ViewBag.HistoryOrders = new List<RiderController.OrderViewModel>();
            ViewBag.Period = "day";
        }

        // 计算距离的辅助方法（这是一个模拟计算方法）
        private double CalculateDistance(string startAddress, string endAddress)
        {
            // 实际项目中应该使用地图API计算真实距离
            Random rand = new Random();
            return Math.Round(rand.NextDouble() * 5 + 1, 1); // 生成1.0-6.0之间的随机距离
        }

        // GET: Rider/AcceptOrder/5
        public ActionResult AcceptOrder(int? id)
        {
            try
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }

                if (Session["UserId"] == null || Session["UserType"] as string != UserTypes.Rider)
                {
                    return RedirectToAction("Login", "Account");
                }

                int riderId = (int)Session["UserId"];
                var order = db.Orders.Find(id);

                if (order == null)
                {
                    return HttpNotFound();
                }
                
                // 输出订单信息以便调试
                System.Diagnostics.Debug.WriteLine($"骑手 {riderId} 尝试接单: #{order.OrderNumber}, 状态: {order.Status}, 当前骑手ID: {order.RiderId}");

                // 只允许接受ReadyForDelivery状态的订单
                if (order.Status == OrderStatus.ReadyForDelivery && order.RiderId == null)
                {
                    // 设置骑手ID
                    order.RiderId = riderId;
                    
                    // 更新订单状态为配送中
                    order.Status = OrderStatus.InDelivery;
                    order.DeliveryStartTime = DateTime.Now;
                    order.UpdatedAt = DateTime.Now;

                    // 预计送达时间为当前时间加上30分钟（示例）
                    order.EstimatedDeliveryTime = DateTime.Now.AddMinutes(30);

                    db.Entry(order).State = EntityState.Modified;
                    db.SaveChanges();
                    
                    System.Diagnostics.Debug.WriteLine($"骑手 {riderId} 成功接单: #{order.OrderNumber}");
                    
                    // 确保骑手处于在线状态
                    var rider = db.Riders.Find(riderId);
                    if (rider != null && !rider.IsOnline)
                    {
                        rider.IsOnline = true;
                        rider.IsAvailable = true;
                        db.SaveChanges();
                        System.Diagnostics.Debug.WriteLine($"接单后更新骑手状态为在线: {riderId}");
                    }
                    
                    TempData["SuccessMessage"] = "接单成功！订单已进入待配送状态。";
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"骑手 {riderId} 接单失败: #{order.OrderNumber}, 状态不符合条件或已被其他骑手接单");
                    TempData["ErrorMessage"] = "接单失败！该订单可能已被其他骑手接单或状态已变更。";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AcceptOrder 发生异常: {ex.Message}");
                TempData["ErrorMessage"] = "接单出错！" + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // GET: Rider/CompleteOrder/5
        public ActionResult CompleteOrder(int? id)
        {
            try
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }

                if (Session["UserId"] == null || Session["UserType"] as string != UserTypes.Rider)
                {
                    return RedirectToAction("Login", "Account");
                }

                int riderId = (int)Session["UserId"];
                var order = db.Orders.Find(id);

                if (order == null)
                {
                    System.Diagnostics.Debug.WriteLine($"订单不存在: ID={id}");
                    TempData["ErrorMessage"] = "订单不存在！";
                    return RedirectToAction("Index");
                }
                
                if (order.RiderId != riderId)
                {
                    System.Diagnostics.Debug.WriteLine($"骑手 {riderId} 尝试完成不属于自己的订单: #{order.OrderNumber}, 实际骑手ID: {order.RiderId}");
                    TempData["ErrorMessage"] = "该订单不属于您！";
                    return RedirectToAction("Index");
                }

                System.Diagnostics.Debug.WriteLine($"骑手 {riderId} 尝试完成订单: #{order.OrderNumber}, 状态: {order.Status}");

                if (order.Status == OrderStatus.InDelivery)
                {
                    order.Status = OrderStatus.Delivered;
                    order.UpdatedAt = DateTime.Now;
                    order.ActualDeliveryTime = DateTime.Now;

                    // 更新骑手收入
                    var rider = db.Riders.Find(riderId);
                    if (rider != null)
                    {
                        rider.TodayEarning += order.DeliveryFee;
                        rider.TotalEarning += order.DeliveryFee;
                        rider.TotalCompletedOrders += 1;
                        db.Entry(rider).State = EntityState.Modified;
                    }

                    db.Entry(order).State = EntityState.Modified;
                    db.SaveChanges();
                    
                    System.Diagnostics.Debug.WriteLine($"骑手 {riderId} 成功完成订单: #{order.OrderNumber}");
                    TempData["SuccessMessage"] = "订单已完成！配送费 ¥" + order.DeliveryFee + " 已计入您的账户。";
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"骑手 {riderId} 尝试完成订单失败: #{order.OrderNumber}, 当前状态: {order.Status}");
                    TempData["ErrorMessage"] = "该订单当前状态不允许操作！";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CompleteOrder 发生异常: {ex.Message}");
                TempData["ErrorMessage"] = "完成订单出错！" + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // POST: Rider/ToggleOnlineStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleOnlineStatus()
        {
            try
            {
                // 检查是否已登录
                if (Session["UserId"] == null || Session["UserType"] as string != UserTypes.Rider)
                {
                    return Json(new { success = false, message = "请先登录" });
                }

                int riderId;
                if (int.TryParse(Session["UserId"].ToString(), out riderId))
                {
                    // 获取骑手信息
                    var rider = db.Riders.Find(riderId);
                    
                    // 如果骑手记录不存在，创建一个新记录
                    if (rider == null)
                    {
                        rider = new Rider
                        {
                            RiderId = riderId,
                            Name = Session["Username"] as string ?? "骑手",
                            Username = Session["Username"] as string ?? "rider",
                            Avatar = "/Content/Images/placeholders/32x32/user.png",
                            IsOnline = false,
                            IsAvailable = true,
                            Rating = 4.5,
                            TodayEarning = 0.0M,
                            TotalEarning = 0.0M,
                            TotalCompletedOrders = 0,
                            PhoneNumber = "",
                            Address = "",
                            CreatedAt = DateTime.Now
                        };
                        
                        db.Riders.Add(rider);
                        db.SaveChanges();
                        
                        System.Diagnostics.Debug.WriteLine($"ToggleOnlineStatus: 已为用户 {riderId} 创建了骑手记录");
                    }

                    // 切换在线状态
                    rider.IsOnline = !rider.IsOnline;
                    
                    // 如果骑手设置为上线，也需要设置为可接单
                    if (rider.IsOnline)
                    {
                        rider.IsAvailable = true;
                    }
                    // 如果骑手设置为下线，也需要设置为不可接单
                    else
                    {
                        rider.IsAvailable = false;
                    }

                    db.SaveChanges();
                    
                    System.Diagnostics.Debug.WriteLine($"骑手 {riderId} 切换状态: IsOnline={rider.IsOnline}, IsAvailable={rider.IsAvailable}");

                    return Json(new { 
                        success = true, 
                        message = rider.IsOnline ? "您已上线，可以接单了" : "您已下线，将不再接收新订单",
                        isOnline = rider.IsOnline 
                    });
                }
                else
                {
                    return Json(new { success = false, message = "无效的用户ID" });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ToggleOnlineStatus 发生异常: {ex.Message}");
                return Json(new { success = false, message = "操作失败: " + ex.Message });
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