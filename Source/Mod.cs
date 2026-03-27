using UnityEngine;
using Verse;
using System.Collections.Generic;

namespace RimStats {
    public class RimStatsMod : Mod {
        public static RimStatsSettings settings;

        public RimStatsMod(ModContentPack content) : base(content) {
            settings = GetSettings<RimStatsSettings>();
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