using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Js.BrowserChat.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Js.BrowserChat.Web.Controllers
{
    public class HomeController : Controller
    {
        // IdentityUser comes from Microsoft.Extensions.Identity.Stores
        private readonly UserManager<IdentityUser> userManager;

        /// <summary>
        /// Constructor used to inject the UserManager
        /// </summary>
        /// <param name="userManager">Identity user manager</param>
        public HomeController(UserManager<IdentityUser> userManager)
        {
            this.userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

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
        /// <returns>Success view if successful</returns>
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
        /// <returns>Redirect to home if successful</returns>
        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if user exists
                var user = await userManager.FindByNameAsync(model.UserName);
                // Check the password
                if (user != null && await userManager.CheckPasswordAsync(user, model.Password))
                {
                    // Create a new claims identity using our authentication schema (to say this authority issued this identity)
                    var identity = new ClaimsIdentity("cookies");
                    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
                    identity.AddClaim(new Claim(ClaimTypes.Name, user.UserName));

                    await HttpContext.SignInAsync("cookies", new ClaimsPrincipal(identity));

                    // Redirect the user back to the homepage
                    return RedirectToAction("Index");
                }

                // Return something generic
                ModelState.AddModelError("", "Invalid user name or password.");

            }
            return View();
        }

        [HttpGet]
        public IActionResult Chat()
        {
            return View();
        }

    }
}
