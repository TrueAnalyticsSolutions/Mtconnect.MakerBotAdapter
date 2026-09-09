# MakerBot local protocol notes

These notes preserve the useful parts of the retired `MakerBotAgentAdapterCore` protocol resources. The executable wrappers and DTOs in this project are authoritative.

## Discovery and endpoints

- mDNS service: `_makerbot-jsonrpc._tcp`
- Legacy UDP discovery: send to port `12307`, listen on `12308`, source port `12309`
- JSON-RPC: plain TCP, normally port `9999`
- Pairing and token exchange: HTTP `/auth`, normally port `80`
- Camera and upload contexts use separate access tokens; the handshake commonly advertises SSL port `12309`

Discovery data includes `machine_name`, `iserial`, `ip`, `port`, `ssl_port`, `machine_type`, `bot_type`, and `firmware_version`. Treat `iserial` as the stable identity; addresses can change on Wi-Fi.

## Pairing flow

1. Open JSON-RPC and call `handshake` without authentication.
2. Request `/auth?response_type=code` with `client_id` and `client_secret`.
3. Ask the user to approve the request with the printer dial.
4. Poll `/auth?response_type=answer&answer_code=...` until accepted or rejected.
5. Exchange the returned authorization code through `/auth?response_type=token&context=jsonrpc`.
6. Call JSON-RPC `authenticate` with the access token.

The long-lived authorization code may be reused to obtain a fresh short-lived access token. It is therefore a credential and must not be logged or committed.

## Message framing

JSON-RPC messages may be fragmented across TCP reads or combined in one read. Correlate responses by numeric `id`; messages without a matching `id` are notifications. Do not assume that a newline maps one-to-one to a message.

The printer's HTTP service may return `Transfer-Encoding: chunked`, so callers must decode HTTP chunks before parsing JSON.

## Command families observed in the legacy implementation

- Session: `handshake`, `authenticate`, `ping`, `get_system_information`, `get_config`
- Authorization: `authorize`, `deauthorize`, `reauthorize`, `get_authorized`, `clear_authorized`
- State: `network_state`, `get_machine_config`, `get_queue_status`, `get_statistics`, `get_persistent_statistics`
- Printing: `print`, `print_again`, `cancel`, `open_queue`, `close_queue`, `clear_queue`, `execute_queue`
- Motion/calibration: `home`, `park`, `assisted_level`, `manual_level`, `calibrate_z_offset`, `get_z_adjusted_offset`, `set_z_adjusted_offset`
- Tooling: `preheat`, `cool`, `load_filament`, `unload_filament`, `get_tool_usage_stats`
- Wi-Fi: `wifi_scan`, `wifi_connect`, `wifi_disconnect`, `wifi_forget`, `wifi_enable`, `wifi_disable`, `wifi_reset`
- Maintenance: `run_diagnostics`, `download_and_install_firmware`, `zip_logs`, `reset_to_factory`

Some legacy commands were never validated against hardware. Keep safety-sensitive or state-changing wrappers clearly identified until tested on the intended printer generation.

## Stable identifier catalogs

`MakerBotCatalog` contains local copies of stable protocol mappings found in the
generated MakerBot support files distributed with MakerBot Print. MakerBot Print
is a research/provenance source only; this library never loads it at runtime.

The tool table maps exact numeric hardware revisions to a canonical tool family,
display name, and default material capability. For example, tool ID `14` is
`mk13_impla`, displayed as **Tough PLA Smart Extruder+**, with Tough PLA as its
default capability. Call `MakerBotCatalog.TryGetTool` and retain the raw number
when a future firmware reports an unknown ID. When available, a printer's live
`get_machine_config.extruder_profiles.supported_extruders` response remains the
preferred source for forward-compatible canonical tool-family resolution.

Do not confuse the two material number domains:

- A tool's `DefaultMaterial` describes the hardware's default material
  capability. `MakerBotToolMaterial` represents the generated hardware enum. It
  does not prove which spool is loaded.
- RPC spool `material_type` uses `0` = unknown/generic, `1` = PLA, `2` = Tough,
  `3` = PVA, `4` = PETG, `5` = ABS, `6` = HIPS, `8` = SR-30, and `9` = ASA.
  `MakerBotSpoolMaterialType` and `GetSpoolMaterialName` represent this domain.
  Code `7` was not defined in the inspected MakerBot Print support package and
  is intentionally left unknown.

The catalog also maps known `bot_type` strings and the generated machine and
toolhead error enums. Unknown IDs and errors intentionally return no mapping so
callers can preserve their numeric value instead of assigning a misleading name.

### Provenance

The copied tables were inspected in MakerBot Print's unpacked
`MB-support-plugin` package:

- `mb_ir/include/bwcoreutils/tool_mappings.hh` — exact tool IDs, names, types,
  and tool-default materials.
- `lib/constants.js` — printer display names, canonical extruder names, and RPC
  spool-material/display-name mappings.
- `mb_ir/include/bwcoreutils/toolhead_errors.hh` — toolhead error codes.
- `mb_ir/include/bwcoreutils/machine_errors.hh` — machine error codes.

The inspected package includes an explicit firmware compatibility exception for
tool ID `99`, which is normalized to `mk13_experimental` (Experimental Extruder).
