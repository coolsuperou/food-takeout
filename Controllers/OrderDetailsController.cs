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
    public class OrderDetailsController : Controller
    {
        private FoodContext db = new FoodContext();

        // GET: OrderDetails
        // 订单详情列表页面
        public ActionResult Index(int? orderId)
        {
            if (orderId == null)
            {
                var orderDetails = db.OrderDetails.Include(o => o.Dish).Include(o => o.Order);
                return View(orderDetails.ToList());
            }
            else
            {
                var orderDetails = db.OrderDetails
                    .Include(o => o.Dish)
                    .Include(o => o.Order)
                    .Where(o => o.OrderId == orderId);
                ViewBag.OrderId = orderId;
                Order order = db.Orders.Find(orderId);
                ViewBag.Order = order;
                return View(orderDetails.ToList());
            }
        }

        // GET: OrderDetails/Details/5
        // 订单明细详情页面
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            OrderDetail orderDetail = db.OrderDetails
                .Include(o => o.Dish)
                .Include(o => o.Order)
                .FirstOrDefault(o => o.OrderDetailId == id);
            if (orderDetail == null)
            {
                return HttpNotFound();
            }
            return View(orderDetail);
        }

        // GET: OrderDetails/Create
        // 创建订单明细页面
        public ActionResult Create(int? orderId)
        {
            if (orderId != null)
            {
                ViewBag.OrderId = new SelectList(db.Orders.Where(o => o.OrderId == orderId), "OrderId", "OrderId");
                Order order = db.Orders.Find(orderId);
                if (order != null)
                {
                    ViewBag.DishId = new SelectList(db.Dishes.Where(d => d.RestaurantId == order.RestaurantId), "DishId", "Name");
                }
                else
                {
                    ViewBag.DishId = new SelectList(db.Dishes, "DishId", "Name");
                }
            }
            else
            {
                ViewBag.DishId = new SelectList(db.Dishes, "DishId", "Name");
                ViewBag.OrderId = new SelectList(db.Orders, "OrderId", "OrderId");
            }
            return View();
        }

        // POST: OrderDetails/Create
        // 处理创建订单明细请求
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "OrderDetailId,OrderId,DishId,Quantity")] OrderDetail orderDetail)
        {
            if (ModelState.IsValid)
            {
                // 根据菜品价格设置单价
                var dish = db.Dishes.Find(orderDetail.DishId);
                if (dish != null)
                {
                    orderDetail.UnitPrice = dish.Price;
                }
                
                db.OrderDetails.Add(orderDetail);
                db.SaveChanges();
                return RedirectToAction("Details", "Orders", new { id = orderDetail.OrderId });
            }

            ViewBag.DishId = new SelectList(db.Dishes, "DishId", "Name", orderDetail.DishId);
            ViewBag.OrderId = new SelectList(db.Orders, "OrderId", "OrderId", orderDetail.OrderId);
            return View(orderDetail);
        }

        // GET: OrderDetails/Edit/5
        // 编辑订单明细页面
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            OrderDetail orderDetail = db.OrderDetails.Find(id);
            if (orderDetail == null)
            {
                return HttpNotFound();
            }
            
            Order order = db.Orders.Find(orderDetail.OrderId);
            if (order != null)
            {
                ViewBag.DishId = new SelectList(db.Dishes.Where(d => d.RestaurantId == order.RestaurantId), "DishId", "Name", orderDetail.DishId);
            }
            else
            {
                ViewBag.DishId = new SelectList(db.Dishes, "DishId", "Name", orderDetail.DishId);
            }
            
            ViewBag.OrderId = new SelectList(db.Orders, "OrderId", "OrderId", orderDetail.OrderId);
            return View(orderDetail);
        }

        // POST: OrderDetails/Edit/5
        // 处理编辑订单明细请求
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "OrderDetailId,OrderId,DishId,Quantity,UnitPrice")] OrderDetail orderDetail)
        {
            if (ModelState.IsValid)
            {
                db.Entry(orderDetail).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Details", "Orders", new { id = orderDetail.OrderId });
            }
            ViewBag.DishId = new SelectList(db.Dishes, "DishId", "Name", orderDetail.DishId);
            ViewBag.OrderId = new SelectList(db.Orders, "OrderId", "OrderId", orderDetail.OrderId);
            return View(orderDetail);
        }

        // GET: OrderDetails/Delete/5
        // 删除订单明细页面
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            OrderDetail orderDetail = db.OrderDetails
                .Include(o => o.Dish)
                .Include(o => o.Order)
                .FirstOrDefault(o => o.OrderDetailId == id);
            if (orderDetail == null)
            {
                return HttpNotFound();
            }
            return View(orderDetail);
        }

        // POST: OrderDetails/Delete/5
        // 处理删除订单明细请求
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            OrderDetail orderDetail = db.OrderDetails.Find(id);
            int orderId = orderDetail.OrderId;
            db.OrderDetails.Remove(orderDetail);
            db.SaveChanges();
            return RedirectToAction("Details", "Orders", new { id = orderId });
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