using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

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
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "请输入用户名")]
        [Display(Name = "用户名")]
        [MaxLength(50, ErrorMessage = "用户名不能超过50个字符")]
        public string Username { get; set; }
        
        [Required(ErrorMessage = "请输入密码")]
        [Display(Name = "密码")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "密码长度不能少于6个字符")]
        public string Password { get; set; }
        
        [Required(ErrorMessage = "请确认密码")]
        [Display(Name = "确认密码")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "两次输入的密码不一致")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "手机号码")]
        [RegularExpression(@"^1[3-9]\d{9}$", ErrorMessage = "手机号码格式不正确")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "请输入地址")]
        [Display(Name = "送餐地址")]
        [MaxLength(255, ErrorMessage = "地址不能超过255个字符")]
        public string Address { get; set; }
    }
} 