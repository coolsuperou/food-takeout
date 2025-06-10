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
    public class RidersController : Controller
    {
        private FoodContext db = new FoodContext();

        // GET: Riders
        // 骑手列表页面
        public ActionResult Index()
        {
            return View(db.Riders.ToList());
        }

        // GET: Riders/Details/5
        // 骑手详情页面
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Rider rider = db.Riders.Find(id);
            if (rider == null)
            {
                return HttpNotFound();
            }
            return View(rider);
        }

        // GET: Riders/Create
        // 创建骑手页面
        public ActionResult Create()
        {
            return View();
        }

        // POST: Riders/Create
        // 处理创建骑手请求
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "RiderId,Name,PhoneNumber,IsAvailable")] Rider rider)
        {
            if (ModelState.IsValid)
            {
                db.Riders.Add(rider);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(rider);
        }

        // GET: Riders/Edit/5
        // 编辑骑手页面
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Rider rider = db.Riders.Find(id);
            if (rider == null)
            {
                return HttpNotFound();
            }
            return View(rider);
        }

        // POST: Riders/Edit/5
        // 处理编辑骑手请求
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "RiderId,Name,PhoneNumber,IsAvailable")] Rider rider)
        {
            if (ModelState.IsValid)
            {
                db.Entry(rider).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(rider);
        }

        // GET: Riders/Delete/5
        // 删除骑手页面
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Rider rider = db.Riders.Find(id);
            if (rider == null)
            {
                return HttpNotFound();
            }
            return View(rider);
        }

        // POST: Riders/Delete/5
        // 处理删除骑手请求
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Rider rider = db.Riders.Find(id);
            db.Riders.Remove(rider);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: Riders/Orders
        // 骑手订单列表页面
        public ActionResult Orders(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            
            Rider rider = db.Riders.Find(id);
            if (rider == null)
            {
                return HttpNotFound();
            }
            
            var orders = db.Orders
                .Include(o => o.Customer)
                .Include(o => o.Restaurant)
                .Where(o => o.RiderId == id)
                .ToList();
                
            ViewBag.Rider = rider;
            return View(orders);
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