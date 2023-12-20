using System;
using System.Collections.Generic;
using System.IO;
using Com.Kooply.Unity.ExtensionMethods;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Editor.Private
{
    public class FavoriteAssets : EditorWindow
    {
        [Serializable]
        public class DataWrapper
        {
            public List<AssetData> assets = new ();
        }

        [Serializable]
        public class AssetData
        {
            public string guid;
            public string path;
            public string name;
        }

        
        private static string DataFilePathKey => GetPrefix() + "dataFilePath";
        private static string DataKey => GetPrefix() + "pinned";
        
        
        
        [MenuItem("Window/Custom Panels/Favorites")]
        public static void ShowWindow()
        {
            GetWindow<FavoriteAssets>("♥ Favorites");
        }
        
        [MenuItem("Assets/♥")]
        private static void AddToFavorites()
        {
            var instance = GetWindow<FavoriteAssets>();
            if (instance)
            {
                foreach (var assetGUID in Selection.assetGUIDs)
                    instance.PinAsset(assetGUID);
            }
            
            ShowProjectPanel();
        }
        
        private static void ShowProjectPanel()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Project");
        }

        private static string GetPrefix() => Application.productName + "_"; 
        
        
        
        [SerializeField]
        private DataWrapper assetsData;
        

        private Vector2 _scrollView = Vector2.zero;
        private bool _isInEditMode;
        private int _focusedIndex = -1;
        private bool _useDataFile;
        private List<AssetData> _noDataFileAssets;

            
        private DataWrapper AssetsData
        {
            get
            {
                if (assetsData == null)
                    LoadData();
                
                return assetsData;
            }
        }
            
        private void Awake()
        {
            var dataFilePath = GetDataFilePath();
            if (!dataFilePath.IsNullOrEmpty())
            {
                _useDataFile = true;
                LoadData();
            }

            RefreshAssets();
        }

        public void OnGUI() 
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();

            if (!_isInEditMode)
            {
                if (GUILayout.Button("Menu"))
                    _isInEditMode = true;

                if (_useDataFile)
                    GUILayout.Label("Using data file");
            }
            else
            {
                if (GUILayout.Button("<", EditorStyles.miniButton, GUILayout.Width(80), GUILayout.Height(20)))
                {
                    _isInEditMode = false;
                    _focusedIndex = -1;
                }

                var dataFilePath = GetDataFilePath();
                if (GUILayout.Toggle(_useDataFile, "Use data file" + (dataFilePath.IsNullOrEmpty() ? "" : ": " + Path.GetFileName(dataFilePath))))
                {
                    if (_useDataFile == false)
                    {
                        _noDataFileAssets = assetsData.assets;
                        _useDataFile = true;
                        LoadData();
                    }
                }
                else
                {
                    _useDataFile = false;
                    SetDataFile(null);
                    
                    if (!_noDataFileAssets.IsNullOrEmpty())
                    {
                        assetsData.assets = _noDataFileAssets;
                        LoadData();
                    }
                }

                if (GUILayout.Button("Create Data File", EditorStyles.miniButton))
                {
                    var path = EditorUtility.SaveFilePanel("Select data file", Application.dataPath, "favAssetsSettings.json", "json");
                    if (path.Length > 0)
                    {
                        SetDataFile(path);
                        _useDataFile = true;
                        SaveData();
                    }
                    else
                        Debug.LogError("Favorite Assets: Invalid data file path: " + path);
                }
                
                if (GUILayout.Button("Load Data File", EditorStyles.miniButton))
                {
                    var path = EditorUtility.OpenFilePanel("Open data file", Application.dataPath, "json");
                    if (path.Length > 0)
                    {
                        SetDataFile(path);
                        _useDataFile = true;
                        LoadData();
                    }
                }
                
                var previousColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.red;

                if (GUILayout.Button("Clear All", EditorStyles.miniButton))
                {
                    assetsData.assets.Clear();
                    SaveData();
                }
                
                GUI.backgroundColor = previousColor;
                
            }
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Refresh"))
            {
                RefreshAssets();
            }
            
            if (GUILayout.Button("Save"))
            {
                SaveData();
            }

            GUILayout.EndHorizontal();

            var refreshWhenDone = false;
            if (assetsData?.assets != null && assetsData.assets.Count > 0)
            {
                _scrollView = GUILayout.BeginScrollView(_scrollView);

                var index = 0;
                foreach (var assetData in AssetsData.assets)
                {
                    GUILayout.BeginHorizontal();

                    try
                    {

                        if (_isInEditMode)
                        {
                            if (GUILayout.Button(new GUIContent("X", "Remove"), GUILayout.ExpandWidth(false)))
                            {
                                RemovePin(assetData);
                                break;
                            }
                        }

                        if (GUILayout.Button(new GUIContent("Select"), GUILayout.ExpandWidth(false)))
                        {
                            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetData.path);
                            EditorGUIUtility.PingObject(asset);
                            ShowProjectPanel();
                            break;
                        }

                        if (GUILayout.Button(
                                new GUIContent(" " + assetData.name, AssetDatabase.GetCachedIcon(assetData.path)),
                                GUILayout.Height(18)))
                        {
                            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetData.path);
                            if (asset == null)
                            {
                                refreshWhenDone = true;
                                continue;
                            }

                            EditorGUIUtility.PingObject(asset);
                            Selection.activeObject = asset;

                            var extension = Path.GetExtension(assetData.path);
                            if (!extension.IsNullOrEmpty())
                            {
                                AssetDatabase.OpenAsset(asset);
                                ShowProjectPanel();
                            }
                            else if (AssetDatabase.IsValidFolder(assetData.path))
                            {
                                var assetsGUIDInFolder = AssetDatabase.FindAssets("", new[] { assetData.path });
                                if (assetsGUIDInFolder.Length > 0)
                                {
                                    var folderObject = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(assetsGUIDInFolder[0]));
                                    Selection.activeObject = folderObject;
                                    EditorGUIUtility.PingObject(folderObject);
                                }
                                else
                                    AssetDatabase.OpenAsset(asset);

                                ShowProjectPanel();
                            }
                        }

                        if (_isInEditMode && _focusedIndex == index)
                        {
                            GUI.enabled = index > 0;
                            if (GUILayout.Button(new GUIContent("▲", "Move up"), GUILayout.ExpandWidth(false)))
                            {
                                MoveAssetUp(index);
                                break;
                            }

                            GUI.enabled = index < AssetsData.assets.Count - 1;

                            if (GUILayout.Button(new GUIContent("▼", "Move down"), GUILayout.ExpandWidth(false)))
                            {
                                MoveAssetDown(index);
                                break;
                            }

                            GUI.enabled = true;

                            if (GUILayout.Button(new GUIContent("Done"), GUILayout.ExpandWidth(false)))
                            {
                                _focusedIndex = -1;
                                _isInEditMode = false;
                            }
                        }
                        else
                        {
                            if (GUILayout.Button(new GUIContent("Edit"), GUILayout.ExpandWidth(false)))
                            {
                                _focusedIndex = index;
                                _isInEditMode = true;
                            }
                        }
                    }
                    finally
                    {
                        // Workaround to the "Invalid GUILayout state" error message
                        index++;
                        GUILayout.EndHorizontal();
                    }
                }

                GUILayout.EndScrollView();
            }
            else
                GUILayout.Label("No Favorites (right click an asset and select ♥)");
            
            GUILayout.EndVertical();

            if (refreshWhenDone)
                RefreshAssets();
        }

        private void SetDataFile(string path)
        {
            EditorPrefs.SetString(DataFilePathKey, path);
        }

        private string GetDataFilePath()
        {
            return EditorPrefs.GetString(DataFilePathKey);
        }

        private void RefreshAssets()
        {
            _noDataFileAssets = null;
            
            for (var i = assetsData.assets.Count - 1; i >= 0; i--)
            {
                var assetData = assetsData.assets[i];
                assetData.path = AssetDatabase.GUIDToAssetPath(assetData.guid);
                var asset = AssetDatabase.LoadAssetAtPath<Object>(assetData.path);
                if (asset != null)
                    assetData.name = asset.name;
                else
                    assetsData.assets.RemoveAt(i);
            }
        }
        
        private void SaveData()
        {
            var json = JsonUtility.ToJson(AssetsData);

            if (_useDataFile)
            {
                using var writer = new StreamWriter(GetDataFilePath(), false);
                writer.WriteLine(json);
            }
            else
                EditorPrefs.SetString(DataKey, json);
        }

        private void LoadData()
        {
            assetsData = new DataWrapper();
            string json = null;
            
            if (_useDataFile)
            {
                var filePath = GetDataFilePath();
                if (File.Exists(filePath))
                    json = File.ReadAllLines(filePath).JoinStrings("\n");
                else
                    Debug.LogError($"{nameof(FavoriteAssets)}: Data file not found: " + filePath);
            }
            else
            {
                if (EditorPrefs.HasKey(DataKey))
                    json = EditorPrefs.GetString(DataKey);
            }
            
            if (json != null)
                assetsData = JsonUtility.FromJson<DataWrapper>(json);
        }

        private void PinAsset(string assetGuid)
        {
            var assetData = new AssetData
            {
                guid = assetGuid,
                path = AssetDatabase.GUIDToAssetPath(assetGuid)
            };
                    
            var asset = AssetDatabase.LoadAssetAtPath<Object>(assetData.path);
            assetData.name = asset.name;
            assetsData.assets.Add(assetData);
            SaveData();
        }

        private void RemovePin(AssetData assetData)
        {
            assetsData.assets.Remove(assetData);
            _focusedIndex = -1;
            SaveData();
        }

        private void MoveAssetUp(int index)
        {
            if (index <= 0)
                return;

            (assetsData.assets[index - 1], assetsData.assets[index]) = (assetsData.assets[index], assetsData.assets[index - 1]);
            _focusedIndex--;
            SaveData();
        }
        
        private void MoveAssetDown(int index)
        {
            if (index >= assetsData.assets.Count)
                return;

            (assetsData.assets[index + 1], assetsData.assets[index]) = (assetsData.assets[index], assetsData.assets[index + 1]);
            _focusedIndex++;
            SaveData();
        }

        private void OnLostFocus()
        {
            _focusedIndex = -1;
            _isInEditMode = false;
        }
    }
}
