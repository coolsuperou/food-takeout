using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Linq;

namespace food_takeout.Models
{
    /// <summary>
    /// 菜品实体类
    /// </summary>
    public class Dish
    {
        /// <summary>
        /// 菜品ID
        /// </summary>
        [Key]
        [Display(Name = "菜品编号")]
        public int Id { get; set; }
        
        /// <summary>
        /// 兼容原有代码使用DishId
        /// </summary>
        [NotMapped]
        public int DishId 
        { 
            get { return Id; }
            set { Id = value; }
        }
        
        /// <summary>
        /// 所属餐厅ID
        /// </summary>
        [Display(Name = "所属餐厅")]
        public int RestaurantId { get; set; }
        
        /// <summary>
        /// 所属餐厅
        /// </summary>
        [ForeignKey("RestaurantId")]
        public virtual Restaurant Restaurant { get; set; }
        
        /// <summary>
        /// 菜品名称
        /// </summary>
        [Required(ErrorMessage = "请输入菜品名称")]
        [StringLength(50, ErrorMessage = "菜品名称不能超过50个字符")]
        [Display(Name = "菜品名称")]
        public string Name { get; set; }
        
        /// <summary>
        /// 价格
        /// </summary>
        [Required(ErrorMessage = "请输入菜品价格")]
        [Range(0.01, 9999.99, ErrorMessage = "价格必须在0.01到9999.99之间")]
        [DisplayFormat(DataFormatString = "{0:F2}", ApplyFormatInEditMode = true)]
        [Display(Name = "价格")]
        public decimal Price { get; set; }
        
        /// <summary>
        /// 菜品图片URL
        /// </summary>
        [MaxLength(255)]
        [Display(Name = "菜品图片")]
        public string ImageUrl { get; set; }
        
        /// <summary>
        /// 菜品分类
        /// </summary>
        [Required(ErrorMessage = "请选择菜品分类")]
        [StringLength(50)]
        [Display(Name = "菜品分类")]
        public string Category { get; set; }
        
        /// <summary>
        /// 菜品描述
        /// </summary>
        [StringLength(200, ErrorMessage = "描述不能超过200个字符")]
        [Display(Name = "菜品描述")]
        public string Description { get; set; }
        
        /// <summary>
        /// 是否热销
        /// </summary>
        [Display(Name = "是否热销")]
        public bool IsHot { get; set; }
        
        /// <summary>
        /// 今日售出数量
        /// </summary>
        [Display(Name = "今日售出")]
        public int SoldCount { get; set; }
        
        /// <summary>
        /// 菜品状态
        /// </summary>
        [Required]
        [Display(Name = "状态")]
        public string Status { get; set; } = "Active";
        
        /// <summary>
        /// 兼容原有代码使用IsAvailable
        /// </summary>
        [NotMapped]
        public bool IsAvailable 
        { 
            get { return Status == "Active"; }
            set { Status = value ? "Active" : "Inactive"; }
        }
        
        /// <summary>
        /// 创建时间
        /// </summary>
        [Display(Name = "创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.Now;
        
        /// <summary>
        /// 更新时间
        /// </summary>
        [Display(Name = "更新时间")]
        public DateTime? UpdateTime { get; set; }
        
        /// <summary>
        /// 订单详情
        /// </summary>
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
        
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
                // 基于所属餐厅的评分，具体菜品评分可以用订单详情关联计算
                return Restaurant?.Rating ?? 4.5;
            }
            set
            {
                _rating = value;
            }
        }
        
        private double _rating;
        
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