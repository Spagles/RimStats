using RimWorld;

namespace RimStats {
    public class HistoryAutoRecorderWorker_Manual : HistoryAutoRecorderWorker {
        public override float PullRecord() => 0f;
    }

    public class RimStats_HistoryAutoRecorderDef : HistoryAutoRecorderDef {
        public string dataFieldName;
    }
}