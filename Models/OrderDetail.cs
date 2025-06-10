using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace food_takeout.Models
{
    /// <summary>
    /// 订单明细实体类
    /// </summary>
    public class OrderDetail
    {
        /// <summary>
        /// 订单明细ID
        /// </summary>
        [Key]
        [Display(Name = "明细编号")]
        public int OrderDetailId { get; set; }
        
        /// <summary>
        /// 订单ID
        /// </summary>
        [Display(Name = "所属订单")]
        public int OrderId { get; set; }
        
        /// <summary>
        /// 订单信息
        /// </summary>
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }
        
        /// <summary>
        /// 菜品ID
        /// </summary>
        [Display(Name = "菜品")]
        public int DishId { get; set; }
        
        /// <summary>
        /// 菜品信息
        /// </summary>
        [ForeignKey("DishId")]
        public virtual Dish Dish { get; set; }
        
        /// <summary>
        /// 数量
        /// </summary>
        [Required]
        [Display(Name = "数量")]
        public int Quantity { get; set; }
        
        /// <summary>
        /// 单价
        /// </summary>
        [Display(Name = "单价")]
        public decimal UnitPrice { get; set; }
    }
}