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
        float _lastViewportWidth = -1f;

        public override void Bind(object args)
        {
            var data = args as GP_DocumentArgs ?? new GP_DocumentArgs();
            SetTitle(GP_OverlayStrings.Document);
            ShowLoading();
            if (contentLabel != null)
                contentLabel.text = "";

            ApplyTextWidth();

            NativeDocuments.FetchForOverlay(data.type, data.format, OnLoaded, ShowError);
        }

        protected override void Update()
        {
            base.Update();
            ApplyTextWidth();
        }

        void ApplyTextWidth()
        {
            if (contentLayout == null)
                return;
            var viewportWidth = scrollRect != null && scrollRect.viewport != null
                ? scrollRect.viewport.rect.width
                : maxTextWidth;
            if (viewportWidth <= 0f || Mathf.Approximately(viewportWidth, _lastViewportWidth))
                return;
            _lastViewportWidth = viewportWidth;
            contentLayout.preferredWidth = Mathf.Min(maxTextWidth, Mathf.Max(1f, viewportWidth - 64f));
            contentLayout.flexibleWidth = 0f;
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
