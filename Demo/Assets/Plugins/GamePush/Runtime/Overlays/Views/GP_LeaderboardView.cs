using UnityEngine;
using TMPro;
using GamePush.Native;
using GamePush.Overlays.Widgets;

namespace GamePush.Overlays.Views
{
    public sealed class GP_LeaderboardView : GP_OverlayView
    {
        public GP_OverlayList list;
        public GP_OverlayLayoutMode layoutMode;

        [Header("Pinned player row")]
        public GameObject selfRowHolder;

        public GP_LeaderboardRow selfRow;
        public TMP_Text subtitleLabel;

        NativeLeaderboardResult _result;

        public override void Bind(object args)
        {
            var data = args as GP_LeaderboardArgs ?? new GP_LeaderboardArgs();
            SetTitle(GP_OverlayStrings.Leaderboard);
            SetStatus(GP_OverlayStrings.Loading);
            list?.Clear();
            if (selfRowHolder != null)
                selfRowHolder.SetActive(false);

            if (layoutMode != null)
                layoutMode.ModeChanged += OnModeChanged;

            if (data.scoped)
            {
                NativeLeaderboardScoped.FetchForOverlay(data.idOrTag, data.variant, data.order, data.limit,
                    data.showNearest, data.includeFields, data.withMe, OnLoaded, ShowError);
            }
            else
            {
                NativeLeaderboard.FetchForOverlay(data.idOrTag, data.orderBy, data.order, data.limit,
                    data.showNearest, data.withMe, data.includeFields, OnLoaded, ShowError);
            }
        }

        protected override void OnClosing()
        {
            if (layoutMode != null)
                layoutMode.ModeChanged -= OnModeChanged;
        }

        void OnModeChanged(GP_LayoutMode mode) => Render();

        void OnLoaded(NativeLeaderboardResult result)
        {
            _result = result;
            if (!string.IsNullOrEmpty(result?.name))
                SetTitle(result.name);
            Render();
        }

        void Render()
        {
            if (_result == null)
                return;

            var mode = layoutMode != null ? layoutMode.Mode : GP_LayoutMode.Compact;
            var selfId = NativePlayer.Id;

            if (subtitleLabel != null)
            {
                subtitleLabel.text = _result.player != null && _result.player.position > 0
                    ? GP_OverlayStrings.Position + ": " + _result.player.position
                    : "";
                subtitleLabel.color = Skin.textMuted;
                subtitleLabel.gameObject.SetActive(!string.IsNullOrEmpty(subtitleLabel.text));
            }

            if (_result.players.Count == 0)
            {
                ShowEmpty();
                list?.Clear();
                return;
            }

            SetStatus(null);
            list?.Bind(_result.players.Count, (row, index) =>
            {
                var component = row.GetComponent<GP_LeaderboardRow>();
                if (component == null)
                    return;
                var entry = _result.players[index];
                component.Bind(entry, _result.fields, index, entry.id == selfId, mode);
            }, Skin.leaderboardRow);

            // Keep the player visible even after scrolling away from their position.
            var pinned = _result.player;
            if (selfRowHolder == null || selfRow == null || pinned == null)
                return;
            selfRowHolder.SetActive(true);
            selfRow.Bind(pinned, _result.fields, 0, true, mode);
        }
    }
}
