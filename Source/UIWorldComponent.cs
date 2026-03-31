using Verse;
using System.Linq;
using RimWorld.Planet;
using System.Collections.Generic;

namespace RimStats {
    public class UIWorldComponent : WorldComponent {
        public UIWorldComponent(World world) : base(world) {}

        private void DrawGraph()
        {
            var group = Current.Game.history.Groups().FirstOrDefault(x => x.def.defName == "RimStats_WealthPerColonistGroup");
            if (group == null || group.recorders.Count == 0) return;

            var recorder = group.recorders[0];
            recorder.records.Clear();

            List<StatsData> rawData = DataBaseManager.ExtractData<StatsData>(Find.World.ConstantRandSeed);

            List<float> data = GetWealthPerColonist(rawData);

            if (data == null || data.Count == 0) return;
            recorder.records.AddRange(data);
        }

        private List<float> GetWealthPerColonist(List<StatsData> rawData) {
            List<float> data = new List<float>();

            foreach (StatsData record in rawData) {
                float value = record.colonists > 0 ? record.wealth / record.colonists : 0f;
                data.Add(value);
            }

            return data;
        }

    }
}