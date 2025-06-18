namespace food_takeout.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Customer",
                c => new
                    {
                        CustomerId = c.Int(nullable: false, identity: true),
                        Username = c.String(nullable: false, maxLength: 50),
                        Password = c.String(nullable: false, maxLength: 100),
                        Name = c.String(maxLength: 50),
                        Avatar = c.String(maxLength: 255),
                        PhoneNumber = c.String(maxLength: 20),
                        Address = c.String(maxLength: 200),
                        CurrentAddress = c.String(maxLength: 200),
                        Email = c.String(maxLength: 100),
                        CreatedAt = c.DateTime(nullable: false),
                        UpdatedAt = c.DateTime(),
                    })
                .PrimaryKey(t => t.CustomerId);
            
            CreateTable(
                "dbo.Order",
                c => new
                    {
                        OrderId = c.Int(nullable: false, identity: true),
                        OrderNumber = c.String(nullable: false, maxLength: 50),
                        Status = c.Int(nullable: false),
                        TotalAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Remark = c.String(maxLength: 500),
                        DeliveryAddress = c.String(maxLength: 200),
                        Distance = c.Double(nullable: false),
                        DeliveryFee = c.Decimal(nullable: false, precision: 18, scale: 2),
                        EstimatedDeliveryTime = c.DateTime(),
                        ActualDeliveryTime = c.DateTime(),
                        CreatedAt = c.DateTime(nullable: false),
                        UpdatedAt = c.DateTime(),
                        AcceptedTime = c.DateTime(),
                        PickupTime = c.DateTime(),
                        DeliveryStartTime = c.DateTime(),
                        CancelReason = c.String(maxLength: 500),
                        CustomerId = c.Int(nullable: false),
                        RestaurantId = c.Int(nullable: false),
                        RiderId = c.Int(),
                    })
                .PrimaryKey(t => t.OrderId)
                .ForeignKey("dbo.Customer", t => t.CustomerId)
                .ForeignKey("dbo.Restaurant", t => t.RestaurantId)
                .ForeignKey("dbo.Rider", t => t.RiderId)
                .Index(t => t.CustomerId)
                .Index(t => t.RestaurantId)
                .Index(t => t.RiderId);
            
            CreateTable(
                "dbo.OrderDetail",
                c => new
                    {
                        OrderDetailId = c.Int(nullable: false, identity: true),
                        OrderId = c.Int(nullable: false),
                        DishId = c.Int(nullable: false),
                        Quantity = c.Int(nullable: false),
                        Price = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Subtotal = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.OrderDetailId)
                .ForeignKey("dbo.Dish", t => t.DishId)
                .ForeignKey("dbo.Order", t => t.OrderId, cascadeDelete: true)
                .Index(t => t.OrderId)
                .Index(t => t.DishId);
            
            CreateTable(
                "dbo.Dish",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        RestaurantId = c.Int(nullable: false),
                        Name = c.String(nullable: false, maxLength: 50),
                        Price = c.Decimal(nullable: false, precision: 18, scale: 2),
                        ImageUrl = c.String(maxLength: 255),
                        Category = c.String(nullable: false, maxLength: 50),
                        Description = c.String(maxLength: 200),
                        IsHot = c.Boolean(nullable: false),
                        SoldCount = c.Int(nullable: false),
                        Status = c.String(nullable: false),
                        CreateTime = c.DateTime(nullable: false),
                        UpdateTime = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Restaurant", t => t.RestaurantId)
                .Index(t => t.RestaurantId);
            
            CreateTable(
                "dbo.Restaurant",
                c => new
                    {
                        RestaurantId = c.Int(nullable: false, identity: true),
                        Username = c.String(nullable: false, maxLength: 50),
                        Password = c.String(nullable: false, maxLength: 100),
                        Name = c.String(nullable: false, maxLength: 100),
                        PhoneNumber = c.String(maxLength: 20),
                        Address = c.String(nullable: false, maxLength: 200),
                        Location = c.String(maxLength: 100),
                        Description = c.String(maxLength: 500),
                        BusinessHours = c.String(maxLength: 200),
                        ImageUrl = c.String(maxLength: 200),
                        Category = c.String(maxLength: 100),
                        Categories = c.String(maxLength: 200),
                        MerchantId = c.Int(),
                        IsActive = c.Boolean(nullable: false),
                        IsHot = c.Boolean(nullable: false),
                        DeliveryTime = c.Int(nullable: false),
                        CreateTime = c.DateTime(nullable: false),
                        UpdateTime = c.DateTime(),
                    })
                .PrimaryKey(t => t.RestaurantId);
            
            CreateTable(
                "dbo.Review",
                c => new
                    {
                        ReviewId = c.Int(nullable: false, identity: true),
                        OrderId = c.Int(nullable: false),
                        CustomerId = c.Int(nullable: false),
                        RestaurantId = c.Int(nullable: false),
                        Rating = c.Int(nullable: false),
                        Comment = c.String(maxLength: 500),
                        Content = c.String(maxLength: 500),
                        CreatedAt = c.DateTime(nullable: false),
                        UpdatedAt = c.DateTime(),
                        Order_OrderId = c.Int(),
                    })
                .PrimaryKey(t => t.ReviewId)
                .ForeignKey("dbo.Customer", t => t.CustomerId)
                .ForeignKey("dbo.Order", t => t.OrderId)
                .ForeignKey("dbo.Restaurant", t => t.RestaurantId)
                .ForeignKey("dbo.Order", t => t.Order_OrderId)
                .Index(t => t.OrderId)
                .Index(t => t.CustomerId)
                .Index(t => t.RestaurantId)
                .Index(t => t.Order_OrderId);
            
            CreateTable(
                "dbo.Rider",
                c => new
                    {
                        RiderId = c.Int(nullable: false, identity: true),
                        Username = c.String(nullable: false, maxLength: 50),
                        Password = c.String(nullable: false, maxLength: 100),
                        Name = c.String(nullable: false, maxLength: 50),
                        PhoneNumber = c.String(nullable: false, maxLength: 20),
                        Address = c.String(maxLength: 200),
                        IsAvailable = c.Boolean(nullable: false),
                        IsOnline = c.Boolean(nullable: false),
                        IsDelivering = c.Boolean(nullable: false),
                        Avatar = c.String(maxLength: 255),
                        Rating = c.Double(nullable: false),
                        TodayEarning = c.Decimal(nullable: false, precision: 18, scale: 2),
                        TotalEarning = c.Decimal(nullable: false, precision: 18, scale: 2),
                        Longitude = c.Double(),
                        Latitude = c.Double(),
                        LocationEnabled = c.Boolean(nullable: false),
                        CreatedAt = c.DateTime(nullable: false),
                        UpdatedAt = c.DateTime(),
                    })
                .PrimaryKey(t => t.RiderId);
            
            CreateTable(
                "dbo.Merchant",
                c => new
                    {
                        MerchantId = c.Int(nullable: false, identity: true),
                        Username = c.String(nullable: false, maxLength: 50),
                        Password = c.String(nullable: false, maxLength: 100),
                        PhoneNumber = c.String(maxLength: 20),
                        Address = c.String(maxLength: 200),
                        CreatedAt = c.DateTime(nullable: false),
                        UpdatedAt = c.DateTime(),
                    })
                .PrimaryKey(t => t.MerchantId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Order", "RiderId", "dbo.Rider");
            DropForeignKey("dbo.Review", "Order_OrderId", "dbo.Order");
            DropForeignKey("dbo.Order", "RestaurantId", "dbo.Restaurant");
            DropForeignKey("dbo.OrderDetail", "OrderId", "dbo.Order");
            DropForeignKey("dbo.OrderDetail", "DishId", "dbo.Dish");
            DropForeignKey("dbo.Dish", "RestaurantId", "dbo.Restaurant");
            DropForeignKey("dbo.Review", "RestaurantId", "dbo.Restaurant");
            DropForeignKey("dbo.Review", "OrderId", "dbo.Order");
            DropForeignKey("dbo.Review", "CustomerId", "dbo.Customer");
            DropForeignKey("dbo.Order", "CustomerId", "dbo.Customer");
            DropIndex("dbo.Review", new[] { "Order_OrderId" });
            DropIndex("dbo.Review", new[] { "RestaurantId" });
            DropIndex("dbo.Review", new[] { "CustomerId" });
            DropIndex("dbo.Review", new[] { "OrderId" });
            DropIndex("dbo.Dish", new[] { "RestaurantId" });
            DropIndex("dbo.OrderDetail", new[] { "DishId" });
            DropIndex("dbo.OrderDetail", new[] { "OrderId" });
            DropIndex("dbo.Order", new[] { "RiderId" });
            DropIndex("dbo.Order", new[] { "RestaurantId" });
            DropIndex("dbo.Order", new[] { "CustomerId" });
            DropTable("dbo.Merchant");
            DropTable("dbo.Rider");
            DropTable("dbo.Review");
            DropTable("dbo.Restaurant");
            DropTable("dbo.Dish");
            DropTable("dbo.OrderDetail");
            DropTable("dbo.Order");
            DropTable("dbo.Customer");
        }
    }
}
