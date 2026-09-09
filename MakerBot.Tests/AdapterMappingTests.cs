using Mtconnect.MakerBotAdapter;
using Newtonsoft.Json.Linq;
using Xunit;

namespace MakerBot.Tests;

public class AdapterMappingTests
{
    [Fact]
    public void MapsCapturedSystemInformationAndErrors()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "{\"machine\":{\"name\":\"MakerBot+\",\"serialNumber\":\"SERIAL\",\"address\":\"10.0.0.61\",\"rpcPort\":9999,\"sslPort\":12309,\"authenticationCode\":\"redacted-test-value\"}}");
            using var adapter = new MakerBotRPCAdapter(path);
            Assert.NotNull(adapter.GetSourceDescriptor());
            Assert.Equal("CLOSED", adapter.CurrentModel.ConnectionStatus.Value?.ToString());

            var alarmChanges = 0;
            var extruder1ToolErrorChanges = 0;
            adapter.CurrentModel.Alarm.OnDataItemChanged += (_, _) => alarmChanges++;
            adapter.CurrentModel.Extruder1.ToolError.OnDataItemChanged += (_, _) => extruder1ToolErrorChanges++;

            var normalPayload = JObject.Parse("""
            {
              "jsonrpc":"2.0",
              "id":7,
              "result":{
                "ip":"10.0.0.61",
                "toolheads":{"extruder":[
                  {"index":0,"current_temperature":23,"target_temperature":215,"tool_id":14,"error":0},
                  {"index":1,"current_temperature":24,"target_temperature":0,"tool_id":15,"error":42}
                ]},
                "current_process":{"id":73,"name":"calibration.makerbot","elapsed_time":42,"complete":false,"cancelled":false}
              }
            }
            """);
            adapter.ProcessPayload(normalPayload);
            adapter.ProcessToolOffset(0.125f);

            var observed = adapter.CurrentModel;
            Assert.Equal("ESTABLISHED", observed.ConnectionStatus.Value?.ToString());
            Assert.Equal("AVAILABLE", observed.Availability.Value?.ToString());
            Assert.Equal("ACTIVE", observed.Execution.Value?.ToString());
            Assert.Equal("calibration.makerbot", observed.Program.Value?.ToString());
            Assert.Equal("73", observed.ProcessOccurrenceId.Value?.ToString());
            Assert.Equal(42f, Convert.ToSingle(observed.ProcessTimer.Value));
            Assert.Equal(23f, Convert.ToSingle(observed.Extruder1.CurrentTemperature.Value));
            Assert.Equal(215f, Convert.ToSingle(observed.Extruder1.TargetTemperature.Value));
            Assert.Equal("14", observed.Extruder1.ToolNumber.Value?.ToString());
            Assert.Equal("mk13_impla", observed.Extruder1.ToolType.Value?.ToString());
            Assert.Equal("Tough PLA Smart Extruder+", observed.Extruder1.ToolName.Value?.ToString());
            Assert.Equal("Tough PLA", observed.Extruder1.ToolMaterial.Value?.ToString());
            Assert.Contains("NORMAL", observed.Extruder1.ToolError.ToString());
            Assert.Contains("42", observed.Extruder2.ToolError.ToString());
            Assert.Contains("Unknown MakerBot toolhead error 42", observed.Extruder2.ToolError.ToString());
            Assert.Contains("NORMAL", observed.Alarm.ToString());
            Assert.Equal(0.125f, Convert.ToSingle(observed.ToolOffset.Value));

            var normalAlarmChanges = alarmChanges;
            var normalToolErrorChanges = extruder1ToolErrorChanges;
            adapter.ProcessPayload(normalPayload);
            Assert.Equal(normalAlarmChanges, alarmChanges);
            Assert.Equal(normalToolErrorChanges, extruder1ToolErrorChanges);

            var faultPayload = JObject.Parse("""
            {
              "result": {
                "toolheads": {"extruder":[{"index":0,"tool_id":14,"error":81}]},
                "current_process": {
                  "id":73,
                  "name":"calibration.makerbot",
                  "elapsed_time":43,
                  "complete":false,
                  "cancelled":false,
                  "error":{"code":1041,"message":"Filament is required"}
                }
              }
            }
            """);
            adapter.ProcessPayload(faultPayload);
            Assert.Contains("81", observed.Extruder1.ToolError.ToString());
            Assert.Contains("Filament Slip", observed.Extruder1.ToolError.ToString());
            Assert.Contains("1041", observed.Alarm.ToString());
            Assert.Contains("Out Of Filament: Filament is required", observed.Alarm.ToString());

            var faultAlarmChanges = alarmChanges;
            var faultToolErrorChanges = extruder1ToolErrorChanges;
            adapter.ProcessPayload(faultPayload);
            Assert.Equal(faultAlarmChanges, alarmChanges);
            Assert.Equal(faultToolErrorChanges, extruder1ToolErrorChanges);

            adapter.ProcessPayload(normalPayload);
            Assert.True(alarmChanges > faultAlarmChanges);
            Assert.True(extruder1ToolErrorChanges > faultToolErrorChanges);
            var clearedAlarmChanges = alarmChanges;
            var clearedToolErrorChanges = extruder1ToolErrorChanges;
            adapter.ProcessPayload(normalPayload);
            Assert.Equal(clearedAlarmChanges, alarmChanges);
            Assert.Equal(clearedToolErrorChanges, extruder1ToolErrorChanges);

            adapter.ProcessPayload(JObject.Parse("{\"error\":{\"message\":\"not authenticated\"}}"));
            Assert.Contains("RPC_ERROR", observed.Alarm.ToString());
            Assert.Contains("not authenticated", observed.Alarm.ToString());

            adapter.SetUnavailable();
            Assert.Equal("CLOSED", observed.ConnectionStatus.Value?.ToString());
            Assert.Equal("UNAVAILABLE", observed.Availability.Value?.ToString());
        }
        finally { File.Delete(path); }
    }
}
