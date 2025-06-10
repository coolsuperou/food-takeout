using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public int DishId { get; set; }
        
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
        [Required]
        [MaxLength(100)]
        [Display(Name = "菜品名称")]
        public string Name { get; set; }
        
        /// <summary>
        /// 菜品价格
        /// </summary>
        [Required]
        [Range(0, double.MaxValue)]
        [Display(Name = "菜品价格")]
        public decimal Price { get; set; }
        
        /// <summary>
        /// 菜品图片URL
        /// </summary>
        [MaxLength(255)]
        [Display(Name = "菜品图片")]
        public string ImageUrl { get; set; }
        
        /// <summary>
        /// 菜品是否可用
        /// </summary>
        [Display(Name = "是否可用")]
        public bool IsAvailable { get; set; } = true;
    }
} 