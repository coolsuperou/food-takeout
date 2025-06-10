using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;

namespace food_takeout.Models
{
    /// <summary>
    /// 订单状态枚举
    /// </summary>
    public enum OrderStatus
    {
        /// <summary>
        /// 待处理
        /// </summary>
        [Display(Name = "待处理")]
        Pending,
        /// <summary>
        /// 准备中
        /// </summary>
        [Display(Name = "准备中")]
        Preparing,
        /// <summary>
        /// 准备配送
        /// </summary>
        [Display(Name = "准备配送")]
        ReadyForDelivery,
        /// <summary>
        /// 配送中
        /// </summary>
        [Display(Name = "配送中")]
        InDelivery,
        /// <summary>
        /// 已送达
        /// </summary>
        [Display(Name = "已送达")]
        Delivered,
        /// <summary>
        /// 已取消
        /// </summary>
        [Display(Name = "已取消")]
        Cancelled
    }

    /// <summary>
    /// 订单实体类
    /// </summary>
    public class Order
    {
        /// <summary>
        /// 订单ID
        /// </summary>
        [Key]
        [Display(Name = "订单编号")]
        public int OrderId { get; set; }
        
        /// <summary>
        /// 顾客ID
        /// </summary>
        [Display(Name = "顾客")]
        public int CustomerId { get; set; }
        
        /// <summary>
        /// 顾客信息
        /// </summary>
        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; }
        
        /// <summary>
        /// 餐厅ID
        /// </summary>
        [Display(Name = "餐厅")]
        public int RestaurantId { get; set; }
        
        /// <summary>
        /// 餐厅信息
        /// </summary>
        [ForeignKey("RestaurantId")]
        public virtual Restaurant Restaurant { get; set; }
        
        /// <summary>
        /// 骑手ID
        /// </summary>
        [Display(Name = "配送骑手")]
        public int? RiderId { get; set; }
        
        /// <summary>
        /// 骑手信息
        /// </summary>
        [ForeignKey("RiderId")]
        public virtual Rider Rider { get; set; }
        
        /// <summary>
        /// 订单状态
        /// </summary>
        [Display(Name = "订单状态")]
        public OrderStatus Status { get; set; }
        
        /// <summary>
        /// 创建时间
        /// </summary>
        [Display(Name = "创建时间")]
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// 更新时间
        /// </summary>
        [Display(Name = "更新时间")]
        public DateTime? UpdatedAt { get; set; }
        
        /// <summary>
        /// 订单明细
        /// </summary>
        [Display(Name = "订单明细")]
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
    }
} 