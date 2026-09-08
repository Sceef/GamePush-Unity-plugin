using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GamePush.Overlays.Widgets
{
    public sealed class GP_GameCard : MonoBehaviour
    {
        public Image background;
        public GP_RemoteImage icon;
        public TMP_Text nameLabel;
        public TMP_Text playLabel;
        public Button button;

        public void Bind(Games game, int index)
        {
            var skin = GP_OverlaySkin.Instance;

            if (background != null)
                background.color = skin.RowColor(index);

            if (nameLabel != null)
            {
                nameLabel.text = game.name ?? "";
                nameLabel.color = skin.text;
            }

            if (playLabel != null)
            {
                playLabel.text = GP_OverlayStrings.Play;
                playLabel.color = skin.accent;
            }

            if (icon != null)
                icon.Load(game.icon, skin.iconPlaceholder);

            if (button == null)
                return;
            button.onClick.RemoveAllListeners();
            var url = game.url;
            button.interactable = !string.IsNullOrEmpty(url);
            if (!string.IsNullOrEmpty(url))
                button.onClick.AddListener(() => Application.OpenURL(url));
        }
    }
}
