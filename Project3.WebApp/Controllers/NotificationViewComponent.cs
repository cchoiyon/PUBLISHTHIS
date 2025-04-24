using Microsoft.AspNetCore.Mvc;

namespace Project3.WebApp.Controllers
{
    public class NotificationViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(string message = null, string type = "info")
        {
            if (TempData.ContainsKey("SuccessMessage"))
            {
                ViewBag.NotificationMessage = TempData["SuccessMessage"];
                ViewBag.NotificationType = "success";
            }
            else if (TempData.ContainsKey("ErrorMessage"))
            {
                ViewBag.NotificationMessage = TempData["ErrorMessage"];
                ViewBag.NotificationType = "danger";
            }
            else if (TempData.ContainsKey("InfoMessage"))
            {
                ViewBag.NotificationMessage = TempData["InfoMessage"];
                ViewBag.NotificationType = "info";
            }
            else if (TempData.ContainsKey("WarningMessage"))
            {
                ViewBag.NotificationMessage = TempData["WarningMessage"];
                ViewBag.NotificationType = "warning";
            }
            else if (!string.IsNullOrEmpty(message))
            {
                ViewBag.NotificationMessage = message;
                ViewBag.NotificationType = type;
            }
            else
            {
                // No notification to display
                return Content("");
            }

            return View();
        }
    }
} 