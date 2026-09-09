using MakerBot.Rpc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MakerBot
{
    public sealed class MakerBotPairingRequiredException : UnauthorizedAccessException
    {
        public MakerBotPairingRequiredException(string message) : base(message) { }
    }

    public class Machine : IDisposable
    {
        private readonly ILogger<Machine> _logger;
        public RpcConnection Connection { get; private set; }
        public MachineConfig Config { get; private set; }
        public string VID { get; private set; }
        public int PID { get; private set; }
        public string ApiVersion { get; private set; }
        public string FirmwareVersion { get; private set; }
        public int SSL { get; private set; }
        public string MotorDriveVersion { get; private set; }
        public string BotType { get; private set; }
        public string MachineType { get; private set; }

        public Machine(MachineConfig config, ILoggerFactory logFactory = null)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrWhiteSpace(config.Address)) throw new ArgumentException("Machine address is required.", nameof(config));
            if (config.RpcPort < 1 || config.RpcPort > 65535) throw new ArgumentOutOfRangeException(nameof(config), "RPC port is invalid.");
            _logger = logFactory?.CreateLogger<Machine>();
            Connection = new RpcConnection(config.Address, config.RpcPort, logFactory);
        }

        public Machine(Broadcast discovery, ILoggerFactory logFactory = null) : this(FromDiscovery(discovery), logFactory) { }

        private static MachineConfig FromDiscovery(Broadcast discovery)
        {
            if (discovery == null) throw new ArgumentNullException(nameof(discovery));
            return new MachineConfig
            {
                Name = discovery.machine_name,
                SerialNumber = discovery.iserial,
                Address = discovery.ip,
                RpcPort = int.TryParse(discovery.port, out var port) ? port : 9999,
                SslPort = int.TryParse(discovery.ssl_port, out var sslPort) ? sslPort : 12309,
                MachineType = discovery.machine_type,
                BotType = discovery.bot_type,
                FirmwareVersion = discovery.firmware_version?.ToString()
            };
        }

        public void Start(CancellationToken cancellationToken = default) => StartAsync(false, cancellationToken).GetAwaiter().GetResult();
        public Task StartPairingAsync(CancellationToken cancellationToken = default) => StartAsync(true, cancellationToken);

        public async Task StartAsync(bool allowPairing, CancellationToken cancellationToken = default)
        {
            var expectedSerial = Config.SerialNumber;
            await Connection.StartAsync(cancellationToken).ConfigureAwait(false);
            var handshake = await Connection.Handshake(cancellationToken: cancellationToken).ConfigureAwait(false);
            if (handshake?.result == null) throw new InvalidOperationException("MakerBot handshake did not return machine information.");
            ApplyHandshake(handshake.result);

            if (!string.IsNullOrWhiteSpace(expectedSerial) && !string.Equals(expectedSerial, Config.SerialNumber, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Configured MakerBot serial '{expectedSerial}' does not match '{Config.SerialNumber}' at {Config.Address}.");

            if (string.IsNullOrWhiteSpace(Config.AuthenticationCode))
            {
                if (!allowPairing)
                    throw new MakerBotPairingRequiredException("MakerBot authorization is missing. Run MakerBot.Configurator and approve pairing on the printer.");
                _logger?.LogInformation("Requesting physical authorization from {Machine}", Config.Name);
                Config.AuthenticationCode = await FastCGI.GetAuthCode(Connection.Endpoint.Address, Config.ClientId, Config.ClientSecret, cancellationToken).ConfigureAwait(false);
            }

            Config.RpcToken = await FastCGI.GetAccessToken(Connection.Endpoint.Address, Config.AuthenticationCode, Config.ClientId, Config.ClientSecret, FastCGI.AccessTokenContexts.jsonrpc, cancellationToken).ConfigureAwait(false);
            Connection.AccessTokens[FastCGI.AccessTokenContexts.jsonrpc] = Config.RpcToken;
            if (!await Connection.Authenticate(cancellationToken).ConfigureAwait(false))
                throw new UnauthorizedAccessException("MakerBot rejected the saved authorization. Run MakerBot.Configurator to pair again.");
        }

        private void ApplyHandshake(Handshake.Result info)
        {
            Config.Name = info.machine_name;
            Config.SerialNumber = info.iserial;
            Config.Address = info.ip ?? Config.Address;
            Config.RpcPort = int.TryParse(info.port, out var rpcPort) ? rpcPort : Config.RpcPort;
            Config.SslPort = info.ssl_port;
            Config.MachineType = info.machine_type;
            Config.BotType = info.bot_type;
            Config.FirmwareVersion = info.firmware_version?.ToString();
            MachineType = info.machine_type;
            VID = info.vid.ToString();
            PID = info.pid;
            ApiVersion = info.api_version;
            SSL = info.ssl_port;
            MotorDriveVersion = info.motor_driver_version;
            BotType = info.bot_type;
            FirmwareVersion = info.firmware_version?.ToString();
        }

        public void Stop() => Connection?.Stop();
        public void Dispose() => Connection?.Dispose();
    }
}
