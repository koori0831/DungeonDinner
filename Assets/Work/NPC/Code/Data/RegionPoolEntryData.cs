using System.Collections.Generic;

namespace Work.NPC.Code.Data
{
    public sealed class RegionPoolEntryData
    {
        public string RegionId { get; }
        public string NpcId { get; }
        public int Weight { get; }
        public int MinDay { get; }
        public int CooldownDays { get; }
        public string PoolType { get; }
        public string Condition { get; }

        public RegionPoolEntryData(
            string regionId,
            string npcId,
            int weight,
            int minDay,
            int cooldownDays,
            string poolType,
            string condition)
        {
            RegionId = regionId;
            NpcId = npcId;
            Weight = weight;
            MinDay = minDay;
            CooldownDays = cooldownDays;
            PoolType = poolType;
            Condition = condition;
        }

        public static RegionPoolEntryData FromRow(IReadOnlyDictionary<string, string> row)
        {
            return new RegionPoolEntryData(
                CsvRowReader.Get(row, "RegionId"),
                CsvRowReader.Get(row, "NpcId"),
                CsvRowReader.GetInt(row, "Weight", 1),
                CsvRowReader.GetInt(row, "MinDay", 1),
                CsvRowReader.GetInt(row, "CooldownDays", 2),
                CsvRowReader.Get(row, "PoolType", "Normal"),
                CsvRowReader.Get(row, "Condition"));
        }
    }
}
