using ConsoulLibrary;
using MakerBot;
using MakerBot.Rpc;
using Mtconnect.MakerBotAdapter;
using System.Text.Json;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        try
        {
            var outputPath = GetOption(args, "--output") ?? Path.Combine(Directory.GetCurrentDirectory(), "adapterconfig.json");
            var address = GetOption(args, "--address");
            MakerBot.MachineConfig machineConfig;

            if (!string.IsNullOrWhiteSpace(address))
            {
                machineConfig = new MakerBot.MachineConfig { Address = address, RpcPort = ParsePort(GetOption(args, "--port")) };
            }
            else
            {
                Console.WriteLine("Discovering MakerBot printers with mDNS and legacy UDP...");
                using var factory = new MachineFactory();
                var discoveries = await factory.DiscoverAsync();
                if (discoveries.Length == 0)
                    throw new InvalidOperationException("No MakerBots were discovered. Retry with --address <ip-or-hostname> [--port 9999].");
                var table = new TableView(new TableRenderOptions());
                table.AddHeader("Name");
                table.AddHeader("Serial Number");
                table.AddHeader("Address");
                table.AddHeader("RPC Port");
                table.AddHeader("Firmware");
                foreach (var discovery in discoveries)
                    table.AddRow(new[] { discovery.machine_name ?? string.Empty, discovery.iserial ?? string.Empty, discovery.ip ?? string.Empty, discovery.port ?? "9999", discovery.firmware_version?.ToString() ?? string.Empty }, false);
                var selection = table.Prompt("Select a MakerBot printer", ConsoleColor.Cyan, false, false, CancellationToken.None);
                if (!selection.HasValue || selection.Value < 0 || selection.Value >= discoveries.Length)
                    throw new InvalidOperationException("Printer selection was cancelled.");
                var selected = discoveries[selection.Value];
                machineConfig = new MakerBot.MachineConfig
                {
                    Name = selected.machine_name,
                    SerialNumber = selected.iserial,
                    Address = selected.ip,
                    RpcPort = int.TryParse(selected.port, out var port) ? port : 9999,
                    SslPort = int.TryParse(selected.ssl_port, out var ssl) ? ssl : 12309
                };
            }

            using var machine = new Machine(machineConfig);
            Console.WriteLine("Starting pairing. When the printer asks for approval, press its control-panel dial.");
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(130));
            await machine.StartPairingAsync(timeout.Token);
            var information = await machine.Connection.GetSystemInformation(timeout.Token);
            if (information?["error"] != null) throw new UnauthorizedAccessException("Authenticated system-information validation failed: " + information["error"]);

            var config = new AdapterConfiguration { Machine = machine.Config };
            var fullPath = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            var tempPath = fullPath + ".tmp";
            await File.WriteAllTextAsync(tempPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(tempPath, fullPath, true);
            Console.WriteLine($"Validated {machine.Config.Name} ({machine.Config.SerialNumber}) and wrote {fullPath}");
            Console.WriteLine("Protect this file: it contains the printer authorization code.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static string? GetOption(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
        return null;
    }

    private static int ParsePort(string? value) => int.TryParse(value, out var port) ? port : 9999;
}
