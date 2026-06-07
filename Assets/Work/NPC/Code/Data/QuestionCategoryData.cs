using System.Collections.Generic;

namespace Work.NPC.Code.Data
{
    public sealed class QuestionCategoryData
    {
        public string CategoryId { get; }
        public string DisplayName { get; }
        public string DialogueGroup { get; }

        public QuestionCategoryData(string categoryId, string displayName, string dialogueGroup)
        {
            CategoryId = categoryId;
            DisplayName = displayName;
            DialogueGroup = dialogueGroup;
        }

        public static QuestionCategoryData FromRow(IReadOnlyDictionary<string, string> row)
        {
            string categoryId = CsvRowReader.Get(row, "CategoryId");
            string displayName = CsvRowReader.Get(row, "DisplayName", categoryId);
            string dialogueGroup = CsvRowReader.Get(row, "DialogueGroup", $"Question_{categoryId}");

            return new QuestionCategoryData(categoryId, displayName, dialogueGroup);
        }
    }

    public static class NpcQuestionCategoryIds
    {
        public const string Taste = "Taste";
        public const string TextureTemp = "TextureTemp";
        public const string Condition = "Condition";
        public const string Avoid = "Avoid";
    }
}
