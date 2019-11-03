using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Js.BrowserChat.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace Js.BrowserChat.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<IdentityUser> userManager;

        /// <summary>
        /// Constructor used to inject the UserManager
        /// </summary>
        /// <param name="userManager">Identity user manager</param>
        /*
        public HomeController(UserManager<IdentityUser> userManager)
        {
            this.userManager = userManager;
        }
        */
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

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

        /// <summary>
        /// Regitration action
        /// </summary>
        /// <returns>View</returns>
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        /// <summary>
        /// Registration posting
        /// </summary>
        /// <param name="model">Data for registration</param>
        /// <returns>Success view</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if user exists
                var user = await userManager.FindByNameAsync(model.UserName);
                if (user == null)
                {
                    user = new IdentityUser
                    {
                        UserName = model.UserName
                    };
                    // Create the user
                    var result = await userManager.CreateAsync(user, model.Password);
                }

                return View("Success");
            }
            return View();
        }

        /// <summary>
        /// Login page
        /// </summary>
        /// <returns>View</returns>
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// Login to the application
        /// </summary>
        /// <param name="model">Login model</param>
        /// <returns>View</returns>
        [HttpPost]
        public IActionResult Login(LoginModel model)
        {
            return View();
        }

    }
}
