using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;

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
        }

        /// <summary>
        /// 顾客表
        /// </summary>
        public DbSet<Customer> Customers { get; set; }
        
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
        /// 骑手表
        /// </summary>
        public DbSet<Rider> Riders { get; set; }
        
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
            
            // 设置Order和Restaurant之间的级联删除规则为NO ACTION
            modelBuilder.Entity<Order>()
                .HasRequired(o => o.Restaurant)
                .WithMany()
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
                .WithMany()
                .HasForeignKey(od => od.DishId)
                .WillCascadeOnDelete(false);
                
            // 修改：设置Dish和Restaurant之间的级联删除规则为NO ACTION
            modelBuilder.Entity<Dish>()
                .HasRequired(d => d.Restaurant)
                .WithMany()
                .HasForeignKey(d => d.RestaurantId)
                .WillCascadeOnDelete(false);
                
            // 设置Review和Customer之间的级联删除规则为NO ACTION
            modelBuilder.Entity<Review>()
                .HasRequired(r => r.Customer)
                .WithMany()
                .HasForeignKey(r => r.CustomerId)
                .WillCascadeOnDelete(false);
                
            // 设置Review和Restaurant之间的级联删除规则为NO ACTION
            modelBuilder.Entity<Review>()
                .HasRequired(r => r.Restaurant)
                .WithMany()
                .HasForeignKey(r => r.RestaurantId)
                .WillCascadeOnDelete(false);
                
            // 设置Review和Order之间的级联删除规则为NO ACTION
            modelBuilder.Entity<Review>()
                .HasRequired(r => r.Order)
                .WithMany()
                .HasForeignKey(r => r.OrderId)
                .WillCascadeOnDelete(false);
        }
    }
} 