using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace loguin_A.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}