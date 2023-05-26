using System.Collections.Generic;
using System.Linq;
using static MakerBot.Rpc.SystemInformation.Result.Toolheads;

namespace Mtconnect.MakerBotAdapter.Lookups
{
    public static class Materials
    {
        private static List<MakerBotMaterial> _materials = new List<MakerBotMaterial>()
        {
            new MakerBotMaterial()
            {
                Type = "generic_model",
                Name = "Unknown Material"
            },
            new MakerBotMaterial()
            {
                Type = "pla",
                Name = "PLA"
            },
            new MakerBotMaterial()
            {
                Type = "im-pla",
                Name = "Tough PLA"
            },
            new MakerBotMaterial()
            {
                Type = "pva",
                Name = "PVA"
            },
            new MakerBotMaterial()
            {
                Type = "pet",
                Name = "PETG"
            },
            new MakerBotMaterial()
            {
                Type = "abs",
                Name = "ABS"
            },
            new MakerBotMaterial()
            {
                Type = "hips",
                Name = "HIPS"
            },
            new MakerBotMaterial()
            {
                Type = "sr30",
                Name = "SR-30"
            },
            new MakerBotMaterial()
            {
                Type = "asa",
                Name = "ASA"
            },
            new MakerBotMaterial()
            {
                Type = "nylon",
                Name = "Nylon"
            },
            new MakerBotMaterial()
            {
                Type = "pc-abs",
                Name = "PC-ABS"
            },
            new MakerBotMaterial()
            {
                Type = "pc-abs-fr",
                Name = "PC-ABS-FR"
            },
            new MakerBotMaterial()
            {
                Type = "nylon-cf",
                Name = "Nylon Carbon Fiber"
            },
            new MakerBotMaterial()
            {
                Type = "nylon12-cf",
                Name = "Nylon 12 Carbon Fiber"
            },
            new MakerBotMaterial()
            {
                Type = "im-pla-esd",
                Name = "ESD"
            }
        };

        public static string GetNameByType(string id)
        {
            return _materials.Where(o => o.Type == id).Select(o => o.Name).FirstOrDefault();
        }
    }
    public struct MakerBotMaterial
    {
        public string Type { get; set; }

        public string Name { get; set; }
    }
}
