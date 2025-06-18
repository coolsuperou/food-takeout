using System;
using System.Web;
using System.Web.Mvc;

namespace food_takeout.Models
{
    public class AdminOnlyAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var userType = httpContext.Session["UserType"] as string;
            return userType == UserTypes.Admin;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            filterContext.Result = new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden, "只有管理员可以访问此功能");
        }
    }
} 