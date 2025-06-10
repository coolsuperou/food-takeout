using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using food_takeout.Models;

namespace food_takeout.Controllers
{
    public class CartController : Controller
    {
        private FoodContext db = new FoodContext();

        // 获取购物车数据
        private List<CartItem> GetCart()
        {
            List<CartItem> cart = Session["Cart"] as List<CartItem>;
            if (cart == null)
            {
                cart = new List<CartItem>();
                Session["Cart"] = cart;
            }
            return cart;
        }

        // GET: Cart
        public ActionResult Index()
        {
            // 检查用户是否登录
            if (Session["CustomerId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = GetCart();
            return View(cart);
        }

        // POST: Cart/AddToCart
        [HttpPost]
        public ActionResult AddToCart(int dishId, int quantity = 1)
        {
            // 检查用户是否登录
            if (Session["CustomerId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // 查找菜品
            var dish = db.Dishes.Find(dishId);
            if (dish == null)
            {
                return HttpNotFound();
            }

            // 获取购物车
            var cart = GetCart();
            
            // 检查购物车中是否已有该菜品
            var existingItem = cart.FirstOrDefault(i => i.DishId == dishId);
            if (existingItem != null)
            {
                // 更新数量
                existingItem.Quantity += quantity;
            }
            else
            {
                // 添加新项
                cart.Add(new CartItem
                {
                    DishId = dish.DishId,
                    DishName = dish.Name,
                    Quantity = quantity,
                    UnitPrice = dish.Price,
                    RestaurantId = dish.RestaurantId,
                    RestaurantName = dish.Restaurant.Name
                });
            }

            // 保存购物车
            Session["Cart"] = cart;

            // 返回操作结果
            return RedirectToAction("Index");
        }

        // POST: Cart/RemoveFromCart
        [HttpPost]
        public ActionResult RemoveFromCart(int dishId)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(i => i.DishId == dishId);
            if (item != null)
            {
                cart.Remove(item);
                Session["Cart"] = cart;
            }
            return RedirectToAction("Index");
        }

        // POST: Cart/Checkout
        [HttpPost]
        public ActionResult Checkout()
        {
            // 检查用户是否登录
            if (Session["CustomerId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = GetCart();
            if (cart.Count == 0)
            {
                ModelState.AddModelError("", "购物车为空，无法下单");
                return View("Index", cart);
            }

            // 获取当前用户ID
            int customerId = (int)Session["CustomerId"];
            
            // 获取餐厅ID（假设一次只能从一个餐厅下单）
            int restaurantId = cart[0].RestaurantId;

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
            foreach (var item in cart)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.OrderId,
                    DishId = item.DishId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                };
                db.OrderDetails.Add(orderDetail);
            }

            db.SaveChanges();

            // 清空购物车
            cart.Clear();
            Session["Cart"] = cart;

            // 重定向到订单详情页
            return RedirectToAction("Details", "Orders", new { id = order.OrderId });
        }
    }

    // 购物车项模型
    public class CartItem
    {
        public int DishId { get; set; }
        public string DishName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int RestaurantId { get; set; }
        public string RestaurantName { get; set; }
        public decimal TotalPrice { get { return Quantity * UnitPrice; } }
    }
} 