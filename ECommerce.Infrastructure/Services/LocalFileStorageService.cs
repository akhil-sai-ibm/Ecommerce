using System;
using System.IO;
using System.Threading.Tasks;
using ECommerce.Application.DTOs.Common;
using ECommerce.Application.Interfaces.Services;
using ECommerce.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Services
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly ILogger<LocalFileStorageService> _logger;
        private readonly string _webRootPath;

        public LocalFileStorageService(ILogger<LocalFileStorageService> logger)
        {
            _logger = logger;
            // In ASP.NET Core, GetCurrentDirectory() returns the project root where wwwroot lives
            _webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        public async Task<string> SaveFileAsync(FileData fileData, string directory)
        {
            if (fileData == null || fileData.Content == null || fileData.Length == 0)
                throw new DomainException("Invalid file data.");

            // Basic path traversal prevention
            if (directory.Contains("..") || directory.Contains("/") || directory.Contains("\\"))
                throw new DomainException("Invalid directory name.");

            // Generate unique filename
            string extension = Path.GetExtension(fileData.FileName);
            string newFileName = $"{Guid.NewGuid()}{extension}";

            string uploadsFolder = Path.Combine(_webRootPath, "uploads", directory);
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string filePath = Path.Combine(uploadsFolder, newFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await fileData.Content.CopyToAsync(fileStream);
            }

            return $"/uploads/{directory}/{newFileName}";
        }

        public Task DeleteFileAsync(string relativeFilePath)
        {
            if (string.IsNullOrWhiteSpace(relativeFilePath))
                return Task.CompletedTask;

            try
            {
                // relativeFilePath should be like "/uploads/products/123.jpg"
                var path = relativeFilePath.TrimStart('/');
                var fullPath = Path.Combine(_webRootPath, path);
                
                // Path traversal check
                var fullWebRoot = Path.GetFullPath(_webRootPath);
                var resolvedPath = Path.GetFullPath(fullPath);

                if (resolvedPath.StartsWith(fullWebRoot) && File.Exists(resolvedPath))
                {
                    File.Delete(resolvedPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {RelativeFilePath}", relativeFilePath);
            }

            return Task.CompletedTask;
        }
    }
}
