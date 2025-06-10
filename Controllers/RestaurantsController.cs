using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using food_takeout.Models;
using food_takeout.Helpers;

namespace food_takeout.Controllers
{
    public class RestaurantsController : Controller
    {
        private FoodContext db = new FoodContext();
        private const string ImageFolder = "Content/Images/Restaurants";

        // GET: Restaurants
        // 餐厅列表，所有用户可访问
        public ActionResult Index()
        {
            return View(db.Restaurants.ToList());
        }

        // GET: Restaurants/Details/5
        // 餐厅详情，所有用户可访问
        public ActionResult Details(int? id)
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

            // 获取该餐厅的菜品
            var dishes = db.Dishes.Where(d => d.RestaurantId == id && d.IsAvailable).ToList();
            ViewBag.Dishes = dishes;

            return View(restaurant);
        }

        // GET: Restaurants/Create
        // 创建餐厅，仅管理员可访问
        [AdminOnly]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Restaurants/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnly]
        public ActionResult Create([Bind(Include = "RestaurantId,Name,Location")] Restaurant restaurant, HttpPostedFileBase imageFile)
        {
            if (ModelState.IsValid)
            {
                // 处理图片上传
                if (imageFile != null && imageFile.ContentLength > 0)
                {
                    restaurant.ImageUrl = FileUploadHelper.UploadFile(imageFile, ImageFolder);
                }
                
                db.Restaurants.Add(restaurant);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(restaurant);
        }

        // GET: Restaurants/Edit/5
        [AdminOnly]
        public ActionResult Edit(int? id)
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
            return View(restaurant);
        }

        // POST: Restaurants/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnly]
        public ActionResult Edit([Bind(Include = "RestaurantId,Name,Location,ImageUrl")] Restaurant restaurant, HttpPostedFileBase imageFile)
        {
            if (ModelState.IsValid)
            {
                // 处理图片上传
                if (imageFile != null && imageFile.ContentLength > 0)
                {
                    restaurant.ImageUrl = FileUploadHelper.UploadFile(imageFile, ImageFolder);
                }
                
                db.Entry(restaurant).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(restaurant);
        }

        // GET: Restaurants/Delete/5
        [AdminOnly]
        public ActionResult Delete(int? id)
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
            return View(restaurant);
        }

        // POST: Restaurants/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AdminOnly]
        public ActionResult DeleteConfirmed(int id)
        {
            Restaurant restaurant = db.Restaurants.Find(id);
            db.Restaurants.Remove(restaurant);
            db.SaveChanges();
            return RedirectToAction("Index");
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