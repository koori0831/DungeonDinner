using System.Collections.Generic;

namespace Work.NPC.Code.Data
{
    public sealed class NpcData
    {
        public string NpcId { get; }
        public string DisplayName { get; }
        public string Race { get; }
        public string Role { get; }
        public IReadOnlyList<string> PreferredTags { get; }
        public IReadOnlyList<string> PreferredFoodTypes { get; }
        public IReadOnlyList<string> AvoidTags { get; }
        public string Notes { get; }
        public bool RequestAvailable { get; }
        public int RequestUnlockLevel { get; }
        public string RequestUnlockEvent { get; }

        public NpcData(
            string npcId,
            string displayName,
            string race,
            string role,
            IReadOnlyList<string> preferredTags,
            IReadOnlyList<string> preferredFoodTypes,
            IReadOnlyList<string> avoidTags,
            string notes,
            bool requestAvailable,
            int requestUnlockLevel,
            string requestUnlockEvent)
        {
            NpcId = npcId;
            DisplayName = displayName;
            Race = race;
            Role = role;
            PreferredTags = preferredTags;
            PreferredFoodTypes = preferredFoodTypes;
            AvoidTags = avoidTags;
            Notes = notes;
            RequestAvailable = requestAvailable;
            RequestUnlockLevel = requestUnlockLevel;
            RequestUnlockEvent = requestUnlockEvent;
        }

        public static NpcData FromRow(IReadOnlyDictionary<string, string> row)
        {
            string npcId = CsvRowReader.Get(row, "NpcId");
            string displayName = CsvRowReader.Get(row, "DisplayName", npcId);

            return new NpcData(
                npcId,
                displayName,
                CsvRowReader.Get(row, "Race"),
                CsvRowReader.Get(row, "Role"),
                CsvRowReader.GetList(row, "PreferredTags"),
                CsvRowReader.GetList(row, "PreferredFoodTypes"),
                CsvRowReader.GetList(row, "AvoidTags"),
                CsvRowReader.Get(row, "Notes"),
                CsvRowReader.GetBool(row, "RequestAvailable"),
                CsvRowReader.GetInt(row, "RequestUnlockLevel", 5),
                CsvRowReader.Get(row, "RequestUnlockEvent"));
        }
    }
}
