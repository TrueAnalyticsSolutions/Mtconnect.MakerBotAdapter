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

        public RpcConnection Connection { get; private set; }

        public IPEndPoint Address => Connection.Endpoint;

        public MachineConfig Config { get; set; } = new MachineConfig();

        public string VID { get; set; }

        public int PID { get; set; }

        public string ApiVersion { get; set; }

        public string FirmwareVersion { get; set; }

        public int SSL { get; set; }

        public string MotorDriveVersion { get; set; }

        public string BotType { get; set; }

        public string MachineType { get; set; }

        public Machine(IPAddress host, int port, ILoggerFactory logFactory = default)
        {
            _logger = logFactory?.CreateLogger<Machine>();

            Config.Address = host.ToString();
            Config.Port = port;

            Connection = new RpcConnection(host, port, logFactory);
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
            Connection?.Start(cancellationToken);
            _logger?.LogInformation("Started machine connection @{Address}", Address);

            if (!Connection.IsAuthenticated)
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
