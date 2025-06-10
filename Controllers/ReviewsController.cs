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
    public class ReviewsController : Controller
    {
        private FoodContext db = new FoodContext();

        // GET: Reviews
        // 评价列表页面
        public ActionResult Index()
        {
            var reviews = db.Reviews.Include(r => r.Customer).Include(r => r.Order).Include(r => r.Restaurant);
            return View(reviews.ToList());
        }

        // GET: Reviews/Details/5
        // 评价详情页面
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Review review = db.Reviews.Include(r => r.Customer).Include(r => r.Order).Include(r => r.Restaurant).FirstOrDefault(r => r.ReviewId == id);
            if (review == null)
            {
                return HttpNotFound();
            }
            return View(review);
        }

        // GET: Reviews/Create
        // 创建评价页面
        public ActionResult Create(int? orderId)
        {
            if (orderId != null)
            {
                Order order = db.Orders.Find(orderId);
                if (order != null)
                {
                    var review = new Review
                    {
                        OrderId = order.OrderId,
                        CustomerId = order.CustomerId,
                        RestaurantId = order.RestaurantId,
                        CreatedAt = DateTime.Now
                    };
                    return View(review);
                }
            }
            
            ViewBag.CustomerId = new SelectList(db.Customers, "CustomerId", "PhoneNumber");
            ViewBag.OrderId = new SelectList(db.Orders, "OrderId", "OrderId");
            ViewBag.RestaurantId = new SelectList(db.Restaurants, "RestaurantId", "Name");
            return View();
        }

        // POST: Reviews/Create
        // 处理创建评价请求
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ReviewId,OrderId,CustomerId,RestaurantId,Rating,Comment")] Review review)
        {
            if (ModelState.IsValid)
            {
                review.CreatedAt = DateTime.Now;
                db.Reviews.Add(review);
                db.SaveChanges();
                return RedirectToAction("Details", "Orders", new { id = review.OrderId });
            }

            ViewBag.CustomerId = new SelectList(db.Customers, "CustomerId", "PhoneNumber", review.CustomerId);
            ViewBag.OrderId = new SelectList(db.Orders, "OrderId", "OrderId", review.OrderId);
            ViewBag.RestaurantId = new SelectList(db.Restaurants, "RestaurantId", "Name", review.RestaurantId);
            return View(review);
        }

        // GET: Reviews/Edit/5
        // 编辑评价页面
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Review review = db.Reviews.Find(id);
            if (review == null)
            {
                return HttpNotFound();
            }
            ViewBag.CustomerId = new SelectList(db.Customers, "CustomerId", "PhoneNumber", review.CustomerId);
            ViewBag.OrderId = new SelectList(db.Orders, "OrderId", "OrderId", review.OrderId);
            ViewBag.RestaurantId = new SelectList(db.Restaurants, "RestaurantId", "Name", review.RestaurantId);
            return View(review);
        }

        // POST: Reviews/Edit/5
        // 处理编辑评价请求
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ReviewId,OrderId,CustomerId,RestaurantId,Rating,Comment,CreatedAt")] Review review)
        {
            if (ModelState.IsValid)
            {
                db.Entry(review).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Details", "Orders", new { id = review.OrderId });
            }
            ViewBag.CustomerId = new SelectList(db.Customers, "CustomerId", "PhoneNumber", review.CustomerId);
            ViewBag.OrderId = new SelectList(db.Orders, "OrderId", "OrderId", review.OrderId);
            ViewBag.RestaurantId = new SelectList(db.Restaurants, "RestaurantId", "Name", review.RestaurantId);
            return View(review);
        }

        // GET: Reviews/Delete/5
        // 删除评价页面
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Review review = db.Reviews.Include(r => r.Customer).Include(r => r.Order).Include(r => r.Restaurant).FirstOrDefault(r => r.ReviewId == id);
            if (review == null)
            {
                return HttpNotFound();
            }
            return View(review);
        }

        // POST: Reviews/Delete/5
        // 处理删除评价请求
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Review review = db.Reviews.Find(id);
            int orderId = review.OrderId;
            db.Reviews.Remove(review);
            db.SaveChanges();
            return RedirectToAction("Details", "Orders", new { id = orderId });
        }

        // GET: Reviews/RestaurantReviews/5
        // 餐厅评价列表页面
        public ActionResult RestaurantReviews(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            
            Restaurant restaurant = db.Restaurants.Find(id);
            if (restaurant == null)
            {
                return HttpNotFound();
            }
            
            var reviews = db.Reviews
                .Include(r => r.Customer)
                .Where(r => r.RestaurantId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
                
            ViewBag.Restaurant = restaurant;
            return View(reviews);
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