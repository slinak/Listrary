using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ListManager.Models;

namespace ListManager.Controllers;

public class ListsController : Controller
{
    public static List<ListManager.Models.List> Lists = new List<ListManager.Models.List>();
    private readonly ILogger<ListsController> _logger;

    public ListsController(ILogger<ListsController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View(Lists);
    }

    public IActionResult Create()
    {
        return View("Create");
    }

    [HttpPost]
    public IActionResult Create(List list)
    {
        Lists.Add(list);
        return RedirectToAction("Index");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
