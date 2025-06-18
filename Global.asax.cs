using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Data.Entity;
using food_takeout.Models;
using System.IO;

namespace food_takeout
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Application_Start 开始执行");
                
                AreaRegistration.RegisterAllAreas();
                FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
                RouteConfig.RegisterRoutes(RouteTable.Routes);
                BundleConfig.RegisterBundles(BundleTable.Bundles);
                
                // 确保数据库初始化
                System.Diagnostics.Debug.WriteLine("开始初始化数据库...");
                Database.SetInitializer(new MigrateDatabaseToLatestVersion<FoodContext, Migrations.Configuration>());
                
                using (var context = new FoodContext())
                {
                    System.Diagnostics.Debug.WriteLine("开始数据库迁移...");
                    context.Database.Initialize(force: true);
                    System.Diagnostics.Debug.WriteLine("数据库初始化完成");
                }
                
                System.Diagnostics.Debug.WriteLine("Application_Start 执行完成");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Application_Start 发生异常: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            }
        }
        
        protected void Application_Error(object sender, EventArgs e)
        {
            Exception exception = Server.GetLastError();
            
            // 记录错误到文件
            string logPath = Server.MapPath("~/App_Data/error_log.txt");
            using (StreamWriter writer = new StreamWriter(logPath, true))
            {
                writer.WriteLine("错误时间: " + DateTime.Now.ToString());
                writer.WriteLine("错误URL: " + Request.Url.ToString());
                writer.WriteLine("错误信息: " + exception.Message);
                writer.WriteLine("堆栈跟踪: " + exception.StackTrace);
                writer.WriteLine("-----------------------------------");
            }
            
            // 清除错误
            Server.ClearError();
            
            // 重定向到错误页面
            Response.Redirect("~/Home/Error");
        }
    }
}
