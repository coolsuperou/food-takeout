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
        public ActionResult Index()
            {
            return View(db.Dishes.ToList());
        }

        // GET: Dishes/ManageDishes
        public ActionResult ManageDishes()
        {
            var merchantId = Session["UserId"] as int?;
            if (!merchantId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // 首先获取商家关联的餐厅
                var restaurant = db.Restaurants.FirstOrDefault(r => r.MerchantId == merchantId.Value);
            if (restaurant == null)
            {
                    TempData["ErrorMessage"] = "您还没有创建餐厅，请先创建一个餐厅。";
                    return RedirectToAction("CreateRestaurant", "Merchant");
                }

                var restaurantId = restaurant.RestaurantId;
                System.Diagnostics.Debug.WriteLine($"ManageDishes - 商家ID: {merchantId.Value}, 餐厅ID: {restaurantId}");
                
                // 查询此餐厅的所有菜品
                var dishes = db.Dishes.Where(d => d.RestaurantId == restaurantId).ToList();
                return View(dishes);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ManageDishes出错: {ex.Message}");
                TempData["ErrorMessage"] = $"获取菜品数据失败: {ex.Message}";
                return View(new List<Dish>());
            }
        }

        // GET: Dishes/Create
        public ActionResult Create()
        {
            var merchantId = Session["UserId"] as int?;
            if (!merchantId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // 获取商家关联的餐厅
                var restaurant = db.Restaurants.FirstOrDefault(r => r.MerchantId == merchantId.Value);
                if (restaurant == null)
                {
                    TempData["ErrorMessage"] = "您还没有创建餐厅，请先创建一个餐厅。";
                    return RedirectToAction("CreateRestaurant", "Merchant");
                }

                ViewBag.RestaurantId = restaurant.RestaurantId;
                ViewBag.RestaurantName = restaurant.Name;
            return View();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dishes/Create出错: {ex.Message}");
                TempData["ErrorMessage"] = $"加载创建菜品页面失败: {ex.Message}";
                return RedirectToAction("Dashboard", "Merchant");
            }
        }

        // POST: Dishes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Name,Description,Price,Category,ImageUrl")] Dish dish, HttpPostedFileBase imageFile)
        {
            var merchantId = Session["UserId"] as int?;
            if (!merchantId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            // 获取商家关联的餐厅
            var restaurant = db.Restaurants.FirstOrDefault(r => r.MerchantId == merchantId.Value);
            if (restaurant == null)
            {
                TempData["ErrorMessage"] = "您还没有创建餐厅，请先创建一个餐厅。";
                return RedirectToAction("CreateRestaurant", "Merchant");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    dish.RestaurantId = restaurant.RestaurantId; // 使用餐厅ID而不是商家ID
                    dish.Status = "Active";
                    dish.CreateTime = DateTime.Now;
                    dish.IsAvailable = true; // 设置为可用状态

                if (imageFile != null && imageFile.ContentLength > 0)
                {
                        dish.ImageUrl = FileUploadHelper.UploadFile(imageFile, "Content/Images/Dishes");
                }
                    else
                    {
                        // 设置默认图片
                        dish.ImageUrl = "/Content/Images/Dishes/default.jpg";
                    }

                db.Dishes.Add(dish);
                db.SaveChanges();
                    TempData["SuccessMessage"] = "菜品创建成功！";
                return RedirectToAction("ManageDishes");
            }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"创建菜品失败: {ex.Message}");
                    ModelState.AddModelError("", "创建菜品失败：" + ex.Message);
                }
            }
            else
            {
                // 记录验证错误
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"验证错误 - {state.Key}: {error.ErrorMessage}");
                    }
                }
            }

            ViewBag.RestaurantId = restaurant.RestaurantId;
            ViewBag.RestaurantName = restaurant.Name;
            return View(dish);
        }

        // GET: Dishes/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            var merchantId = Session["UserId"] as int?;
            if (!merchantId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // 获取商家的餐厅ID
                var restaurant = db.Restaurants.FirstOrDefault(r => r.MerchantId == merchantId.Value);
                if (restaurant == null)
                {
                    TempData["ErrorMessage"] = "找不到您的餐厅信息";
                    return RedirectToAction("Dashboard", "Merchant");
                }

            Dish dish = db.Dishes.Find(id);
            if (dish == null)
            {
                return HttpNotFound();
            }

                // 验证菜品是否属于该商家的餐厅
                if (dish.RestaurantId != restaurant.RestaurantId)
                {
                    TempData["ErrorMessage"] = "您没有权限编辑此菜品";
                    return RedirectToAction("ManageDishes");
                }

                ViewBag.RestaurantId = restaurant.RestaurantId;
                ViewBag.RestaurantName = restaurant.Name;
                return View(dish);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"编辑菜品加载失败: {ex.Message}");
                TempData["ErrorMessage"] = $"加载菜品信息失败: {ex.Message}";
                return RedirectToAction("ManageDishes");
            }
        }

        // POST: Dishes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "DishId,Name,Description,Price,Category,IsAvailable,IsHot")] Dish dish, HttpPostedFileBase imageFile)
        {
            var merchantId = Session["UserId"] as int?;
            if (!merchantId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // 获取商家的餐厅
                var restaurant = db.Restaurants.FirstOrDefault(r => r.MerchantId == merchantId.Value);
                if (restaurant == null)
                {
                    TempData["ErrorMessage"] = "找不到您的餐厅信息";
                    return RedirectToAction("Dashboard", "Merchant");
            }

            if (ModelState.IsValid)
                {
                    try
            {
                var existingDish = db.Dishes.Find(dish.DishId);
                if (existingDish == null)
                {
                    return HttpNotFound();
                }

                        // 验证菜品是否属于该商家的餐厅
                        if (existingDish.RestaurantId != restaurant.RestaurantId)
                {
                            TempData["ErrorMessage"] = "您没有权限编辑此菜品";
                            return RedirectToAction("ManageDishes");
                        }

                        // 更新菜品信息
                    existingDish.Name = dish.Name;
                        existingDish.Description = dish.Description;
                    existingDish.Price = dish.Price;
                    existingDish.Category = dish.Category;
                    existingDish.IsAvailable = dish.IsAvailable;
                    existingDish.IsHot = dish.IsHot;
                        existingDish.UpdateTime = DateTime.Now;
                    
                    // 处理图片上传
                    if (imageFile != null && imageFile.ContentLength > 0)
                    {
                            try
                            {
                                // 删除旧图片（如果不是默认图片）
                                if (!string.IsNullOrEmpty(existingDish.ImageUrl) && 
                                    !existingDish.ImageUrl.Contains("default.jpg") &&
                                    existingDish.ImageUrl.StartsWith("/Content/"))
                                {
                                    var oldImagePath = Server.MapPath("~" + existingDish.ImageUrl);
                                    if (System.IO.File.Exists(oldImagePath))
                                    {
                                        System.IO.File.Delete(oldImagePath);
                                        System.Diagnostics.Debug.WriteLine($"删除旧图片: {oldImagePath}");
                                    }
                                }

                                // 上传新图片
                                existingDish.ImageUrl = FileUploadHelper.UploadFile(imageFile, "Content/Images/Dishes");
                                System.Diagnostics.Debug.WriteLine($"上传新图片: {existingDish.ImageUrl}");
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"处理图片时出错: {ex.Message}");
                                // 继续保存其他数据，不因为图片错误而中断
                            }
                        }

                        db.Entry(existingDish).State = EntityState.Modified;
                        db.SaveChanges();
                        TempData["SuccessMessage"] = "菜品更新成功！";
                        return RedirectToAction("ManageDishes");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"更新菜品失败: {ex.Message}");
                        ModelState.AddModelError("", "更新菜品失败：" + ex.Message);
                    }
                    }
                    else
                    {
                    // 记录验证错误
                    foreach (var state in ModelState)
                    {
                        foreach (var error in state.Value.Errors)
                        {
                            System.Diagnostics.Debug.WriteLine($"验证错误 - {state.Key}: {error.ErrorMessage}");
                        }
                    }
                }

                ViewBag.RestaurantId = restaurant.RestaurantId;
                ViewBag.RestaurantName = restaurant.Name;
                return View(dish);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"编辑菜品提交时出错: {ex.Message}");
                TempData["ErrorMessage"] = $"保存菜品信息失败: {ex.Message}";
                    return RedirectToAction("ManageDishes");
                }
        }

        // POST: Dishes/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var merchantId = Session["UserId"] as int?;
            if (!merchantId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // 获取商家的餐厅
                var restaurant = db.Restaurants.FirstOrDefault(r => r.MerchantId == merchantId.Value);
                if (restaurant == null)
                {
                    TempData["ErrorMessage"] = "找不到您的餐厅信息";
                    return RedirectToAction("Dashboard", "Merchant");
            }

                // 获取菜品
            Dish dish = db.Dishes.Find(id);
            if (dish == null)
            {
                    TempData["ErrorMessage"] = "找不到要删除的菜品";
                    return RedirectToAction("ManageDishes");
            }
            
                // 验证菜品是否属于该商家的餐厅
                if (dish.RestaurantId != restaurant.RestaurantId)
                {
                    TempData["ErrorMessage"] = "您没有权限删除此菜品";
                    return RedirectToAction("ManageDishes");
                }

                try
                {
                    // 删除图片文件，但不删除默认图片
                    if (!string.IsNullOrEmpty(dish.ImageUrl) && 
                        !dish.ImageUrl.Contains("default.jpg") &&
                        dish.ImageUrl.StartsWith("/Content/"))
                    {
                        var imagePath = Server.MapPath("~" + dish.ImageUrl);
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                            System.Diagnostics.Debug.WriteLine($"删除菜品图片: {imagePath}");
                        }
                    }

                    // 先检查该菜品是否有关联的订单详情
                    var hasOrderDetails = db.OrderDetails.Any(od => od.DishId == dish.DishId);
                    
                    if (hasOrderDetails)
                    {
                        // 如果有关联订单，则只设置为不可用状态，不删除记录
                        dish.IsAvailable = false;
                        dish.UpdateTime = DateTime.Now;
                        db.Entry(dish).State = EntityState.Modified;
                        db.SaveChanges();
                        
                        TempData["SuccessMessage"] = "菜品已标记为下架状态。由于该菜品已有订单记录，无法完全删除。";
                    }
                    else
                    {
                        // 如果没有关联订单，则可以安全删除
                db.Dishes.Remove(dish);
                db.SaveChanges();
                        TempData["SuccessMessage"] = "菜品已成功删除。";
            }
                    
                    return RedirectToAction("ManageDishes");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"删除菜品失败: {ex.Message}");
                    TempData["ErrorMessage"] = "删除菜品失败：" + ex.Message;
                    return RedirectToAction("ManageDishes");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"删除菜品处理时出错: {ex.Message}");
                TempData["ErrorMessage"] = $"处理删除请求失败: {ex.Message}";
            return RedirectToAction("ManageDishes");
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