using Verse;
using System.Linq;
using RimWorld.Planet;
using System.Collections.Generic;
using RimWorld;
using System.Reflection;
using System;

namespace RimStats {
    public class UIWorldComponent : WorldComponent {
        public UIWorldComponent(World world) : base(world) {}

        public override void FinalizeInit(bool fromLoad)
        {
            base.FinalizeInit(fromLoad);

            CreateGraphs();
            UpdateGraphs(); 
        }

        private void CreateGraphs()
        {
            var groupDef = DefDatabase<HistoryAutoRecorderGroupDef>.GetNamed("RimStats_WealthPerColonistGroup", false);
            if (groupDef == null) return;
            groupDef.label = groupDef.label.Translate();

            if (Current.Game.history.Groups().Any(x => x.def == groupDef)) return;

            HistoryAutoRecorderGroup newGroup = new HistoryAutoRecorderGroup { def = groupDef };

            var myRecorders = DefDatabase<HistoryAutoRecorderDef>.AllDefsListForReading
                                .Where(d => d is RimStats_HistoryAutoRecorderDef)
                                .Cast<RimStats_HistoryAutoRecorderDef>() // Приводим к нашему типу
                                .ToList();

            foreach (var recDef in myRecorders) {
                if (!recDef.label.NullOrEmpty()) {
                    recDef.label = recDef.label.Translate();
                }

                newGroup.recorders.Add(new HistoryAutoRecorder {
                    def = recDef,
                    records = new List<float>()
                });
            }

            Current.Game.history.Groups().Add(newGroup);
        }

        public void UpdateGraphs()
        {
            int worldSeed = Find.World.info.Seed; 
            List<StatsData> data = DatabaseManager.ExtractData<StatsData>(worldSeed);
            
            if (data == null || data.Count == 0) {
                Log.Warning("[RimStats] Data inside of data base were not found for this seed: " + worldSeed);
                return;
            }

            var group = Current.Game.history.Groups().FirstOrDefault(x => x.def.defName == "RimStats_WealthPerColonistGroup");
            if (group == null) return;

            foreach (var recorder in group.recorders) {
                if (recorder.def is RimStats_HistoryAutoRecorderDef def) {
                    FieldInfo fieldInfo = typeof(StatsData).GetField(def.dataFieldName);
                    
                    if (fieldInfo == null) {
                        Log.Error($"[RimStats] The field {def.dataFieldName} not found in StatsData!");
                        continue;
                    }

                    recorder.records.Clear();
                    foreach (var dataRow in data) {
                        float rawValue = Convert.ToSingle(fieldInfo.GetValue(dataRow));
                        float perColonist = dataRow.colonists > 0 ? rawValue / dataRow.colonists : 0f;
                        
                        recorder.records.Add(perColonist);
                    }
                }
            }
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (Find.TickManager.TicksGame % 30000 == 0) UpdateGraphs();
        }
    }
}