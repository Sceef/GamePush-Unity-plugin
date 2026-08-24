using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using GamePush;
using GamePush.Data;
using Plugins.GamePush.Editor;

namespace GamePushEditor
{
    public class GP_Window : EditorWindow
    {
        private const string SITE_URL = "https://gamepush.com";
        private const string VERSION = PluginData.SDK_VERSION;
        private const string INIT_SCENE = "Assets/Plugins/GamePush/InitScene/AwaitInit.unity";
        private const string ApiSecretPref = "GamePush.ApiSecret";

        private static readonly string[] TabNames = { "Main", "Platform emulator", "In-apps" };

        private static SavedProjectData _projectData;
        private static GUIStyle _titleStyle;
        private static int _menuOpened;

        private bool _revealProjectId;
        private bool _revealToken;
        private bool _revealApiKey;
        private string _apiKey = string.Empty;
        private bool _loadingProducts;

        private Vector2 _emulatorScroll;
        private Vector2 _paymentsScroll;
        private Editor _platformEditor;
        private Editor _paymentsEditor;
        private GP_PlatformSettings _platformSettings;
        private GP_PaymentsStub _paymentsStub;

        private static SavedDataSO DataLinker => Resources.Load<SavedDataSO>("GP_DataLinker");

        [MenuItem("Tools/GamePush")]
        private static void ShowWindow()
        {
            var window = GetWindow<GP_Window>();
            window.minSize = new Vector2(360, 480);
            window.titleContent = new GUIContent("GamePush Settings");
            window.Show();
        }

        private void OnEnable()
        {
            _projectData = GetSavedProjectData();
            _apiKey = EditorPrefs.GetString(ApiSecretPref, string.Empty);
            EnsureAssetEditors();
        }

        private void OnBecameVisible()
        {
            _titleStyle = new GUIStyle
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = new GUIStyleState { textColor = Color.white }
            };

            _projectData = GetSavedProjectData();
            EnsureAssetEditors();
        }

        private void OnDisable()
        {
            DestroyEditor(ref _platformEditor);
            DestroyEditor(ref _paymentsEditor);
        }


        private static SavedProjectData GetSavedProjectData()
        {
            var path = AssetDatabase.GetAssetPath(DataLinker.saveFile);
            var file = new System.IO.StreamReader(path);
            var json = file.ReadToEnd();
            file.Close();

            if (string.IsNullOrWhiteSpace(json))
            {
                SaveProjectData();
                return new SavedProjectData(){ id = _projectData.id, token = _projectData.token };
            }

            var savedProjectData = JsonUtility.FromJson<SavedProjectData>(json);
            if (json.IndexOf("\"adsStubs\"", StringComparison.Ordinal) < 0)
                savedProjectData.adsStubs = true;
            if (json.IndexOf("\"paymentsStubs\"", StringComparison.Ordinal) < 0)
                savedProjectData.paymentsStubs = true;
            return savedProjectData;
        }

        private static void SaveProjectData()
        {
            var path = AssetDatabase.GetAssetPath(DataLinker.saveFile);
            var json = JsonUtility.ToJson(_projectData);

            System.IO.File.WriteAllText(path, json);
            AssetDatabase.Refresh();
        }

        private static void SetProjectDataToWebTemplate()
        {
            PlayerSettings.SetTemplateCustomValue("PROJECT_ID", _projectData.id.ToString());
            PlayerSettings.SetTemplateCustomValue("TOKEN", _projectData.token.ToString());
            PlayerSettings.SetTemplateCustomValue("SHOW_PRELOADER_AD", _projectData.showPreloadAd.ToString());
            PlayerSettings.SetTemplateCustomValue("GAMEREADY_AUTOCALL", _projectData.gameReadyAuto.ToString());
        }

        private static void SaveProjectDataToScript()
        {
            SaveProjectDataToJavaScript();
            SaveProjectDataToSharp();
        }

