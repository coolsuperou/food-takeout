using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

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
        [Display(Name = "订单明细编号")]
        public int OrderDetailId { get; set; }
        
        /// <summary>
        /// 订单ID
        /// </summary>
        [Required]
        [Display(Name = "所属订单")]
        public int OrderId { get; set; }
        
        /// <summary>
        /// 菜品ID
        /// </summary>
        [Required]
        [Display(Name = "菜品")]
        public int DishId { get; set; }
        
        /// <summary>
        /// 数量
        /// </summary>
        [Required]
        [Range(1, int.MaxValue)]
        [Display(Name = "数量")]
        public int Quantity { get; set; }
        
        /// <summary>
        /// 单价
        /// </summary>
        [Required]
        [Range(0, 99999.99)]
        [DisplayFormat(DataFormatString = "{0:F2}", ApplyFormatInEditMode = true)]
        [Display(Name = "单价")]
        public decimal Price { get; set; }
        
        /// <summary>
        /// 小计金额
        /// </summary>
        [Required]
        [Range(0, 999999.99)]
        [DisplayFormat(DataFormatString = "{0:F2}", ApplyFormatInEditMode = true)]
        [Display(Name = "小计金额")]
        public decimal Subtotal { get; set; }
        
        /// <summary>
        /// 订单信息
        /// </summary>
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }
        
        /// <summary>
        /// 菜品信息
        /// </summary>
        [ForeignKey("DishId")]
        public virtual Dish Dish { get; set; }
    }
}