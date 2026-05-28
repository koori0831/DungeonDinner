using Work.FSM.Code;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace Work.FSM.Editor
{
    [UnityEditor.CustomEditor(typeof(StateSO))]
    public class StateSOEditor : UnityEditor.Editor
    {
        [SerializeField] private VisualTreeAsset editorUI = default;
        
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();
            editorUI.CloneTree(root);
            
            DropdownField dropdown = root.Q<DropdownField>("ClassDropdownField");
            CreateDropdownList(dropdown);
            
            return root;
        }

        private void CreateDropdownList(DropdownField dropdown)
        {
            dropdown.choices.Clear();
            
            Assembly assembly = Assembly.GetAssembly(typeof(State));
            List<string> derivedTypes = assembly.GetTypes()
                .Where(type => type.IsAbstract == false 
                               && type.IsSubclassOf(typeof(State)))
                .Select(type => type.FullName)
                .ToList();
            
            dropdown.choices.AddRange(derivedTypes);
        }
    }
}