using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using GenerateCV.Models;
using Microsoft.AspNetCore.Authorization;

namespace GenerateCV.Controllers;

[AllowAnonymous]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        try
        {
            // Test için geçici olarak session'a email ekliyoruz
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
            {
                HttpContext.Session.SetString("UserEmail", "test@example.com");
                _logger.LogInformation("Set test email in session: test@example.com");
            }

            var userEmail = HttpContext.Session.GetString("UserEmail");
            ViewData["UserEmail"] = userEmail;
            _logger.LogInformation("Current user email from session: {Email}", userEmail);

            if (TempData["ErrorMessage"] != null)
            {
                ViewData["ErrorMessage"] = TempData["ErrorMessage"];
            }

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Index action");
            return RedirectToAction("Error");
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
