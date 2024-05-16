using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using XEDAPVIP.Models;

namespace XEDAPVIP.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }
    public IActionResult Sale()
    {
        return View();
    }
    
    public IActionResult Product()
    {
        return View();
    }
    
    public IActionResult Service()
    {
        return View();
    }
    public IActionResult Cart()
    {
        return View();
    }

    public IActionResult Check_out(){
        return View();
    }
    
    public IActionResult Product_information(){
        return View();
    }

    public IActionResult Product_select(){
        return View();
    }

    public IActionResult Address(){
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
