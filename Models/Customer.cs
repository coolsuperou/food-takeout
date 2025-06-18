using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace food_takeout.Models
{
    /// <summary>
    /// 顾客实体类
    /// </summary>
    public class Customer
    {
        /// <summary>
        /// 顾客ID
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "顾客编号")]
        public int CustomerId { get; set; }
        
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
        /// 姓名
        /// </summary>
        [StringLength(50)]
        [Display(Name = "姓名")]
        public string Name { get; set; }
        
        /// <summary>
        /// 头像
        /// </summary>
        [StringLength(255)]
        [Display(Name = "头像")]
        public string Avatar { get; set; }
        
        /// <summary>
        /// 手机号码
        /// </summary>
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
        /// 当前使用的地址
        /// </summary>
        [StringLength(200)]
        [Display(Name = "当前地址")]
        public string CurrentAddress { get; set; }
        
        /// <summary>
        /// 邮箱
        /// </summary>
        [StringLength(100)]
        [EmailAddress]
        [Display(Name = "邮箱")]
        public string Email { get; set; }

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
        /// 评价
        /// </summary>
        public virtual ICollection<Review> Reviews { get; set; }
        
        /// <summary>
        /// 订单
        /// </summary>
        public virtual ICollection<Order> Orders { get; set; }
        
        // 计算属性
        [NotMapped]
        public int ActiveOrdersCount
        {
            get
            {
                if (Orders == null) return 0;
                return Orders.Count(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled);
            }
        }
        
        [NotMapped]
        public IEnumerable<Order> ActiveOrders
        {
            get
            {
                if (Orders == null) return new List<Order>();
                return Orders.Where(o => o.Status != OrderStatus.Delivered && o.Status != OrderStatus.Cancelled)
                    .OrderByDescending(o => o.CreatedAt);
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