        private static void SaveProjectDataToSharp()
        {
            var path = AssetDatabase.GetAssetPath(DataLinker.projectData);
            var file = new System.IO.StreamWriter(path);

            string gameReadyBool = _projectData.gameReadyAuto.ToString().ToLower();
            string showStickyBool = _projectData.showStickyOnStart.ToString().ToLower();
            string waitPluginBool = _projectData.waitPluginReady.ToString().ToLower();
            string autoPauseBool = _projectData.autoPause.ToString().ToLower();
            string adsStubsBool = _projectData.adsStubs.ToString().ToLower();
            string paymentsStubsBool = _projectData.paymentsStubs.ToString().ToLower();

            file.WriteLine("namespace GamePush.Data");
            file.WriteLine("{");
            file.WriteLine("    public static class ProjectData");
            file.WriteLine("    {");
            file.WriteLine($"        public static string SDK_VERSION = \"{VERSION}\";");
            file.WriteLine($"        public static string ID = \"{_projectData.id}\";");
            file.WriteLine($"        public static string TOKEN = \"{_projectData.token}\";");
            file.WriteLine($"        public static bool GAMEREADY_AUTOCALL = {gameReadyBool};");
            file.WriteLine($"        public static bool SHOW_STICKY_ON_START = {showStickyBool};");
            file.WriteLine($"        public static bool WAIT_PLAGIN_READY = {waitPluginBool};");
            file.WriteLine($"        public static bool AUTO_PAUSE_ON_ADS = {autoPauseBool};");
            file.WriteLine($"        public static bool ADS_STUBS = {adsStubsBool};");
            file.WriteLine($"        public static bool PAYMENTS_STUBS = {paymentsStubsBool};");
            file.WriteLine("    }");
            file.WriteLine("}");
            file.Close();
            AssetDatabase.Refresh();
        }

