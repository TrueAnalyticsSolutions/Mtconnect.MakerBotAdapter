using System.Collections.Generic;
using System.Linq;
using static MakerBot.Rpc.SystemInformation.Result.Toolheads;

namespace Mtconnect.MakerBotAdapter.Lookups
{
    public static class Models
    {
        private static List<MakerBotModel> _models = new List<MakerBotModel>()
        {
            new MakerBotModel()
            {
                Codename = "wildduck",
                Name = "Replicator Mini+",
                BotType = "mini_8",
                Generation = "5",
                PID = "0008"
            },
            new MakerBotModel()
            {
                Codename = "platypus",
                Name = "Replicator 5th Gen",
                BotType = "replicator_5",
                Generation = "5",
                PID = "0005"
            },
            new MakerBotModel()
            {
                Codename = "horseshoe",
                Name = "Replicator+",
                BotType = "replicator_b",
                Generation = "5",
                PID = "000b"
            },
            new MakerBotModel()
            {
                Codename = "moose",
                Name = "Replicator Z18",
                BotType = "z18_6",
                Generation = "5",
                PID = "0006"
            },
            new MakerBotModel()
            {
                Codename = "sombrero_fire",
                Name = "Method",
                BotType = "fire_e",
                Generation = "6",
                PID = "000e"
            },
            new MakerBotModel()
            {
                Codename = "sombrero_lava",
                Name = "Method X",
                BotType = "lava_f",
                Generation = "6",
                PID = "000f"
            },
            new MakerBotModel()
            {
                Codename = "whitesmith",
                Name = "Sketch",
                BotType = "sketch",
                Generation = "5",
                PID = "1627"
            }
        };

        public static string GetNameByType(string type)
        {
            return _models.Where(o => o.BotType == type).Select(o => o.Name).FirstOrDefault();
        }
        public static string GetNameByCodename(string codename)
        {
            return _models.Where(o => o.Codename == codename).Select(o => o.Name).FirstOrDefault();
        }
    }
    public struct MakerBotModel
    {
        public string Codename;
        public string Name;
        public string BotType;
        public string Generation;
        public string PID;
    }
}
