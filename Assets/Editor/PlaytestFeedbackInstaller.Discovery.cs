using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Work.Adventure.Code;
using Work.Adventure.Code.DialogMethod;
using Work.Cook.Code.Runtime.Systems;

public static partial class PlaytestFeedbackInstaller
{
    private static void ConfigureDiscovery()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:AdventureEventSO"))
        {
            var evt = AssetDatabase.LoadAssetAtPath<AdventureEventSO>(AssetDatabase.GUIDToAssetPath(guid));
            foreach (var line in evt.dialogDatas) Observe(line);
            var visited = new HashSet<Options>();
            void Visit(Options option)
            {
                if (option == null || !visited.Add(option)) return;
                foreach (var line in option.ResultdialogDatas) Observe(line);
                foreach (var child in option.followUpOptions) Visit(child);
            }
            foreach (var option in evt.options) Visit(option);
            EditorUtility.SetDirty(evt);
        }
    }

    private static void Observe(AdventrueDialogData line)
    {
        var ids = line.DiscoveryEntryIds;
        ids.Clear();
        string text = line.Context ?? string.Empty;
        if (Regex.IsMatch(text, "새싹\\s*슬라임")) ids.Add("monster:sprout_slime");
        else if (Regex.IsMatch(text, "암염\\s*슬라임")) ids.Add("monster:rock_salt_slime");
        else if (Regex.IsMatch(text, "슬라임") && !Regex.IsMatch(text, "슬라임\\s*(점액|핵)만")) ids.Add("monster:slime");
        if (Regex.IsMatch(text, "머쉬룸맨|머시룸맨|버섯\\s*주민")) ids.Add("monster:mushroom_man");
        if (Regex.IsMatch(text, "코코넛\\s*게(?!살)")) ids.Add("monster:coconut_crab");
        foreach (var method in line.method)
        {
            if (!(method is CreateImageEvent)) continue;
            var image = typeof(CreateImageEvent).GetField("imagePrefab", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(method) as UnityEngine.UI.Image;
            string id = image == null ? null : image.name switch
            {
                "MushroomMan" => "monster:mushroom_man", "CoconutCrab" => "monster:coconut_crab",
                "RockSaltSlime" => "monster:rock_salt_slime", "HurbSlime" => text.Contains("새싹") ? "monster:sprout_slime" : "monster:slime", _ => null
            };
            if (id != null && !ids.Contains(id)) ids.Add(id);
        }
    }

    private static void ClearKnowledgeSeeds(CookingKnowledgeStore knowledge)
    {
        var so = new SerializedObject(knowledge);
        foreach (string field in new[] { "initialDiscoveredRecipes", "initialKnownRecipeTags", "initialKnownPreparationEffects" }) so.FindProperty(field).arraySize = 0;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
