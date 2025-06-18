namespace food_takeout.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using System.Collections.Generic;
    using food_takeout.Models;

    internal sealed class Configuration : DbMigrationsConfiguration<food_takeout.Models.FoodContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            AutomaticMigrationDataLossAllowed = true;
        }

        protected override void Seed(food_takeout.Models.FoodContext context)
        {
            //  This method will be called after migrating to the latest version.
            try
            {
                // 添加测试用户数据
                SeedUsers(context);
                
                // 添加测试餐厅数据
                SeedRestaurants(context);
                
                // 添加测试菜品数据
                SeedDishes(context);
                
                // 添加测试订单数据
                SeedOrders(context);
                
                // 保存更改
                context.SaveChanges();
                
                System.Diagnostics.Debug.WriteLine("成功初始化测试数据");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化测试数据失败: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"异常堆栈: {ex.StackTrace}");
            }
        }
        
        private void SeedUsers(food_takeout.Models.FoodContext context)
        {
            // 添加测试顾客
            if (!context.Customers.Any())
            {
                context.Customers.AddOrUpdate(
                    c => c.PhoneNumber,
                    new Customer { Name = "测试顾客1", PhoneNumber = "13800138001", Address = "测试地址1", Password = "123456" },
                    new Customer { Name = "测试顾客2", PhoneNumber = "13800138002", Address = "测试地址2", Password = "123456" }
                );
                
                context.SaveChanges();
            }
            
            // 添加测试商家
            if (!context.Merchants.Any())
            {
                context.Merchants.AddOrUpdate(
                    m => m.PhoneNumber,
                    new Merchant { Name = "测试商家1", PhoneNumber = "13900139001", Password = "123456" },
                    new Merchant { Name = "测试商家2", PhoneNumber = "13900139002", Password = "123456" }
                );
                
                context.SaveChanges();
            }
            
            // 添加测试骑手
            if (!context.Riders.Any())
            {
                context.Riders.AddOrUpdate(
                    r => r.PhoneNumber,
                    new Rider { Name = "测试骑手1", PhoneNumber = "13700137001", Password = "123456", Status = RiderStatus.Available },
                    new Rider { Name = "测试骑手2", PhoneNumber = "13700137002", Password = "123456", Status = RiderStatus.Available }
                );
                
                context.SaveChanges();
            }
        }
        
        private void SeedRestaurants(food_takeout.Models.FoodContext context)
        {
            // 添加测试餐厅
            if (!context.Restaurants.Any())
            {
                var merchants = context.Merchants.ToList();
                if (merchants.Count > 0)
                {
                    context.Restaurants.AddOrUpdate(
                        r => r.Name,
                        new Restaurant
                        {
                            Name = "测试餐厅1",
                            Description = "这是一家测试餐厅1",
                            Address = "测试地址1",
                            PhoneNumber = "13800138001",
                            BusinessHours = "09:00-21:00",
                            IsActive = true,
                            IsHot = true,
                            Rating = 4.5,
                            DeliveryTime = 30,
                            Categories = "中餐,快餐",
                            MerchantId = merchants[0].MerchantId,
                            ImageUrl = "/Content/Images/Restaurants/default.jpg"
                        },
                        new Restaurant
                        {
                            Name = "测试餐厅2",
                            Description = "这是一家测试餐厅2",
                            Address = "测试地址2",
                            PhoneNumber = "13800138002",
                            BusinessHours = "10:00-22:00",
                            IsActive = true,
                            IsHot = false,
                            Rating = 4.0,
                            DeliveryTime = 40,
                            Categories = "西餐,快餐",
                            MerchantId = merchants.Count > 1 ? merchants[1].MerchantId : merchants[0].MerchantId,
                            ImageUrl = "/Content/Images/Restaurants/default.jpg"
                        }
                    );
                    
                    context.SaveChanges();
                }
            }
        }
        
        private void SeedDishes(food_takeout.Models.FoodContext context)
        {
            // 添加测试菜品
            if (!context.Dishes.Any())
            {
                var restaurants = context.Restaurants.ToList();
                if (restaurants.Count > 0)
                {
                    context.Dishes.AddOrUpdate(
                        d => d.Name,
                        new Dish
                        {
                            Name = "测试菜品1",
                            Description = "这是一道测试菜品1",
                            Price = 28.5M,
                            IsAvailable = true,
                            IsHot = true,
                            Rating = 4.8,
                            RestaurantId = restaurants[0].RestaurantId,
                            ImageUrl = "/Content/Images/Dishes/default.jpg"
                        },
                        new Dish
                        {
                            Name = "测试菜品2",
                            Description = "这是一道测试菜品2",
                            Price = 32.0M,
                            IsAvailable = true,
                            IsHot = true,
                            Rating = 4.5,
                            RestaurantId = restaurants[0].RestaurantId,
                            ImageUrl = "/Content/Images/Dishes/default.jpg"
                        },
                        new Dish
                        {
                            Name = "测试菜品3",
                            Description = "这是一道测试菜品3",
                            Price = 25.0M,
                            IsAvailable = true,
                            IsHot = false,
                            Rating = 4.0,
                            RestaurantId = restaurants.Count > 1 ? restaurants[1].RestaurantId : restaurants[0].RestaurantId,
                            ImageUrl = "/Content/Images/Dishes/default.jpg"
                        }
                    );
                    
                    context.SaveChanges();
                }
            }
        }
        
        private void SeedOrders(food_takeout.Models.FoodContext context)
        {
            // 添加测试订单
            if (!context.Orders.Any())
            {
                var customers = context.Customers.ToList();
                var restaurants = context.Restaurants.ToList();
                var dishes = context.Dishes.ToList();
                var riders = context.Riders.ToList();
                
                if (customers.Count > 0 && restaurants.Count > 0 && dishes.Count > 0)
                {
                    // 创建一个订单
                    var order = new Order
                    {
                        OrderNumber = "TEST" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                        CustomerId = customers[0].CustomerId,
                        RestaurantId = restaurants[0].RestaurantId,
                        Status = OrderStatus.Delivered,
                        TotalAmount = 0, // 稍后计算
                        CreatedAt = DateTime.Now.AddDays(-1),
                        DeliveryAddress = customers[0].Address,
                        RiderId = riders.Count > 0 ? riders[0].RiderId : (int?)null
                    };
                    
                    context.Orders.Add(order);
                    context.SaveChanges();
                    
                    // 添加订单明细
                    if (dishes.Count > 0)
                    {
                        decimal totalAmount = 0;
                        
                        foreach (var dish in dishes.Take(2))
                        {
                            var quantity = new Random().Next(1, 4); // 随机1-3个数量
                            var orderDetail = new OrderDetail
                            {
                                OrderId = order.OrderId,
                                DishId = dish.DishId,
                                Quantity = quantity,
                                Price = dish.Price,
                                Subtotal = dish.Price * quantity
                            };
                            
                            context.OrderDetails.Add(orderDetail);
                            totalAmount += orderDetail.Subtotal;
                        }
                        
                        // 更新订单总金额
                        order.TotalAmount = totalAmount;
                        context.Entry(order).State = EntityState.Modified;
                        
                        context.SaveChanges();
                    }
                    
                    // 创建第二个进行中的订单
                    var pendingOrder = new Order
                    {
                        OrderNumber = "TEST" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                        CustomerId = customers[0].CustomerId,
                        RestaurantId = restaurants[0].RestaurantId,
                        Status = OrderStatus.InDelivery,
                        TotalAmount = 0, // 稍后计算
                        CreatedAt = DateTime.Now.AddHours(-1),
                        DeliveryAddress = customers[0].Address,
                        RiderId = riders.Count > 0 ? riders[0].RiderId : (int?)null
                    };
                    
                    context.Orders.Add(pendingOrder);
                    context.SaveChanges();
                    
                    // 添加订单明细
                    if (dishes.Count > 0)
                    {
                        decimal totalAmount = 0;
                        var dish = dishes[0]; // 使用第一个菜品
                        var quantity = 2;
                        
                        var orderDetail = new OrderDetail
                        {
                            OrderId = pendingOrder.OrderId,
                            DishId = dish.DishId,
                            Quantity = quantity,
                            Price = dish.Price,
                            Subtotal = dish.Price * quantity
                        };
                        
                        context.OrderDetails.Add(orderDetail);
                        totalAmount += orderDetail.Subtotal;
                        
                        // 更新订单总金额
                        pendingOrder.TotalAmount = totalAmount;
                        context.Entry(pendingOrder).State = EntityState.Modified;
                        
                        context.SaveChanges();
                    }
                }
            }
        }
    }
}
