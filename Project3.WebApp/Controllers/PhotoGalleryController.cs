using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project3.Shared.Models.Domain;
using Project3.Shared.Models.ViewModels;
using Project3.Shared.Repositories;
using Project3.Shared.Services;

namespace Project3.WebApp.Controllers
{
    public class PhotoGalleryController : Controller
    {
        private readonly ILogger<PhotoGalleryController> _logger;
        private readonly FileStorageService _fileStorageService;
        private readonly PhotoRepository _photoRepository;
        private readonly IWebHostEnvironment _hostEnvironment;

        public PhotoGalleryController(
            ILogger<PhotoGalleryController> logger,
            IConfiguration configuration,
            IWebHostEnvironment hostEnvironment,
            PhotoRepository photoRepository)
        {
            _logger = logger;
            _hostEnvironment = hostEnvironment;
            _fileStorageService = new FileStorageService(hostEnvironment.ContentRootPath, configuration);
            _photoRepository = photoRepository;
        }

        /// <summary>
        /// Displays photos for a specific restaurant
        /// </summary>
        [HttpGet]
        public IActionResult Index(int restaurantId)
        {
            try
            {
                var photos = _photoRepository.GetRestaurantPhotos(restaurantId);
                var viewModel = new PhotoGalleryViewModel
                {
                    RestaurantId = restaurantId,
                    Photos = photos
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting photos for restaurant with ID {RestaurantId}", restaurantId);
                TempData["ErrorMessage"] = "An error occurred while loading photos.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Uploads a photo for a restaurant
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Upload(int restaurantId, IFormFile file, string caption = null, string returnUrl = null)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please select a file to upload.";
                return RedirectToAction(nameof(Index), new { restaurantId });
            }

            try
            {
                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!Array.Exists(allowedExtensions, ext => ext == fileExtension))
                {
                    TempData["ErrorMessage"] = "Only JPG, JPEG, PNG, and GIF files are allowed.";
                    return RedirectToAction(nameof(Index), new { restaurantId });
                }
                
                // Validate file size (5MB max)
                if (file.Length > 5 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "File size must be less than 5MB.";
                    return RedirectToAction(nameof(Index), new { restaurantId });
                }
                
                // Save file to FileStorage
                var photoUrl = await _fileStorageService.SaveRestaurantPhotoAsync(file, restaurantId);
                
                // Add to database
                var photo = new Photo(restaurantId, photoUrl, caption);
                int photoId = _photoRepository.AddPhoto(photo);
                
                if (photoId > 0)
                {
                    TempData["SuccessMessage"] = "Photo uploaded successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to save photo information to the database.";
                }
                
                // If returnUrl is specified, redirect there, otherwise go to the gallery index
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                
                return RedirectToAction(nameof(Index), new { restaurantId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading photo for restaurant with ID {RestaurantId}", restaurantId);
                TempData["ErrorMessage"] = "An error occurred while uploading the photo.";
                
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                
                return RedirectToAction(nameof(Index), new { restaurantId });
            }
        }

        /// <summary>
        /// Updates a photo's caption
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Update(int photoId, string caption, string returnUrl = null)
        {
            try
            {
                var updatedPhoto = _photoRepository.UpdatePhotoCaption(photoId, caption);
                
                if (updatedPhoto != null)
                {
                    TempData["SuccessMessage"] = "Photo caption updated successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update photo caption.";
                }
                
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                
                return RedirectToAction(nameof(Index), new { restaurantId = updatedPhoto.RestaurantId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating photo with ID {PhotoId}", photoId);
                TempData["ErrorMessage"] = "An error occurred while updating the photo.";
                
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Deletes a photo
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Delete(int photoId, string returnUrl = null)
        {
            try
            {
                // Get photo URL before deleting
                var photoUrl = _photoRepository.DeletePhoto(photoId);
                
                if (photoUrl != null)
                {
                    // Delete the physical file
                    _fileStorageService.DeleteFile(photoUrl);
                    TempData["SuccessMessage"] = "Photo deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Photo not found or already deleted.";
                }
                
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                
                // Since the photo is deleted, we don't know the restaurantId,
                // so redirect to home if no returnUrl is specified
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting photo with ID {PhotoId}", photoId);
                TempData["ErrorMessage"] = "An error occurred while deleting the photo.";
                
                if (!string.IsNullOrEmpty(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                
                return RedirectToAction("Index", "Home");
            }
        }
    }
} 