using System;
using System.Collections.Generic;
using Work.NPC.Code.Runtime;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;

namespace Work.Cook.Code.Runtime.Integration
{
    public static class CookingNpcDishAdapter
    {
        public static bool CanSubmitToNpc(
            NpcConversationRunner npcRunner,
            DishResult result,
            out string reason)
        {
            if (npcRunner == null)
            {
                reason = "NpcConversationRunner is missing.";
                return false;
            }

            if (result == null)
            {
                reason = "DishResult is missing.";
                return false;
            }

            if (npcRunner.HasActiveConversation == false)
            {
                reason = "NPC conversation is not active.";
                return false;
            }

            if (npcRunner.IsPlaying)
            {
                reason = "NPC dialogue is still playing.";
                return false;
            }

            if (npcRunner.IsReadyForCooking == false)
            {
                reason = "NPC order is not ready for dish submission.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

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

        public static string BuildSubmissionDebugSummary(DishResult result)
        {
            return ToNpcDishSubmission(result).BuildDebugSummary();
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
            return SubmitToNpc(npcRunner, result, out _);
        }

        public static bool SubmitToNpc(
            NpcConversationRunner npcRunner,
            DishResult result,
            out string reason)
        {
            if (npcRunner == null || result == null)
            {
                CanSubmitToNpc(npcRunner, result, out reason);
                return false;
            }

            if (CanSubmitToNpc(npcRunner, result, out reason) == false)
                return false;

            npcRunner.SubmitDish(ToNpcDishSubmission(result));
            reason = string.Empty;
            return true;
        }

        private static IReadOnlyList<string> BuildTagIds(DishResult result)
        {
            List<string> tagIds = new List<string>();
            HashSet<string> existingTagIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (result == null)
                return tagIds;

            for (int i = 0; i < result.Tags.Count; i++)
            {
                if (result.Tags[i] == null || string.IsNullOrWhiteSpace(result.Tags[i].TagId))
                    continue;

                if (existingTagIds.Add(result.Tags[i].TagId))
                    tagIds.Add(result.Tags[i].TagId);
            }

            return tagIds;
        }
    }
}
