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

        // GET: Orders/MyOrders
        // 我的订单 - 顾客查看自己的订单
        public ActionResult MyOrders()
        {
            // 检查用户是否登录
            if (Session["CustomerId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int customerId = (int)Session["CustomerId"];
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

            // 检查当前用户是否有权限查看此订单（管理员可查看所有订单，用户只能查看自己的订单）
            if (Session["IsAdmin"] == null || !(bool)Session["IsAdmin"])
            {
                if (Session["CustomerId"] == null || (int)Session["CustomerId"] != order.CustomerId)
                {
                    return RedirectToAction("MyOrders");
                }
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
            // 检查用户是否登录
            if (Session["CustomerId"] == null)
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
            // 检查用户是否登录
            if (Session["CustomerId"] == null)
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
                .Where(d => d.RestaurantId == id && d.IsAvailable)
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
            // 检查用户是否登录
            if (Session["CustomerId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int customerId = (int)Session["CustomerId"];

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

            // 创建订单
            var order = new Order
            {
                CustomerId = customerId,
                RestaurantId = restaurantId,
                Status = OrderStatus.Pending,
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
                        UnitPrice = dish.Price
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
            // 检查用户是否登录
            if (Session["CustomerId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int customerId = (int)Session["CustomerId"];

            // 获取订单
            var order = db.Orders.Find(id);
            if (order == null)
            {
                return HttpNotFound();
            }

            // 检查订单是否属于当前用户
            if (order.CustomerId != customerId && (Session["IsAdmin"] == null || !(bool)Session["IsAdmin"]))
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
            ViewBag.CustomerId = new SelectList(db.Customers, "CustomerId", "PhoneNumber");
            ViewBag.RestaurantId = new SelectList(db.Restaurants, "RestaurantId", "Name");
            ViewBag.RiderId = new SelectList(db.Riders.Where(r => r.IsAvailable), "RiderId", "Name");
            return View();
        }

        // POST: Orders/Create
        // 处理创建订单请求 - 仅管理员可访问
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnly]
        public ActionResult Create([Bind(Include = "OrderId,CustomerId,RestaurantId,RiderId,Status")] Order order)
        {
            if (ModelState.IsValid)
            {
                order.CreatedAt = DateTime.Now;
                db.Orders.Add(order);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.CustomerId = new SelectList(db.Customers, "CustomerId", "PhoneNumber", order.CustomerId);
            ViewBag.RestaurantId = new SelectList(db.Restaurants, "RestaurantId", "Name", order.RestaurantId);
            ViewBag.RiderId = new SelectList(db.Riders.Where(r => r.IsAvailable), "RiderId", "Name", order.RiderId);
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
            ViewBag.CustomerId = new SelectList(db.Customers, "CustomerId", "PhoneNumber", order.CustomerId);
            ViewBag.RestaurantId = new SelectList(db.Restaurants, "RestaurantId", "Name", order.RestaurantId);
            ViewBag.RiderId = new SelectList(db.Riders, "RiderId", "Name", order.RiderId);
            return View(order);
        }

        // POST: Orders/Edit/5
        // 处理编辑订单请求 - 仅管理员可访问
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnly]
        public ActionResult Edit([Bind(Include = "OrderId,CustomerId,RestaurantId,RiderId,Status,CreatedAt")] Order order)
        {
            if (ModelState.IsValid)
            {
                order.UpdatedAt = DateTime.Now;
                db.Entry(order).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.CustomerId = new SelectList(db.Customers, "CustomerId", "PhoneNumber", order.CustomerId);
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