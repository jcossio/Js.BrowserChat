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
using Microsoft.AspNetCore.Http;
using RabbitMQ.Client;
using Js.BrowserChat.Core.Models;
using Js.BrowserChat.Core;

namespace Js.BrowserChat.Web.Controllers
{
    public class HomeController : Controller
    {
        // IdentityUser comes from Microsoft.Extensions.Identity.Stores
        private readonly UserManager<IdentityUser> userManager;

        private static ConnectionFactory _factory;
        private static IConnection _connection;
        private static IModel _model;
        private const string ExchangeName = "Chat_Exchange";

        /// <summary>
        /// Constructor used to inject the UserManager
        /// </summary>
        /// <param name="userManager">Identity user manager</param>
        public HomeController(UserManager<IdentityUser> userManager)
        {
            this.userManager = userManager;

            // Initialize connection to rabbitmq
            // Connnect to a localhost instance of rabbit mq with default credentials.
            _factory = new ConnectionFactory { HostName = "localhost", UserName = "guest", Password = "guest" };
            _connection = _factory.CreateConnection();
            _model = _connection.CreateModel();
            // Say we want to have a fanout exchange 
            _model.ExchangeDeclare(ExchangeName, ExchangeType.Fanout, durable: true);
        }

        public IActionResult Index()
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
                    _ = await userManager.CreateAsync(user, model.Password);
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
                    // Sign in
                    await HttpContext.SignInAsync("cookies", new ClaimsPrincipal(identity));

                    // Create a private queue for this user on RabbitMQ
                    CreateUserQueue(user.UserName);

                    // Redirect the user back to the homepage
                    return RedirectToAction("Index");
                }

                // Return something generic
                ModelState.AddModelError("", "Invalid user name or password.");

            }
            return View();
        }

        /// <summary>
        /// Create user queue to start receiving messages here
        /// </summary>
        /// <param name="userName">Our username</param>
        private void CreateUserQueue(string userName)
        {
            // Declare a user queue to see the messages
            _model.QueueDeclare($"Chat_{userName}_Queue", durable: true, exclusive: false, autoDelete: false);
            // Bind to our exchange
            _model.QueueBind($"Chat_{userName}_Queue", ExchangeName, "chat");
        }

        /// <summary>
        /// View the chat page
        /// </summary>
        /// <returns>Available chat entries</returns>
        [HttpGet]
        [Authorize]
        public IActionResult Chat()
        {
            var chatModel = new ChatModel();

            // Check if we have the ChatEntries queue initialized
            var chatEntriesQueue = HttpContext.Session.Get<Queue<ChatEntry>>("ChatEntriesQueue");
            if (chatEntriesQueue == null)
            {
                // Initialize the queue
                chatEntriesQueue = new Queue<ChatEntry>();
            }

            // Connect to our queue and retrieve messages
            _model.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);
            var queueName = $"Chat_{User.Identity.Name}_Queue";
            var _consumer = new QueueingBasicConsumer(_model);
            _model.BasicConsume(queue: queueName, autoAck: false, _consumer);
            while (true)
            {
                if (!_consumer.Queue.Dequeue(millisecondsTimeout: 100, out var result))
                    break;
                var message = (ChatEntry)result.Body.DeSerialize(typeof(ChatEntry));
                // Check for stock commands
                if (!message.Text.StartsWith("/stock=",StringComparison.InvariantCultureIgnoreCase))
                    chatEntriesQueue.Enqueue(new ChatEntry { DatePosted = message.DatePosted, Text = message.Text, WhoPosted = message.WhoPosted });
                // Acknowledge
                _model.BasicAck(result.DeliveryTag, false);
            }

            // Take the last 50 entries of the local queue
            chatModel.ChatEntries = chatEntriesQueue.TakeLast(50).ToList();
            // Remove old ones
            while(chatEntriesQueue.Count > 50)
                chatEntriesQueue.Dequeue();
            // Add the local queue back to the session
            HttpContext.Session.Set<Queue<ChatEntry>>("ChatEntriesQueue", chatEntriesQueue);

            return View(chatModel);
        }

        /// <summary>
        /// Chat message post
        /// </summary>
        /// <param name="model">Chat info to post</param>
        /// <returns>View</returns>
        [HttpPost]
        [Authorize]
        public IActionResult Chat(ChatModel model)
        {
            var outModel = new ChatModel();

            // Check if we have the ChatEntries queue initialized
            var chatEntriesQueue = HttpContext.Session.Get<Queue<ChatEntry>>("ChatEntriesQueue");
            if (chatEntriesQueue == null)
            {
                // Initialize the queue 
                chatEntriesQueue = new Queue<ChatEntry>();
            }

            if (ModelState.IsValid)
            {
                // Add the entry to the queue
                var newChat = new ChatEntry { DatePosted = DateTime.UtcNow, Text = model.ChatText, WhoPosted = User.Identity.Name };
                _model.BasicPublish(ExchangeName, "chat", null, newChat.Serialize());
            }

            // Connect to our rabbit queue and retrieve messages
            _model.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);
            var queueName = $"Chat_{User.Identity.Name}_Queue";
            var _consumer = new QueueingBasicConsumer(_model);
            _model.BasicConsume(queue: queueName, autoAck: false, _consumer);
            while (true)
            {
                if (!_consumer.Queue.Dequeue(millisecondsTimeout: 100, out var result))
                    break;
                var message = (ChatEntry)result.Body.DeSerialize(typeof(ChatEntry));
                // Check for stock commands
                if (!message.Text.StartsWith("/stock=", StringComparison.InvariantCultureIgnoreCase))
                    chatEntriesQueue.Enqueue(new ChatEntry { DatePosted = message.DatePosted, Text = message.Text, WhoPosted = message.WhoPosted });
                // Acknowledge
                _model.BasicAck(result.DeliveryTag, false);
            }

            // Take the last 50 entries of the local queue
            outModel.ChatEntries = chatEntriesQueue.TakeLast(50).ToList();
            // Remove old ones
            while (chatEntriesQueue.Count > 50)
                chatEntriesQueue.Dequeue();
            // Add the local queue back to the session
            HttpContext.Session.Set<Queue<ChatEntry>>("ChatEntriesQueue", chatEntriesQueue);

            return View(outModel);
        }
    }
}
