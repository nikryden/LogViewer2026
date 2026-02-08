using System.Text;
using System.Text.Json;

namespace LogViewer2026.TestUtilities;

public static class LogFileGenerator
{
    private static readonly string[] _messages = 
    [
        "User logged in successfully",
        "Processing payment transaction",
        "Database query completed",
        "Cache miss occurred",
        "Email sent to user",
        "File upload completed",
        "API request received",
        "Background job started",
        "Configuration loaded",
        "Service initialized"
    ];

    private static readonly string[] _sourceContexts = 
    [
        "MyApp.Controllers.UserController",
        "MyApp.Services.PaymentService",
        "MyApp.Data.Repository",
        "MyApp.Services.CacheService",
        "MyApp.Services.EmailService",
        "MyApp.Controllers.FileController",
        "MyApp.API.Gateway",
        "MyApp.Jobs.BackgroundWorker",
        "MyApp.Configuration.Loader",
        "MyApp.Services.Initializer"
    ];

    public static void GenerateJsonLogFile(string filePath, int lineCount)
    {
        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        var random = new Random(42);
        var startTime = DateTime.UtcNow.AddDays(-7);

        for (int i = 0; i < lineCount; i++)
        {
            var timestamp = startTime.AddSeconds(i * 10);
            var level = GetRandomLogLevel(random, i);
            var message = _messages[random.Next(_messages.Length)];
            var sourceContext = _sourceContexts[random.Next(_sourceContexts.Length)];

            var logEntry = new
            {
                Timestamp = timestamp,
                Level = level.ToString(),
                MessageTemplate = message,
                RenderedMessage = message,
                SourceContext = sourceContext,
                Properties = new
                {
                    UserId = random.Next(1, 1000),
                    RequestId = Guid.NewGuid().ToString()
                }
            };

            var json = JsonSerializer.Serialize(logEntry);
            writer.WriteLine(json);
        }
    }

    public static void GenerateTextLogFile(string filePath, int lineCount)
    {
        using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        var random = new Random(42);
        var startTime = DateTime.UtcNow.AddDays(-7);

        for (int i = 0; i < lineCount; i++)
        {
            var timestamp = startTime.AddSeconds(i * 10);
            var level = GetRandomLogLevel(random, i);
            var levelCode = GetLevelCode(level);
            var message = _messages[random.Next(_messages.Length)];
            var sourceContext = _sourceContexts[random.Next(_sourceContexts.Length)];

            writer.WriteLine($"{timestamp:yyyy-MM-dd HH:mm:ss.fff} [{levelCode}] {sourceContext} {message}");
        }
    }

    private static string GetLevelCode(string level) => level switch
    {
        "Verbose" => "VRB",
        "Debug" => "DBG",
        "Information" => "INF",
        "Warning" => "WRN",
        "Error" => "ERR",
        "Fatal" => "FTL",
        _ => "INF"
    };

    private static string GetRandomLogLevel(Random random, int index)
    {
        if (index % 100 == 0) return "Error";
        if (index % 50 == 0) return "Warning";
        if (index % 20 == 0) return "Debug";
        
        var value = random.Next(100);
        return value switch
        {
            < 60 => "Information",
            < 75 => "Debug",
            < 90 => "Warning",
            < 98 => "Error",
            _ => "Fatal"
        };
    }
}
