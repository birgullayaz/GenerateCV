using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using GenerateCV.Models;

namespace GenerateCV.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        var userEmail = TempData["UserEmail"]?.ToString();
        if (!string.IsNullOrEmpty(userEmail))
        {
            ViewData["UserEmail"] = userEmail;
        }
        return View();
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
