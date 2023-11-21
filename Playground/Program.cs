using ConsoulLibrary;
using MakerBot;
using Microsoft.Extensions.Logging;
using Mtconnect;
using Mtconnect.AdapterSdk;
using Mtconnect.AdapterSdk.DeviceConfiguration;
using Mtconnect.MakerBotAdapter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal class Program
{
    private const string PUTTY_EXE = "C:\\Program Files\\PuTTY\\putty.exe";
    private const string CMD_EXE = "C:\\windows\\system32\\cmd.exe";
    private class AdapterLogger : IAdapterLogger
    {
        private readonly ILogger _logger;

        public AdapterLogger(ILogger logger)
        {
            _logger = logger;
        }

        public void LogDebug(string message, params object[] args)
            => _logger?.LogDebug(message, args);

        public void LogError(string message, params object[] args)
            => _logger?.LogError(message, args);

        public void LogError(Exception exception, string message, params object[] args)
            => _logger?.LogError(exception, message, args);

        public void LogInformation(string message, params object[] args)
            => _logger?.LogInformation(message, args);

        public void LogTrace(string message, params object[] args)
            => _logger?.LogTrace(message, args);

        public void LogWarning(string message, params object[] args)
            => _logger?.LogWarning(message, args);

        public void LogWarning(Exception exception, string message, params object[] args)
            => _logger?.LogWarning(exception, message, args);
    }
    private static void Main(string[] args)
    {
        var loggerFactory = LoggerFactory.Create((o) =>
        {
            o.SetMinimumLevel(LogLevel.Debug);
            o.AddConsoulLogger();

#if DEBUG
            o.SetMinimumLevel(LogLevel.Debug);
#endif
        });

        var machineConfigs = new List<MakerBot.MachineConfig>();
        string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "machines.json");

        if (File.Exists(configPath))
        {
            var loadConfig = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(configPath));
            if (loadConfig != null && loadConfig.ContainsKey("machines"))
            {
                var configMachines = loadConfig["machines"]?.ToArray();
                if (configMachines?.Length > 0)
                {
                    foreach (var jConfig in configMachines)
                    {
                        var machineConfig = jConfig.ToObject<MakerBot.MachineConfig>();
                        if (machineConfig != null) machineConfigs.Add(machineConfig);
                    }
                }
            }
        }

        var machineMonitoring = new List<Task>();
        using(var cancellationSource = new CancellationTokenSource())
        {
            using (var mf = new MachineFactory(loggerFactory))
            {
                var discoveries = mf.Discover();
                foreach (var discovery in discoveries)
                {
                    if (machineConfigs.Count > 0 && machineConfigs.Any(o => o.Serial == discovery.iserial)) continue;
                    Consoul.Write($"Machine Discovered: {discovery.machine_name}", ConsoleColor.Green);
                    machineConfigs.Add(new MakerBot.MachineConfig()
                    {
                        Serial = discovery.iserial,
                        Address = discovery.ip,
                        Port = int.Parse(discovery.port),
                        Name = discovery.machine_name
                    });
                }
            }

            // Begin monitoring
            Consoul.Write("Starting monitoring...");

            var options = new TcpAdapterOptions();
            options.UpdateFromConfig();

            var adapter = new TcpAdapter(options, new AdapterLogger(loggerFactory.CreateLogger<MakerBotRPCAdapter>()));
            adapter.OnStarted += Adapter_OnStarted;
            adapter.OnStopped += Adapter_OnStopped;
            var modelSources = new List<MakerBotRPCAdapter>();
            foreach (var machine in machineConfigs)
            {
                var model = new MakerBotRPCAdapter(machine.Serial, machine.AuthenticationCode, 5000, loggerFactory);
                modelSources.Add(model);
            }
            adapter.Start(modelSources.ToArray(), token: cancellationSource.Token);

            //Task.Run(() => SaveDevices(adapter));

            Consoul.Write("Reporting: AVAILABILITY, Mouse X-Position, Mouse Y-Position, Active Window Title");
            Consoul.Write($"Adapter running @ http://*:{adapter.Port}");

            if (File.Exists(PUTTY_EXE) && Consoul.Ask("Would you like to run PuTTY?"))
            {
                using (var cmd = new System.Diagnostics.Process())
                {
                    cmd.StartInfo.FileName = CMD_EXE;
                    cmd.StartInfo.UseShellExecute = false;
                    cmd.StartInfo.RedirectStandardInput = true;
                    cmd.StartInfo.RedirectStandardOutput = true;
                    cmd.StartInfo.CreateNoWindow = true;

                    cmd.Start();

                    cmd.StandardInput.WriteLine($"\"{PUTTY_EXE}\" -raw -P {adapter.Port} localhost");
                }
            }

            Consoul.Wait(cancellationToken: cancellationSource.Token);
            cancellationSource.Cancel();

            adapter.Stop();

            // Save JSON config
            Consoul.Write("Saving config to " + configPath);
            var allConfigs = modelSources.Select(o => o.Machine.Config).ToArray();
            File.WriteAllText(configPath, JsonConvert.SerializeObject(new { machines = allConfigs }));

            Consoul.Write("Done!", ConsoleColor.Green);
        }

    }

    private static async void SaveDevices(TcpAdapter adapter)
    {
        await Task.Delay(30_000);
        var dcf = new DeviceModelFactory();
        var doc = dcf.Create(adapter);
        string filename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Devices.xml");
        doc.Save(filename);
    }

    private static void Adapter_OnStarted(IAdapter sender, object e)
    {
    }

    private static void Adapter_OnStopped(IAdapter sender, AdapterStoppedEventArgs e)
    {
    }
}