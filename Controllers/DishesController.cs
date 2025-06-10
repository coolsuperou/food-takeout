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
    public class DishesController : Controller
    {
        private FoodContext db = new FoodContext();
        private const string ImageFolder = "Content/Images/Dishes";

        // GET: Dishes
        // 菜品列表页面，允许所有用户查看
        public ActionResult Index()
        {
            var dishes = db.Dishes.Include(d => d.Restaurant);
            return View(dishes.ToList());
        }

        // GET: Dishes/Details/5
        // 菜品详情页面，允许所有用户查看
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Dish dish = db.Dishes.Include(d => d.Restaurant).FirstOrDefault(d => d.DishId == id);
            if (dish == null)
            {
                return HttpNotFound();
            }
            return View(dish);
        }

        // GET: Dishes/Create
        // 创建菜品页面，仅管理员可用
        [AdminOnly]
        public ActionResult Create()
        {
            ViewBag.RestaurantId = new SelectList(db.Restaurants, "RestaurantId", "Name");
            return View();
        }

        // POST: Dishes/Create
        // 处理创建菜品请求，仅管理员可用
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnly]
        public ActionResult Create([Bind(Include = "DishId,RestaurantId,Name,Price,IsAvailable")] Dish dish, HttpPostedFileBase imageFile)
        {
            if (ModelState.IsValid)
            {
                // 处理图片上传
                if (imageFile != null && imageFile.ContentLength > 0)
                {
                    dish.ImageUrl = FileUploadHelper.UploadFile(imageFile, ImageFolder);
                }
                
                db.Dishes.Add(dish);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.RestaurantId = new SelectList(db.Restaurants, "RestaurantId", "Name", dish.RestaurantId);
            return View(dish);
        }

        // GET: Dishes/Edit/5
        // 编辑菜品页面，仅管理员可用
        [AdminOnly]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Dish dish = db.Dishes.Find(id);
            if (dish == null)
            {
                return HttpNotFound();
            }
            ViewBag.RestaurantId = new SelectList(db.Restaurants, "RestaurantId", "Name", dish.RestaurantId);
            return View(dish);
        }

        // POST: Dishes/Edit/5
        // 处理编辑菜品请求，仅管理员可用
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AdminOnly]
        public ActionResult Edit([Bind(Include = "DishId,RestaurantId,Name,Price,ImageUrl,IsAvailable")] Dish dish, HttpPostedFileBase imageFile)
        {
            if (ModelState.IsValid)
            {
                // 处理图片上传
                if (imageFile != null && imageFile.ContentLength > 0)
                {
                    dish.ImageUrl = FileUploadHelper.UploadFile(imageFile, ImageFolder);
                }
                
                db.Entry(dish).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.RestaurantId = new SelectList(db.Restaurants, "RestaurantId", "Name", dish.RestaurantId);
            return View(dish);
        }

        // GET: Dishes/Delete/5
        // 删除菜品页面，仅管理员可用
        [AdminOnly]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Dish dish = db.Dishes.Include(d => d.Restaurant).FirstOrDefault(d => d.DishId == id);
            if (dish == null)
            {
                return HttpNotFound();
            }
            return View(dish);
        }

        // POST: Dishes/Delete/5
        // 处理删除菜品请求，仅管理员可用
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AdminOnly]
        public ActionResult DeleteConfirmed(int id)
        {
            Dish dish = db.Dishes.Find(id);
            db.Dishes.Remove(dish);
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