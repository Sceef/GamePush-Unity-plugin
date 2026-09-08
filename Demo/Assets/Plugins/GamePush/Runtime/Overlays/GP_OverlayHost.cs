using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GamePush.Overlays
{
    /// <summary>
    /// Owns the overlay canvas, the shared EventSystem and the window stack.
    /// Lives on the GamePushSDK object and survives scene loads.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GP_OverlayHost : MonoBehaviour
    {
        public const int SortingOrder = 30000;

        static GP_OverlayHost _instance;

        readonly List<GP_OverlayView> _stack = new List<GP_OverlayView>();

        Canvas _canvas;
        CanvasScaler _scaler;
        RectTransform _root;
        GameObject _ownedEventSystem;
        Vector2 _lastScreen;

        public static GP_OverlayHost Instance => _instance;

        public GP_OverlaySkin Skin => GP_OverlaySkin.Instance;
        public RectTransform Root => _root;
        public Canvas Canvas => _canvas;
        public bool IsAnyOpen => _stack.Count > 0;
        public int OpenCount => _stack.Count;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;
            BuildCanvas();
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
            if (_ownedEventSystem != null)
                Destroy(_ownedEventSystem);
        }

        void Update()
        {
            if (_stack.Count == 0)
                return;

            var screen = new Vector2(Screen.width, Screen.height);
            if (screen != _lastScreen)
            {
                _lastScreen = screen;
                ApplyScalerMatch();
            }

            if (BackPressed())
            {
                var top = _stack[_stack.Count - 1];
                if (top != null && top.CloseOnBack)
                    top.Close();
            }
        }

        public bool IsOpen(GP_OverlayKind kind)
        {
            foreach (var view in _stack)
            {
                if (view != null && view.Kind == kind)
                    return true;
            }
            return false;
        }

        public bool Show(GP_OverlayKind kind, object args)
        {
            EnsureEventSystem();

            var prefab = Skin.PrefabFor(kind);
            if (prefab == null)
            {
                GP_Logger.Warn("OVERLAYS", "No prefab for " + kind +
                                           ". Run GamePush/Overlays/Rebuild Default Prefabs or assign one in GP_OverlaySkin.");
                return false;
            }

            var existing = FindOpen(kind);
            if (existing != null)
            {
                existing.transform.SetAsLastSibling();
                _stack.Remove(existing);
                _stack.Add(existing);
                existing.Bind(args);
                return true;
            }

            var instance = Instantiate(prefab, _root);
            instance.name = prefab.name;
            var view = instance.GetComponent<GP_OverlayView>();
            if (view == null)
            {
                GP_Logger.Warn("OVERLAYS", "Prefab " + prefab.name + " has no GP_OverlayView component");
                Destroy(instance);
                return false;
            }

            view.Attach(this, kind);
            _stack.Add(view);
            ApplyScalerMatch();
            view.Bind(args);
            view.PlayShow();
            GP_Overlays.RaiseOpen(kind);
            return true;
        }

        public void Hide(GP_OverlayKind kind)
        {
            var view = FindOpen(kind);
            view?.Close();
        }

        public void HideTop()
        {
            if (_stack.Count == 0)
                return;
            _stack[_stack.Count - 1]?.Close();
        }

        public void HideAll()
        {
            for (var i = _stack.Count - 1; i >= 0; i--)
                _stack[i]?.Close();
        }

        internal void Detach(GP_OverlayView view)
        {
            if (view == null)
                return;
            _stack.Remove(view);
            GP_Overlays.RaiseClose(view.Kind, _stack.Count > 0);
        }

        GP_OverlayView FindOpen(GP_OverlayKind kind)
        {
            foreach (var view in _stack)
            {
                if (view != null && view.Kind == kind)
                    return view;
            }
            return null;
        }

        void BuildCanvas()
        {
            var go = new GameObject("GP_OverlayCanvas");
            go.transform.SetParent(transform, false);
            go.layer = LayerMask.NameToLayer("UI");

            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = SortingOrder;

            _scaler = go.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = Skin.referenceResolution;
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            go.AddComponent<GraphicRaycaster>();

            _root = _canvas.GetComponent<RectTransform>();
            _lastScreen = new Vector2(Screen.width, Screen.height);
            ApplyScalerMatch();
        }

        /// <summary>
        /// Scales against whichever axis is the tighter one so content never spills off screen
        /// when the device rotates or the desktop window is resized.
        /// </summary>
        void ApplyScalerMatch()
        {
            if (_scaler == null)
                return;
            var reference = Skin.referenceResolution;
            if (reference.x <= 0f || reference.y <= 0f)
                return;
            var screenAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            var referenceAspect = reference.x / reference.y;
            _scaler.referenceResolution = reference;
            _scaler.matchWidthOrHeight = screenAspect > referenceAspect ? 1f : 0f;
        }

        void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;
#if UNITY_2023_1_OR_NEWER
            var existing = FindFirstObjectByType<EventSystem>();
#else
            var existing = FindObjectOfType<EventSystem>();
#endif
            if (existing != null)
                return;

            _ownedEventSystem = new GameObject("GP_EventSystem");
            DontDestroyOnLoad(_ownedEventSystem);
            _ownedEventSystem.AddComponent<EventSystem>();
            AddInputModule(_ownedEventSystem);
        }

        /// <summary>
        /// Picks the input module that matches the project's active input handling. Resolved by
        /// name so the plugin does not need a hard reference to com.unity.inputsystem.
        /// </summary>
        static void AddInputModule(GameObject target)
        {
            var moduleType = Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem", false);
            if (moduleType != null)
            {
                target.AddComponent(moduleType);
                return;
            }
#if ENABLE_LEGACY_INPUT_MANAGER
            target.AddComponent<StandaloneInputModule>();
#else
            GP_Logger.Warn("OVERLAYS",
                "No usable input module found; overlays will render but not receive input.");
#endif
        }

        static bool BackPressed()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            var keyboardType = Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem", false);
            if (keyboardType == null)
                return false;
            var current = keyboardType.GetProperty("current")?.GetValue(null);
            if (current == null)
                return false;
            var key = keyboardType.GetProperty("escapeKey")?.GetValue(current);
            var pressed = key?.GetType().GetProperty("wasPressedThisFrame")?.GetValue(key);
            return pressed is bool flag && flag;
#endif
        }
    }
}
