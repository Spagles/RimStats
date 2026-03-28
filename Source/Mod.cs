using HarmonyLib;
using UnityEngine;
using Verse;

namespace RimStats {
    public class RimStatsMod : Mod {
        public static RimStatsSettings settings;
        public const string Prefix = "[RimStats]";
        public const string Id = "Progme.RimStats";

        public RimStatsMod(ModContentPack content) : base(content) {
            settings = GetSettings<RimStatsSettings>();

            Harmony harmony = new Harmony(Id);
            harmony.PatchAll();

            if (settings.logEnabled) Log.Message($"{Prefix} Harmony successfuly patched original game");
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();

            listingStandard.Begin(inRect);
            
            listingStandard.CheckboxLabeled("RimStats_StatsRegistration".Translate(), ref settings.registerStatsEnabled);
            listingStandard.CheckboxLabeled("RimStats_EventsRegistration".Translate(), ref settings.registerEventsEnabled);
            listingStandard.Label($"{"RimStats_RegistrationInterval".Translate()} : {settings.statsRegisterIntervalDays}");
            settings.statsRegisterIntervalDays = listingStandard.Slider(settings.statsRegisterIntervalDays, 0.1f, 5.0f);

            listingStandard.CheckboxLabeled("RimStats_EnableLogging".Translate(), ref settings.logEnabled);

            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory() => "RimStats";
    }
}