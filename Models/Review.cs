using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace food_takeout.Models
{
    /// <summary>
    /// 评价实体类
    /// </summary>
    public class Review
    {
        /// <summary>
        /// 评价ID
        /// </summary>
        [Key]
        [Display(Name = "评价编号")]
        public int ReviewId { get; set; }
        
        /// <summary>
        /// 订单ID
        /// </summary>
        [Required]
        [Display(Name = "订单")]
        public int OrderId { get; set; }
        
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
        /// 评分
        /// </summary>
        [Required]
        [Range(1, 5, ErrorMessage = "评分必须在1-5之间")]
        [Display(Name = "评分")]
        public int Rating { get; set; }
        
        /// <summary>
        /// 评价内容
        /// </summary>
        [StringLength(500)]
        [Display(Name = "评价内容")]
        public string Comment { get; set; }
        
        /// <summary>
        /// 评价内容（与Comment相同，为保持兼容性）
        /// </summary>
        [StringLength(500)]
        [Display(Name = "评价内容")]
        public string Content 
        { 
            get { return Comment; } 
            set { Comment = value; } 
        }
        
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
        /// 订单信息
        /// </summary>
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }
        
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
        /// Count属性，用于防止ViewBag.Count错误
        /// </summary>
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