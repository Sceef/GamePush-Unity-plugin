using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GamePush.Native;
using GamePush.Overlays.Widgets;

namespace GamePush.Overlays.Views
{
    public sealed class GP_DocumentView : GP_OverlayView
    {
        public ScrollRect scrollRect;
        public TMP_Text contentLabel;

        [Tooltip("Documents stay readable only up to a certain line length, even on a wide panel.")]
        public LayoutElement contentLayout;

        public float maxTextWidth = 700f;

        public override void Bind(object args)
        {
            var data = args as GP_DocumentArgs ?? new GP_DocumentArgs();
            SetTitle(GP_OverlayStrings.Document);
            ShowLoading();
            if (contentLabel != null)
                contentLabel.text = "";

            if (contentLayout != null)
            {
                contentLayout.preferredWidth = maxTextWidth;
                contentLayout.flexibleWidth = 0f;
            }

            NativeDocuments.FetchForOverlay(data.type, data.format, OnLoaded, ShowError);
        }

        void OnLoaded(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                ShowEmpty();
                return;
            }
            SetStatus(null);
            if (contentLabel != null)
            {
                contentLabel.text = content;
                contentLabel.color = Skin.text;
            }
            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }
    }
}
