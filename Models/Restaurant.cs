using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        /// 餐厅名称
        /// </summary>
        [Required]
        [MaxLength(100)]
        [Display(Name = "餐厅名称")]
        public string Name { get; set; }
        
        /// <summary>
        /// 餐厅位置
        /// </summary>
        [MaxLength(255)]
        [Display(Name = "餐厅位置")]
        public string Location { get; set; }
        
        /// <summary>
        /// 餐厅图片路径
        /// </summary>
        [MaxLength(255)]
        [Display(Name = "餐厅图片")]
        public string ImageUrl { get; set; }
    }
} 