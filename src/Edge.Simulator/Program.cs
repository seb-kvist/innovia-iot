using System;
using System.Collections.Generic;
using MQTTnet;
using MQTTnet.Client;
using System.Text.Json;
using System.Text;

var factory = new MqttFactory();
var client = factory.CreateMqttClient();

var options = new MqttClientOptionsBuilder()
    .WithTcpServer("localhost", 1883)
    .Build();

Console.WriteLine("Edge.Simulator starting… connecting to MQTT at localhost:1883");
try
{
    await client.ConnectAsync(options);
    Console.WriteLine("✅ Connected to MQTT broker.");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Failed to connect to MQTT broker: {ex.Message}");
    throw;
}

var tenantSlug = "sebastians-hub";
var rand = new Random();

var deviceSimulators = new List<DeviceSimulator>
{
    new("toshi001", () => new object[]
    {
        new { type = "temperature", value = 21 + rand.NextDouble() * 2, unit = "C" }
    }),
    new("toshi002", () => new object[]
    {
        new { type = "temperature", value = 20 + rand.NextDouble() * 2.5, unit = "C" }
    }),
    new("toshi003", () => new object[]
    {
        new { type = "temperature", value = 22 + rand.NextDouble() * 1.5, unit = "C" }
    }),
    new("toshi004", () => new object[]
    {
        new { type = "co2", value = 650 + rand.Next(0, 400), unit = "ppm" }
    }),
    new("toshi005", () => new object[]
    {
        new { type = "co2", value = 700 + rand.Next(0, 450), unit = "ppm" }
    }),
    new("toshi006", () => new object[]
    {
        new { type = "co2", value = 680 + rand.Next(0, 420), unit = "ppm" }
    }),
    new("toshi007", () => new object[]
    {
        new { type = "humidity", value = 35 + rand.NextDouble() * 10, unit = "%" }
    }),
    new("toshi008", () => new object[]
    {
        new { type = "humidity", value = 40 + rand.NextDouble() * 8, unit = "%" }
    }),
    new("toshi009", () => new object[]
    {
        new { type = "humidity", value = 42 + rand.NextDouble() * 6, unit = "%" }
    }),
    new("toshi010", () =>
    {
        var motionDetected = rand.NextDouble() < 0.2; // 20% chans rörelse
        return new object[]
        {
            new { type = "motion", value = motionDetected ? 1 : 0, unit = "bool" }
        };
    })
};

while (true)
{
    await PublishBatchAsync(deviceSimulators, tenantSlug, client);
    await Task.Delay(TimeSpan.FromSeconds(10));
}

async Task PublishBatchAsync(IEnumerable<DeviceSimulator> simulators, string tenant, IMqttClient mqttClient)
{
    var timestamp = DateTimeOffset.UtcNow;
    foreach (var simulator in simulators)
    {
        var payload = new
        {
            deviceId = simulator.Serial,
            apiKey = $"{simulator.Serial}-key",
            timestamp,
            metrics = simulator.GenerateMetrics()
        };

        var topic = $"tenants/{tenant}/devices/{simulator.Serial}/measurements";
        var json = JsonSerializer.Serialize(payload);

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(Encoding.UTF8.GetBytes(json))
            .Build();

        await mqttClient.PublishAsync(message);
        Console.WriteLine($"[{timestamp:o}] Published to '{topic}': {json}");
    }
}
record DeviceSimulator(string Serial, Func<object[]> GenerateMetrics);
