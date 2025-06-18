using System;
using System.Web;
using System.Web.Mvc;
using food_takeout.Models;

namespace food_takeout.Models
{
    /// <summary>
    /// 限制只有已登录的顾客可以访问的特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class CustomerOnlyAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            // 检查用户是否已登录且是顾客身份
            return httpContext.Session["UserId"] != null && 
                   httpContext.Session["UserType"] != null && 
                   httpContext.Session["UserType"].ToString() == UserTypes.Customer;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            // 未授权时重定向到登录页面，并传递当前URL作为returnUrl
            string returnUrl = filterContext.HttpContext.Request.RawUrl;
            filterContext.Result = new RedirectResult($"~/Account/Login?returnUrl={returnUrl}");
        }
    }
} 