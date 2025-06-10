using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace food_takeout.Models
{
    /// <summary>
    /// 骑手实体类
    /// </summary>
    public class Rider
    {
        /// <summary>
        /// 骑手ID
        /// </summary>
        [Key]
        [Display(Name = "骑手编号")]
        public int RiderId { get; set; }
        
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
        [MaxLength(20)]
        [Display(Name = "手机号码")]
        public string PhoneNumber { get; set; }
        
        /// <summary>
        /// 是否可用
        /// </summary>
        [Display(Name = "是否可用")]
        public bool IsAvailable { get; set; }
        
        /// <summary>
        /// 关联订单
        /// </summary>
        public virtual ICollection<Order> Orders { get; set; }
    }
} 