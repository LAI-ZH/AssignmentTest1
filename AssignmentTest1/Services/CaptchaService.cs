using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Text;

namespace AssignmentTest1.Services
{
    public class CaptchaService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string SessionKey = "CaptchaCode";

        public CaptchaService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GenerateCaptchaCode()
        {
            var random = new Random();
            var code = new StringBuilder();
            for (int i = 0; i < 6; i++)
            {
                code.Append(random.Next(0, 9));
            }
            return code.ToString();
        }

        public byte[] GenerateCaptchaImage(string captchaCode)
        {
            // 保存到 Session
            _httpContextAccessor.HttpContext?.Session.SetString(SessionKey, captchaCode);

            int width = 200;
            int height = 80;
            var random = new Random();

            using var bitmap = new Bitmap(width, height);
            using var graphics = Graphics.FromImage(bitmap);

            // 平滑处理
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

            // 背景色
            graphics.Clear(Color.FromArgb(240, 240, 240));

            // 绘制随机背景线条
            for (int i = 0; i < 15; i++)
            {
                var penColor = Color.FromArgb(
                    random.Next(100, 200),
                    random.Next(100, 200),
                    random.Next(100, 200)
                );
                using var pen = new Pen(penColor, 1);
                var x1 = random.Next(0, width);
                var y1 = random.Next(0, height);
                var x2 = random.Next(0, width);
                var y2 = random.Next(0, height);
                graphics.DrawLine(pen, x1, y1, x2, y2);
            }

            // 绘制验证码字符
            var fontFamilies = new[] { "Arial", "Verdana", "Tahoma", "Times New Roman" };

            for (int i = 0; i < captchaCode.Length; i++)
            {
                var fontSize = random.Next(28, 40);
                var fontFamily = fontFamilies[random.Next(0, fontFamilies.Length)];
                var color = Color.FromArgb(
                    random.Next(0, 100),
                    random.Next(0, 100),
                    random.Next(0, 100)
                );

                using var font = new Font(fontFamily, fontSize, FontStyle.Bold);
                using var brush = new SolidBrush(color);

                // 随机位置偏移
                var x = 15 + i * 30 + random.Next(-5, 5);
                var y = 15 + random.Next(-5, 10);

                // 随机旋转
                var rotateAngle = random.Next(-25, 25);
                graphics.TranslateTransform(x, y);
                graphics.RotateTransform(rotateAngle);
                graphics.DrawString(
                    captchaCode[i].ToString(),
                    font,
                    brush,
                    0,
                    0
                );
                graphics.RotateTransform(-rotateAngle);
                graphics.TranslateTransform(-x, -y);
            }

            // 添加噪点
            for (int i = 0; i < 300; i++)
            {
                var color = Color.FromArgb(
                    random.Next(0, 255),
                    random.Next(0, 255),
                    random.Next(0, 255)
                );
                bitmap.SetPixel(
                    random.Next(0, width),
                    random.Next(0, height),
                    color
                );
            }

            // 添加边框
            using var borderPen = new Pen(Color.FromArgb(200, 200, 200), 1);
            graphics.DrawRectangle(borderPen, 0, 0, width - 1, height - 1);

            // 保存为 PNG
            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }

        public bool ValidateCaptcha(string userInput)
        {
            if (string.IsNullOrEmpty(userInput))
                return false;

            var storedCode = _httpContextAccessor.HttpContext?.Session.GetString(SessionKey);
            if (string.IsNullOrEmpty(storedCode))
                return false;

            // 清除 Session 中的验证码（防止重复使用）
            _httpContextAccessor.HttpContext?.Session.Remove(SessionKey);

            return storedCode.Equals(userInput, StringComparison.OrdinalIgnoreCase);
        }
    }
}