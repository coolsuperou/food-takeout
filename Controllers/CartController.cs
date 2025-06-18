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
    public class CartItem
    {
        public int DishId { get; set; }
        public string DishName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public int RestaurantId { get; set; }
        public string RestaurantName { get; set; }
        public decimal TotalPrice { get { return Quantity * Price; } }
    }

    public class CartController : Controller
    {
        private FoodContext db = new FoodContext();

        // GET: Cart
        public ActionResult Index()
        {
            // 检查用户是否已登录
            if (Session["UserId"] == null || Session["UserType"] == null || Session["UserType"].ToString() != UserTypes.Customer)
            {
                // 未登录，重定向到登录页面
                return RedirectToAction("Login", "Account", new { returnUrl = Request.Url.ToString() });
            }

            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();
            return View(cart);
        }

        // GET: Cart/Add
        public ActionResult Add(int? dishId = null, int quantity = 1, string returnUrl = null)
        {
            // 检查用户是否已登录
            if (Session["UserId"] == null || Session["UserType"] == null || Session["UserType"].ToString() != UserTypes.Customer)
            {
                // 未登录，保存当前请求信息到TempData，然后重定向到登录页面
                if (dishId.HasValue)
                {
                    TempData["PendingDishId"] = dishId.Value;
                    TempData["PendingQuantity"] = quantity;
                    TempData["PendingAction"] = "AddToCart";
                }
                
                // 如果没有提供returnUrl，则使用当前请求的URL
                if (string.IsNullOrEmpty(returnUrl))
                {
                    returnUrl = Request.UrlReferrer?.ToString() ?? Url.Action("Index", "Restaurants");
                }
                
                return RedirectToAction("Login", "Account", new { returnUrl = returnUrl });
            }

            // 如果没有提供dishId，则只显示添加成功页面
            if (!dishId.HasValue)
            {
                return View();
            }

            var dish = db.Dishes.Find(dishId.Value);
            if (dish == null)
            {
                return HttpNotFound();
            }

            var cart = Session["Cart"] as List<CartItem> ?? new List<CartItem>();
            
            // 检查购物车中是否已有其他餐厅的商品
            if (cart.Any() && cart.First().RestaurantId != dish.RestaurantId)
            {
                // 如果有其他餐厅的商品，提示用户
                TempData["ErrorMessage"] = "您的购物车中已有其他餐厅的商品，不能同时从多家餐厅点餐。";
                
                // 如果没有提供returnUrl，则使用当前请求的URL
                if (string.IsNullOrEmpty(returnUrl))
                {
                    returnUrl = Request.UrlReferrer?.ToString() ?? Url.Action("Index", "Restaurants");
                }
                
                return Redirect(returnUrl);
            }
            
            var cartItem = cart.FirstOrDefault(c => c.DishId == dishId.Value);
            
            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    DishId = dishId.Value,
                    DishName = dish.Name,
                    Quantity = quantity,
                    Price = dish.Price,
                    RestaurantId = dish.RestaurantId,
                    RestaurantName = dish.Restaurant.Name
                });
            }

            Session["Cart"] = cart;
            
            // 返回Add视图
            return View();
        }

        // POST: Cart/Update
        [HttpPost]
        public ActionResult Update(int dishId, int quantity)
        {
            // 检查用户是否已登录
            if (Session["UserId"] == null || Session["UserType"] == null || Session["UserType"].ToString() != UserTypes.Customer)
            {
                // 未登录，重定向到登录页面
                return RedirectToAction("Login", "Account", new { returnUrl = Request.Url.ToString() });
            }

            var cart = Session["Cart"] as List<CartItem>;
            if (cart == null)
            {
                return HttpNotFound();
            }

            var cartItem = cart.FirstOrDefault(c => c.DishId == dishId);
            if (cartItem != null)
            {
                if (quantity <= 0)
            {
                    cart.Remove(cartItem);
                }
                else
                {
                    cartItem.Quantity = quantity;
                }
            }

                Session["Cart"] = cart;
            return RedirectToAction("Index");
        }

        // POST: Cart/Remove
        [HttpPost]
        public ActionResult Remove(int dishId)
        {
            // 检查用户是否已登录
            if (Session["UserId"] == null || Session["UserType"] == null || Session["UserType"].ToString() != UserTypes.Customer)
            {
                // 未登录，重定向到登录页面
                return RedirectToAction("Login", "Account", new { returnUrl = Request.Url.ToString() });
            }

            var cart = Session["Cart"] as List<CartItem>;
            if (cart != null)
            {
                var cartItem = cart.FirstOrDefault(c => c.DishId == dishId);
                if (cartItem != null)
                {
                    cart.Remove(cartItem);
                }
            }

            Session["Cart"] = cart;
            return RedirectToAction("Index");
        }

        // POST: Cart/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Checkout()
        {
            // 检查用户是否已登录
            if (Session["UserId"] == null || Session["UserType"] == null || Session["UserType"].ToString() != UserTypes.Customer)
            {
                // 未登录，重定向到登录页面
                return RedirectToAction("Login", "Account", new { returnUrl = Request.Url.ToString() });
            }

            var cart = Session["Cart"] as List<CartItem>;
            if (cart == null || !cart.Any())
            {
                return RedirectToAction("Index");
            }

            var customerId = (int)Session["UserId"];
            var customer = db.Customers.Find(customerId);
            
            // 计算商品总价
            decimal subtotal = cart.Sum(item => item.TotalPrice);
            
            // 固定配送费5元
            decimal deliveryFee = 5.00M;
            
            // 订单总价 = 商品总价 + 配送费
            decimal totalAmount = subtotal + deliveryFee;
            
            var order = new Order
            {
                CustomerId = customerId,
                RestaurantId = cart.First().RestaurantId,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.Now,
                DeliveryAddress = customer.Address,
                OrderNumber = GenerateOrderNumber(),
                DeliveryFee = deliveryFee,
                TotalAmount = totalAmount
            };

            db.Orders.Add(order);
            db.SaveChanges();

            foreach (var item in cart)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.OrderId,
                    DishId = item.DishId,
                    Quantity = item.Quantity,
                    Price = item.Price,
                    Subtotal = item.Price * item.Quantity
                };
                db.OrderDetails.Add(orderDetail);
            }

            db.SaveChanges();
            Session["Cart"] = null;

            return RedirectToAction("Details", "Orders", new { id = order.OrderId });
        }

        // 生成订单编号
        private string GenerateOrderNumber()
        {
            // 生成格式：年月日时分秒+3位随机数
            string orderNumber = DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(100, 999).ToString();
            
            // 确保订单号是唯一的
            while (db.Orders.Any(o => o.OrderNumber == orderNumber))
            {
                orderNumber = DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(100, 999).ToString();
                System.Threading.Thread.Sleep(10); // 稍等片刻以改变时间戳
            }
            
            return orderNumber;
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