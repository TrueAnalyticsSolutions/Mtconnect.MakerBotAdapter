using ConsoulLibrary;
using Microsoft.Extensions.Logging;
using Mtconnect;
using Mtconnect.AdapterSdk;
using Mtconnect.AdapterSdk.DeviceConfiguration;
using Mtconnect.MakerBotAdapter;

internal static class Program
{
    private sealed class LoggerAdapter : IAdapterLogger
    {
        private readonly ILogger _logger;
        public LoggerAdapter(ILogger logger) => _logger = logger;
        public void LogDebug(string message, params object[] args) => _logger.LogDebug(message, args);
        public void LogError(string message, params object[] args) => _logger.LogError(message, args);
        public void LogError(Exception exception, string message, params object[] args) => _logger.LogError(exception, message, args);
        public void LogInformation(string message, params object[] args) => _logger.LogInformation(message, args);
        public void LogTrace(string message, params object[] args) => _logger.LogTrace(message, args);
        public void LogWarning(string message, params object[] args) => _logger.LogWarning(message, args);
        public void LogWarning(Exception exception, string message, params object[] args) => _logger.LogWarning(exception, message, args);
    }

    private static int Main(string[] args)
    {
        try
        {
            var configPath = ResolveConfigPath(args);
            using var loggerFactory = LoggerFactory.Create(options => { options.AddConsoulLogger(); options.SetMinimumLevel(LogLevel.Debug); });
            ValidationService.Instance = new StandardValidationHelper();
            var logger = new LoggerAdapter(loggerFactory.CreateLogger<MakerBotRPCAdapter>());
            using var source = new MakerBotRPCAdapter(configPath, logger);
            using var adapter = new TcpAdapter(new TcpAdapterOptions(), logger);
            adapter.OnDataModelRecieved += _adapter_OnDataModelRecieved;
            using var cancellation = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) => { e.Cancel = true; cancellation.Cancel(); };
            adapter.Start(source, token: cancellation.Token);
            Consoul.Write($"MakerBot SHDR adapter running on port {adapter.Port}. Press Ctrl+C to stop.", ConsoleColor.Green);
            Consoul.Wait(cancellationToken: cancellation.Token);
            adapter.Stop();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
    }

    private static void _adapter_OnDataModelRecieved(IAdapter sender, AdapterDataModelReceivedEventArgs e)
    {
        if (sender is TcpAdapter adapter) SaveDevices(adapter);
    }
    private static async void SaveDevices(TcpAdapter adapter)
    {
        await Task.Delay(20_000);
        try
        {
            var dcf = new DeviceModelFactory();
            var doc = dcf.Create(adapter);
            string filename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Devices.xml");
            doc.Save(filename);
            Consoul.Write($"Saved device configuration to {filename}", ConsoleColor.Green);
        }
        catch (Exception ex)
        {
            Consoul.Write("Failed to save device configuration due to error:\r\n" + ex.ToString(), ConsoleColor.Red);
        }
    }
    private static string ResolveConfigPath(string[] args)
    {
        string? path = null;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--config" && i + 1 < args.Length) path = args[++i];
            else if (!args[i].StartsWith("-") && path == null) path = args[i];
        }
        path = path ?? Path.Combine(Directory.GetCurrentDirectory(), "adapterconfig.json");
        path = Path.GetFullPath(path);
        if (!File.Exists(path)) path = Consoul.PromptForFilepath("Enter the path to adapterconfig.json:", true);
        if (!File.Exists(path)) throw new FileNotFoundException("MakerBot adapter configuration was not found.", path);
        return path;
    }
}
