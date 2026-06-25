using Microsoft.AspNetCore.Mvc;

namespace loguin_A.Controllers;

public class UsuarioController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}