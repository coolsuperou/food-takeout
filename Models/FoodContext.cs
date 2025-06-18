using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.ComponentModel.DataAnnotations.Schema;

namespace food_takeout.Models
{
    /// <summary>
    /// 数据库上下文
    /// </summary>
    public class FoodContext : DbContext
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public FoodContext() : base("FoodContext")
        {
            // 使用MigrateDatabaseToLatestVersion初始化器，确保数据库与模型同步
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<FoodContext, Migrations.Configuration>());
            
            // 启用SQL查询日志
            Database.Log = s => System.Diagnostics.Debug.WriteLine(s);
            
            // 启用延迟加载和代理创建
            this.Configuration.LazyLoadingEnabled = true;
            this.Configuration.ProxyCreationEnabled = true;
        }

        /// <summary>
        /// 顾客表
        /// </summary>
        public DbSet<Customer> Customers { get; set; }
        
        /// <summary>
        /// 商家表
        /// </summary>
        public DbSet<Merchant> Merchants { get; set; }
        
        /// <summary>
        /// 骑手表
        /// </summary>
        public DbSet<Rider> Riders { get; set; }
        
        /// <summary>
        /// 餐厅表
        /// </summary>
        public DbSet<Restaurant> Restaurants { get; set; }
        
        /// <summary>
        /// 菜品表
        /// </summary>
        public DbSet<Dish> Dishes { get; set; }
        
        /// <summary>
        /// 订单表
        /// </summary>
        public DbSet<Order> Orders { get; set; }
        
        /// <summary>
        /// 订单明细表
        /// </summary>
        public DbSet<OrderDetail> OrderDetails { get; set; }
        
        /// <summary>
        /// 评价表
        /// </summary>
        public DbSet<Review> Reviews { get; set; }

        /// <summary>
        /// 配置模型创建
        /// </summary>
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // 移除复数表名约定
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            
            // 配置decimal类型的精度和小数位
            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18, 2);
                
            modelBuilder.Entity<OrderDetail>()
                .Property(od => od.Price)
                .HasPrecision(18, 2);
                
            modelBuilder.Entity<OrderDetail>()
                .Property(od => od.Subtotal)
                .HasPrecision(18, 2);
                
            modelBuilder.Entity<Dish>()
                .Property(d => d.Price)
                .HasPrecision(18, 2);
                
            // 设置Order和Restaurant之间的级联删除规则为NO ACTION
            modelBuilder.Entity<Order>()
                .HasRequired(o => o.Restaurant)
                .WithMany(r => r.Orders)
                .HasForeignKey(o => o.RestaurantId)
                .WillCascadeOnDelete(false);
                
            // 设置OrderDetail和Order之间的级联删除规则
            modelBuilder.Entity<OrderDetail>()
                .HasRequired(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .WillCascadeOnDelete(true);
                
            // 设置OrderDetail和Dish之间的级联删除规则为NO ACTION
            modelBuilder.Entity<OrderDetail>()
                .HasRequired(od => od.Dish)
                .WithMany(d => d.OrderDetails)
                .HasForeignKey(od => od.DishId)
                .WillCascadeOnDelete(false);
                
            // 修改：设置Dish和Restaurant之间的级联删除规则为NO ACTION
            modelBuilder.Entity<Dish>()
                .HasRequired(d => d.Restaurant)
                .WithMany(r => r.Dishes)
                .HasForeignKey(d => d.RestaurantId)
                .WillCascadeOnDelete(false);
                
            // 设置Review和Customer之间的级联删除规则为NO ACTION
            modelBuilder.Entity<Review>()
                .HasRequired(r => r.Customer)
                .WithMany(c => c.Reviews)
                .HasForeignKey(r => r.CustomerId)
                .WillCascadeOnDelete(false);
                
            // 设置Review和Restaurant之间的级联删除规则为NO ACTION
            modelBuilder.Entity<Review>()
                .HasRequired(r => r.Restaurant)
                .WithMany(r => r.Reviews)
                .HasForeignKey(r => r.RestaurantId)
                .WillCascadeOnDelete(false);
                
            // 修改：设置Review和Order之间的关系
            modelBuilder.Entity<Review>()
                .HasRequired(r => r.Order)
                .WithMany()
                .HasForeignKey(r => r.OrderId)
                .WillCascadeOnDelete(false);
                
            // 设置Order和Customer之间的级联删除规则为NO ACTION
            modelBuilder.Entity<Order>()
                .HasRequired(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId)
                .WillCascadeOnDelete(false);
                
            // 设置Order和Rider之间的级联删除规则为NO ACTION
            modelBuilder.Entity<Order>()
                .HasOptional(o => o.Rider)
                .WithMany(r => r.Orders)
                .HasForeignKey(o => o.RiderId)
                .WillCascadeOnDelete(false);
        }
    }
} 