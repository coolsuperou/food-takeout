using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using food_takeout.Models;
using food_takeout.ViewModels;
using System.Data.Entity;

namespace food_takeout.Controllers
{
    public class AccountController : Controller
    {
        private FoodContext db = new FoodContext();

        // GET: Account/Login
        public ActionResult Login(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                if (model.UserType == UserTypes.Merchant)
                {
                    var merchant = db.Merchants.FirstOrDefault(r => r.Username == model.Username && r.Password == model.Password);
                    if (merchant != null)
                    {
                        // 登录成功，设置Session
                        Session["UserId"] = merchant.MerchantId;
                        Session["Username"] = merchant.Username;
                        Session["UserType"] = UserTypes.Merchant;
                        
                        // 获取商家关联的餐厅状态并保存到Session
                        var restaurant = db.Restaurants
                            .FirstOrDefault(r => r.MerchantId == merchant.MerchantId);
                        
                        if (restaurant != null)
                        {
                            // 保存餐厅状态到Session
                            Session["RestaurantStatus"] = restaurant.IsActive;
                            Session["RestaurantId"] = restaurant.RestaurantId;
                            
                            // 记录日志
                            System.Diagnostics.Debug.WriteLine($"商家登录: 自动获取餐厅状态 - 餐厅ID: {restaurant.RestaurantId}, 状态: {(restaurant.IsActive ? "营业中" : "休息中")}");
                        }
                        else
                        {
                            // 餐厅不存在，设置默认状态
                            Session["RestaurantStatus"] = false;
                            System.Diagnostics.Debug.WriteLine($"商家登录: 无关联餐厅，设置默认状态为休息中");
                        }
                        
                        return RedirectToAction("Dashboard", "Merchant");
                    }
                }
                else if (model.UserType == UserTypes.Customer)
                {
                    var customer = db.Customers.FirstOrDefault(c => c.Username == model.Username && c.Password == model.Password);
                    if (customer != null)
                    {
                        // 登录成功，设置Session
                        Session["UserId"] = customer.CustomerId;
                        Session["Username"] = customer.Username;
                        Session["UserType"] = UserTypes.Customer;
                        
                        // 如果Name为空，则使用Username更新
                        if (string.IsNullOrEmpty(customer.Name))
                        {
                            customer.Name = customer.Username;
                            db.Entry(customer).State = EntityState.Modified;
                            db.SaveChanges();
                        }
                        
                        // 检查是否有未完成的购物车操作
                        if (TempData["PendingAction"] != null && TempData["PendingAction"].ToString() == "AddToCart" && 
                            TempData["PendingDishId"] != null && TempData["PendingQuantity"] != null)
                        {
                            int dishId = (int)TempData["PendingDishId"];
                            int quantity = (int)TempData["PendingQuantity"];
                            
                            // 清除TempData
                            TempData.Remove("PendingAction");
                            TempData.Remove("PendingDishId");
                            TempData.Remove("PendingQuantity");
                            
                            // 重定向到购物车添加操作，使用GET方法
                            return RedirectToAction("Add", "Cart", new { dishId = dishId, quantity = quantity, returnUrl = returnUrl });
                        }
                        
                        // 如果有returnUrl，则重定向到该URL
                        if (!string.IsNullOrEmpty(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        
                        return RedirectToAction("Index", "Customer");
                    }
                }
                else if (model.UserType == UserTypes.Rider)
                {
                    var rider = db.Riders.FirstOrDefault(r => r.Username == model.Username && r.Password == model.Password);
                    if (rider != null)
                    {
                        // 登录成功，设置Session
                        Session["UserId"] = rider.RiderId;
                        Session["Username"] = rider.Username;
                        Session["UserType"] = UserTypes.Rider;
                        return RedirectToAction("Index", "Rider");
                    }
                }
                
                ModelState.AddModelError("", "用户名或密码错误");
            }
            return View(model);
        }

        // GET: Account/Register
        public ActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.UserType == UserTypes.Merchant)
                {
                    // 检查用户名是否已存在
                    if (db.Merchants.Any(r => r.Username == model.Username))
                    {
                        ModelState.AddModelError("Username", "该用户名已被使用");
                        return View(model);
                    }

                    // 创建新商家
                    var merchant = new Merchant
                    {
                        Username = model.Username,
                        Password = model.Password,
                        PhoneNumber = model.PhoneNumber,
                        Address = model.Address,
                        Name = model.Username, // 确保Name属性有值，默认使用用户名
                        CreatedAt = DateTime.Now
                    };

                    db.Merchants.Add(merchant);
                    db.SaveChanges();
                    
                    int restaurantId = 0;
                    bool restaurantIsActive = true; // 默认开启营业状态
                    
                    // 创建餐厅
                    if (!string.IsNullOrEmpty(model.RestaurantName))
                    {
                        var restaurant = new Restaurant
                        {
                            Username = model.Username,
                            Password = model.Password,
                            Name = model.RestaurantName,
                            PhoneNumber = model.PhoneNumber,
                            Address = model.Address,
                            MerchantId = merchant.MerchantId,
                            IsActive = restaurantIsActive,
                            CreateTime = DateTime.Now
                        };
                        
                        db.Restaurants.Add(restaurant);
                        db.SaveChanges();
                        
                        restaurantId = restaurant.RestaurantId;
                    }

                    // 自动登录
                    Session["UserId"] = merchant.MerchantId;
                    Session["Username"] = merchant.Username;
                    Session["UserType"] = UserTypes.Merchant;
                    
                    // 保存餐厅状态和ID到Session
                    Session["RestaurantStatus"] = restaurantIsActive;
                    Session["RestaurantId"] = restaurantId;
                    
                    System.Diagnostics.Debug.WriteLine($"商家注册: 设置餐厅状态 - 餐厅ID: {restaurantId}, 状态: {(restaurantIsActive ? "营业中" : "休息中")}");

                    return RedirectToAction("Dashboard", "Merchant");
                }
                else if (model.UserType == UserTypes.Customer)
                {
                    // 检查用户名是否已存在
                    if (db.Customers.Any(c => c.Username == model.Username))
                    {
                        ModelState.AddModelError("Username", "该用户名已被使用");
                        return View(model);
                    }

                    // 创建新顾客
                    var customer = new Customer
                    {
                        Username = model.Username,
                        Password = model.Password,
                        PhoneNumber = model.PhoneNumber,
                        Address = model.Address,
                        Name = model.Username, // 确保Name属性有值
                        CreatedAt = DateTime.Now
                    };

                    db.Customers.Add(customer);
                    db.SaveChanges();

                    // 自动登录
                    Session["UserId"] = customer.CustomerId;
                    Session["Username"] = customer.Username;
                    Session["UserType"] = UserTypes.Customer;

                    return RedirectToAction("Index", "Customer");
                }
                else if (model.UserType == UserTypes.Rider)
                {
                    // 检查用户名是否已存在
                    if (db.Riders.Any(r => r.Username == model.Username))
                    {
                        ModelState.AddModelError("Username", "该用户名已被使用");
                        return View(model);
                    }

                    // 创建新骑手
                    var rider = new Rider
                    {
                        Username = model.Username,
                        Password = model.Password,
                        PhoneNumber = model.PhoneNumber,
                        Address = model.Address,
                        Name = model.Username, // 默认使用用户名作为骑手名
                        IsAvailable = true,
                        IsOnline = true,
                        IsDelivering = false,
                        CreatedAt = DateTime.Now
                    };

                    db.Riders.Add(rider);
                    db.SaveChanges();

                    // 自动登录
                    Session["UserId"] = rider.RiderId;
                    Session["Username"] = rider.Username;
                    Session["UserType"] = UserTypes.Rider;

                    return RedirectToAction("Index", "Rider");
                }
            }

            // 如果模型验证失败，或者用户类型不是商家、顾客或骑手，返回注册视图
            return View(model);
        }

        // GET: Account/Logout
        public ActionResult Logout()
        {
            // 检查是否是商家用户，如果是，记录日志
            if (Session["UserType"] != null && Session["UserType"].ToString() == UserTypes.Merchant && Session["RestaurantStatus"] != null)
            {
                bool isActive = (bool)Session["RestaurantStatus"];
                System.Diagnostics.Debug.WriteLine($"商家注销: 餐厅状态保持为 {(isActive ? "营业中" : "休息中")}");
            }
            
            // 清除Session
            Session.Clear();
            Session.Abandon();
            
            return RedirectToAction("Index", "Home");
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