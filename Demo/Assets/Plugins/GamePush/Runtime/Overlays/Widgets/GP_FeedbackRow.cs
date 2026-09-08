using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GamePush.Overlays.Widgets
{
    public sealed class GP_FeedbackRow : MonoBehaviour
    {
        public Image background;
        public TMP_Text textLabel;
        public TMP_Text statusLabel;
        public TMP_Text dateLabel;
        public Button button;

        public void Bind(FeedbackData feedback, int index, bool selected, Action onClick)
        {
            var skin = GP_OverlaySkin.Instance;

            if (background != null)
                background.color = selected ? skin.accent : skin.RowColor(index);

            if (textLabel != null)
            {
                textLabel.text = string.IsNullOrEmpty(feedback.text) ? GP_OverlayStrings.NewFeedback : feedback.text;
                textLabel.color = skin.text;
            }

            if (statusLabel != null)
            {
                statusLabel.text = feedback.status ?? "";
                statusLabel.color = skin.textMuted;
            }

            if (dateLabel != null)
            {
                dateLabel.text = FormatDate(feedback.createdAt);
                dateLabel.color = skin.textMuted;
            }

            if (button == null)
                return;
            button.onClick.RemoveAllListeners();
            if (onClick != null)
                button.onClick.AddListener(() => onClick());
        }

        static string FormatDate(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return "";
            return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var utc)
                ? utc.ToLocalTime().ToString("dd.MM.yyyy", CultureInfo.CurrentCulture)
                : "";
        }
    }
}
