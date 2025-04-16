using RabbitMQ.Client;
using System.Text;

namespace backend_api.Services
{
    public class MessagePublisher
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public MessagePublisher()
        {
            var factory = new ConnectionFactory
            {
                HostName = "localhost",
                UserName = "admin",
                Password = "password"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(
                queue: "download-queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );
        }

        public void Publish(string message)
        {
            var body = Encoding.UTF8.GetBytes(message);

            _channel.BasicPublish(
                exchange: "",
                routingKey: "download-queue",
                basicProperties: null,
                body: body
            );
        }
    }
}
