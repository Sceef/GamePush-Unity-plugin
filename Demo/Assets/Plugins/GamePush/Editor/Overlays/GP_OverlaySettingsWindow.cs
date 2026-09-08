using UnityEditor;
using UnityEngine;
using GamePush.Overlays;

namespace GamePushEditor.Overlays
{
    public sealed class GP_OverlaySettingsWindow : EditorWindow
    {
        enum PreviewPreset
        {
            PcLandscape,
            MobilePortrait,
            MobileLandscape,
            Square
        }

        GP_OverlaySkin _skin;
        Editor _skinEditor;
        GP_OverlayKind _kind;
        GP_OverlayPreviewState _state;
        GP_OverlayPreviewLanguage _language;
        PreviewPreset _preset;
        Vector2 _settingsScroll;
        GP_OverlayPreviewSession _preview;
        bool _previewDirty = true;

        public static void Open()
        {
            var window = GetWindow<GP_OverlaySettingsWindow>();
            window.titleContent = new GUIContent("GamePush Overlays");
            window.minSize = new Vector2(900f, 560f);
            window.Show();
        }

        void OnEnable()
        {
            _preview = new GP_OverlayPreviewSession();
            SetSkin(GP_OverlayPrefabBuilder.DefaultSkin);
            EditorApplication.update += PreviewUpdate;
        }

        void OnDisable()
        {
            EditorApplication.update -= PreviewUpdate;
            _preview?.Cleanup();
            _preview = null;
            if (_skinEditor != null)
                DestroyImmediate(_skinEditor);
        }

        void PreviewUpdate()
        {
            if (!_previewDirty)
                return;
            _previewDirty = false;
            _preview?.Rebuild(_skin, _kind, _state, _language, PreviewResolution());
            Repaint();
        }

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            DrawSettings();
            DrawPreview();
            EditorGUILayout.EndHorizontal();
        }

        void DrawSettings()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(390f));
            EditorGUILayout.Space(8f);

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Shared template", _skin, typeof(GP_OverlaySkin), false);

            EditorGUI.BeginChangeCheck();
            _kind = (GP_OverlayKind)EditorGUILayout.EnumPopup("Overlay", _kind);
            _state = (GP_OverlayPreviewState)EditorGUILayout.EnumPopup("State", _state);
            _language = (GP_OverlayPreviewLanguage)EditorGUILayout.EnumPopup("Language", _language);
            _preset = (PreviewPreset)EditorGUILayout.EnumPopup("Viewport", _preset);
            if (EditorGUI.EndChangeCheck())
                _previewDirty = true;

            EditorGUILayout.Space(8f);
            DrawPrefabActions();
            EditorGUILayout.Space(8f);

            _settingsScroll = EditorGUILayout.BeginScrollView(_settingsScroll);
            if (_skinEditor != null)
            {
                EditorGUI.BeginChangeCheck();
                _skinEditor.OnInspectorGUI();
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(_skin);
                    _previewDirty = true;
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        void DrawPrefabActions()
        {
            var entry = _skin != null ? _skin.Find(_kind) : null;
            EditorGUILayout.LabelField("Prefab", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(entry == null || entry.prefab == null))
            {
                if (GUILayout.Button("Open default prefab"))
                    AssetDatabase.OpenAsset(entry.prefab);
                if (GUILayout.Button("Create and assign custom copy"))
                    CreateCustomCopy(entry);
            }

            if (entry != null)
            {
                EditorGUI.BeginChangeCheck();
                var custom = (GameObject)EditorGUILayout.ObjectField("Custom override", entry.customPrefab,
                    typeof(GameObject), false);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_skin, "Assign overlay prefab");
                    entry.customPrefab = custom;
                    EditorUtility.SetDirty(_skin);
                    _previewDirty = true;
                }
                using (new EditorGUI.DisabledScope(entry.customPrefab == null))
                {
                    if (GUILayout.Button("Reset to generated default"))
                    {
                        Undo.RecordObject(_skin, "Reset overlay prefab");
                        entry.customPrefab = null;
                        EditorUtility.SetDirty(_skin);
                        _previewDirty = true;
                    }
                }
            }

            EditorGUILayout.Space(4f);
            if (GUILayout.Button("Rebuild selected default"))
                RebuildSelected();
            if (GUILayout.Button("Apply template and rebuild all defaults"))
                RebuildAll();
            if (!GP_OverlayPrefabBuilder.IsTextMeshProReady)
                EditorGUILayout.HelpBox("Import TextMeshPro Essential Resources before rebuilding.",
                    MessageType.Warning);
            EditorGUILayout.HelpBox(
                "Rebuild overwrites generated defaults only. Custom copies and overrides are preserved.",
                MessageType.Info);
        }

        void DrawPreview()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            var rect = GUILayoutUtility.GetRect(300f, 10000f, 300f, 10000f,
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(rect, new Color(0.025f, 0.03f, 0.035f, 1f));
            _preview?.Render(rect);
            EditorGUILayout.EndVertical();
        }

        void SetSkin(GP_OverlaySkin skin)
        {
            if (_skinEditor != null)
                DestroyImmediate(_skinEditor);
            _skin = skin != null ? skin : GP_OverlayPrefabBuilder.DefaultSkin;
            _skinEditor = _skin != null ? Editor.CreateEditor(_skin) : null;
            _previewDirty = true;
        }

        void RebuildSelected()
        {
            if (!GP_OverlayPrefabBuilder.EnsureTextMeshPro())
                return;
            if (!EditorUtility.DisplayDialog("GamePush",
                    "Rebuild the generated " + _kind + " prefab? Its generated asset will be overwritten.",
                    "Rebuild", "Cancel"))
                return;
            GP_OverlayPrefabBuilder.RebuildScreen(_kind, _skin);
            _previewDirty = true;
        }

        void RebuildAll()
        {
            if (!GP_OverlayPrefabBuilder.EnsureTextMeshPro())
                return;
            if (!EditorUtility.DisplayDialog("GamePush",
                    "Apply this template and rebuild all generated overlay prefabs? Custom copies are preserved.",
                    "Rebuild all", "Cancel"))
                return;
            GP_OverlayPrefabBuilder.Rebuild(_skin);
            _previewDirty = true;
        }

        void CreateCustomCopy(GP_OverlayPrefabEntry entry)
        {
            var source = AssetDatabase.GetAssetPath(entry.prefab);
            var path = EditorUtility.SaveFilePanelInProject("Create custom overlay prefab",
                _kind + "Custom", "prefab", "Choose a project-owned location for the custom prefab.");
            if (string.IsNullOrEmpty(path))
                return;
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            if (!AssetDatabase.CopyAsset(source, path))
            {
                EditorUtility.DisplayDialog("GamePush", "Could not copy the prefab.", "OK");
                return;
            }
            var copy = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Undo.RecordObject(_skin, "Create custom overlay prefab");
            entry.customPrefab = copy;
            EditorUtility.SetDirty(_skin);
            AssetDatabase.SaveAssets();
            Selection.activeObject = copy;
            EditorGUIUtility.PingObject(copy);
            _previewDirty = true;
        }

        Vector2Int PreviewResolution()
        {
            switch (_preset)
            {
                case PreviewPreset.MobilePortrait: return new Vector2Int(540, 960);
                case PreviewPreset.MobileLandscape: return new Vector2Int(960, 540);
                case PreviewPreset.Square: return new Vector2Int(720, 720);
                default: return new Vector2Int(1280, 720);
            }
        }
    }
}
