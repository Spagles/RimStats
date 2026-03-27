using RimWorld.Planet;
using Verse;
using System;
using System.Collections;
using System.IO;

namespace RimStats {
    public class StatsWorldComponent : WorldComponent {
        public StatsWorldComponent(World world) : base(world) {}

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            if (Find.TickManager.TicksGame % 60_000 == 0) RegisterData();
        }

        private void RegisterData() {
            Map map = Find.AnyPlayerHomeMap;

            if (map == null) return;
            if (Scribe.loader == null || Scribe.loader.curPathRelToParent.NullOrEmpty()) return;

            string saveName = Path.GetFileNameWithoutExtension(Scribe.loader.curPathRelToParent);
            float wealth = map.wealthWatcher.WealthTotal;
            int colonists = map.mapPawns.ColonistCount;
            int tick = Find.TickManager.TicksGame;
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            StatsData statsData = new StatsData(saveName, wealth, colonists, tick, timestamp);
            DataBaseManager.InsertData(statsData);
        }
    }
}