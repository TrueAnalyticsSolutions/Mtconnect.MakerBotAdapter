[![validate-mtconnect](https://github.com/TrueAnalyticsSolutions/Mtconnect.MakerBotAdapter/actions/workflows/validate-mtconnect.yml/badge.svg)](https://github.com/TrueAnalyticsSolutions/Mtconnect.MakerBotAdapter/actions/workflows/validate-mtconnect.yml)

# MTConnect MakerBot Adapter

An independent MTConnect SHDR adapter for MakerBot printers that expose the MakerBot JSON-RPC service. MakerBot Print is not required. The adapter connects directly over Ethernet or Wi-Fi, authenticates with a saved printer authorization, and publishes printer state through the MTConnect Adapter SDK.

## Projects

- `MakerBot.Rpc` (`netstandard2.0`) contains discovery, pairing, authentication, JSON-RPC transport, DTOs, and MakerBot commands.
- `Mtconnect.MakerBotAdapter` (`netcoreapp3.1`) maps MakerBot state to Adapter SDK source data.
- `MakerBot.Configurator` (`net10.0`) discovers and pairs a printer, then writes `adapterconfig.json`.
- `Terminal` (`net10.0`) hosts one configured source with `Mtconnect.ShdrAdapter`.
- `MakerBot.Tests` contains protocol and configuration tests.

The private `NuGet_TAMS` feed is configured by the repository's `NuGet.Config`. `Mtconnect.*` packages are mapped to that feed.

## Pair a printer

Close MakerBot Print before testing independent operation. The computer and printer must be on networks that permit direct traffic to the printer.

Discover printers and select one interactively:

```powershell
dotnet run --project MakerBot.Configurator
```

Or pair a known address directly:

```powershell
dotnet run --project MakerBot.Configurator -- --address 10.0.0.61 --port 9999 --output adapterconfig.json
```

When prompted, press the printer's control-panel dial. The configurator waits up to 120 seconds, validates the resulting authorization with `get_system_information`, and atomically writes the configuration file.

`adapterconfig.json` contains a plaintext printer authorization code. It is ignored by Git; protect it like a credential. A safe template is available at `Mtconnect.MakerBotAdapter/adapterconfig.example.json`.

## Run the SHDR adapter

Pass an absolute or current-working-directory-relative configuration path:

```powershell
dotnet run --project Terminal -- --config adapterconfig.json
```

If no path is supplied, Terminal uses `adapterconfig.json` in the current directory when present, or prompts for a path. The default SHDR listener is TCP port 7878.

Unattended startup never initiates pairing. If authorization is missing or invalid, run the configurator again. The adapter first tries the configured endpoint, verifies the printer serial during the JSON-RPC handshake, and then attempts rediscovery by serial through mDNS (`_makerbot-jsonrpc._tcp`) and legacy UDP. A disconnect publishes `UNAVAILABLE`; successful authenticated reconnection restores `AVAILABLE`.

## Configuration

One configuration file represents one printer and one adapter source:

```json
{
  "machine": {
    "name": "MakerBot+",
    "serialNumber": "23C1000B3C7059020FBB",
    "address": "10.0.0.61",
    "rpcPort": 9999,
    "sslPort": 12309,
    "authenticationCode": "",
    "clientId": "MakerWare",
    "clientSecret": "MakerBotAgentAdapterCore"
  },
  "pollIntervalMilliseconds": 5000,
  "reconnectIntervalMilliseconds": 10000
}
```

`machine.serialNumber`, `machine.address`, and `machine.authenticationCode` are required for unattended adapter startup. The serial number is used as the stable device identity and prevents a changed address from connecting to the wrong printer.

## Build and test

```powershell
dotnet restore Mtconnect.MakerBotAdapter.sln
dotnet build Mtconnect.MakerBotAdapter.sln --no-restore
dotnet test MakerBot.Tests/MakerBot.Tests.csproj --no-build
```

Thanks to [gryphius/makerbot-gen5-api](https://github.com/gryphius/makerbot-gen5-api), which helped document the MakerBot JSON-RPC workflow.
