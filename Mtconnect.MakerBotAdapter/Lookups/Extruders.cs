using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mtconnect.MakerBotAdapter.Lookups
{
    public static class Extruders
    {
        private static List<MakerBotExtruder> _extruders = new List<MakerBotExtruder>()
        {
            new MakerBotExtruder()
            {
                Type = "mk12",
                Name = "Smart Extruder"
            },
            new MakerBotExtruder()
            {
                Type = "mk13",
                Name = "Smart Extruder+"
            },
            new MakerBotExtruder()
            {
                Type = "mk13_impla",
                Name = "Tough Smart Extruder+"
            },
            new MakerBotExtruder()
            {
                Type = "mk13_experimental",
                Name = "Experimental Extruder"
            },
            new MakerBotExtruder()
            {
                Type = "mk14",
                Name = "Model 1A"
            },
            new MakerBotExtruder()
            {
                Type = "mk14_s",
                Name = "Support 2A"
            },
            new MakerBotExtruder()
            {
                Type = "mk14_hot",
                Name = "Model 1XA"
            },
            new MakerBotExtruder()
            {
                Type = "mk14_hot_s",
                Name = "Support 2XA"
            },
            new MakerBotExtruder()
            {
                Type = "mk14_e",
                Name = "LABS Extruder"
            },
            new MakerBotExtruder()
            {
                Type = "mk14_c",
                Name = "Model 1C"
            },
            new MakerBotExtruder()
            {
                Type = "sketch_extruder",
                Name = "Sketch Extruder"
            }
        };

        public static string GetNameByType(string type)
        {
            return _extruders.Where(o => o.Type == type).Select(o => o.Name).FirstOrDefault();
        }
    }
    public struct MakerBotExtruder
    {
        public string Type { get; set; }

        public string Name { get; set; }
    }
}
