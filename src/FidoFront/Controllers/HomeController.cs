﻿using Microsoft.AspNetCore.Mvc;

namespace FidoFront.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
