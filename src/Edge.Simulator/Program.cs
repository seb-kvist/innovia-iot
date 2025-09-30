using MQTTnet;
using MQTTnet.Client;
using System.Text.Json;

var factory = new MqttFactory();
var client = factory.CreateMqttClient();
await client.ConnectAsync(new MqttClientOptionsBuilder().WithTcpServer("localhost",1883).Build());

var rand = new Random();
while (true)
{
    var payload = new {
        deviceId = "dev-101",
        apiKey = "dev-101-key",
        timestamp = DateTimeOffset.UtcNow,
        metrics = new object[] {
            new { type = "temperature", value = 21.5 + rand.NextDouble(), unit = "C" },
            new { type = "co2", value = 900 + rand.Next(0,700), unit = "ppm" }
        }
    };
    var msg = new MqttApplicationMessageBuilder()
        .WithTopic("tenants/innovia/devices/dev-101/measurements")
        .WithPayload(JsonSerializer.Serialize(payload))
        .Build();
    await client.PublishAsync(msg);
    await Task.Delay(2000);
}
