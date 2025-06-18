using System;

namespace food_takeout.Models
{
    /// <summary>
    /// 菜品视图模型，用于传递菜品数据到视图
    /// </summary>
    public class DishViewModel
    {
        /// <summary>
        /// 菜品ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 菜品名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 菜品价格
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// 菜品分类
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// 菜品描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 菜品图片路径
        /// </summary>
        public string ImageUrl { get; set; }

        /// <summary>
        /// 是否热销
        /// </summary>
        public bool IsHot { get; set; }

        /// <summary>
        /// 销售数量
        /// </summary>
        public int SoldCount { get; set; }

        /// <summary>
        /// 兼容属性，返回Id
        /// </summary>
        public int DishId { get { return Id; } set { Id = value; } }

        /// <summary>
        /// 兼容属性，避免ViewBag/ViewData访问错误
        /// </summary>
        public int Count { get { return 1; } }
    }
} 