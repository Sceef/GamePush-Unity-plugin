using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GamePush.Overlays;

namespace GamePushEditor.Overlays
{
    /// <summary>
    /// Small helpers for assembling uGUI hierarchies in code. Used only by the prefab builder;
    /// the prefabs it produces are ordinary assets that games are free to edit afterwards.
    /// </summary>
    internal static class GP_OverlayUIFactory
    {
        static GP_OverlaySkin _skin;

        internal static GP_OverlaySkin Skin
        {
            get => _skin != null ? _skin : GP_OverlaySkin.Instance;
            set => _skin = value;
        }

        internal static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        internal static RectTransform Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        internal static Image Panel(string name, Transform parent, Color color, Sprite sprite = null)
        {
            var rect = Rect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = true;
            return image;
        }

        /// <summary>Paints an existing node instead of adding a child, so layout groups ignore it.</summary>
        internal static Image Background(RectTransform rect, Color color, Sprite sprite = null)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            return image;
        }

        internal static TMP_Text Text(string name, Transform parent, string value, float size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            var rect = Rect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            if (Skin.font != null)
                text.font = Skin.font;
            return text;
        }

        internal static Button Button(string name, Transform parent, string label, Color background,
            out TMP_Text labelText)
        {
            var image = Panel(name, parent, background, Skin.buttonSprite);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = Skin.ButtonColors(background);

            labelText = Text("Label", image.transform, label, Skin.bodySize, Skin.text,
                TextAlignmentOptions.Center);
            Stretch((RectTransform)labelText.transform);
            return button;
        }

        internal static Button IconButton(string name, Transform parent, string glyph, Color background,
            float size = -1f)
        {
            var button = Button(name, parent, glyph, background, out _);
            size = size > 0f ? size : Skin.compactControlHeight;
            Size(button.GetComponent<RectTransform>(), new Vector2(size, size));
            var element = button.gameObject.AddComponent<LayoutElement>();
            element.minWidth = size;
            element.preferredWidth = size;
            element.minHeight = size;
            element.preferredHeight = size;
            return button;
        }

        internal static Image Divider(string name, Transform parent, bool vertical = false)
        {
            var divider = Panel(name, parent, Skin.border);
            if (vertical)
                Element(divider.gameObject, minWidth: 1f, preferredWidth: 1f, flexibleHeight: 1f);
            else
                Element(divider.gameObject, minHeight: 1f, preferredHeight: 1f, flexibleWidth: 1f);
            divider.raycastTarget = false;
            return divider;
        }

        internal static void StyleSelectable(Selectable selectable, Color normal)
        {
            if (selectable == null)
                return;
            selectable.transition = Selectable.Transition.ColorTint;
            selectable.colors = Skin.ButtonColors(normal);
        }

        internal static RectTransform Size(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            return rect;
        }

        internal static VerticalLayoutGroup Vertical(GameObject target, RectOffset padding, float spacing)
        {
            var layout = target.AddComponent<VerticalLayoutGroup>();
            layout.padding = padding;
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return layout;
        }

        internal static HorizontalLayoutGroup Horizontal(GameObject target, RectOffset padding, float spacing)
        {
            var layout = target.AddComponent<HorizontalLayoutGroup>();
            layout.padding = padding;
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleLeft;
            return layout;
        }

        internal static LayoutElement Element(GameObject target, float minWidth = -1f, float minHeight = -1f,
            float flexibleWidth = -1f, float flexibleHeight = -1f, float preferredWidth = -1f,
            float preferredHeight = -1f)
        {
            var element = target.GetComponent<LayoutElement>() ?? target.AddComponent<LayoutElement>();
            element.minWidth = minWidth;
            element.minHeight = minHeight;
            element.flexibleWidth = flexibleWidth;
            element.flexibleHeight = flexibleHeight;
            element.preferredWidth = preferredWidth;
            element.preferredHeight = preferredHeight;
            return element;
        }

        internal static RectTransform Spacer(Transform parent)
        {
            var rect = Rect("Spacer", parent);
            Element(rect.gameObject, flexibleWidth: 1f);
            return rect;
        }

        /// <summary>Builds a ScrollRect with a pooled row host and returns the GP_OverlayList.</summary>
        internal static GamePush.Overlays.Widgets.GP_OverlayList ScrollList(string name, Transform parent,
            bool horizontalFit, float spacing = 8f)
        {
            var scrollRect = Rect(name, parent);
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;

            var viewport = Rect("Viewport", scrollRect);
            viewport.gameObject.AddComponent<RectMask2D>();
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0f);

            var content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;

            if (horizontalFit)
            {
                var grid = content.gameObject.AddComponent<GridLayoutGroup>();
                grid.padding = new RectOffset(8, 8, 8, 8);
                grid.spacing = new Vector2(spacing, spacing);
                var flexible = content.gameObject.AddComponent<GamePush.Overlays.Widgets.GP_FlexibleGrid>();
                flexible.compactColumns = 2;
                flexible.wideColumns = 4;
            }
            else
            {
                Vertical(content.gameObject, new RectOffset(8, 8, 8, 8), spacing);
            }

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;

            var list = scrollRect.gameObject.AddComponent<GamePush.Overlays.Widgets.GP_OverlayList>();
            list.scrollRect = scroll;
            list.content = content;
            Element(scrollRect.gameObject, flexibleHeight: 1f, flexibleWidth: 1f);
            return list;
        }

        internal static GamePush.Overlays.Widgets.GP_RemoteImage Avatar(string name, Transform parent, float size)
        {
            var image = Panel(name, parent, Color.white, Skin.avatarPlaceholder);
            image.raycastTarget = false;
            Size((RectTransform)image.transform, new Vector2(size, size));
            Element(image.gameObject, minWidth: size, minHeight: size, preferredWidth: size, preferredHeight: size);
            var remote = image.gameObject.AddComponent<GamePush.Overlays.Widgets.GP_RemoteImage>();
            remote.placeholder = Skin.avatarPlaceholder;
            return remote;
        }
    }
}
