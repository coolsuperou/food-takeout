using System;
using System.Web;
using System.Web.Mvc;

namespace food_takeout.Controllers
{
    /// <summary>
    /// 限制只有管理员可以访问的特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AdminOnlyAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            // 检查用户是否是管理员
            return httpContext.Session["IsAdmin"] != null && (bool)httpContext.Session["IsAdmin"];
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            // 未授权时重定向到首页
            filterContext.Result = new RedirectResult("~/Home/Index");
        }
    }
} 