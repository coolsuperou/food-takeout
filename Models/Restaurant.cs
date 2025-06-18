using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Linq;

namespace food_takeout.Models
{
    /// <summary>
    /// 餐厅实体类
    /// </summary>
    public class Restaurant
    {
        /// <summary>
        /// 餐厅ID
        /// </summary>
        [Key]
        [Display(Name = "餐厅编号")]
        public int RestaurantId { get; set; }
        
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
        [DataType(DataType.Password)]
        [Display(Name = "密码")]
        public string Password { get; set; }
        
        /// <summary>
        /// 餐厅名称
        /// </summary>
        [Required(ErrorMessage = "请输入店铺名称")]
        [StringLength(100, ErrorMessage = "店铺名称不能超过100个字符")]
        [Display(Name = "店铺名称")]
        public string Name { get; set; }
        
        /// <summary>
        /// 手机号码
        /// </summary>
        [StringLength(20, ErrorMessage = "联系电话不能超过20个字符")]
        [Display(Name = "联系电话")]
        [Phone(ErrorMessage = "请输入有效的电话号码")]
        public string PhoneNumber { get; set; }
        
        /// <summary>
        /// 地址
        /// </summary>
        [Required(ErrorMessage = "请输入店铺地址")]
        [StringLength(200, ErrorMessage = "地址不能超过200个字符")]
        [Display(Name = "店铺地址")]
        public string Address { get; set; }

        /// <summary>
        /// 餐厅位置/区域
        /// </summary>
        [StringLength(100)]
        [Display(Name = "位置/区域")]
        public string Location { get; set; }

        /// <summary>
        /// 餐厅简介（非必填）
        /// </summary>
        [StringLength(1000, ErrorMessage = "店铺描述不能超过1000个字符")]
        [Display(Name = "店铺描述")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        /// <summary>
        /// 营业时间
        /// </summary>
        [Display(Name = "营业时间")]
        [StringLength(100, ErrorMessage = "营业时间不能超过100个字符")]
        public string BusinessHours { get; set; }

        /// <summary>
        /// 餐厅图片
        /// </summary>
        [StringLength(200)]
        [Display(Name = "餐厅图片")]
        public string ImageUrl { get; set; }

        /// <summary>
        /// 餐厅分类
        /// </summary>
        [StringLength(100)]
        [Display(Name = "餐厅分类")]
        public string Category { get; set; }

        /// <summary>
        /// 相关餐厅分类列表，逗号分隔
        /// </summary>
        [StringLength(200)]
        [Display(Name = "分类列表")]
        public string Categories { get; set; }

        /// <summary>
        /// 关联的商家ID
        /// </summary>
        [Display(Name = "商家ID")]
        public int? MerchantId { get; set; }
        
        /// <summary>
        /// 关联的商家
        /// </summary>
        [ForeignKey("MerchantId")]
        public virtual Merchant Merchant { get; set; }

        /// <summary>
        /// 是否营业
        /// </summary>
        [Display(Name = "是否营业")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 是否为热门餐厅
        /// </summary>
        [Display(Name = "是否热门")]
        public bool IsHot { get; set; }

        /// <summary>
        /// 预计配送时间(分钟)
        /// </summary>
        [Display(Name = "配送时间")]
        public int DeliveryTime { get; set; } = 30;

        /// <summary>
        /// 配送费
        /// </summary>
        [Display(Name = "配送费")]
        [Range(0, 100, ErrorMessage = "配送费必须在0-100元之间")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal DeliveryFee { get; set; }

        /// <summary>
        /// 最低订单金额
        /// </summary>
        [Display(Name = "最低订单金额")]
        [Range(0, 1000, ErrorMessage = "最低订单金额必须在0-1000元之间")]
        [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = false)]
        public decimal MinimumOrderAmount { get; set; }

        /// <summary>
        /// 预计送达时间
        /// </summary>
        [Display(Name = "预计送达时间")]
        [Range(0, 180, ErrorMessage = "预计送达时间必须在0-180分钟之间")]
        public int EstimatedDeliveryTime { get; set; }

        /// <summary>
        /// 菜系
        /// </summary>
        [Display(Name = "菜系")]
        [StringLength(50, ErrorMessage = "菜系不能超过50个字符")]
        public string Cuisine { get; set; }

        /// <summary>
        /// 店铺Logo
        /// </summary>
        [Display(Name = "店铺Logo")]
        public string LogoUrl { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required]
        [Display(Name = "创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        [Display(Name = "更新时间")]
        public DateTime? UpdateTime { get; set; }

        /// <summary>
        /// 平均评分
        /// </summary>
        [Display(Name = "平均评分")]
        [Range(0, 5, ErrorMessage = "平均评分必须在0-5之间")]
        public decimal AverageRating { get; set; }

        /// <summary>
        /// 评价数量
        /// </summary>
        [Display(Name = "评价数量")]
        public int ReviewCount { get; set; }

        // 导航属性
        public virtual ICollection<Dish> Dishes { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }

        // 计算属性
        [NotMapped]
        public double Rating
        {
            get
            {
                if (_rating > 0)
                {
                    return _rating;
                }
                
                if (Reviews != null && Reviews.Any())
                {
                    return Math.Round(Reviews.Average(r => r.Rating), 1);
                }
                return 4.5; // 默认评分
            }
            set
            {
                _rating = value;
            }
        }
        
        private double _rating;

        [NotMapped]
        public bool IsNew
        {
            get
            {
                return (DateTime.Now - CreateTime).TotalDays < 30;
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