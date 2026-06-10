using System.Collections.Generic;
using Work.NPC.Code.Runtime;

namespace Work.Cook.Code.Runtime
{
    public static class CookingNpcDishAdapter
    {
        public static NpcDishSubmission ToNpcDishSubmission(DishResult result)
        {
            if (result == null)
                return new NpcDishSubmission(string.Empty, string.Empty, new List<string>());

            return new NpcDishSubmission(
                result.RecipeId,
                result.CategoryId,
                BuildTagIds(result),
                result.IsDisgusting);
        }

        public static bool TryBuildMatchReport(
            NpcConversationRunner npcRunner,
            DishResult result,
            out NpcDishMatchReport report)
        {
            report = null;
            if (npcRunner == null || result == null)
                return false;

            return npcRunner.TryBuildDishMatchReport(ToNpcDishSubmission(result), out report);
        }

        public static bool SubmitToNpc(NpcConversationRunner npcRunner, DishResult result)
        {
            if (npcRunner == null || result == null)
                return false;

            npcRunner.SubmitDish(ToNpcDishSubmission(result));
            return true;
        }

        private static IReadOnlyList<string> BuildTagIds(DishResult result)
        {
            List<string> tagIds = new List<string>();
            if (result == null)
                return tagIds;

            for (int i = 0; i < result.Tags.Count; i++)
            {
                if (result.Tags[i] == null || string.IsNullOrWhiteSpace(result.Tags[i].TagId))
                    continue;

                if (tagIds.Contains(result.Tags[i].TagId) == false)
                    tagIds.Add(result.Tags[i].TagId);
            }

            return tagIds;
        }
    }
}
