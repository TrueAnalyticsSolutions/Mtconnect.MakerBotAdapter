using MakerBot.Rpc;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml;

namespace MakerBot
{
    public class Machine : IDisposable
    {
        private readonly ILogger<Machine> _logger;

        /// <summary>
        /// Reference to the current connection to the onboard RPC service
        /// </summary>
        public RpcConnection Connection { get; private set; }

        /// <summary>
        /// IP Address of the machine's API
        /// </summary>
        public IPEndPoint Address => Connection.Endpoint;

        /// <summary>
        /// Reference to the configuration of this instance. Can be used to re-establish connection to this machine.
        /// </summary>
        public MachineConfig Config { get; set; } = new MachineConfig();

        /// <summary>
        /// Reference to the machine's response for its current configuration.
        /// </summary>
        public MakerBot.Rpc.MachineConfig.Result MachineConfigResponse { get; set; } = null;

        /// <summary>
        /// Vendor ID.
        /// </summary>
        public string VID { get; set; }

        /// <summary>
        /// Product ID.
        /// </summary>
        public int PID { get; set; }

        /// <summary>
        /// Version of the RPC API.
        /// </summary>
        public string ApiVersion { get; set; }

        /// <summary>
        /// Version of the machine's firmware.
        /// </summary>
        public string FirmwareVersion { get; set; }

        public int SSL { get; set; }

        public string MotorDriveVersion { get; set; }

        /// <summary>
        /// MakerBot's internal id for the machine model.
        /// </summary>
        public string BotType { get; set; }

        /// <summary>
        /// MakerBot's internal codename for the machine model.
        /// </summary>
        public string MachineType { get; set; }

        public Machine(IPAddress host, int port, ILoggerFactory logFactory = default)
        {
            _logger = logFactory?.CreateLogger<Machine>();

            Config.Address = host.ToString();
            Config.Port = port;

            Connection = new RpcConnection(host.ToString(), port, logFactory);
        }

        public Machine(Broadcast broadcastResponse, ILoggerFactory logFactory = default) : this(IPAddress.Parse(broadcastResponse.ip), int.Parse(broadcastResponse.port), logFactory)
        {
            Config.Name = broadcastResponse.machine_name;
            Config.Serial = broadcastResponse.iserial;
            MachineType = broadcastResponse.machine_type;
        }

        public void Start(CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("Starting machine connection @{Address}...", Address);
            Connection?.ConnectAsync(Config.RpcToken, ct: cancellationToken);
            _logger?.LogInformation("Started machine connection @{Address}", Address);

            if (!Connection.IsConnected)
            {
                _logger?.LogWarning("RPC connection is not authenticated...");
                if (string.IsNullOrEmpty(Config.AuthenticationCode))
                {
                    _logger?.LogInformation("Retrieving Authentication Code...");
                    Config.AuthenticationCode = FastCGI.GetAuthCode(Address.Address, Config.ClientId, Config.ClientSecret).Result;
                }

                _logger?.LogInformation("Retrieving JSON-RPC Access Token...");
                Config.RpcToken = FastCGI.GetAccessToken(Address.Address, Config.AuthenticationCode, Config.ClientId, Config.ClientSecret, FastCGI.AccessTokenContexts.jsonrpc).Result;

                Connection.AccessTokens[FastCGI.AccessTokenContexts.jsonrpc] = Config.RpcToken;
                _logger?.LogInformation("Attempting to Authenticate with RPC service...");
                if (!Connection.Authenticate().Result)
                {
                    _logger?.LogInformation("Authentication failed, retrieving JSON-RPC Access Token again...");
                    // Try getting the RPC token again?
                    Config.RpcToken = FastCGI.GetAccessToken(Address.Address, Config.AuthenticationCode, Config.ClientId, Config.ClientSecret, FastCGI.AccessTokenContexts.jsonrpc).Result;
                    Connection.AccessTokens[FastCGI.AccessTokenContexts.jsonrpc] = Config.RpcToken;
                    _logger?.LogInformation("Retrying Authentication with RPC service...");
                    if (!Connection.Authenticate().Result)
                    {
                        throw new Exception("Could not authenticate");
                    }
                }
            }


            _logger?.LogDebug("Shaking hands with machine @{Address}...", Address);
            var handshake = Connection.Handshake()?.Result;
            if (handshake == null)
            {
                var handshakeIncomplete = new Exception("Failed to complete handshake");
                _logger?.LogError(handshakeIncomplete, "Failed to complete handshake @{Address}", Address);
                throw handshakeIncomplete;
            }
            _logger?.LogDebug("Shook hands with machine @{Address}...", Address);

            var info = handshake.result;

            Config.Name = info.machine_name;
            MachineType = info.machine_type;
            VID = info.vid.ToString();
            PID = info.pid;
            ApiVersion = info.api_version;
            Config.Serial = info.iserial;
            SSL = info.ssl_port;
            MotorDriveVersion = info.motor_driver_version;
            BotType = info.bot_type;


            // Authenticate with the JSON-RPC service
            bool isAuthenticated = Connection.Authenticate().Result;
            if (!isAuthenticated) _logger?.LogWarning("Could not authenticate with JSON-RPC service @{Address}", Address);

            // Get machine configuration
            var machineConfig = Connection.GetMachineConfig().Result;
            if (machineConfig != null)
            {
                MachineConfigResponse = machineConfig.result;
            }
        }

        public void Stop()
        {
            _logger?.LogInformation("Stopping machine connection @{Address}...", Address);
            Connection?.Stop();
            _logger?.LogInformation("Stopped machine connection @{Address}", Address);
        }

        public void Dispose()
        {
            Connection?.Dispose();
        }
    }
}
