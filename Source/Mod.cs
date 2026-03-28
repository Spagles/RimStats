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

            listingStandard.Label($"Registration Interval Days : {settings.statsRegisterIntervalDays}");
            settings.statsRegisterIntervalDays = listingStandard.Slider(settings.statsRegisterIntervalDays, 0.1f, 5.0f);

            listingStandard.CheckboxLabeled("Enable Logging", ref settings.logEnabled);
            listingStandard.CheckboxLabeled("Enable Stats Registration", ref settings.registerStatsEnabled);
            listingStandard.CheckboxLabeled("Enable Events Registration", ref settings.registerEventsEnabled);

            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory() => "RimStats";
    }
}