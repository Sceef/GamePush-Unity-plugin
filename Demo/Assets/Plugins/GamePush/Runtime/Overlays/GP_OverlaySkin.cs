using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GamePush.Overlays
{
    [Serializable]
    public sealed class GP_OverlayPrefabEntry
    {
        public GP_OverlayKind kind;

        [Tooltip("Generated GamePush prefab. Rebuild updates this reference.")]
        public GameObject prefab;

        [Tooltip("Optional project-owned prefab. Rebuild never changes or overwrites it.")]
        public GameObject customPrefab;

        [Tooltip("Upper bound on the panel's width/height ratio. 1 keeps the panel inside a square.")]
        public float maxAspect = 1f;

        [Tooltip("Panel size limits in reference-resolution units.")]
        public Vector2 minSize = new Vector2(320f, 320f);

        public Vector2 maxSize = new Vector2(1400f, 1800f);
    }

    /// <summary>
    /// Everything a game can restyle without touching plugin code: the prefab per overlay,
    /// the row prefabs used by the lists, the palette and the font.
    /// </summary>
    [CreateAssetMenu(menuName = "GamePush/Overlay Skin", fileName = "GP_OverlaySkin")]
    public sealed class GP_OverlaySkin : ScriptableObject
    {
        public const string ResourcePath = "GamePush/GP_OverlaySkin";

        [Header("Screens")]
        public List<GP_OverlayPrefabEntry> screens = new List<GP_OverlayPrefabEntry>();

        [Header("Rows")]
        public GameObject achievementRow;
        public GameObject leaderboardRow;
        public GameObject messageRow;
        public GameObject memberRow;
        public GameObject gameCard;
        public GameObject feedbackRow;

        [Header("Layout")]
        public Vector2 referenceResolution = new Vector2(1080f, 1920f);

        [Tooltip("Margin between the safe area and the panel, in reference units.")]
        public Vector2 screenPadding = new Vector2(48f, 48f);

        [Tooltip("Panel aspect at which the views switch from Compact to Wide.")]
        public float wideThreshold = 1.15f;

        [Header("Spacing")]
        [Min(0f)] public float spacingSmall = 8f;
        [Min(0f)] public float spacing = 16f;
        [Min(0f)] public float spacingLarge = 24f;
        [Min(0f)] public float contentPadding = 24f;
        [Min(32f)] public float controlHeight = 64f;
        [Min(32f)] public float compactControlHeight = 52f;

        [Header("Palette")]
        public Color backdrop = new Color(0f, 0f, 0f, 0.65f);
        public Color panel = new Color(0.055f, 0.067f, 0.078f, 1f);
        public Color header = new Color(0.075f, 0.086f, 0.098f, 1f);
        public Color row = new Color(0.09f, 0.102f, 0.114f, 1f);
        public Color rowAlt = new Color(0.115f, 0.126f, 0.14f, 1f);
        public Color input = new Color(0.065f, 0.075f, 0.086f, 1f);
        public Color border = new Color(0.22f, 0.24f, 0.27f, 1f);
        public Color hover = new Color(0.15f, 0.17f, 0.19f, 1f);
        public Color pressed = new Color(0.10f, 0.46f, 0.28f, 1f);
        public Color disabled = new Color(0.20f, 0.21f, 0.23f, 0.75f);
        public Color accent = new Color(0.10f, 0.72f, 0.39f, 1f);
        public Color danger = new Color(0.90f, 0.31f, 0.31f, 1f);
        public Color text = new Color(0.95f, 0.96f, 0.98f, 1f);
        public Color textMuted = new Color(0.64f, 0.67f, 0.74f, 1f);

        [Header("Typography")]
        public TMP_FontAsset font;
        public float titleSize = 42f;
        public float bodySize = 32f;
        public float captionSize = 26f;

        [Header("Sprites")]
        public Sprite panelSprite;
        public Sprite rowSprite;
        public Sprite buttonSprite;
        public Sprite avatarPlaceholder;
        public Sprite iconPlaceholder;

        public ColorBlock ButtonColors(Color normal)
        {
            return new ColorBlock
            {
                normalColor = normal,
                highlightedColor = normal == accent ? accent : hover,
                pressedColor = normal == accent ? pressed : rowAlt,
                selectedColor = normal,
                disabledColor = disabled,
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
        }

        static GP_OverlaySkin _instance;

        public static GP_OverlaySkin Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;
                _instance = Resources.Load<GP_OverlaySkin>(ResourcePath);
                if (_instance == null)
                {
                    _instance = CreateInstance<GP_OverlaySkin>();
                    _instance.name = "GP_OverlaySkin (runtime default)";
                }
                return _instance;
            }
        }

        public GP_OverlayPrefabEntry Find(GP_OverlayKind kind)
        {
            foreach (var entry in screens)
            {
                if (entry != null && entry.kind == kind)
                    return entry;
            }
            return null;
        }

        public GameObject PrefabFor(GP_OverlayKind kind)
        {
            var custom = GP_Overlays.GetPrefabOverride(kind);
            if (custom != null)
                return custom;
            var entry = Find(kind);
            if (entry != null)
            {
                if (entry.customPrefab != null)
                    return entry.customPrefab;
                if (entry.prefab != null)
                    return entry.prefab;
            }
            return Resources.Load<GameObject>("GamePush/Overlays/" + kind);
        }

        public Color RowColor(int index) => index % 2 == 0 ? row : rowAlt;
    }
}
