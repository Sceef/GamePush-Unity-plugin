using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace GamePush.Overlays
{
    [Serializable]
    public sealed class GP_OverlayPrefabEntry
    {
        public GP_OverlayKind kind;
        public GameObject prefab;

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

        [Header("Palette")]
        public Color backdrop = new Color(0f, 0f, 0f, 0.65f);
        public Color panel = new Color(0.11f, 0.12f, 0.15f, 1f);
        public Color header = new Color(0.15f, 0.16f, 0.21f, 1f);
        public Color row = new Color(0.16f, 0.17f, 0.22f, 1f);
        public Color rowAlt = new Color(0.19f, 0.20f, 0.26f, 1f);
        public Color accent = new Color(0.29f, 0.56f, 0.99f, 1f);
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
            if (entry != null && entry.prefab != null)
                return entry.prefab;
            return Resources.Load<GameObject>("GamePush/Overlays/" + kind);
        }

        public Color RowColor(int index) => index % 2 == 0 ? row : rowAlt;
    }
}
