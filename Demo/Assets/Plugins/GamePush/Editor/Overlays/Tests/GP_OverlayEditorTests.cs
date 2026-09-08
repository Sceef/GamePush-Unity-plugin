using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GamePush;
using GamePush.Native;
using GamePush.Overlays;
using GamePush.Overlays.Views;
using GamePush.Overlays.Widgets;

namespace GamePushEditor.Overlays.Tests
{
    public sealed class GP_OverlayEditorTests
    {
        [Test]
        public void BuilderCreatesEveryOverlayView()
        {
            var skin = GP_OverlayPrefabBuilder.DefaultSkin;
            foreach (GP_OverlayKind kind in System.Enum.GetValues(typeof(GP_OverlayKind)))
            {
                var root = GP_OverlayPrefabBuilder.BuildPreview(kind, skin);
                try
                {
                    Assert.That(root, Is.Not.Null, kind.ToString());
                    Assert.That(root.GetComponent<GP_OverlayView>(), Is.Not.Null, kind.ToString());
                    Assert.That(root.GetComponent<GP_OverlayView>().panel, Is.Not.Null, kind.ToString());
                }
                finally
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        [Test]
        public void GeneratedOverlayHeaderKeepsCompactHeight()
        {
            var root = GP_OverlayPrefabBuilder.BuildPreview(GP_OverlayKind.Leaderboard,
                GP_OverlayPrefabBuilder.DefaultSkin);
            try
            {
                var view = root.GetComponent<GP_OverlayView>();
                view.panel.sizeDelta = new Vector2(1450f, 1000f);
                LayoutRebuilder.ForceRebuildLayoutImmediate(view.panel);
                var header = view.titleLabel.rectTransform.parent as RectTransform;

                Assert.That(header, Is.Not.Null);
                Assert.That(header.rect.height, Is.EqualTo(72f).Within(0.1f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CustomPrefabTakesPriorityWithoutReplacingGeneratedPrefab()
        {
            var skin = ScriptableObject.CreateInstance<GP_OverlaySkin>();
            var generated = new GameObject("Generated");
            var custom = new GameObject("Custom");
            skin.screens.Add(new GP_OverlayPrefabEntry
            {
                kind = GP_OverlayKind.Confirm,
                prefab = generated,
                customPrefab = custom
            });

            try
            {
                Assert.That(skin.PrefabFor(GP_OverlayKind.Confirm), Is.SameAs(custom));
                Assert.That(skin.Find(GP_OverlayKind.Confirm).prefab, Is.SameAs(generated));
            }
            finally
            {
                Object.DestroyImmediate(generated);
                Object.DestroyImmediate(custom);
                Object.DestroyImmediate(skin);
            }
        }

        [Test]
        public void ResponsiveSizeUsesWideAndCompactShapes()
        {
            var minimum = new Vector2(360f, 400f);
            var maximum = new Vector2(1650f, 1500f);

            var portrait = GP_OverlayResponsive.CalculatePanelSize(
                new Vector2(984f, 1824f), minimum, maximum, 1.65f, 1.15f);
            var landscape = GP_OverlayResponsive.CalculatePanelSize(
                new Vector2(3000f, 1700f), minimum, maximum, 1.65f, 1.15f);

            Assert.That(portrait.x / portrait.y, Is.LessThan(1.15f));
            Assert.That(landscape.x / landscape.y, Is.GreaterThanOrEqualTo(1.15f));
            Assert.That(landscape.x / landscape.y, Is.LessThanOrEqualTo(1.65f + 0.001f));
        }

        [Test]
        public void MobileSheetFillsPortraitAvailable()
        {
            var available = new Vector2(984f, 1824f);
            var size = GP_OverlayResponsive.CalculatePanelSize(available,
                new Vector2(360f, 400f), new Vector2(1650f, 1500f), 1.65f, 1.15f,
                GP_OverlaySizeMode.Sheet);

            Assert.That(size.x, Is.EqualTo(available.x).Within(0.1f));
            Assert.That(size.y, Is.EqualTo(available.y).Within(0.1f));
            Assert.That(size.x, Is.LessThanOrEqualTo(available.x));
        }

        [Test]
        public void MobileSheetFillsLandscapeHeight()
        {
            var available = new Vector2(3000f, 1700f);
            var size = GP_OverlayResponsive.CalculatePanelSize(available,
                new Vector2(360f, 400f), new Vector2(1650f, 1500f), 1.65f, 1.15f,
                GP_OverlaySizeMode.Sheet);

            Assert.That(size.y, Is.EqualTo(available.y).Within(0.1f));
            Assert.That(size.x, Is.LessThanOrEqualTo(available.x));
        }

        [Test]
        public void DialogClampsToNarrowAvailable()
        {
            var available = new Vector2(400f, 700f);
            var size = GP_OverlayResponsive.CalculatePanelSize(available,
                new Vector2(420f, 260f), new Vector2(820f, 620f), 1.6f, 1.15f,
                GP_OverlaySizeMode.Dialog);

            Assert.That(size.x, Is.LessThanOrEqualTo(available.x));
            Assert.That(size.y, Is.LessThanOrEqualTo(available.y));
        }

        [Test]
        public void DocumentLandscapeFillsHeight()
        {
            var available = new Vector2(3000f, 1700f);
            var size = GP_OverlayResponsive.CalculatePanelSize(available,
                new Vector2(480f, 320f), new Vector2(2400f, 1800f), 2f, 1.15f,
                GP_OverlaySizeMode.Document);

            Assert.That(size.y, Is.EqualTo(available.y).Within(0.1f));
            Assert.That(size.x, Is.LessThanOrEqualTo(available.x));
            Assert.That(size.x, Is.GreaterThan(0f));
        }

        [Test]
        public void DocumentPortraitFillsWidth()
        {
            var available = new Vector2(984f, 1824f);
            var size = GP_OverlayResponsive.CalculatePanelSize(available,
                new Vector2(480f, 320f), new Vector2(2400f, 1800f), 2f, 1.15f,
                GP_OverlaySizeMode.Document);

            Assert.That(size.x, Is.EqualTo(available.x).Within(0.1f));
            Assert.That(size.y, Is.EqualTo(available.y).Within(0.1f));
        }

        [Test]
        public void OverlayFitClassifiesKinds()
        {
            Assert.That(GP_OverlayFit.IsSheet(GP_OverlayKind.Achievements), Is.True);
            Assert.That(GP_OverlayFit.IsSheet(GP_OverlayKind.Document), Is.False);
            Assert.That(GP_OverlayFit.IsDialog(GP_OverlayKind.Confirm), Is.True);
            Assert.That(GP_OverlayFit.Resolve(GP_OverlayKind.Achievements, true),
                Is.EqualTo(GP_OverlaySizeMode.Sheet));
            Assert.That(GP_OverlayFit.Resolve(GP_OverlayKind.Achievements, false),
                Is.EqualTo(GP_OverlaySizeMode.Modal));
            Assert.That(GP_OverlayFit.Resolve(GP_OverlayKind.Document, true),
                Is.EqualTo(GP_OverlaySizeMode.Document));
            Assert.That(GP_OverlayFit.IsMobilePreview(new Vector2Int(540, 960)), Is.True);
            Assert.That(GP_OverlayFit.IsMobilePreview(new Vector2Int(1280, 720)), Is.False);
        }

        [Test]
        public void LandscapeReferenceKeepsDesignedContentScale()
        {
            var designed = new Vector2(1080f, 1920f);
            var landscape = GP_OverlayFit.ReferenceFor(designed, 1920, 1080);
            var portrait = GP_OverlayFit.ReferenceFor(designed, 1080, 1920);

            Assert.That(landscape, Is.EqualTo(new Vector2(1920f, 1080f)));
            Assert.That(portrait, Is.EqualTo(new Vector2(1080f, 1920f)));

            var landscapeCanvas = GP_OverlayFit.CanvasLogicalSize(designed, 1920, 1080);
            Assert.That(landscapeCanvas.y, Is.EqualTo(1080f).Within(1f));
            Assert.That(landscapeCanvas.x / landscapeCanvas.y, Is.EqualTo(1920f / 1080f).Within(0.02f));
        }

        [Test]
        public void DocumentReaderUsesLargeWrappingText()
        {
            var root = GP_OverlayPrefabBuilder.BuildPreview(GP_OverlayKind.Document,
                GP_OverlayPrefabBuilder.DefaultSkin);
            try
            {
                var view = root.GetComponent<GP_DocumentView>();
                Assert.That(view, Is.Not.Null);
                Assert.That(view.scrollRect, Is.Not.Null);
                Assert.That(view.scrollRect.horizontal, Is.False);
                Assert.That(view.contentLabel.fontSize, Is.EqualTo(GP_DocumentView.ReaderFontSize).Within(0.1f));
                Assert.That(view.maxTextWidth, Is.EqualTo(0f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AchievementsCounterDoesNotDemandWideHeader()
        {
            var root = GP_OverlayPrefabBuilder.BuildPreview(GP_OverlayKind.Achievements,
                GP_OverlayPrefabBuilder.DefaultSkin);
            try
            {
                var view = root.GetComponent<GP_AchievementsView>();
                var layout = view.counterLabel.GetComponent<LayoutElement>();
                Assert.That(layout.minWidth, Is.LessThanOrEqualTo(0f));
                Assert.That(layout.preferredWidth, Is.LessThanOrEqualTo(200.01f));
                Assert.That(view.counterLabel.overflowMode, Is.EqualTo(TMPro.TextOverflowModes.Ellipsis));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AchievementsGridUsesSingleCompactColumn()
        {
            var root = GP_OverlayPrefabBuilder.BuildPreview(GP_OverlayKind.Achievements,
                GP_OverlayPrefabBuilder.DefaultSkin);
            try
            {
                var grid = root.GetComponentInChildren<GP_FlexibleGrid>(true);
                Assert.That(grid, Is.Not.Null);
                Assert.That(grid.compactColumns, Is.EqualTo(1));
                Assert.That(grid.wideColumns, Is.EqualTo(2));
                Assert.That(grid.cellHeight, Is.EqualTo(120f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CloseButtonDoesNotUseLetterGlyph()
        {
            var root = GP_OverlayPrefabBuilder.BuildPreview(GP_OverlayKind.Achievements,
                GP_OverlayPrefabBuilder.DefaultSkin);
            try
            {
                var close = root.GetComponent<GP_OverlayView>().closeButton;
                Assert.That(close, Is.Not.Null);
                foreach (var text in close.GetComponentsInChildren<TMPro.TMP_Text>(true))
                    Assert.That(text.text, Is.Not.EqualTo("X"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SplitOverlayStatusSitsInContentColumn()
        {
            var chatRoot = GP_OverlayPrefabBuilder.BuildPreview(GP_OverlayKind.Chat,
                GP_OverlayPrefabBuilder.DefaultSkin);
            var feedbackRoot = GP_OverlayPrefabBuilder.BuildPreview(GP_OverlayKind.Feedbacks,
                GP_OverlayPrefabBuilder.DefaultSkin);
            var achievementRoot = GP_OverlayPrefabBuilder.BuildPreview(GP_OverlayKind.Achievements,
                GP_OverlayPrefabBuilder.DefaultSkin);
            try
            {
                var chat = chatRoot.GetComponent<GP_ChatView>();
                Assert.That(chat.statusLabel.transform.parent, Is.SameAs(chat.messagesPanel.transform));
                Assert.That(chat.statusLabel.GetComponent<LayoutElement>().ignoreLayout, Is.True);

                var feedbacks = feedbackRoot.GetComponent<GP_FeedbacksView>();
                Assert.That(feedbacks.statusLabel.transform.parent, Is.SameAs(feedbacks.listPanel.transform));

                var achievements = achievementRoot.GetComponent<GP_AchievementsView>();
                Assert.That(achievements.statusLabel.transform.parent, Is.SameAs(achievements.list.transform));
            }
            finally
            {
                Object.DestroyImmediate(chatRoot);
                Object.DestroyImmediate(feedbackRoot);
                Object.DestroyImmediate(achievementRoot);
            }
        }

        [Test]
        public void AchievementRowUsesStatusChipInsteadOfLetterBadge()
        {
            var row = GP_OverlayPrefabBuilder.BuildRowPreview(GP_OverlayKind.Achievements);
            try
            {
                var achievement = row.GetComponent<GP_AchievementRow>();
                Assert.That(achievement, Is.Not.Null);
                Assert.That(achievement.unlockedBadge, Is.Not.Null);
                var label = achievement.unlockedBadge.GetComponentInChildren<TMPro.TMP_Text>(true);
                Assert.That(label, Is.Not.Null);
                Assert.That(label.text, Is.Not.EqualTo("U"));
                Assert.That(label.text.Length, Is.GreaterThan(1));
            }
            finally
            {
                Object.DestroyImmediate(row);
            }
        }

        [Test]
        public void AchievementRowHidesMissingIconAndKeepsProgressThin()
        {
            var row = GP_OverlayPrefabBuilder.BuildRowPreview(GP_OverlayKind.Achievements);
            try
            {
                var achievement = row.GetComponent<GP_AchievementRow>();
                achievement.Bind(new AchievementsFetch
                {
                    name = "Test",
                    description = "Desc",
                    icon = "",
                    maxProgress = 10,
                    lockedVisible = true,
                    lockedDescriptionVisible = true
                }, new AchievementsFetchPlayer { progress = 3, unlocked = false }, 0);

                Assert.That(achievement.icon.gameObject.activeSelf, Is.False);
                Assert.That(achievement.progressFill.type, Is.Not.EqualTo(Image.Type.Filled));
                Assert.That(achievement.progressFill.rectTransform.anchorMax.x, Is.EqualTo(0.3f).Within(0.01f));
                var bar = achievement.progressFill.transform.parent.GetComponent<LayoutElement>();
                Assert.That(bar.preferredHeight, Is.EqualTo(8f));
                var chipIcon = achievement.lockedBadge.transform.Find("Icon");
                Assert.That(chipIcon, Is.Not.Null);
                Assert.That(chipIcon.GetComponent<GP_LayoutSquare>(), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(row);
            }
        }

        [Test]
        public void LeaderboardValueColumnsHaveFixedWidth()
        {
            var row = GP_OverlayPrefabBuilder.BuildRowPreview(GP_OverlayKind.Leaderboard);
            try
            {
                var leaderboard = row.GetComponent<GP_LeaderboardRow>();
                var fields = new[]
                {
                    new NativeLeaderboardField { key = "score", name = "score" },
                    new NativeLeaderboardField { key = "gold", name = "gold" }
                };
                leaderboard.Bind(new NativeLeaderboardEntry
                {
                    id = 2,
                    position = 2,
                    name = "Очень длинный ник игрока для проверки колонок",
                    score = 10,
                    json = "{\"score\":10,\"gold\":5}"
                }, fields, 1, false, GP_LayoutMode.Wide);
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)row.transform);
                var extra = leaderboard.extraColumns.GetComponent<LayoutElement>();
                Assert.That(extra.flexibleWidth, Is.EqualTo(0f));
                var valueWidth = 0f;
                var counted = 0;
                for (var i = 0; i < leaderboard.extraColumns.childCount; i++)
                {
                    var child = leaderboard.extraColumns.GetChild(i);
                    if (!child.gameObject.activeSelf)
                        continue;
                    var column = child.GetComponent<LayoutElement>();
                    Assert.That(column.preferredWidth, Is.EqualTo(GP_LeaderboardRow.ColumnWidth));
                    Assert.That(column.flexibleWidth, Is.EqualTo(0f));
                    counted++;
                    valueWidth = column.preferredWidth;
                }
                Assert.That(counted, Is.EqualTo(2));
                Assert.That(valueWidth, Is.EqualTo(GP_LeaderboardRow.ColumnWidth));
            }
            finally
            {
                Object.DestroyImmediate(row);
            }
        }

        [Test]
        public void LeaderboardHeaderHidesAvatarPlaceholder()
        {
            var row = GP_OverlayPrefabBuilder.BuildRowPreview(GP_OverlayKind.Leaderboard);
            try
            {
                var leaderboard = row.GetComponent<GP_LeaderboardRow>();
                leaderboard.BindHeader(new[]
                {
                    new NativeLeaderboardField { key = "score", name = "score" }
                });
                Assert.That(leaderboard.avatar, Is.Not.Null);
                Assert.That(leaderboard.avatar.gameObject.activeSelf, Is.True);
                Assert.That(leaderboard.avatar.GetComponent<Image>().enabled, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(row);
            }
        }

        [Test]
        public void LeaderboardVisibleFieldsPreferDisplayNames()
        {
            var fields = new List<NativeLeaderboardField>
            {
                new NativeLeaderboardField { key = "name", name = "Name" },
                new NativeLeaderboardField { key = "gold", name = "Золото" },
                new NativeLeaderboardField { key = "score", name = "Очки" }
            };
            var visible = NativeLeaderboard.VisibleFields(fields, "score, gold");
            Assert.That(visible.Count, Is.EqualTo(2));
            Assert.That(visible[0].key, Is.EqualTo("score"));
            Assert.That(visible[1].name, Is.EqualTo("Золото"));
        }

        [Test]
        public void FeedbackSelectionUsesBarInsteadOfAccentFill()
        {
            var row = GP_OverlayPrefabBuilder.BuildRowPreview(GP_OverlayKind.Feedbacks);
            try
            {
                var feedback = row.GetComponent<GP_FeedbackRow>();
                Assert.That(feedback, Is.Not.Null);
                Assert.That(feedback.selectedBar, Is.Not.Null);
                var skin = GP_OverlayPrefabBuilder.DefaultSkin;
                feedback.Bind(new FeedbackData { text = "Test", status = "new", createdAt = "" }, 0, true, null);
                Assert.That(feedback.background.color, Is.Not.EqualTo(skin.accent));
                Assert.That(feedback.selectedBar.gameObject.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(row);
            }
        }

        [Test]
        public void ChatFixturesHideCompactTabsInWideLayout()
        {
            var skin = GP_OverlayPrefabBuilder.DefaultSkin;
            var root = GP_OverlayPrefabBuilder.BuildPreview(GP_OverlayKind.Chat, skin);
            try
            {
                var mode = root.GetComponentInChildren<GP_OverlayLayoutMode>(true);
                Assert.That(mode, Is.Not.Null);
                mode.Evaluate(1450f, 900f);
                GP_OverlayPreviewFixtures.Populate(root, GP_OverlayKind.Chat, skin,
                    GP_OverlayPreviewState.Content, GP_OverlayPreviewLanguage.Russian);
                var chat = root.GetComponent<GamePush.Overlays.Views.GP_ChatView>();
                Assert.That(chat.compactTabs.activeSelf, Is.False);
                Assert.That(chat.messagesPanel.activeSelf, Is.True);
                Assert.That(chat.membersPanel.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MessageAvatarStaysSquareInLayout()
        {
            var row = GP_OverlayPrefabBuilder.BuildRowPreview(GP_OverlayKind.Chat);
            try
            {
                var avatar = row.GetComponent<GP_MessageRow>().avatar;
                Assert.That(avatar, Is.Not.Null);
                var layout = avatar.GetComponent<LayoutElement>();
                Assert.That(layout, Is.Not.Null);
                Assert.That(layout.preferredWidth, Is.EqualTo(layout.preferredHeight));
                Assert.That(layout.flexibleHeight, Is.EqualTo(0f));
                var horizontal = row.GetComponent<HorizontalLayoutGroup>();
                Assert.That(horizontal.childForceExpandHeight, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(row);
            }
        }

        [Test]
        public void ChatPreviewBuildsCompactTabs()
        {
            var root = GP_OverlayPrefabBuilder.BuildPreview(GP_OverlayKind.Chat,
                GP_OverlayPrefabBuilder.DefaultSkin);
            try
            {
                var chat = root.GetComponent<GamePush.Overlays.Views.GP_ChatView>();
                Assert.That(chat.compactTabs, Is.Not.Null);
                Assert.That(chat.messagesTab, Is.Not.Null);
                Assert.That(chat.membersTab, Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MemberOnlineDotStaysSquareInLayout()
        {
            var row = GP_OverlayPrefabBuilder.BuildMemberRowPreview();
            try
            {
                var member = row.GetComponent<GP_MemberRow>();
                Assert.That(member.onlineDot, Is.Not.Null);
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)row.transform);
                var rect = member.onlineDot.rectTransform.rect;
                Assert.That(rect.width, Is.EqualTo(rect.height).Within(0.5f));
                Assert.That(member.onlineDot.preserveAspect, Is.True);
                Assert.That(row.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight, Is.False);
                Assert.That(member.onlineDot.GetComponent<GP_LayoutSquare>(), Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(row);
            }
        }

        [Test]
        public void ApplyChromePaintsPanelHeaderAndBackdropFromSkin()
        {
            var root = GP_OverlayPrefabBuilder.BuildPreview(GP_OverlayKind.Leaderboard,
                GP_OverlayPrefabBuilder.DefaultSkin);
            var skin = ScriptableObject.CreateInstance<GP_OverlaySkin>();
            try
            {
                var view = root.GetComponent<GP_OverlayView>();
                view.panel.GetComponent<Image>().color = Color.magenta;
                view.backdrop.color = Color.magenta;
                var header = view.titleLabel.transform.parent.GetComponent<Image>();
                header.color = Color.magenta;

                skin.panel = Color.red;
                skin.header = Color.green;
                skin.backdrop = new Color(0f, 0f, 1f, 0.5f);
                view.ApplyChrome(skin);

                Assert.That(view.panel.GetComponent<Image>().color, Is.EqualTo(skin.panel));
                Assert.That(header.color, Is.EqualTo(skin.header));
                Assert.That(view.backdrop.color, Is.EqualTo(skin.backdrop));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(skin);
            }
        }

        [Test]
        public void PalettePaintUpdatesButtonsAndKeepsInputOffPanel()
        {
            var root = GP_OverlayPrefabBuilder.BuildPreview(GP_OverlayKind.Achievements,
                GP_OverlayPrefabBuilder.DefaultSkin);
            var skin = ScriptableObject.CreateInstance<GP_OverlaySkin>();
            try
            {
                skin.CopyAppearanceFrom(GP_OverlayPrefabBuilder.DefaultSkin);
                skin.button = Color.blue;
                var view = root.GetComponent<GP_OverlayView>();
                view.ApplyChrome(skin);
                var close = view.closeButton;
                Assert.That(close.colors.normalColor, Is.EqualTo(skin.button));

                var chip = root.GetComponentInChildren<GP_OverlayChip>(true);
                chip.Bind("All", "1", false);
                Assert.That(chip.GetComponent<GP_OverlayTone>().role, Is.EqualTo(GP_OverlayColorRole.Button));
                skin.button = Color.red;
                view.ApplyChrome(skin);
                Assert.That(chip.GetComponent<Button>().colors.normalColor, Is.EqualTo(skin.button));

                var panelTone = view.panel.GetComponent<GP_OverlayTone>();
                Assert.That(panelTone, Is.Not.Null);
                Assert.That(panelTone.role, Is.EqualTo(GP_OverlayColorRole.Panel));
                var progress = FindNamed(root.transform, "ProgressBar");
                if (progress != null)
                    Assert.That(progress.GetComponent<GP_OverlayTone>().role,
                        Is.EqualTo(GP_OverlayColorRole.Input));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(skin);
            }
        }

        [Test]
        public void ApplyChromePaintsHeaderRailWithoutPrefabRebuild()
        {
            var root = GP_OverlayPrefabBuilder.BuildPreview(GP_OverlayKind.Achievements,
                GP_OverlayPrefabBuilder.DefaultSkin);
            var skin = ScriptableObject.CreateInstance<GP_OverlaySkin>();
            try
            {
                skin.CopyAppearanceFrom(GP_OverlayPrefabBuilder.DefaultSkin);
                skin.header = Color.red;
                skin.sidebar = Color.blue;
                var view = root.GetComponent<GP_OverlayView>();
                view.ApplyChrome(skin);
                var bar = view.titleLabel.transform.parent.GetComponent<Image>();
                var rail = FindNamed(root.transform, "WideGroups");
                Assert.That(bar.color, Is.EqualTo(skin.header));
                Assert.That(rail, Is.Not.Null);
                Assert.That(rail.GetComponent<Image>().color, Is.EqualTo(skin.sidebar));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(skin);
            }
        }

        [Test]
        public void RowColorDoesNotTintCloseButton()
        {
            var root = GP_OverlayPrefabBuilder.BuildPreview(GP_OverlayKind.Achievements,
                GP_OverlayPrefabBuilder.DefaultSkin);
            var skin = ScriptableObject.CreateInstance<GP_OverlaySkin>();
            try
            {
                skin.CopyAppearanceFrom(GP_OverlayPrefabBuilder.DefaultSkin);
                skin.row = Color.magenta;
                skin.button = Color.cyan;
                var view = root.GetComponent<GP_OverlayView>();
                view.ApplyChrome(skin);
                Assert.That(view.closeButton.colors.normalColor, Is.EqualTo(skin.button));
                Assert.That(view.closeButton.GetComponent<GP_OverlayTone>().role,
                    Is.EqualTo(GP_OverlayColorRole.Button));
                view.closeButton.GetComponent<GP_OverlayTone>().role = GP_OverlayColorRole.Row;
                view.ApplyChrome(skin);
                Assert.That(view.closeButton.GetComponent<GP_OverlayTone>().role,
                    Is.EqualTo(GP_OverlayColorRole.Button));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(skin);
            }
        }

        [Test]
        public void HeaderColorDoesNotTintSidebarRail()
        {
            var root = GP_OverlayPrefabBuilder.BuildPreview(GP_OverlayKind.Chat,
                GP_OverlayPrefabBuilder.DefaultSkin);
            var skin = ScriptableObject.CreateInstance<GP_OverlaySkin>();
            try
            {
                skin.CopyAppearanceFrom(GP_OverlayPrefabBuilder.DefaultSkin);
                skin.header = Color.red;
                skin.sidebar = Color.green;
                var view = root.GetComponent<GP_OverlayView>();
                view.ApplyChrome(skin);
                var compact = FindNamed(root.transform, "CompactTabs");
                Assert.That(compact, Is.Not.Null);
                Assert.That(compact.GetComponent<Image>().color, Is.EqualTo(skin.sidebar));
                Assert.That(view.titleLabel.transform.parent.GetComponent<Image>().color,
                    Is.EqualTo(skin.header));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(skin);
            }
        }

        [Test]
        public void ButtonSelectedDoesNotTintOddAchievementRows()
        {
            var row = GP_OverlayPrefabBuilder.BuildRowPreview(GP_OverlayKind.Achievements);
            var skin = ScriptableObject.CreateInstance<GP_OverlaySkin>();
            try
            {
                skin.CopyAppearanceFrom(GP_OverlayPrefabBuilder.DefaultSkin);
                var achievement = row.GetComponent<GP_AchievementRow>();
                achievement.Bind(new AchievementsFetch
                {
                    name = "Odd",
                    description = "Desc",
                    icon = "",
                    maxProgress = 1,
                    lockedVisible = true,
                    lockedDescriptionVisible = true
                }, new AchievementsFetchPlayer { progress = 1, unlocked = true }, 1);
                Assert.That(achievement.background.GetComponent<GP_OverlayTone>().role,
                    Is.EqualTo(GP_OverlayColorRole.RowAlt));

                skin.buttonSelected = Color.yellow;
                skin.rowAlt = Color.blue;
                GP_OverlayTone.Apply(row, skin);
                Assert.That(achievement.background.color, Is.EqualTo(skin.rowAlt));
                Assert.That(achievement.unlockedBadge.GetComponent<GP_OverlayTone>().role,
                    Is.EqualTo(GP_OverlayColorRole.Button));
            }
            finally
            {
                Object.DestroyImmediate(row);
                Object.DestroyImmediate(skin);
            }
        }

        static Transform FindNamed(Transform root, string name)
        {
            if (root.name == name)
                return root;
            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindNamed(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        [Test]
        public void ResetAppearanceRestoresPaletteAndKeepsPrefabs()
        {
            var skin = ScriptableObject.CreateInstance<GP_OverlaySkin>();
            var prefab = new GameObject("KeepPrefab");
            try
            {
                skin.screens.Add(new GP_OverlayPrefabEntry
                {
                    kind = GP_OverlayKind.Leaderboard,
                    prefab = prefab
                });
                skin.panel = Color.magenta;
                skin.header = Color.magenta;
                skin.sidebar = Color.magenta;
                skin.button = Color.magenta;
                skin.buttonSelected = Color.magenta;
                skin.backdrop = Color.magenta;
                skin.titleSize = 12f;

                skin.ResetAppearanceToDefaults();

                var defaults = ScriptableObject.CreateInstance<GP_OverlaySkin>();
                Assert.That(skin.panel, Is.EqualTo(defaults.panel));
                Assert.That(skin.header, Is.EqualTo(defaults.header));
                Assert.That(skin.sidebar, Is.EqualTo(defaults.sidebar));
                Assert.That(skin.button, Is.EqualTo(defaults.button));
                Assert.That(skin.buttonSelected, Is.EqualTo(defaults.buttonSelected));
                Assert.That(skin.backdrop, Is.EqualTo(defaults.backdrop));
                Assert.That(skin.titleSize, Is.EqualTo(defaults.titleSize));
                Assert.That(skin.Find(GP_OverlayKind.Leaderboard).prefab, Is.SameAs(prefab));
                Object.DestroyImmediate(defaults);
            }
            finally
            {
                Object.DestroyImmediate(prefab);
                Object.DestroyImmediate(skin);
            }
        }

        [Test]
        public void PaletteMatchesSkinColorsAndIgnoresUnknown()
        {
            var skin = ScriptableObject.CreateInstance<GP_OverlaySkin>();
            try
            {
                Assert.That(GP_OverlayPalette.TryMatch(skin, skin.panel, out var panel), Is.True);
                Assert.That(panel, Is.EqualTo(GP_OverlayColorRole.Panel));
                Assert.That(GP_OverlayPalette.TryMatch(skin, skin.header, out var header), Is.True);
                Assert.That(header, Is.EqualTo(GP_OverlayColorRole.Header));
                Assert.That(GP_OverlayPalette.TryMatch(skin, Color.magenta, out _), Is.False);
                Assert.That(GP_OverlayPalette.TryMatch(skin, skin.row, out var row), Is.True);
                Assert.That(row, Is.EqualTo(GP_OverlayColorRole.Row));
                Assert.That(GP_OverlayPalette.TryMatch(skin, skin.rowAlt, out var rowAlt), Is.True);
                Assert.That(rowAlt, Is.EqualTo(GP_OverlayColorRole.RowAlt));
                Assert.That(GP_OverlayPalette.Approximately(skin.row, skin.rowAlt), Is.False);
                Assert.That(GP_OverlayPalette.SlotFromProperty("accent"),
                    Is.EqualTo(GP_OverlayColorRole.Accent));
                Assert.That(GP_OverlayPalette.SlotFromProperty("sidebar"),
                    Is.EqualTo(GP_OverlayColorRole.Sidebar));
                Assert.That(GP_OverlayPalette.SlotFromProperty("button"),
                    Is.EqualTo(GP_OverlayColorRole.Button));
                Assert.That(GP_OverlayPalette.SlotFromProperty("buttonSelected"),
                    Is.EqualTo(GP_OverlayColorRole.ButtonSelected));
                var secondary = skin.ButtonColors(skin.button);
                Assert.That(secondary.pressedColor, Is.EqualTo(skin.buttonSelected));
                Assert.That(secondary.highlightedColor, Is.EqualTo(skin.hover));
                var primary = skin.ButtonColors(skin.accent);
                Assert.That(primary.pressedColor, Is.EqualTo(skin.pressed));
            }
            finally
            {
                Object.DestroyImmediate(skin);
            }
        }

        [Test]
        public void PreviewPaneMapsCenterToViewportCenter()
        {
            var pane = new Rect(40f, 20f, 800f, 450f);
            var logical = new Vector2Int(1280, 720);
            var dest = GP_OverlayPreviewSession.FittedRect(pane, logical, 1f, Vector2.zero);
            Assert.That(GP_OverlayPreviewSession.TryMapPaneToViewport(pane, dest.center, logical, 1f,
                Vector2.zero, out var viewport), Is.True);
            Assert.That(viewport.x, Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(viewport.y, Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(GP_OverlayPreviewSession.TryMapPaneToViewport(pane, pane.position, logical, 1f,
                Vector2.zero, out _), Is.False);
        }

        [Test]
        public void PreviewRenderTextureMatchesDisplayPixels()
        {
            var pane = new Rect(0f, 0f, 1600f, 900f);
            var logical = new Vector2Int(1280, 720);
            var size = GP_OverlayPreviewSession.RenderTextureSize(pane, logical, 1f);
            Assert.That(size.x, Is.EqualTo(3200));
            Assert.That(size.y, Is.EqualTo(1800));

            var retina = GP_OverlayPreviewSession.RenderTextureSize(pane, logical, 2f);
            Assert.That(retina.x, Is.LessThanOrEqualTo(4096));
            Assert.That(retina.y, Is.LessThanOrEqualTo(4096));
            Assert.That(retina.x / (float)retina.y, Is.EqualTo(16f / 9f).Within(0.02f));
        }

        [TestCase(360f, 640f)]
        [TestCase(640f, 360f)]
        [TestCase(1080f, 1920f)]
        [TestCase(1920f, 1080f)]
        [TestCase(3440f, 1440f)]
        public void ResponsiveSizeStaysInsideAvailableViewport(float width, float height)
        {
            var available = new Vector2(width, height);
            var result = GP_OverlayResponsive.CalculatePanelSize(available,
                new Vector2(360f, 400f), new Vector2(1650f, 1500f), 1.65f, 1.15f);

            Assert.That(result.x, Is.GreaterThan(0f).And.LessThanOrEqualTo(width));
            Assert.That(result.y, Is.GreaterThan(0f).And.LessThanOrEqualTo(height));
            Assert.That(result.x / result.y, Is.LessThanOrEqualTo(1.65f + 0.001f));
        }
    }
}
