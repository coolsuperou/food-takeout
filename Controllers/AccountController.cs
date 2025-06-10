using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using food_takeout.Models;
using food_takeout.ViewModels;

namespace food_takeout.Controllers
{
    public class AccountController : Controller
    {
        private FoodContext db = new FoodContext();

        // GET: Account/Login
        public ActionResult Login()
        {
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 验证用户名和密码
                var customer = db.Customers.FirstOrDefault(c => c.Username == model.Username);
                if (customer != null && customer.Password == model.Password) // 实际应用中应使用密码哈希比对
                {
                    // 登录成功，设置认证Cookie
                    FormsAuthentication.SetAuthCookie(customer.CustomerId.ToString(), model.RememberMe);
                    Session["CustomerId"] = customer.CustomerId;
                    Session["CustomerUsername"] = customer.Username;
                    
                    // 设置管理员标识（这里假设用户名为"admin"的用户是管理员）
                    if (customer.Username.ToLower() == "admin")
                    {
                        Session["IsAdmin"] = true;
                    }
                    else
                    {
                        Session["IsAdmin"] = false;
                    }
                    
                    return RedirectToAction("Index", "Home");
                }
                
                ModelState.AddModelError("", "用户名或密码不正确");
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
                // 检查用户名是否已存在
                if (db.Customers.Any(c => c.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "该用户名已被注册");
                    return View(model);
                }

                // 创建新用户
                var customer = new Customer
                {
                    Username = model.Username,
                    Password = model.Password, // 实际应用中应该使用密码哈希存储
                    PhoneNumber = model.PhoneNumber,
                    Address = model.Address
                };

                db.Customers.Add(customer);
                db.SaveChanges();

                // 自动登录
                FormsAuthentication.SetAuthCookie(customer.CustomerId.ToString(), false);
                Session["CustomerId"] = customer.CustomerId;
                Session["CustomerUsername"] = customer.Username;

                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        // GET: Account/Logout
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
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