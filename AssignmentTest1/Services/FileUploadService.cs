using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace AssignmentTest1.Services
{
    public class FileUploadService
    {
        private readonly IWebHostEnvironment _environment;

        public FileUploadService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string?> UploadFileAsync(IFormFile? file, string subFolder = "uploads")
        {
            Console.WriteLine($"=== UploadFileAsync Called ===");
            Console.WriteLine($"File is null: {file == null}");

            if (file == null || file.Length == 0)
            {
                Console.WriteLine("❌ File is null or empty");
                return null;
            }

            Console.WriteLine($"✅ File Name: {file.FileName}");
            Console.WriteLine($"✅ File Size: {file.Length} bytes");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            Console.WriteLine($"✅ File Extension: {fileExtension}");

            if (!allowedExtensions.Contains(fileExtension))
            {
                Console.WriteLine($"❌ Invalid extension: {fileExtension}");
                return null;
            }

            var fileName = $"{Guid.NewGuid()}{fileExtension}";
            var uploadsFolder = Path.Combine(_environment.WebRootPath, subFolder);

            Console.WriteLine($"📁 Upload folder: {uploadsFolder}");

            if (!Directory.Exists(uploadsFolder))
            {
                Console.WriteLine($"📁 Creating folder: {uploadsFolder}");
                Directory.CreateDirectory(uploadsFolder);
            }

            var filePath = Path.Combine(uploadsFolder, fileName);
            Console.WriteLine($"💾 Saving to: {filePath}");

            using var fileStream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(fileStream);

            var result = $"/{subFolder}/{fileName}";
            Console.WriteLine($"✅ File saved: {result}");

            return result;
        }

        public void DeleteFile(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
    }
}