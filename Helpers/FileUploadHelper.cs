using System;
using System.IO;
using System.Web;
using System.Linq;

namespace food_takeout.Helpers
{
    public static class FileUploadHelper
    {
        /// <summary>
        /// 上传文件并返回保存路径
        /// </summary>
        /// <param name="file">上传的文件</param>
        /// <param name="folderPath">保存文件夹路径(相对于网站根目录)</param>
        /// <returns>保存后的相对路径</returns>
        public static string UploadFile(HttpPostedFileBase file, string folderPath)
        {
            if (file == null || file.ContentLength == 0)
            {
                return null;
            }

            try
            {
                // 验证文件类型
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
                if (!allowedTypes.Contains(file.ContentType.ToLower()))
                {
                    throw new InvalidOperationException("只支持jpg、png、gif格式的图片");
                }

                // 验证文件大小（5MB）
                if (file.ContentLength > 5 * 1024 * 1024)
                {
                    throw new InvalidOperationException("图片大小不能超过5MB");
            }
            
            // 确保文件夹存在
            string absoluteFolderPath = HttpContext.Current.Server.MapPath("~/" + folderPath);
            if (!Directory.Exists(absoluteFolderPath))
            {
                Directory.CreateDirectory(absoluteFolderPath);
            }
            
            // 生成唯一文件名
            string fileExtension = Path.GetExtension(file.FileName);
            string fileName = Guid.NewGuid().ToString() + fileExtension;
            string savePath = Path.Combine(folderPath, fileName);
            string absoluteSavePath = HttpContext.Current.Server.MapPath("~/" + savePath);
            
            // 保存文件
            file.SaveAs(absoluteSavePath);
            
            // 返回相对路径
            return "/" + savePath.Replace("\\", "/");
            }
            catch (Exception ex)
            {
                // 记录异常详情
                System.Diagnostics.Debug.WriteLine($"文件上传失败: {ex.Message}, StackTrace: {ex.StackTrace}");
                throw new InvalidOperationException("文件上传失败：" + ex.Message);
            }
        }
    }
} 