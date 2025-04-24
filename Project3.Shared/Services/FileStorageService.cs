using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Project3.Shared.Services
{
    public class FileStorageService
    {
        private readonly string _contentRootPath;
        private readonly IConfiguration _configuration;
        private readonly string _fileStoragePath;
        private readonly string _fileStorageWebPath;

        public FileStorageService(string contentRootPath, IConfiguration configuration)
        {
            _contentRootPath = contentRootPath;
            _configuration = configuration;
            _fileStoragePath = _configuration["FileStorage:Path"].Replace("~", _contentRootPath);
            _fileStorageWebPath = _configuration["FileStorage:WebPath"];
            
            // Ensure directory exists
            if (!Directory.Exists(_fileStoragePath))
            {
                Directory.CreateDirectory(_fileStoragePath);
            }
        }

        /// <summary>
        /// Saves a restaurant photo file to the FileStorage directory
        /// </summary>
        /// <param name="file">The file to save</param>
        /// <param name="restaurantId">ID of the restaurant</param>
        /// <returns>Relative URL to the saved file</returns>
        public async Task<string> SaveRestaurantPhotoAsync(IFormFile file, int restaurantId)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is null or empty", nameof(file));
            }

            // Create restaurant-specific directory if it doesn't exist
            var restaurantDir = Path.Combine(_fileStoragePath, "Restaurant", restaurantId.ToString());
            if (!Directory.Exists(restaurantDir))
            {
                Directory.CreateDirectory(restaurantDir);
            }

            // Generate unique filename
            var fileName = $"{DateTime.Now.Ticks}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(restaurantDir, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return relative path for database storage
            return $"{_fileStorageWebPath}Restaurant/{restaurantId}/{fileName}";
        }

        /// <summary>
        /// Saves a restaurant profile photo
        /// </summary>
        /// <param name="file">The file to save</param>
        /// <param name="restaurantId">ID of the restaurant</param>
        /// <returns>Relative URL to the saved file</returns>
        public async Task<string> SaveRestaurantProfilePhotoAsync(IFormFile file, int restaurantId)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is null or empty", nameof(file));
            }

            // Create restaurant-specific directory if it doesn't exist
            var restaurantDir = Path.Combine(_fileStoragePath, "Restaurant", restaurantId.ToString(), "Profile");
            if (!Directory.Exists(restaurantDir))
            {
                Directory.CreateDirectory(restaurantDir);
            }

            // Generate unique filename
            var fileExtension = Path.GetExtension(file.FileName);
            var fileName = $"profile_{DateTime.Now.Ticks}{fileExtension}";
            var filePath = Path.Combine(restaurantDir, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return relative path for database storage
            return $"{_fileStorageWebPath}Restaurant/{restaurantId}/Profile/{fileName}";
        }

        /// <summary>
        /// Saves a restaurant logo photo
        /// </summary>
        /// <param name="file">The file to save</param>
        /// <param name="restaurantId">ID of the restaurant</param>
        /// <returns>Relative URL to the saved file</returns>
        public async Task<string> SaveRestaurantLogoPhotoAsync(IFormFile file, int restaurantId)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is null or empty", nameof(file));
            }

            // Create restaurant-specific directory if it doesn't exist
            var restaurantDir = Path.Combine(_fileStoragePath, "Restaurant", restaurantId.ToString(), "Logo");
            if (!Directory.Exists(restaurantDir))
            {
                Directory.CreateDirectory(restaurantDir);
            }

            // Generate unique filename
            var fileExtension = Path.GetExtension(file.FileName);
            var fileName = $"logo_{DateTime.Now.Ticks}{fileExtension}";
            var filePath = Path.Combine(restaurantDir, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return relative path for database storage
            return $"{_fileStorageWebPath}Restaurant/{restaurantId}/Logo/{fileName}";
        }

        /// <summary>
        /// Saves a restaurant gallery image
        /// </summary>
        /// <param name="file">The file to save</param>
        /// <param name="restaurantId">ID of the restaurant</param>
        /// <returns>Relative URL to the saved file</returns>
        public async Task<string> SaveRestaurantGalleryImageAsync(IFormFile file, int restaurantId)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is null or empty", nameof(file));
            }

            // Create restaurant-specific gallery directory if it doesn't exist
            var galleryDir = Path.Combine(_fileStoragePath, "Restaurant", restaurantId.ToString(), "Gallery");
            if (!Directory.Exists(galleryDir))
            {
                Directory.CreateDirectory(galleryDir);
            }

            // Generate unique filename
            var fileExtension = Path.GetExtension(file.FileName);
            var fileName = $"gallery_{DateTime.Now.Ticks}{fileExtension}";
            var filePath = Path.Combine(galleryDir, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return relative path for database storage
            return $"{_fileStorageWebPath}Restaurant/{restaurantId}/Gallery/{fileName}";
        }

        /// <summary>
        /// Deletes a file from the FileStorage directory
        /// </summary>
        /// <param name="fileUrl">Relative URL of the file to delete</param>
        public void DeleteFile(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
            {
                return;
            }

            // Convert relative URL to physical path
            var relativePath = fileUrl.Replace(_fileStorageWebPath, "");
            var filePath = Path.Combine(_fileStoragePath, relativePath);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        /// <summary>
        /// Gets the physical path for a given relative URL
        /// </summary>
        /// <param name="fileUrl">Relative URL of the file</param>
        /// <returns>Physical path to the file</returns>
        public string GetPhysicalPath(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
            {
                return null;
            }

            var relativePath = fileUrl.Replace(_fileStorageWebPath, "");
            return Path.Combine(_fileStoragePath, relativePath);
        }
    }
} 