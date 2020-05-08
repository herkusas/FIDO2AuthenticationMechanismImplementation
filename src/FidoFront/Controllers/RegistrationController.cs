using Microsoft.AspNetCore.Mvc;

namespace FidoFront.Controllers
{
    public class RegistrationController : Controller
    {
        public IActionResult Register()
        {
            return View();
        }
    }
}
