using MQTTnet;
using MQTTnet.Client;
using System.Text.Json;

var factory = new MqttFactory();
var client = factory.CreateMqttClient();

var options = new MqttClientOptionsBuilder()
    .WithTcpServer("localhost", 1883)
    .Build();

await client.ConnectAsync(options);

var rand = new Random();
while (true)
{
    var payload = new
    {
        deviceId = "dev-101",
        apiKey = "dev-101-key",
        timestamp = DateTimeOffset.UtcNow,
        metrics = new object[]
        {
            new { type = "temperature", value = 21.5 + rand.NextDouble(), unit = "C" },
            new { type = "co2", value = 900 + rand.Next(0, 700), unit = "ppm" }
        }
    };

    var message = new MqttApplicationMessageBuilder()
        .WithTopic("tenants/innovia/devices/dev-101/measurements")
        .WithPayload(JsonSerializer.Serialize(payload))
        .Build();

    await client.PublishAsync(message);
    await Task.Delay(TimeSpan.FromSeconds(2));
}
