using System;
using System.Collections.Generic;
using System.IO;
using Work.Core.ObjectPool.RunTime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Work.Core.ObjectPool.Editor
{
    public class PoolManagerEditor : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset visualTreeAsset = default;
        [SerializeField] private PoolManagerSO poolManager = default;
        [SerializeField] private VisualTreeAsset itemAsset = default;

        private string _rootFolder;
        private Button _createBtn;
        private ScrollView _itemView;

        private List<PoolItemUI> _itemList;
        private PoolItemUI _selectedItem;
        
        private UnityEditor.Editor _cachedEditor;
        private VisualElement _inspectorView;

        [MenuItem("Tools/PoolManagerEditor")]
        public static void ShowWindow()
        {
            PoolManagerEditor wnd = GetWindow<PoolManagerEditor>();
            wnd.titleContent = new GUIContent("PoolManagerEditor");
            wnd.minSize = new Vector2(600, 480);
        }

        private void InitializeRootFolder()
        {
            MonoScript monoScript = MonoScript.FromScriptableObject(this);
            string scriptPath = AssetDatabase.GetAssetPath(monoScript);
            _rootFolder = Path.GetDirectoryName(Path.GetDirectoryName(scriptPath)).Replace("\\","/");

            if (visualTreeAsset == null)
            {
                string loadPath = $"{_rootFolder}/Editor/PoolManagerEditor.uxml";
                visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(loadPath);
                Debug.Assert(visualTreeAsset != null, "PoolManagerEditor.uxml is not found");
            }

            if (poolManager == null)
            {
                string filePath = $"{_rootFolder}/PoolManager.asset";
                poolManager = AssetDatabase.LoadAssetAtPath<PoolManagerSO>(filePath);
                if (poolManager == null)
                {
                    Debug.LogWarning("PoolManager script is not exist");
                    poolManager = ScriptableObject.CreateInstance<PoolManagerSO>();
                    AssetDatabase.CreateAsset(poolManager, filePath);
                }
            }

            if (itemAsset == null)
            {
                string loadPath = $"{_rootFolder}/Editor/PoolItemUI.uxml";
                itemAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(loadPath);
                Debug.Assert(itemAsset != null, "PoolItemUI.uxml is not found");
            }
        }

        public void CreateGUI()
        {
            InitializeRootFolder();
            VisualElement root = rootVisualElement;
            visualTreeAsset.CloneTree(root);

            GetElements(root);
            GeneratePoolingItems();
        }

        private void GeneratePoolingItems()
        {
            _itemView.Clear();
            _itemList.Clear();
            
            
            foreach (var item in poolManager.itemList)
            {
                TemplateContainer itemUI = itemAsset.Instantiate();
                PoolItemUI poolItemUI = new PoolItemUI(itemUI, item);
                _itemView.Add(itemUI);
                _itemList.Add(poolItemUI);

                if (_selectedItem != null && _selectedItem.poolItem == item)
                {
                    HandleSelectEvent(poolItemUI);
                }
                
                poolItemUI.Name = item.poolingName;

                poolItemUI.OnSelectEvent += HandleSelectEvent;
                poolItemUI.OnDeleteEvent += HandleDeleteEvent;
            }
        }

        private void HandleDeleteEvent(PoolItemUI item)
        {
            poolManager.itemList.Remove(item.poolItem);
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(item.poolItem));
            EditorUtility.SetDirty(poolManager);
            AssetDatabase.SaveAssets();
            
            if(item==_selectedItem)
                _selectedItem = null;
            
            GeneratePoolingItems();
        }

        private void HandleSelectEvent(PoolItemUI item)
        {
            if(_selectedItem!=null)
                _selectedItem.IsActive = false;
            _selectedItem = item;
            _selectedItem.IsActive = true;
            
            _inspectorView.Clear();
            UnityEditor.Editor.CreateCachedEditor(_selectedItem.poolItem, null, ref _cachedEditor);
            
            VisualElement inspector = _cachedEditor.CreateInspectorGUI();
            
            SerializedObject serializedObject = new SerializedObject(_selectedItem.poolItem);
            inspector.Bind(serializedObject);
            inspector.TrackSerializedObjectValue(serializedObject, so =>
            {
                _selectedItem.Name = so.FindProperty("poolingName").stringValue;
            });
            _inspectorView.Add(inspector);
        }

        private void GetElements(VisualElement root)
        {
            _createBtn = root.Q<Button>("CreateBtn");
            _createBtn.clicked += HandleCreateItem;
            _itemView = root.Q<ScrollView>("ItemView");
            _inspectorView = root.Q<VisualElement>("InspectorView");
            
            _itemList = new List<PoolItemUI>();
        }

        private void HandleCreateItem()
        {
            string itemName = Guid.NewGuid().ToString();
            PoolItemSO newItem = ScriptableObject.CreateInstance<PoolItemSO>();
            newItem.poolingName = itemName;

            if (Directory.Exists($"{_rootFolder}/Items") == false)
            {
                Directory.CreateDirectory($"{_rootFolder}/Items");
            }
            
            AssetDatabase.CreateAsset(newItem, $"{_rootFolder}/Items/{itemName}.asset");
            poolManager.itemList.Add(newItem);
            EditorUtility.SetDirty(poolManager);
            AssetDatabase.SaveAssets();
            
            GeneratePoolingItems();
        }
    }
}
