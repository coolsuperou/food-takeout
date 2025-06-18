using System;
using System.ComponentModel;

namespace food_takeout.Models
{
    /// <summary>
    /// 用户角色枚举
    /// </summary>
    public enum RoleType
    {
        /// <summary>
        /// 顾客
        /// </summary>
        [Description("顾客")]
        Customer = 1,

        /// <summary>
        /// 商家
        /// </summary>
        [Description("商家")]
        Merchant = 2,

        /// <summary>
        /// 骑手
        /// </summary>
        [Description("骑手")]
        Rider = 3
    }
} 