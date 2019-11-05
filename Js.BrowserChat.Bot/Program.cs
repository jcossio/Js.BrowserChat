using Js.BrowserChat.Core;
using Js.BrowserChat.Core.Models;
using RabbitMQ.Client;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Js.BrowserChat.Bot
{
    class Program
    {
        private static ConnectionFactory _factory;
        private static IConnection _connection;
        private static QueueingBasicConsumer _consumer;

        private const string ExchangeName = "Chat_Exchange";
        private const string BotQueueName = "Chat_Bot_Queue";
        private const string BotName = "QouteBot";
        private static string stockPrefix = "/stock=";
        private static string stockUri = "​https://stooq.com/q/l/?s={0}&f=sd2t2ohlcv&h&e=csv​";

        private static readonly HttpClient client = new HttpClient();

        static void Main(string[] args)
        {
            // Connect to RabbitMQ
            _factory = new ConnectionFactory { HostName = "localhost", UserName = "guest", Password = "guest" };
            using (_connection = _factory.CreateConnection())
            {
                using (var channel = _connection.CreateModel())
                {
                    var queueName = DeclareAndBindQueueToExchange(channel);
                    channel.BasicConsume(queueName, autoAck: true, _consumer);

                    // Process all messages in the queue
                    while (true)
                    {
                        // Grab one
                        var ea = _consumer.Queue.Dequeue();
                        // Deserialize
                        var message = (ChatEntry)ea.Body.DeSerialize(typeof(ChatEntry));
                        // Check if its for us
                        if (message.Text.StartsWith(stockPrefix, StringComparison.InvariantCultureIgnoreCase))
                        {
                            _ = ProcessStockRequest(message.Text, channel);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Process a possible stock request and post the results to the queue
        /// </summary>
        /// <param name="message">Message to process</param>
        /// <param name="channel">Channel</param>
        private static async Task ProcessStockRequest(string message, IModel channel)
        {
            var stockCode = message.Substring(stockPrefix.Length);

            if (!string.IsNullOrEmpty(stockCode))
            {
                // Process
                var processedUri = string.Format(stockUri, stockCode);
                var response = await client.GetStringAsync(processedUri);
                if (!string.IsNullOrEmpty(response))
                {
                    // Format (Valid stock quote):
                    // Symbol,Date,Time,Open,High,Low,Close,Volume
                    // AAPL.US,2019-11-04,22:00:11,257.33,257.845,255.38,257.5,25568075
                    // Symbol ,Date      ,Time    ,Open  ,High   ,Low   ,Close,Volume
                    //
                    // Format (Stock quote not found):
                    // Symbol,Date,Time,Open,High,Low,Close,Volume
                    // AAPL.USX,N/D,N/D,N/D,N/D,N/D,N/D,N/D

                    // Strip header
                    response = response.Substring(response.IndexOf('\n'));
                    // Split
                    string[] symbolDataArray = response.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    // Check if valid info
                    if (symbolDataArray.Length > 6 && !symbolDataArray[6].Trim().Equals("N/D", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // We are good to go
                        PostStockQuoteAsBot(channel, symbolDataArray[0], symbolDataArray[6]);
                    }
                }
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Send a message to the exchange with a stock quote
        /// </summary>
        /// <param name="channel">Channel</param>
        /// <param name="symbol">Stock symbol</param>
        /// <param name="quote">Stock Quote value</param>
        private static void PostStockQuoteAsBot(IModel channel, string symbol, string quote)
        {
            var message = new ChatEntry { DatePosted = DateTime.UtcNow, Text = $"{symbol} quote is ${quote} per share", WhoPosted = BotName };
            channel.BasicPublish(ExchangeName, "bot", null, message.Serialize());
            Console.WriteLine($"Bot Sent {message.Text}");
        }

        /// <summary>
        /// Declare and bind the bot queue to the exchange
        /// </summary>
        /// <param name="channel">Channel</param>
        /// <returns>name of the new queue</returns>
        private static string DeclareAndBindQueueToExchange(IModel channel)
        {
            // Say we want to have a fanout exchange
            channel.ExchangeDeclare(ExchangeName, ExchangeType.Fanout, durable: true);
            // Declare a bot queue to see the messages
            var tempQueue = channel.QueueDeclare(BotQueueName, durable: true, exclusive: false, autoDelete: false);
            channel.QueueBind(tempQueue.QueueName, ExchangeName, "bot");
            _consumer = new QueueingBasicConsumer(channel);
            return tempQueue.QueueName;
        }
    }
}
