using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using GamePush.Overlays;
using GamePush.Overlays.Widgets;

namespace GamePushEditor.Overlays
{
    internal sealed class GP_OverlayPreviewSession
    {
        Camera _camera;
        Canvas _canvas;
        RenderTexture _texture;
        GameObject _root;

        internal RenderTexture Texture => _texture;

        internal void Rebuild(GP_OverlaySkin skin, GP_OverlayKind kind, GP_OverlayPreviewState state,
            GP_OverlayPreviewLanguage language, Vector2Int resolution)
        {
            Cleanup();
            if (skin == null)
                return;

            var width = Mathf.Clamp(resolution.x, 240, 1600);
            var height = Mathf.Clamp(resolution.y, 240, 1600);
            _texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                name = "GamePush Overlay Preview",
                hideFlags = HideFlags.HideAndDontSave,
                antiAliasing = 2
            };
            _texture.Create();

            var cameraObject = new GameObject("GP Overlay Preview Camera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            _camera = cameraObject.AddComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.025f, 0.03f, 0.035f, 1f);
            _camera.orthographic = true;
            _camera.nearClipPlane = 0.01f;
            _camera.farClipPlane = 100f;
            _camera.transform.position = new Vector3(0f, 0f, -10f);
            _camera.targetTexture = _texture;

            var canvasObject = new GameObject("GP Overlay Preview Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.hideFlags = HideFlags.HideAndDontSave;
            _canvas = canvasObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = _camera;
            _canvas.planeDistance = 1f;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = skin.referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = width > height ? 1f : 0f;

            _root = GP_OverlayPrefabBuilder.BuildPreview(kind, skin);
            _root.name += " (Preview)";
            _root.hideFlags = HideFlags.HideAndDontSave;
            _root.transform.SetParent(canvasObject.transform, false);

            GP_OverlayPreviewFixtures.Populate(_root, kind, skin, state, language);
            Canvas.ForceUpdateCanvases();
            var responsive = _root.GetComponentInChildren<GP_OverlayResponsive>(true);
            var entry = skin.Find(kind);
            if (responsive != null)
            {
                responsive.Configure(entry);
                var viewportSize = ((RectTransform)_canvas.transform).rect.size;
                if (viewportSize.x <= 0f || viewportSize.y <= 0f)
                {
                    viewportSize = width > height
                        ? new Vector2(skin.referenceResolution.y * width / (float)height,
                            skin.referenceResolution.y)
                        : new Vector2(skin.referenceResolution.x,
                            skin.referenceResolution.x * height / (float)width);
                }
                responsive.ApplyForPreview(viewportSize);
            }
            Canvas.ForceUpdateCanvases();
            _camera.Render();
        }

        internal void Render(Rect rect)
        {
            if (_camera == null || _texture == null)
                return;
            Canvas.ForceUpdateCanvases();
            _camera.Render();
            GUI.DrawTexture(rect, _texture, ScaleMode.ScaleToFit, false);
        }

        internal void Cleanup()
        {
            if (_root != null)
                Object.DestroyImmediate(_root);
            if (_canvas != null)
                Object.DestroyImmediate(_canvas.gameObject);
            if (_camera != null)
                Object.DestroyImmediate(_camera.gameObject);
            if (_texture != null)
            {
                _texture.Release();
                Object.DestroyImmediate(_texture);
            }
            _root = null;
            _canvas = null;
            _camera = null;
            _texture = null;
        }
    }
}
