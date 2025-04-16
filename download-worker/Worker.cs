using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;
using Serilog;

public class Worker : BackgroundService
{
    private readonly ConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public Worker()
    {
        _redis = ConnectionMultiplexer.Connect("localhost:6379");
        _db = _redis.GetDatabase();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Directory.CreateDirectory("downloads");

        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "admin",
            Password = "password"
        };

        var connection = factory.CreateConnection();
        var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: "download-queue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            Log.Information("Message received: {Message}", message);

            try
            {
                var fileName = Path.GetFileName(new Uri(message).LocalPath);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "downloads", fileName);

                if (File.Exists(filePath))
                {
                    var oldName = fileName;
                    fileName = Guid.NewGuid() + "_" + fileName;
                    filePath = Path.Combine(Directory.GetCurrentDirectory(), "downloads", fileName);
                    Log.Warning("File already exists. Renamed from {OldName} to {NewName}", oldName, fileName);
                }

                using var client = new HttpClient();
                using var response = await client.GetAsync(message, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                using var responseStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = File.Create(filePath);

                byte[] buffer = new byte[8192];
                long totalRead = 0;
                int read;
                int lastProgress = 0;

                while ((read = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, read));
                    totalRead += read;

                    if (totalBytes > 0)
                    {
                        var percent = (int)((totalRead * 100) / totalBytes);

                        if (percent >= lastProgress + 25)
                        {
                            lastProgress = percent;
                            await _db.StringSetAsync($"download:{fileName}", $"downloading:{percent}%");
                            Log.Information("Progress: {Progress}%", percent);
                        }
                    }
                }

                await _db.StringSetAsync($"download:{fileName}", "completed");
                Log.Information("File downloaded to: {Path}", filePath);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error downloading file");
                var fallbackName = Path.GetFileName(new Uri(message).LocalPath);
                await _db.StringSetAsync($"download:{fallbackName}", "failed");
            }

            Log.Debug("ACK sent for message: {DeliveryTag}", ea.DeliveryTag);
            channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        };

        channel.BasicConsume(
            queue: "download-queue",
            autoAck: false,
            consumer: consumer
        );

        return Task.CompletedTask;
    }
}