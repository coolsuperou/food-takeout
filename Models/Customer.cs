using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        [Display(Name = "顾客编号")]
        public int CustomerId { get; set; }
        
        /// <summary>
        /// 用户名
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Display(Name = "用户名")]
        public string Username { get; set; }
        
        /// <summary>
        /// 密码
        /// </summary>
        [Required]
        [MaxLength(100)]
        [Display(Name = "密码")]
        public string Password { get; set; }
        
        /// <summary>
        /// 手机号码
        /// </summary>
        [MaxLength(20)]
        [Display(Name = "手机号码")]
        public string PhoneNumber { get; set; }
        
        /// <summary>
        /// 地址
        /// </summary>
        [MaxLength(255)]
        [Display(Name = "地址")]
        public string Address { get; set; }
    }
} 