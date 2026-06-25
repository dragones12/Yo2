using Microsoft.AspNetCore.Mvc;

namespace loguin_A.Controllers;

public class ClienteController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}