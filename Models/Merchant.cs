using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace food_takeout.Models
{
    /// <summary>
    /// 商家实体类
    /// </summary>
    public class Merchant
    {
        /// <summary>
        /// 商家ID
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "商家编号")]
        public int MerchantId { get; set; }
        
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
        /// 商家名称
        /// </summary>
        [Required]
        [StringLength(50)]
        [Display(Name = "商家名称")]
        public string Name { get; set; }
        
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
        /// 关联的餐厅
        /// </summary>
        public virtual ICollection<Restaurant> Restaurants { get; set; }
    }
} 