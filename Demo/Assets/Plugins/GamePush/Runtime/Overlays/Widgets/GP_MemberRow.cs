using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GamePush.Overlays.Widgets
{
    public sealed class GP_MemberRow : MonoBehaviour
    {
        public Image background;
        public GP_RemoteImage avatar;
        public TMP_Text nameLabel;
        public TMP_Text stateLabel;
        public Image onlineDot;
        public Button muteButton;
        public Button kickButton;

        public void Bind(string json, int index, bool canMute, bool canKick, Action onMute, Action onKick)
        {
            var skin = GP_OverlaySkin.Instance;

            var id = Native.GpJson.GetInt(json, "id");
            var isOnline = Native.GpJson.GetBool(json, "isOnline");
            var state = Native.GpJson.GetObject(json, "state");
            var mute = Native.GpJson.GetObject(json, "mute");
            var isMuted = mute != null && Native.GpJson.GetBool(mute, "isMuted");

            var displayName = "";
            var avatarUrl = "";
            if (!string.IsNullOrEmpty(state))
            {
                if (Native.GpJson.TryGetString(state, "name", out var stateName))
                    displayName = stateName ?? "";
                if (Native.GpJson.TryGetString(state, "avatar", out var stateAvatar))
                    avatarUrl = stateAvatar ?? "";
            }

            if (background != null)
                background.color = skin.RowColor(index);

            if (nameLabel != null)
            {
                nameLabel.text = string.IsNullOrEmpty(displayName) ? "#" + id : displayName;
                nameLabel.color = skin.text;
            }

            if (stateLabel != null)
            {
                stateLabel.text = isMuted ? "🔇" : "";
                stateLabel.color = skin.textMuted;
            }

            if (onlineDot != null)
                onlineDot.color = isOnline ? skin.accent : skin.textMuted;

            if (avatar != null)
                avatar.Load(avatarUrl, skin.avatarPlaceholder);

            Wire(muteButton, canMute, onMute);
            Wire(kickButton, canKick, onKick);
        }

        static void Wire(Button button, bool visible, Action action)
        {
            if (button == null)
                return;
            button.gameObject.SetActive(visible && action != null);
            button.onClick.RemoveAllListeners();
            if (visible && action != null)
                button.onClick.AddListener(() => action());
        }
    }
}
