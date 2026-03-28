using RimWorld.Planet;
using Verse;
using System;
using RimWorld;

namespace RimStats {
    public class StatsWorldComponent : WorldComponent {
        public StatsWorldComponent(World world) : base(world) {}

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();

            int invervalTicks = (int) (RimStatsMod.settings.statsRegisterIntervalDays * 60_000f);
            if (Find.TickManager.TicksGame % invervalTicks == 0) RegisterData();
        }

        private void RegisterData() {
            Map map = Find.AnyPlayerHomeMap;

            if (map == null) return;

            int randSeed = Find.World.ConstantRandSeed;
            string factionName = Faction.OfPlayer.Name;
            float wealth = map.wealthWatcher.WealthTotal;
            int colonists = map.mapPawns.ColonistCount;
            int tick = Find.TickManager.TicksGame;
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            StatsData statsData = new StatsData(randSeed, tick, factionName, wealth, colonists, timestamp);

            if (DataBaseManager.InsertData(statsData, "Stats") && RimStatsMod.settings.registerStatsEnabled) {
                Log.Message($"{RimStatsMod.Prefix} Stats successfully inserted");
            }
        }
    }
}