using HarmonyLib;
using UnityEngine;
using Verse;

namespace RimStats {
    public class RimStatsMod : Mod {
        public static RimStatsSettings settings;

        public RimStatsMod(ModContentPack content) : base(content) {
            settings = GetSettings<RimStatsSettings>();

            Harmony harmony = new Harmony("Progme.RimStats");
            harmony.PatchAll();

            if (settings.logEnabled) Log.Message("[RimStats] Harmony successfuly patched original game");
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();

            listingStandard.Begin(inRect);

            listingStandard.Label($"Registration Interval Days : {settings.statsRegisterIntervalDays}");
            settings.statsRegisterIntervalDays = listingStandard.Slider(settings.statsRegisterIntervalDays, 0.1f, 5.0f);

            listingStandard.CheckboxLabeled("Enable Logging", ref settings.logEnabled);

            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory() => "RimStats";
    }
}