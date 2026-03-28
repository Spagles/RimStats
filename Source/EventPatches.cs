using Verse;
using HarmonyLib;
using UnityEngine;
using RimWorld;
using System.Diagnostics;
using RimWorld.BaseGen;

namespace RimStats {
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
    public static class Patch_Pawn_Kill {
        public static void Postfix(Pawn __instance) {
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

            DataBaseManager.InsertData<EventData>(deathData, "Events");
            if (RimStatsMod.settings.logEnabled) Log.Message($"[RimStats] Colonist {victimName} death reported");
        }
    }

    [HarmonyPatch(typeof(IncidentWorker_Raid), "TryExecuteWorker")]
    public static class Patch_Raid_Start {
        public static void Postfix(IncidentParms parms, bool __result) {
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

            DataBaseManager.InsertData(raidEvent, "Events");
            if (RimStatsMod.settings.logEnabled) Log.Message($"[RimStats] Incomming raid of {factionName} event reported");
        }
    }

    /*
    [HarmonyPatch(typeof(InteractionWorker_RecruitAttempt), nameof(InteractionWorker_RecruitAttempt.Interacted))]
    public static class Patch_Rectuit {
        public static void Prefix(Pawn recipient, out bool __state) {
            __state = (recipient?.Faction != Faction.OfPlayer);
        }

        public static void Postfix(Pawn initiator, Pawn recipient, bool __state) {
            if (initiator == null || recipient == null) return;
            if (!__state || recipient.Faction != Faction.OfPlayer) return;

            int randSeed = Find.World.ConstantRandSeed;
            int tick = Find.TickManager.TicksGame;

            string recruterName = initiator.Name.ToStringShort;
            string newColonistName = recipient.Name.ToStringShort;

            EventData recruitEvent = new EventData(
                randSeed,
                tick,
                eventType: "Recruit",
                eventLabel: $"New colonist: {newColonistName}",
                importance: "High",
                details: $"Successfully recruted prisoner {newColonistName}. Interogator: {recruterName}"
            );

            DataBaseManager.InsertData(recruitEvent, "Events");
            if (RimStatsMod.settings.logEnabled) Log.Message($"[RimStats] Recruitement of {newColonistName} reported");
        }
    }
    */

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

            DataBaseManager.InsertData(tradeEvent, "Events");
            if (RimStatsMod.settings.logEnabled) Log.Message($"[RimStats] Trade with {factionName} reported");
        }
    }
}