        private static void SaveProjectDataToJavaScript()
        {
            var pathToJS = AssetDatabase.GetAssetPath(DataLinker.jsAnchor);
            var pathJspre = pathToJS.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(DataLinker.jsAnchor)), "_dataFields.jspre");
            
            var filePre = new StreamWriter(pathJspre);

            filePre.WriteLine($"const dataProjectId = \'{_projectData.id}\';");
            filePre.WriteLine($"const dataPublicToken = \'{_projectData.token}\';");
            filePre.WriteLine($"const showPreloaderAd = \'{_projectData.showPreloadAd}\';");

            filePre.Close();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        #region GUI

        private void OnGUI()
        {
            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    normal = new GUIStyleState { textColor = Color.white }
                };
            }

            if (_projectData == null)
                _projectData = GetSavedProjectData();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _menuOpened = GUILayout.Toolbar(
                _menuOpened,
                TabNames,
                EditorStyles.toolbarButton,
                GUI.ToolbarButtonSize.FitToContents);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            switch (_menuOpened)
            {
                case 1:
                    OnPlatformEmulatorGUI();
                    break;
                case 2:
                    OnPaymentsGUI();
                    break;
                default:
                    OnLoginGUI();
                    break;
            }

            GUILayout.Space(30);
            DrawSeparator();

            if (GUILayout.Button("<color=#04bc04>GamePush 2024</color>",
                    new GUIStyle { alignment = TextAnchor.LowerRight, richText = true }))
                Application.OpenURL(SITE_URL);

            GUILayout.Label($"<color=white>v{VERSION}</color>", new GUIStyle { alignment = TextAnchor.LowerRight });
        }

        private void OnLoginGUI()
        {
            GUILayout.Space(20);
            GUILayout.Label(" Enter project ID and token", _titleStyle);
            GUILayout.Space(10);

            _projectData.id = DrawSecretIntField("Project ID", _projectData.id, ref _revealProjectId);
            GUILayout.Space(5);
            _projectData.token = DrawSecretTextField("Token", _projectData.token, ref _revealToken);

            GUILayout.Space(15);
            GUILayout.Label(" Additional settings", _titleStyle);
            GUILayout.Space(10);

            _projectData.showPreloadAd = EditorGUILayout.Toggle("Show Preloader Ad", _projectData.showPreloadAd);
            GUILayout.Space(5);
            _projectData.showStickyOnStart = EditorGUILayout.Toggle("Show Sticky on Start", _projectData.showStickyOnStart);
            GUILayout.Space(5);
            _projectData.gameReadyAuto = EditorGUILayout.Toggle("GameReady Autocall", _projectData.gameReadyAuto);
            GUILayout.Space(5);
            _projectData.waitPluginReady = EditorGUILayout.Toggle("Await plugin ready", _projectData.waitPluginReady);
            GUILayout.Space(5);
            _projectData.autoPause = EditorGUILayout.Toggle("Pause music on ads", _projectData.autoPause);

            GUILayout.Space(15);
            GUILayout.Label(" Editor Settings", _titleStyle);
            GUILayout.Space(10);

            _projectData.adsStubs = EditorGUILayout.Toggle("Ads stubs", _projectData.adsStubs);
            GUILayout.Space(5);
            _projectData.paymentsStubs = EditorGUILayout.Toggle("Payments stubs", _projectData.paymentsStubs);

            GUILayout.Space(25);

            if (GUILayout.Button("Save", GUILayout.Height(30)))
                SaveConfig();
        }

        private void OnPlatformEmulatorGUI()
        {
            EnsureAssetEditors();
            if (_platformEditor == null)
            {
                EditorGUILayout.HelpBox("GP_PlatformSettings asset was not found in Resources or Project Settings.", MessageType.Warning);
                return;
            }

            _emulatorScroll = EditorGUILayout.BeginScrollView(_emulatorScroll);
            _platformEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
        }

        private void OnPaymentsGUI()
        {
            EnsureAssetEditors();

            EditorGUILayout.Space(8);
            _apiKey = DrawSecretTextField("API key", _apiKey, ref _revealApiKey);
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(_loadingProducts);
            if (GUILayout.Button("Load products", GUILayout.Height(24)))
                LoadProductsFromApi();
            EditorGUI.EndDisabledGroup();
            if (GUILayout.Button("Clear key", GUILayout.Width(80), GUILayout.Height(24)))
            {
                _apiKey = string.Empty;
                EditorPrefs.DeleteKey(ApiSecretPref);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Import JSON", GUILayout.Height(24)))
                ImportProductsFile("json");
            if (GUILayout.Button("Import CSV", GUILayout.Height(24)))
                ImportProductsFile("csv");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            if (_paymentsEditor == null)
            {
                EditorGUILayout.HelpBox("GP_PaymentsStub asset was not found in Resources or Project Settings.", MessageType.Warning);
                return;
            }

            _paymentsScroll = EditorGUILayout.BeginScrollView(_paymentsScroll);
            _paymentsEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
        }

        private void EnsureAssetEditors()
        {
            _platformSettings = ResolvePlatformSettings();
            _paymentsStub = ResolvePaymentsStub();

            if (_platformEditor == null || _platformEditor.target != _platformSettings)
            {
                DestroyEditor(ref _platformEditor);
                if (_platformSettings != null)
                    _platformEditor = Editor.CreateEditor(_platformSettings);
            }

            if (_paymentsEditor == null || _paymentsEditor.target != _paymentsStub)
            {
                DestroyEditor(ref _paymentsEditor);
                if (_paymentsStub != null)
                    _paymentsEditor = Editor.CreateEditor(_paymentsStub);
            }
        }

        private static GP_PlatformSettings ResolvePlatformSettings()
        {
            GP_Settings settings = GP_SettingsWrap.instance != null ? GP_SettingsWrap.instance.settings : null;
            if (settings != null && settings.platformSettings != null)
                return settings.platformSettings;
            return Resources.Load<GP_PlatformSettings>("GP_PlatformSettings");
        }

        private static GP_PaymentsStub ResolvePaymentsStub()
        {
            GP_Settings settings = GP_SettingsWrap.instance != null ? GP_SettingsWrap.instance.settings : null;
            if (settings != null && settings.paymentsStub != null)
                return settings.paymentsStub;
            return Resources.Load<GP_PaymentsStub>("GP_PaymentsStub");
        }

        private static void DestroyEditor(ref Editor editor)
        {
            if (editor == null)
                return;
            DestroyImmediate(editor);
            editor = null;
        }

        private void ImportProductsFile(string extension)
        {
            string path = EditorUtility.OpenFilePanel("Import GamePush products", "", extension);
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                string text = File.ReadAllText(path);
                List<FetchProducts> products = GP_ProductCatalogMapper.Parse(text, CurrentPlatform());
                ApplyProducts(products);
            }
            catch (Exception)
            {
                EditorUtility.DisplayDialog("GamePush Error", "Failed to import products file.", "OK");
            }
        }

        private async void LoadProductsFromApi()
        {
            if (_projectData == null || _projectData.id == 0)
            {
                EditorUtility.DisplayDialog("GamePush Error", "Save a Project ID on the Main tab first.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                EditorUtility.DisplayDialog("GamePush Error", "Enter the account API key (Account Settings → API Keys).", "OK");
                return;
            }

            EditorPrefs.SetString(ApiSecretPref, _apiKey);
            _loadingProducts = true;
            Repaint();
            try
            {
                List<FetchProducts> products = await GP_ProductCatalogClient.FetchProducts(
                    _projectData.id,
                    _apiKey,
                    CurrentPlatform());
                ApplyProducts(products);
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog("GamePush Error", exception.Message, "OK");
            }
            finally
            {
                _loadingProducts = false;
                Repaint();
            }
        }

        private void ApplyProducts(List<FetchProducts> products)
        {
            if (_paymentsStub == null)
            {
                EditorUtility.DisplayDialog("GamePush Error", "GP_PaymentsStub asset was not found.", "OK");
                return;
            }

            if (products == null || products.Count == 0)
            {
                EditorUtility.DisplayDialog("GamePush", "No products found.", "OK");
                return;
            }

            Undo.RecordObject(_paymentsStub, "Import GamePush products");
            _paymentsStub.MergeProducts(products);
            EditorUtility.SetDirty(_paymentsStub);
            AssetDatabase.SaveAssets();
            DestroyEditor(ref _paymentsEditor);
            _paymentsEditor = Editor.CreateEditor(_paymentsStub);
            EditorUtility.DisplayDialog("GamePush", $"Imported {products.Count} products.", "OK");
        }

        private static Platform CurrentPlatform()
        {
            GP_PlatformSettings settings = ResolvePlatformSettings();
            return settings != null ? settings.PlatformToEmulate : Platform.NONE;
        }

        private static int DrawSecretIntField(string label, int value, ref bool reveal)
        {
            EditorGUILayout.BeginHorizontal();
            if (reveal)
            {
                value = EditorGUILayout.IntField(label, value);
            }
            else
            {
                string raw = EditorGUILayout.PasswordField(label, value == 0 ? string.Empty : value.ToString());
                if (string.IsNullOrEmpty(raw))
                    value = 0;
                else if (int.TryParse(raw, out int parsed))
                    value = parsed;
            }

            if (DrawRevealToggle(reveal))
                reveal = !reveal;
            EditorGUILayout.EndHorizontal();
            return value;
        }

        private static string DrawSecretTextField(string label, string value, ref bool reveal)
        {
            EditorGUILayout.BeginHorizontal();
            value = reveal
                ? EditorGUILayout.TextField(label, value)
                : EditorGUILayout.PasswordField(label, value ?? string.Empty);
            if (DrawRevealToggle(reveal))
                reveal = !reveal;
            EditorGUILayout.EndHorizontal();
            return value;
        }

        private static bool DrawRevealToggle(bool revealed)
        {
            return GUILayout.Button(
                RevealButtonContent(revealed),
                EditorStyles.miniButton,
                GUILayout.Width(24),
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }

        private static GUIContent RevealButtonContent(bool revealed)
        {
            string iconName = revealed ? "d_scenevis_visible" : "d_scenevis_hidden";
            GUIContent icon = EditorGUIUtility.IconContent(iconName);
            if (icon != null && icon.image != null)
            {
                icon.tooltip = revealed ? "Hide value" : "Show value";
                return icon;
            }

            return new GUIContent(revealed ? "Hide" : "Show", revealed ? "Hide value" : "Show value");
        }

        private static void DrawSeparator()
        {
            GUILayout.Space(10);
            var rect = EditorGUILayout.BeginHorizontal();
            Handles.color = Color.gray;
            Handles.DrawLine(new Vector2(rect.x - 15, rect.y), new Vector2(rect.width + 15, rect.y));
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
        }
        #endregion

        private static void SaveConfig()
        {
            //Console.Log("Saving data");
            if (_projectData.id == 0 || string.IsNullOrEmpty(_projectData.token))
            {
                EditorUtility.DisplayDialog("GamePush Error", "Please fill all the fields.", "OK");
                return;
            }

            if (!ValidateToken(_projectData.token)) return;

            SaveProjectData();

            SetProjectDataToWebTemplate();
            SaveProjectDataToScript();

            IniSceneHandle();

            GP_Logger.SystemLog("Data saved");
        }

        private static bool ValidateToken(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                EditorUtility.DisplayDialog("GamePush Error", "Please enter project token.", "OK");
                return false;
            }

            if (!Regex.IsMatch(input, @"^[a-zA-Z0-9]+$"))
            {
                EditorUtility.DisplayDialog("GamePush Error", "The project token can only contain alphabetical letters and numbers", "OK");
                return false;
            }
            return true;
        }

        #region InitScene

        private static void IniSceneHandle()
        {
            if (_projectData.waitPluginReady)
                AddSceneToBuildSettings();
            else
                RemoveSceneToBuildSettings();
        }

        private static void AddSceneToBuildSettings()
        {
            if (!System.IO.File.Exists(INIT_SCENE))
                return;

            var scenes = EditorBuildSettings.scenes;

            // Проверяем, добавлена ли уже сцена
            foreach (var scene in scenes)
            {
                if (scene.path == INIT_SCENE)
                {
                    return;
                }
            }

            // Добавляем сцену в список Build Settings
            var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
            newScenes[0] = new EditorBuildSettingsScene(INIT_SCENE, true);
            for (int i = 0; i < scenes.Length; i++)
            {
                newScenes[i+1] = scenes[i];
            }
            
            EditorBuildSettings.scenes = newScenes;
        }

        private static void RemoveSceneToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes;
            bool needToRemove = false;

            foreach (var scene in scenes)
            {
                if (scene.path == INIT_SCENE)
                {
                    needToRemove = true;
                    break;
                }
            }

            if (needToRemove)
            {
                var newScenes = new EditorBuildSettingsScene[scenes.Length - 1];
                for (int i = 0; i < newScenes.Length; i++)
                {
                    newScenes[i] = scenes[i+1];
                }

                EditorBuildSettings.scenes = newScenes;
            }
        }

        #endregion
        
    }
}
