using Verse;

namespace RimStats {
    public class RimStatsSettings : ModSettings {
        public float statsRegisterIntervalDays = 1f;
        public bool logEnabled = true;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref statsRegisterIntervalDays, "statsRegisterIntervalDays");
            Scribe_Values.Look(ref logEnabled, "logEnabled");
        }


    }
}