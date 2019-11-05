using System;
using Js.BrowserChat.Core;
using Js.BrowserChat.Core.Models;
using RabbitMQ.Client;

namespace Js.BrowserChat.SamplePublisher
{
    class Program
    {
        private static ConnectionFactory _factory;
        private static IConnection _connection;
        private static IModel _model;
        private const string ExchangeName = "Chat_Exchange";

        static void Main(string[] args)
        {
            // Sample data to feed to the queue
            var msg1 = new ChatEntry { Text= "Hello team", WhoPosted = "jcossio", DatePosted = DateTime.UtcNow };
            var msg2 = new ChatEntry { Text = "Hi to you", WhoPosted = "johnDoe", DatePosted = DateTime.UtcNow };
            var msg3 = new ChatEntry { Text = "Heading to the island", WhoPosted = "rMontalban", DatePosted = DateTime.UtcNow };
            var msg4 = new ChatEntry { Text = "Playing guitar", WhoPosted = "JHendrix", DatePosted = DateTime.UtcNow };
            var msg5 = new ChatEntry { Text = "Me too", WhoPosted = "MSimmons", DatePosted = DateTime.UtcNow };
            var msg6 = new ChatEntry { Text = "Sounds good", WhoPosted = "jcossio", DatePosted = DateTime.UtcNow };
            var msg7 = new ChatEntry { Text = "Let me join", WhoPosted = "rCharles", DatePosted = DateTime.UtcNow };
            var msg8 = new ChatEntry { Text = "Pitch in", WhoPosted = "dEllington", DatePosted = DateTime.UtcNow };
            var msg9 = new ChatEntry { Text = "Agreed", WhoPosted = "rHoward", DatePosted = DateTime.UtcNow };
            var msg10 = new ChatEntry { Text = "Moonwalking here", WhoPosted = "mJackson", DatePosted = DateTime.UtcNow };
            // Bot commands
            var msg11 = new ChatEntry { Text = "/stock=AAPL.US", WhoPosted = "mJackson", DatePosted = DateTime.UtcNow };
            var msg12 = new ChatEntry { Text = "/stock=AAPL.USX", WhoPosted = "mJackson", DatePosted = DateTime.UtcNow };
            var msg13 = new ChatEntry { Text = "/stock=NVDA.US", WhoPosted = "mJackson", DatePosted = DateTime.UtcNow };

            CreateConnection();

            SendMessage(msg1);
            SendMessage(msg2);
            SendMessage(msg3);
            SendMessage(msg4);
            SendMessage(msg5);
            SendMessage(msg6);
            SendMessage(msg7);
            SendMessage(msg8);
            SendMessage(msg9);
            SendMessage(msg10);

            SendMessage(msg11);
            SendMessage(msg12);
            SendMessage(msg13);

        }

        private static void CreateConnection()
        {
            // Connnect to a localhost instance of rabbit mq with default credentials.
            _factory = new ConnectionFactory { HostName = "localhost", UserName = "guest", Password = "guest" };
            _connection = _factory.CreateConnection();
            _model = _connection.CreateModel();
            // Say we want to have a fanout exchange
            _model.ExchangeDeclare(ExchangeName, ExchangeType.Fanout, durable: true);
            // Declare a temp queue to see the messages
            var tempQueue = _model.QueueDeclare("Chat_Temp_Queue", durable: true, exclusive: false, autoDelete: false);
            _model.QueueBind(tempQueue.QueueName, ExchangeName, "chat");
        }

        private static void SendMessage(ChatEntry message)
        {
            _model.BasicPublish(ExchangeName, "chat", null, message.Serialize());
            Console.WriteLine($"Message Sent by {message.WhoPosted}: {message.Text}");
        }
    }
}
