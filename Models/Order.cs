using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using System.Linq;

namespace food_takeout.Models
{
    /// <summary>
    /// 订单状态枚举
    /// </summary>
    public enum OrderStatus
    {
        /// <summary>
        /// 待接单
        /// </summary>
        [Display(Name = "待接单")]
        Pending,
        /// <summary>
        /// 已接单
        /// </summary>
        [Display(Name = "已接单")]
        Accepted,
        /// <summary>
        /// 制作中
        /// </summary>
        [Display(Name = "制作中")]
        Preparing,
        /// <summary>
        /// 待配送
        /// </summary>
        [Display(Name = "待配送")]
        ReadyForDelivery,
        /// <summary>
        /// 配送中
        /// </summary>
        [Display(Name = "配送中")]
        InDelivery,
        /// <summary>
        /// 已完成
        /// </summary>
        [Display(Name = "已完成")]
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
        /// 订单编号
        /// </summary>
        [Required]
        [StringLength(50)]
        [Display(Name = "订单编号")]
        public string OrderNumber { get; set; }
        
        /// <summary>
        /// 订单状态
        /// </summary>
        [Required]
        [Display(Name = "订单状态")]
        public OrderStatus Status { get; set; }
        
        /// <summary>
        /// 总金额
        /// </summary>
        [Required]
        [Range(0, 999999.99)]
        [DisplayFormat(DataFormatString = "{0:F2}", ApplyFormatInEditMode = true)]
        [Display(Name = "订单总额")]
        public decimal TotalAmount { get; set; }
        
        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(500)]
        [Display(Name = "备注")]
        public string Remark { get; set; }
        
        /// <summary>
        /// 配送地址
        /// </summary>
        [StringLength(200)]
        [Display(Name = "配送地址")]
        public string DeliveryAddress { get; set; }
        
        /// <summary>
        /// 配送距离(公里)
        /// </summary>
        [Range(0, 100)]
        [DisplayFormat(DataFormatString = "{0:F1}")]
        [Display(Name = "配送距离")]
        public double Distance { get; set; }
        
        /// <summary>
        /// 配送费
        /// </summary>
        [Range(0, 100)]
        [DisplayFormat(DataFormatString = "{0:F1}")]
        [Display(Name = "配送费")]
        public decimal DeliveryFee { get; set; }
        
        /// <summary>
        /// 预计送达时间
        /// </summary>
        [Display(Name = "预计送达时间")]
        public DateTime? EstimatedDeliveryTime { get; set; }
        
        /// <summary>
        /// 实际送达时间
        /// </summary>
        [Display(Name = "实际送达时间")]
        public DateTime? ActualDeliveryTime { get; set; }
        
        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [Display(Name = "创建时间")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        /// <summary>
        /// 更新时间
        /// </summary>
        [Display(Name = "更新时间")]
        public DateTime? UpdatedAt { get; set; }
        
        /// <summary>
        /// 接单时间
        /// </summary>
        [Display(Name = "接单时间")]
        public DateTime? AcceptedTime { get; set; }
        
        /// <summary>
        /// 取餐时间
        /// </summary>
        [Display(Name = "取餐时间")]
        public DateTime? PickupTime { get; set; }
        
        /// <summary>
        /// 开始配送时间
        /// </summary>
        [Display(Name = "开始配送时间")]
        public DateTime? DeliveryStartTime { get; set; }
        
        /// <summary>
        /// 取消原因
        /// </summary>
        [StringLength(500)]
        [Display(Name = "取消原因")]
        public string CancelReason { get; set; }
        
        /// <summary>
        /// 顾客ID
        /// </summary>
        [Required]
        [Display(Name = "顾客")]
        public int CustomerId { get; set; }
        
        /// <summary>
        /// 餐厅ID
        /// </summary>
        [Required]
        [Display(Name = "餐厅")]
        public int RestaurantId { get; set; }
        
        /// <summary>
        /// 骑手ID
        /// </summary>
        [Display(Name = "骑手")]
        public int? RiderId { get; set; }
        
        /// <summary>
        /// 顾客信息
        /// </summary>
        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; }
        
        /// <summary>
        /// 餐厅信息
        /// </summary>
        [ForeignKey("RestaurantId")]
        public virtual Restaurant Restaurant { get; set; }
        
        /// <summary>
        /// 骑手信息
        /// </summary>
        [ForeignKey("RiderId")]
        public virtual Rider Rider { get; set; }
        
        /// <summary>
        /// 订单明细
        /// </summary>
        [Display(Name = "订单明细")]
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
        
        /// <summary>
        /// 评价
        /// </summary>
        [Display(Name = "评价")]
        public virtual ICollection<Review> Reviews { get; set; }
        
        // 计算属性
        [NotMapped]
        public string RestaurantName
        {
            get
            {
                return Restaurant?.Name ?? string.Empty;
            }
        }
        
        [NotMapped]
        public string RestaurantImageUrl
        {
            get
            {
                return Restaurant?.ImageUrl;
            }
        }
        
        [NotMapped]
        public string StatusInfo
        {
            get
            {
                switch (Status)
                {
                    case OrderStatus.Pending:
                        return "等待商家接单";
                    case OrderStatus.Accepted:
                        return "商家已接单";
                    case OrderStatus.Preparing:
                        return "商家准备中";
                    case OrderStatus.ReadyForDelivery:
                        return "准备配送";
                    case OrderStatus.InDelivery:
                        return $"预计送达：{EstimatedDeliveryTime?.ToString("HH:mm") ?? DateTime.Now.AddMinutes(30).ToString("HH:mm")}";
                    case OrderStatus.Delivered:
                        return $"已送达：{ActualDeliveryTime?.ToString("HH:mm") ?? UpdatedAt?.ToString("HH:mm") ?? ""}";
                    case OrderStatus.Cancelled:
                        return "已取消";
                    default:
                        return "";
                }
            }
        }
        
        [NotMapped]
        public string Items
        {
            get
            {
                if (OrderDetails == null || !OrderDetails.Any()) return "";
                return string.Join(", ", OrderDetails.Select(od => $"{od.Dish?.Name ?? "未知菜品"} x {od.Quantity}"));
            }
        }
        
        [NotMapped]
        public bool IsAccepted
        {
            get
            {
                return Status != OrderStatus.Pending;
            }
        }
        
        [NotMapped]
        public int Count
        {
            get
            {
                return 1; // 防止ViewBag.Count错误
            }
        }
        
        [NotMapped]
        public double TimePassedInMinutes
        {
            get
            {
                return (DateTime.Now - CreatedAt).TotalMinutes;
            }
        }
        
        /// <summary>
        /// 商家收入计算属性
        /// </summary>
        [NotMapped]
        public decimal MerchantRevenue
        {
            get
            {
                return TotalAmount - DeliveryFee;
            }
        }
        
        /// <summary>
        /// 支付方式
        /// </summary>
        [StringLength(50)]
        [Display(Name = "支付方式")]
        public string PaymentMethod { get; set; }
    }
} 