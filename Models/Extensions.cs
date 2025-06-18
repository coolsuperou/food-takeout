using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace food_takeout.Models
{
    /// <summary>
    /// 扩展方法类
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// 获取枚举的Display特性Name值
        /// </summary>
        /// <param name="value">枚举值</param>
        /// <returns>Display特性的Name值，如果没有则返回枚举名称</returns>
        public static string GetDisplayName(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attributes = field.GetCustomAttributes(typeof(DisplayAttribute), false) as DisplayAttribute[];
            
            if (attributes != null && attributes.Length > 0)
            {
                return attributes[0].Name;
            }
            
            return value.ToString();
        }

        public static string GetDescription(this Enum value)
        {
            FieldInfo field = value.GetType().GetField(value.ToString());
            DescriptionAttribute attribute = field.GetCustomAttribute<DescriptionAttribute>();
            return attribute == null ? value.ToString() : attribute.Description;
        }
    }
} 