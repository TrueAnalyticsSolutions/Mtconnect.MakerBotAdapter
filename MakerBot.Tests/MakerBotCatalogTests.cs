using MakerBot.Rpc;
using Xunit;

namespace MakerBot.Tests;

public class MakerBotCatalogTests
{
    [Fact]
    public void ResolvesExactToolRevision()
    {
        Assert.True(MakerBotCatalog.TryGetTool(14, out var tool));
        Assert.Equal("mk13_impla", tool.Type);
        Assert.Equal("Tough PLA Smart Extruder+", tool.Name);
        Assert.Equal("im-pla", tool.DefaultMaterial);
        Assert.Equal("Tough PLA", tool.DefaultMaterialName);
        Assert.Equal(14, (int)MakerBotToolId.Mark13ToughPla);
    }

    [Theory]
    [InlineData(0, "Unknown Material")]
    [InlineData(1, "PLA")]
    [InlineData(2, "Tough")]
    [InlineData(4, "PETG")]
    [InlineData(8, "SR-30")]
    [InlineData(9, "ASA")]
    public void ResolvesRpcSpoolMaterialCodes(int code, string expected)
    {
        Assert.Equal(expected, MakerBotCatalog.GetSpoolMaterialName(code));
    }

    [Fact]
    public void KeepsToolCapabilityAndSpoolMaterialDomainsSeparate()
    {
        Assert.True(MakerBotCatalog.TryGetTool(1, out var tool));
        Assert.Equal("PLA", tool.DefaultMaterialName);
        Assert.Equal("Unknown Material", MakerBotCatalog.GetSpoolMaterialName(0));
        Assert.Equal("Unknown Material", MakerBotCatalog.GetSpoolMaterialName(MakerBotSpoolMaterialType.GenericModel));
        Assert.NotEqual((int)MakerBotToolMaterial.Pla, (int)MakerBotSpoolMaterialType.Pla);
    }

    [Theory]
    [InlineData(81, "Filament Slip")]
    [InlineData(1041, "Out Of Filament")]
    public void ResolvesReadableErrorNames(int code, string expected)
    {
        var actual = code < 256
            ? MakerBotCatalog.GetToolheadErrorName(code)
            : MakerBotCatalog.GetMachineErrorName(code);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LeavesUnknownValuesUnmapped()
    {
        Assert.False(MakerBotCatalog.TryGetTool(4096, out _));
        Assert.Null(MakerBotCatalog.GetSpoolMaterialName(7));
        Assert.Null(MakerBotCatalog.GetToolheadErrorName(42));
        Assert.Equal("future_bot", MakerBotCatalog.GetPrinterName("future_bot"));
    }
}
