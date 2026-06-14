using Work.FSM.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Work.FSM.Editor
{
    [UnityEditor.CustomEditor(typeof(StateSO))]
    public class StateSOEditor : UnityEditor.Editor
    {
        [SerializeField] private VisualTreeAsset editorUI = default;
        private readonly Dictionary<string, string> _typeNamesByDisplayName = new Dictionary<string, string>();
        
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
            if (dropdown == null)
            {
                return;
            }

            dropdown.choices.Clear();
            _typeNamesByDisplayName.Clear();
            
            List<Type> derivedTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(GetLoadableTypes)
                .Where(type => type.IsAbstract == false && type.IsSubclassOf(typeof(State)))
                .OrderBy(type => type.FullName)
                .ToList();
            
            foreach (Type type in derivedTypes)
            {
                string displayName = type.FullName;
                _typeNamesByDisplayName[displayName] = type.AssemblyQualifiedName;
                dropdown.choices.Add(displayName);
            }

            UnityEditor.SerializedProperty targetClassProperty = serializedObject.FindProperty(nameof(StateSO.targetClass));
            dropdown.value = GetDisplayName(targetClassProperty.stringValue);
            dropdown.RegisterValueChangedCallback(evt =>
            {
                if (_typeNamesByDisplayName.TryGetValue(evt.newValue, out string assemblyQualifiedName) == false)
                {
                    return;
                }

                serializedObject.Update();
                targetClassProperty.stringValue = assemblyQualifiedName;
                serializedObject.ApplyModifiedProperties();
            });
        }

        private static IEnumerable<Type> GetLoadableTypes(System.Reflection.Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (System.Reflection.ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null);
            }
        }

        private string GetDisplayName(string savedTypeName)
        {
            if (string.IsNullOrWhiteSpace(savedTypeName))
            {
                return null;
            }

            Type savedType = Type.GetType(savedTypeName);
            if (savedType != null)
            {
                return savedType.FullName;
            }

            foreach (string displayName in _typeNamesByDisplayName.Keys)
            {
                if (displayName == savedTypeName)
                {
                    return displayName;
                }
            }

            return savedTypeName;
        }
    }
}
