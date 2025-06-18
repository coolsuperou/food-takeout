using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace food_takeout
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "CartAdd",
                url: "Cart/Add/{dishId}/{quantity}",
                defaults: new { controller = "Cart", action = "Add", quantity = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Merchant",
                url: "Merchant/{action}/{id}",
                defaults: new { controller = "Merchant", action = "Index", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
