using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace food_takeout.Models
{
    /// <summary>
    /// 骑手状态枚举
    /// </summary>
    public enum RiderStatus
    {
        /// <summary>
        /// 空闲中
        /// </summary>
        [Display(Name = "空闲中")]
        Available,
        
        /// <summary>
        /// 配送中
        /// </summary>
        [Display(Name = "配送中")]
        Delivering,
        
        /// <summary>
        /// 休息中
        /// </summary>
        [Display(Name = "休息中")]
        Resting,
        
        /// <summary>
        /// 离线
        /// </summary>
        [Display(Name = "离线")]
        Offline
    }

    /// <summary>
    /// 骑手实体类
    /// </summary>
    public class Rider
    {
        /// <summary>
        /// 骑手ID
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "骑手编号")]
        public int RiderId { get; set; }
        
        /// <summary>
        /// 用户名
        /// </summary>
        [Required]
        [StringLength(50)]
        [Display(Name = "用户名")]
        public string Username { get; set; }
        
        /// <summary>
        /// 密码
        /// </summary>
        [Required]
        [StringLength(100)]
        [Display(Name = "密码")]
        public string Password { get; set; }
        
        /// <summary>
        /// 骑手姓名
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Display(Name = "姓名")]
        public string Name { get; set; }
        
        /// <summary>
        /// 手机号码
        /// </summary>
        [Required]
        [StringLength(20)]
        [Display(Name = "手机号码")]
        public string PhoneNumber { get; set; }
        
        /// <summary>
        /// 地址
        /// </summary>
        [StringLength(200)]
        [Display(Name = "地址")]
        public string Address { get; set; }
        
        /// <summary>
        /// 骑手状态
        /// </summary>
        [Display(Name = "骑手状态")]
        public RiderStatus Status { get; set; } = RiderStatus.Available;
        
        /// <summary>
        /// 是否可用
        /// </summary>
        [Display(Name = "是否可用")]
        public bool IsAvailable { get; set; }
        
        /// <summary>
        /// 是否在线
        /// </summary>
        [Display(Name = "是否在线")]
        public bool IsOnline { get; set; }
        
        /// <summary>
        /// 是否接单中
        /// </summary>
        [Display(Name = "是否接单中")]
        public bool IsDelivering { get; set; }
        
        /// <summary>
        /// 骑手头像
        /// </summary>
        [StringLength(255)]
        [Display(Name = "头像")]
        public string Avatar { get; set; }
        
        /// <summary>
        /// 骑手评分
        /// </summary>
        [Range(0, 5)]
        [DisplayFormat(DataFormatString = "{0:F1}")]
        [Display(Name = "评分")]
        public double Rating { get; set; } = 4.5;
        
        /// <summary>
        /// 今日收入
        /// </summary>
        [Range(0, 9999.99)]
        [DisplayFormat(DataFormatString = "{0:F2}")]
        [Display(Name = "今日收入")]
        public decimal TodayEarning { get; set; }
        
        /// <summary>
        /// 总收入
        /// </summary>
        [Range(0, 999999.99)]
        [DisplayFormat(DataFormatString = "{0:F2}")]
        [Display(Name = "总收入")]
        public decimal TotalEarning { get; set; }
        
        /// <summary>
        /// 已完成订单总数
        /// </summary>
        [Display(Name = "总完成订单")]
        public int TotalCompletedOrders { get; set; }
        
        /// <summary>
        /// 位置经度
        /// </summary>
        [Display(Name = "经度")]
        public double? Longitude { get; set; }
        
        /// <summary>
        /// 位置纬度
        /// </summary>
        [Display(Name = "纬度")]
        public double? Latitude { get; set; }
        
        /// <summary>
        /// 位置是否启用
        /// </summary>
        [Display(Name = "位置是否启用")]
        public bool LocationEnabled { get; set; } = true;

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
        /// 关联订单
        /// </summary>
        public virtual ICollection<Order> Orders { get; set; }
        
        // 计算属性
        [NotMapped]
        public int CompletedOrdersToday
        {
            get
            {
                if (Orders == null) return 0;
                
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                
                // 使用日期范围比较而不是.Date属性
                return Orders.Count(o => o.Status == OrderStatus.Delivered && 
                    o.CreatedAt >= today && 
                    o.CreatedAt < tomorrow);
            }
        }
        
        [NotMapped]
        public int AverageDeliveryTime
        {
            get
            {
                return 25; // 默认平均配送时间，实际应当计算
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
    }
} 