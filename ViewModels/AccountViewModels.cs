using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using food_takeout.Models;

namespace food_takeout.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "请输入用户名")]
        [Display(Name = "用户名")]
        [MaxLength(50, ErrorMessage = "用户名不能超过50个字符")]
        public string Username { get; set; }
        
        [Required(ErrorMessage = "请输入密码")]
        [Display(Name = "密码")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        
        [Display(Name = "记住我")]
        public bool RememberMe { get; set; }

        [Required(ErrorMessage = "请选择用户角色")]
        [Display(Name = "用户角色")]
        public string UserType { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "请输入用户名")]
        [Display(Name = "用户名")]
        [MaxLength(50, ErrorMessage = "用户名不能超过50个字符")]
        public string Username { get; set; }
        
        [Required(ErrorMessage = "请输入密码")]
        [StringLength(100, ErrorMessage = "{0} 必须至少包含 {2} 个字符。", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "密码")]
        public string Password { get; set; }
        
        [DataType(DataType.Password)]
        [Display(Name = "确认密码")]
        [Compare("Password", ErrorMessage = "密码和确认密码不匹配。")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "请输入手机号")]
        [Phone(ErrorMessage = "请输入有效的手机号码")]
        [Display(Name = "手机号")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "请输入地址")]
        [Display(Name = "地址")]
        [MaxLength(255, ErrorMessage = "地址不能超过255个字符")]
        public string Address { get; set; }

        [Required(ErrorMessage = "请选择用户角色")]
        [Display(Name = "用户角色")]
        public string UserType { get; set; }

        [Display(Name = "餐厅名称")]
        public string RestaurantName { get; set; }
    }
} 