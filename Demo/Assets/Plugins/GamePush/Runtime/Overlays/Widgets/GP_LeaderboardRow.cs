using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GamePush.Native;

namespace GamePush.Overlays.Widgets
{
    public sealed class GP_LeaderboardRow : MonoBehaviour
    {
        public Image background;
        public TMP_Text positionLabel;
        public GP_RemoteImage avatar;
        public TMP_Text nameLabel;
        public TMP_Text scoreLabel;

        [Tooltip("Extra columns filled from includeFields; only shown in Wide mode.")]
        public RectTransform extraColumns;

        public TMP_Text extraColumnTemplate;

        readonly List<TMP_Text> _extras = new List<TMP_Text>();

        public void Bind(NativeLeaderboardEntry entry, IReadOnlyList<NativeLeaderboardField> fields, int index,
            bool isSelf, GP_LayoutMode mode)
        {
            var skin = GP_OverlaySkin.Instance;

            if (background != null)
                background.color = isSelf ? skin.accent : skin.RowColor(index);

            if (positionLabel != null)
            {
                positionLabel.text = entry.position > 0 ? entry.position.ToString() : (index + 1).ToString();
                positionLabel.color = skin.text;
            }

            if (nameLabel != null)
            {
                var displayName = string.IsNullOrEmpty(entry.name) ? GP_OverlayStrings.You : entry.name;
                nameLabel.text = isSelf ? displayName + " (" + GP_OverlayStrings.You + ")" : displayName;
                nameLabel.color = skin.text;
            }

            if (scoreLabel != null)
            {
                scoreLabel.text = Format(entry.score);
                scoreLabel.color = skin.text;
            }

            if (avatar != null)
                avatar.Load(entry.avatar, skin.avatarPlaceholder);

            BindExtras(entry, fields, mode);
        }

        void BindExtras(NativeLeaderboardEntry entry, IReadOnlyList<NativeLeaderboardField> fields,
            GP_LayoutMode mode)
        {
            if (extraColumns == null || extraColumnTemplate == null)
                return;

            var visible = mode == GP_LayoutMode.Wide && fields != null ? fields.Count : 0;
            extraColumns.gameObject.SetActive(visible > 0);

            for (var i = _extras.Count; i < visible; i++)
            {
                var label = Instantiate(extraColumnTemplate, extraColumns);
                label.gameObject.SetActive(true);
                _extras.Add(label);
            }

            for (var i = 0; i < _extras.Count; i++)
            {
                var show = i < visible;
                _extras[i].gameObject.SetActive(show);
                if (!show)
                    continue;
                var field = fields[i];
                _extras[i].text = entry.Get(field.key);
                _extras[i].color = GP_OverlaySkin.Instance.textMuted;
            }
        }

        static string Format(double value)
        {
            if (System.Math.Abs(value - System.Math.Round(value)) < 0.0001)
                return ((long)System.Math.Round(value)).ToString();
            return value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
