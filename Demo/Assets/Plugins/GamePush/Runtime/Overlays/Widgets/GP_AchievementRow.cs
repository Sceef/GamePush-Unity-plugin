using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GamePush.Overlays.Widgets
{
    public sealed class GP_AchievementRow : MonoBehaviour
    {
        public GP_RemoteImage icon;
        public Image background;
        public TMP_Text titleLabel;
        public TMP_Text descriptionLabel;
        public TMP_Text progressLabel;
        public Image progressFill;
        public GameObject lockedBadge;
        public GameObject unlockedBadge;

        public void Bind(AchievementsFetch achievement, AchievementsFetchPlayer progress, int index)
        {
            var skin = GP_OverlaySkin.Instance;
            var unlocked = progress != null && progress.unlocked;
            var current = progress?.progress ?? 0;
            var max = Mathf.Max(0, achievement.maxProgress);

            // A locked achievement can be configured to hide its name and/or description.
            var showName = unlocked || achievement.lockedVisible;
            var showDescription = unlocked || achievement.lockedDescriptionVisible;

            if (background != null)
                background.color = skin.RowColor(index);

            if (titleLabel != null)
            {
                titleLabel.text = showName ? achievement.name : GP_OverlayStrings.HiddenAchievement;
                titleLabel.color = unlocked ? skin.text : skin.textMuted;
            }

            if (descriptionLabel != null)
            {
                descriptionLabel.text = showDescription ? achievement.description : "";
                descriptionLabel.color = skin.textMuted;
                descriptionLabel.gameObject.SetActive(!string.IsNullOrEmpty(descriptionLabel.text));
            }

            if (icon != null)
            {
                var url = unlocked ? achievement.icon : achievement.lockedIcon;
                if (string.IsNullOrEmpty(url))
                    url = achievement.icon;
                icon.Load(url, skin.iconPlaceholder);
            }

            var hasProgress = max > 0;
            if (progressFill != null)
            {
                progressFill.transform.parent.gameObject.SetActive(hasProgress);
                progressFill.fillAmount = hasProgress ? Mathf.Clamp01((float)current / max) : 0f;
                progressFill.color = unlocked ? skin.accent : skin.textMuted;
            }

            if (progressLabel != null)
            {
                progressLabel.gameObject.SetActive(hasProgress);
                progressLabel.text = hasProgress ? current + " / " + max : "";
                progressLabel.color = skin.textMuted;
            }

            if (lockedBadge != null)
                lockedBadge.SetActive(!unlocked);
            if (unlockedBadge != null)
                unlockedBadge.SetActive(unlocked);
        }
    }
}
