using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GamePush.Native;
using GamePush.Overlays.Widgets;

namespace GamePush.Overlays.Views
{
    public sealed class GP_AchievementsView : GP_OverlayView
    {
        public GP_OverlayList list;
        public GP_OverlayLayoutMode layoutMode;

        [Header("Group filter (left rail in Wide, chips in Compact)")]
        public RectTransform groupRail;

        public Button groupButtonTemplate;
        public TMP_Text counterLabel;

        readonly List<AchievementsFetch> _all = new List<AchievementsFetch>();
        readonly List<AchievementsFetch> _visible = new List<AchievementsFetch>();
        readonly List<AchievementsFetchGroups> _groups = new List<AchievementsFetchGroups>();
        readonly List<Button> _groupButtons = new List<Button>();

        int _selectedGroup = -1;

        public override void Bind(object args)
        {
            SetTitle(GP_OverlayStrings.Achievements);
            ShowLoading();
            list?.Clear();

            GP_Achievements.OnAchievementsFetch += OnFetched;
            GP_Achievements.OnAchievementsFetchGroups += OnGroups;
            GP_Achievements.OnAchievementsFetchError += OnError;
            GP_Achievements.Fetch();
        }

        protected override void OnClosing()
        {
            GP_Achievements.OnAchievementsFetch -= OnFetched;
            GP_Achievements.OnAchievementsFetchGroups -= OnGroups;
            GP_Achievements.OnAchievementsFetchError -= OnError;
        }

        void OnError() => ShowError(null);

        void OnGroups(List<AchievementsFetchGroups> groups)
        {
            _groups.Clear();
            if (groups != null)
                _groups.AddRange(groups);
            BuildGroupRail();
        }

        void OnFetched(List<AchievementsFetch> achievements)
        {
            _all.Clear();
            if (achievements != null)
                _all.AddRange(achievements);
            Refresh();
        }

        void BuildGroupRail()
        {
            if (groupRail == null || groupButtonTemplate == null)
                return;

            var needed = _groups.Count + 1;
            for (var i = _groupButtons.Count; i < needed; i++)
            {
                var button = Instantiate(groupButtonTemplate, groupRail);
                button.gameObject.SetActive(true);
                _groupButtons.Add(button);
            }

            for (var i = 0; i < _groupButtons.Count; i++)
            {
                var button = _groupButtons[i];
                var show = i < needed;
                button.gameObject.SetActive(show);
                if (!show)
                    continue;

                var groupIndex = i - 1;
                var label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = groupIndex < 0 ? GP_OverlayStrings.Achievements : _groups[groupIndex].name;
                    label.color = groupIndex == _selectedGroup ? Skin.accent : Skin.textMuted;
                }

                button.onClick.RemoveAllListeners();
                var captured = groupIndex;
                button.onClick.AddListener(() =>
                {
                    _selectedGroup = captured;
                    BuildGroupRail();
                    Refresh();
                });
            }

            groupRail.gameObject.SetActive(_groups.Count > 0);
        }

        void Refresh()
        {
            _visible.Clear();
            if (_selectedGroup < 0 || _selectedGroup >= _groups.Count)
            {
                _visible.AddRange(_all);
            }
            else
            {
                var ids = _groups[_selectedGroup].achievements;
                foreach (var achievement in _all)
                {
                    if (System.Array.IndexOf(ids, achievement.id) >= 0)
                        _visible.Add(achievement);
                }
            }

            if (_visible.Count == 0)
            {
                ShowEmpty();
                list?.Clear();
                UpdateCounter();
                return;
            }

            SetStatus(null);
            list?.Bind(_visible.Count, (row, index) =>
            {
                var component = row.GetComponent<GP_AchievementRow>();
                if (component == null)
                    return;
                var achievement = _visible[index];
                component.Bind(achievement, NativeAchievements.PlayerEntry(achievement.id), index);
            }, Skin.achievementRow);
            UpdateCounter();
        }

        void UpdateCounter()
        {
            if (counterLabel == null)
                return;
            var unlocked = 0;
            foreach (var achievement in _visible)
            {
                var entry = NativeAchievements.PlayerEntry(achievement.id);
                if (entry != null && entry.unlocked)
                    unlocked++;
            }
            counterLabel.text = unlocked + " / " + _visible.Count;
            counterLabel.color = Skin.textMuted;
        }
    }
}
