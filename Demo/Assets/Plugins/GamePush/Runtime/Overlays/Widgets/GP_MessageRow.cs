using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GamePush.Native;

namespace GamePush.Overlays.Widgets
{
    public sealed class GP_MessageRow : MonoBehaviour
    {
        public Image bubble;
        public GP_RemoteImage avatar;
        public TMP_Text authorLabel;
        public TMP_Text textLabel;
        public TMP_Text timeLabel;
        public HorizontalLayoutGroup layout;
        public Button deleteButton;

        /// <summary>Feedback threads carry no player object, only an author role and a timestamp.</summary>
        public void Bind(FeedbackMessageData message, bool isOwn)
        {
            Bind(new NativeChatMessage
            {
                id = message.id,
                text = message.text ?? "",
                createdAt = message.createdAt ?? "",
                authorName = message.author ?? ""
            }, isOwn, null);
        }

        public void Bind(NativeChatMessage message, bool isOwn, Action onDelete)
        {
            var skin = GP_OverlaySkin.Instance;

            if (bubble != null)
                bubble.color = isOwn ? skin.accent : skin.row;

            if (authorLabel != null)
            {
                authorLabel.text = isOwn
                    ? GP_OverlayStrings.You
                    : string.IsNullOrEmpty(message.authorName) ? "#" + message.authorId : message.authorName;
                authorLabel.color = skin.textMuted;
            }

            if (textLabel != null)
            {
                textLabel.text = message.text;
                textLabel.color = skin.text;
            }

            if (timeLabel != null)
            {
                timeLabel.text = FormatTime(message.createdAt);
                timeLabel.color = skin.textMuted;
            }

            if (avatar != null)
                avatar.Load(message.authorAvatar, skin.avatarPlaceholder);

            // Own messages hug the right edge, everyone else's the left.
            if (layout != null)
                layout.reverseArrangement = isOwn;

            if (deleteButton == null)
                return;
            deleteButton.gameObject.SetActive(isOwn && onDelete != null);
            deleteButton.onClick.RemoveAllListeners();
            if (isOwn && onDelete != null)
                deleteButton.onClick.AddListener(() => onDelete());
        }

        static string FormatTime(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return "";
            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var utc))
                return utc.ToLocalTime().ToString("HH:mm", CultureInfo.CurrentCulture);
            return "";
        }
    }
}
