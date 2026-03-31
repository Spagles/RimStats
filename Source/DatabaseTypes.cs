using System.Collections.Generic;
using System.Reflection;

namespace RimStats {
    public abstract class BaseData {
        public int randSeed;
        public int tick;
        
        public BaseData() {}

        protected BaseData(int randSeed, int tick) {
            this.randSeed = randSeed;
            this.tick = tick;
        }

        public Dictionary<string, object> ToDictionary() {
            var dict = new Dictionary<string, object>();
            FieldInfo[] fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (FieldInfo field in fields) {
                dict.Add(field.Name, field.GetValue(this));
            }

            return dict;
        }
    }
    public class EventData : BaseData {
        public readonly string eventType;
        public readonly string eventLabel;
        public readonly string importance;
        public readonly string details;

        public EventData(int randSeed, int tick, string eventType, string importance, string eventLabel, string details) : base(randSeed, tick) {
            this.eventLabel = eventLabel;
            this.eventType = eventType;
            this.importance = importance;
            this.details = details;
        }
    }

    public class StatsData : BaseData {
        public readonly string factionName;
        public readonly float wealth;
        public readonly float wealthItems;
        public readonly float wealthBuildings;
        public readonly int colonists;
        public readonly string timestamp;

        public StatsData(int randSeed, int tick, string factionName, float wealth, float wealthItems, float wealthBuildings, int colonists, string timestamp) : base(randSeed, tick) {
            this.factionName = factionName;
            this.wealth = wealth;
            this.colonists = colonists;
            this.timestamp = timestamp;
            this.wealthItems = wealthItems;
            this.wealthBuildings = wealthBuildings;
        }
    }
}