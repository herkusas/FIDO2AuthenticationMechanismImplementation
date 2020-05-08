using Microsoft.AspNetCore.Mvc;

namespace FidoFront.Controllers
{
    public class AuthenticationController : Controller
    {
        public IActionResult Authenticate()
        {
            return View();
        }
    }
}
