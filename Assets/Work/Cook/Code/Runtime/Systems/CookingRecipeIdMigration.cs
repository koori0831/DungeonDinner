using System;
using System.Collections.Generic;
using System.Text;

namespace Work.Cook.Code.Runtime.Systems
{
    /// <summary>Retains soup knowledge authored before its permanent recipe ID was assigned.</summary>
    internal static class CookingRecipeIdMigration
    {
        private const string LegacySoupId = "NewRecipe";
        private const string SoupId = "mushroom_soup_pane";

        public static void Apply(CookingKnowledgeSaveData data)
        {
            if (data == null)
                return;

            RenameIds(data.discoveredRecipeIds);
            RenameIds(data.attemptedRecipeIds);
            if (data.knownRecipeTags != null)
                foreach (KnownRecipeTagSaveData entry in data.knownRecipeTags)
                    if (entry != null)
                        entry.recipeId = Rename(entry.recipeId);
            if (data.knownRecipeVariants != null)
                foreach (KnownRecipeVariantSaveData entry in data.knownRecipeVariants)
                {
                    if (entry == null)
                        continue;
                    entry.recipeId = Rename(entry.recipeId);
                    if (entry.variantKeys != null)
                        for (int i = 0; i < entry.variantKeys.Count; i++)
                            entry.variantKeys[i] = RenameLegacyKey(entry.variantKeys[i]);
                }

            if (data.recipeRecords == null)
                return;
            foreach (KnownRecipeRecord recipe in data.recipeRecords)
            {
                if (recipe == null || !IsLegacy(recipe.recipeId))
                    continue;
                recipe.recipeId = SoupId;
                if (recipe.variants == null)
                    continue;
                foreach (KnownRecipeVariantRecord variant in recipe.variants)
                {
                    if (variant == null)
                        continue;
                    variant.legacyVariantKey = RenameLegacyKey(variant.legacyVariantKey);
                    // V2 variant hashes include recipeId. Re-key without changing the
                    // recorded ingredients, preparation effects, counts, or replay data.
                    if (variant.variantId != null
                        && variant.variantId.StartsWith("variant_", StringComparison.Ordinal)
                        && variant.identityComponents != null && variant.identityComponents.Count > 0)
                        variant.variantId = BuildVariantId(variant.identityComponents);
                }
            }
        }

        private static string BuildVariantId(IReadOnlyList<VariantComponentRecord> components)
        {
            var keys = new List<string>();
            foreach (VariantComponentRecord component in components)
            {
                if (component == null)
                    continue;
                keys.Add(Part(component.requirementId) + ":" + (int)component.kind
                    + ":" + Part(component.ingredientId) + ":" + Part(component.preparationOptionId)
                    + ":" + Part(component.variantEffectId));
            }
            keys.Sort(StringComparer.OrdinalIgnoreCase);
            var source = new StringBuilder(SoupId);
            foreach (string key in keys)
                source.Append('|').Append(key);
            // Must match CookingVariantIdentityBuilder's FNV-1a V2 identity format.
            unchecked
            {
                ulong hash = 14695981039346656037UL;
                for (int i = 0; i < source.Length; i++)
                {
                    hash ^= source[i];
                    hash *= 1099511628211UL;
                }
                return "variant_" + hash.ToString("x16");
            }
        }

        private static void RenameIds(List<string> ids)
        {
            if (ids == null)
                return;
            for (int i = 0; i < ids.Count; i++)
                ids[i] = Rename(ids[i]);
        }

        private static string RenameLegacyKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return key;
            int separator = key.IndexOf('|');
            return separator >= 0 && IsLegacy(key.Substring(0, separator))
                ? SoupId + key.Substring(separator) : key;
        }

        private static bool IsLegacy(string id) => string.Equals(id?.Trim(), LegacySoupId, StringComparison.OrdinalIgnoreCase);
        private static string Rename(string id) => IsLegacy(id) ? SoupId : id;
        private static string Part(string value) => string.IsNullOrWhiteSpace(value) ? "none" : value.Trim().ToLowerInvariant();
    }
}
