using Microsoft.AspNetCore.Mvc;

namespace BeerMall.Api.Controllers
{
    [Route("api/upload")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        // 注入运行环境，用于获取 wwwroot 物理路径
        public UploadController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpPost("image")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            // 1. 校验文件
            if (file == null || file.Length == 0)
                return BadRequest("没有选择文件或文件为空");

            // 限制文件大小 (例如 5MB)
            if (file.Length > 5 * 1024 * 1024)
                return BadRequest("图片大小不能超过 5MB");

            // 校验后缀名，防止上传木马脚本
            var ext = Path.GetExtension(file.FileName).ToLower();
            var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (!allowedExts.Contains(ext))
                return BadRequest("只允许上传图片格式 (jpg, png, gif, webp)");

            // 2. 生成存储路径
            // 物理路径: wwwroot/uploads/images/
            string webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string uploadFolder = Path.Combine(webRootPath, "uploads", "images");

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            // 生成唯一文件名，防止重名覆盖
            string newFileName = Guid.NewGuid().ToString("N") + ext;
            string filePath = Path.Combine(uploadFolder, newFileName);

            // 3. 保存文件到磁盘
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 4. 返回前端可访问的相对 URL
            // 例如: /uploads/images/abc123xyz.jpg
            string fileUrl = $"/uploads/images/{newFileName}";

            return Ok(new { url = fileUrl, success = true });
        }
    }
}