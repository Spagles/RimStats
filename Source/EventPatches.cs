using Verse;
using HarmonyLib;
using RimWorld;

namespace RimStats {
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_Pawn_Kill {
        public static void Postfix(Pawn __instance) {
            if (!RimStatsMod.settings.registerEventsEnabled) return;
            if (__instance == null || !__instance.IsColonist) return;

            int randSeed = Find.World.ConstantRandSeed;
            int tick = Find.TickManager.TicksGame;

            string victimName = __instance.LabelShort;
            string deathCause = __instance.health?.summaryHealth?.SummaryHealthPercent < 0.01f ? "Critical damage" : "Blood loss/injury";

            EventData deathData = new EventData(
                randSeed,
                tick,
                eventType : "Death",
                importance : "Critical",
                eventLabel : $"Death: {victimName}",
                details: deathCause
            );

            if (DatabaseManager.InsertData(deathData) && RimStatsMod.settings.logEnabled) {
                Log.Message($"{RimStatsMod.Prefix} Colonist {victimName} death registered");
            }
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_Raid), "TryExecuteWorker")]
    public static class Patch_Raid_Start {
        public static void Postfix(IncidentParms parms, bool __result) {
            if (!RimStatsMod.settings.registerEventsEnabled) return;

            if (!__result) return;
            if (!(parms.target is Map)) return;

            int randSeed = Find.World.ConstantRandSeed;
            int tick = Find.TickManager.TicksGame;

            string factionName = parms.faction?.Name ?? "Unknown Enemies";
            float points = parms.points;
            string strategy = parms.raidStrategy?.defName ?? "Usual Attack";

            EventData raidEvent = new EventData(
                randSeed,
                tick,
                eventType: "Raid",
                importance: "Critical",
                eventLabel: $"Raid: {factionName}",
                details: $"Power: {points:F0} points. Strategy: {strategy}. Entry point: {parms.spawnCenter}"
            );

            if (DatabaseManager.InsertData(raidEvent) && RimStatsMod.settings.logEnabled) {
                Log.Message($"{RimStatsMod.Prefix} Incomming raid of {factionName} event registered");
            }
        }
    }

    [HarmonyPatch(typeof(InteractionWorker_RecruitAttempt), nameof(InteractionWorker_RecruitAttempt.Interacted))]
    public static class Patch_Recruit
    {
        public static void Prefix(Pawn recipient, out bool __state)
        {
            __state = recipient != null && recipient.IsPrisoner;
        }

        public static void Postfix(Pawn initiator, Pawn recipient, bool __state)
        {
            if (!RimStatsMod.settings.registerEventsEnabled) return;
            if (initiator == null || recipient == null) return;

            bool recruitmentSuccess = __state && !recipient.IsPrisoner && recipient.Faction == Faction.OfPlayer;
            if (!recruitmentSuccess) return;

            int randSeed = Find.World.info.Seed;
            int tick = Find.TickManager.TicksGame;

            string recruiterName = initiator.LabelShort;
            string newColonistName = recipient.LabelShort;

            EventData recruitEvent = new EventData(
                randSeed: randSeed,
                tick: tick,
                eventType: "Recruit",
                eventLabel: $"New colonist: {newColonistName}",
                importance: "High",
                details: $"Successfully recruited {newColonistName}. Recruiter: {recruiterName}"
            );

            if (DatabaseManager.InsertData(recruitEvent) && RimStatsMod.settings.logEnabled) {
                Log.Message($"{RimStatsMod.Prefix} Recruitment of {newColonistName} registered");
            }
        }
    }

    [HarmonyPatch(typeof(Tradeable), nameof(Tradeable.ResolveTrade))]
    public static class Patch_Trade_Resolve {
        private static int lastTradeTick = -1;

        public static void Postfix() {
            if (Find.TickManager.TicksGame == lastTradeTick) return;

            lastTradeTick = Find.TickManager.TicksGame;

            string traderName = TradeSession.trader?.TraderName ?? "Trader";
            string factionName = TradeSession.trader?.Faction?.Name ?? "Caravan";

            EventData tradeEvent = new EventData(
                Find.World.ConstantRandSeed,
                tick: Find.TickManager.TicksGame,
                eventLabel: $"Deal: {traderName}",
                eventType: "Trade",
                importance: "Medium",
                details: $"Trade with faction {factionName}. Place: {Find.CurrentMap?.Parent?.LabelCap ?? "Planet"}"
            );

            if (RimStatsMod.settings.registerEventsEnabled && DatabaseManager.InsertData(tradeEvent)) {
                Log.Message($"{RimStatsMod.Prefix} Trade with {factionName} registered");
            }
        }
    }
}