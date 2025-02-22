using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace GenerateCV.Controllers
{
    public class LoginController(ILogger<LoginController> logger) : Controller
    {
        private readonly ILogger<LoginController> _logger = logger;

        public ActionResult Index(string email, string password)
        {
            _logger.LogInformation("Login attempt for email: {Email}", email ?? "null");

            if (email == "engineer.birgul@gmail.com" && password == "123456")
            {
                HttpContext.Session.SetString("UserEmail", email);
                TempData["SuccessMessage"] = "Hoşgeldiniz!";
                _logger.LogInformation("Login successful for user: {Email}", email);
                return RedirectToAction("Index", "Home");
            }
            else if (email != null && password != null)
            {
                _logger.LogWarning("Failed login attempt for email: {Email}", email);
                ViewBag.Message = "Giriş yapılamadı. Email veya şifre hatalı.";
                return View();
            }

            _logger.LogInformation("Initial login page view");
            return View();
        }

        [HttpPost]
        public ActionResult Logout()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            _logger.LogInformation("User logged out: {Email}", email ?? "unknown");
            
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Çıkış yapıldı.";
            return RedirectToAction("Index", "Login");
        }
    }
}

