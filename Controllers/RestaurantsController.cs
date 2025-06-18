using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using food_takeout.Models;
using System.IO;

namespace food_takeout.Controllers
{
    public class RestaurantsController : Controller
    {
        private FoodContext db = new FoodContext();

        // GET: Restaurants
        public ActionResult Index()
        {
            try
            {
                var restaurants = db.Restaurants
                    .OrderByDescending(r => r.IsActive)
                    .ThenByDescending(r => r.IsHot)
                    .ToList();
                return View(restaurants);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("异常信息: " + ex.Message);
                return View(new List<Restaurant>());
            }
        }

        // GET: Restaurants/Details/5
        public ActionResult Details(int? id)
        {
            try
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

                // 直接使用SQL查询获取菜品数据，避免EF复杂性
                var sql = "SELECT Id, Name, Price, Category, Description, ImageUrl, IsHot, SoldCount FROM Dish WHERE RestaurantId = @RestaurantId";
                var dishes = db.Database.SqlQuery<DishViewModel>(sql, 
                    new System.Data.SqlClient.SqlParameter("RestaurantId", id)).ToList();
                
                // 显示详细调试信息
                System.Diagnostics.Debug.WriteLine($"SQL查询结果 - 餐厅ID:{id}, 菜品数量:{dishes.Count}");
                foreach (var dish in dishes)
                {
                    System.Diagnostics.Debug.WriteLine($"菜品:{dish.Id}, {dish.Name}, {dish.Price}");
                }
                
                // 直接放入ViewData，避免ViewBag的动态特性可能导致的问题
                ViewData["DishCount"] = dishes.Count;
                ViewData["DishList"] = dishes;
                
                // 同时保留ViewBag方式
                ViewBag.Dishes = dishes;

                return View(restaurant);
            }
            catch (Exception ex)
            {
                // 记录异常
                System.Diagnostics.Debug.WriteLine($"RestaurantsController.Details 发生异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
                
                // 使用ViewData和ViewBag双保险
                ViewData["DishCount"] = 0;
                ViewData["DishList"] = new List<DishViewModel>();
                ViewBag.Dishes = new List<DishViewModel>();
                
                return View(new Restaurant { Name = "加载餐厅失败", RestaurantId = id ?? 0 });
            }
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
        public ActionResult Create([Bind(Include = "RestaurantId,Name,Address")] Restaurant restaurant, HttpPostedFileBase imageFile)
        {
            if (ModelState.IsValid)
            {
                // 处理图片上传
                if (imageFile != null && imageFile.ContentLength > 0)
                {
                    try
                    {
                        var uploadDir = Server.MapPath("~/Uploads/Restaurants");
                        if (!Directory.Exists(uploadDir))
                        {
                            Directory.CreateDirectory(uploadDir);
                        }

                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                        var filePath = Path.Combine(uploadDir, fileName);
                        imageFile.SaveAs(filePath);

                        restaurant.ImageUrl = "/Uploads/Restaurants/" + fileName;
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("imageFile", "图片上传失败：" + ex.Message);
                        return View(restaurant);
                    }
                }
                
                db.Restaurants.Add(restaurant);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(restaurant);
        }

        // GET: Restaurants/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                // 如果没有提供ID或ID为0，重定向到商家中心
                if (Session["UserType"] as string == UserTypes.Merchant)
            {
                    return RedirectToAction("MyRestaurants", "Merchant");
                }
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "请提供有效的餐厅ID");
            }

            Restaurant restaurant = db.Restaurants.Find(id);
            if (restaurant == null)
            {
                return HttpNotFound();
            }

            // 验证权限
            if (Session["UserType"] as string == UserTypes.Merchant)
            {
                if (restaurant.MerchantId != (int)Session["UserId"])
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "您没有权限修改此餐厅信息");
                }
            }
            else if (Session["UserType"] as string != UserTypes.Admin)
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "您没有权限修改餐厅信息");
            }

            return RedirectToAction("RestaurantSettings", "Merchant", new { id = id });
        }

        // POST: Restaurants/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "RestaurantId,Name,PhoneNumber,Address,IsActive")] Restaurant restaurant, HttpPostedFileBase imageFile)
        {
            if (ModelState.IsValid)
            {
            var existingRestaurant = db.Restaurants.Find(restaurant.RestaurantId);
            if (existingRestaurant == null)
            {
                return HttpNotFound();
            }
            
                // 验证权限
                if (Session["UserType"] as string == UserTypes.Merchant)
                {
                    if (existingRestaurant.MerchantId != (int)Session["UserId"])
                    {
                        return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "您没有权限修改此餐厅信息");
                    }
                }
                else if (Session["UserType"] as string != UserTypes.Admin)
            {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden, "您没有权限修改餐厅信息");
            }

                // 处理图片上传
                if (imageFile != null && imageFile.ContentLength > 0)
                {
                    try
                    {
                        var uploadDir = Server.MapPath("~/Uploads/Restaurants");
                        if (!Directory.Exists(uploadDir))
                        {
                            Directory.CreateDirectory(uploadDir);
                        }

                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                        var filePath = Path.Combine(uploadDir, fileName);
                        imageFile.SaveAs(filePath);

                        // 删除旧图片
                        if (!string.IsNullOrEmpty(existingRestaurant.ImageUrl))
                        {
                            var oldFilePath = Server.MapPath(existingRestaurant.ImageUrl);
                            if (System.IO.File.Exists(oldFilePath))
            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }

                        existingRestaurant.ImageUrl = "/Uploads/Restaurants/" + fileName;
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("imageFile", "图片上传失败：" + ex.Message);
                        return View(restaurant);
                    }
                }

                // 更新其他属性
                existingRestaurant.Name = restaurant.Name;
                existingRestaurant.PhoneNumber = restaurant.PhoneNumber;
                existingRestaurant.Address = restaurant.Address;
                existingRestaurant.IsActive = restaurant.IsActive;

                db.SaveChanges();
                return RedirectToAction("RestaurantSettings", "Merchant", new { id = restaurant.RestaurantId });
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
            if (restaurant == null)
            {
                return HttpNotFound();
            }

            // 删除餐厅图片
            if (!string.IsNullOrEmpty(restaurant.ImageUrl))
            {
                var imagePath = Server.MapPath(restaurant.ImageUrl);
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            db.Restaurants.Remove(restaurant);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult Search(string keyword)
        {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return RedirectToAction("Index");
                }

                var restaurants = db.Restaurants
                .Where(r => r.IsActive && (
                    r.Name.Contains(keyword) ||
                    r.Address.Contains(keyword) ||
                        r.Description.Contains(keyword) || 
                        r.Category.Contains(keyword) || 
                    r.Categories.Contains(keyword)
                ))
                    .OrderByDescending(r => r.IsHot)
                    .ToList()
                    .Select(r => new
                    {
                        RestaurantId = r.RestaurantId,
                        Name = r.Name,
                        Address = r.Address,
                        Description = r.Description ?? "",
                        BusinessHours = r.BusinessHours ?? "09:00-21:00",
                    ImageUrl = r.ImageUrl,
                        Rating = r.Rating,
                        Categories = r.Categories,
                        DeliveryTime = r.DeliveryTime,
                        IsHot = r.IsHot,
                        IsNew = r.IsNew,
                        Count = 1
                    })
                    .ToList();

                ViewBag.Restaurants = restaurants;
            ViewBag.Keyword = keyword;
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