using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using GamePush.Overlays;
using GamePush.Overlays.Views;
using GamePush.Overlays.Widgets;
using static GamePushEditor.Overlays.GP_OverlayUIFactory;

namespace GamePushEditor.Overlays
{
    /// <summary>
    /// Generates the default overlay prefabs. Prefab YAML cannot be written by hand, so the
    /// hierarchy is assembled in code and saved with PrefabUtility. The result is a normal set of
    /// editable assets; rerunning the builder only recreates the defaults.
    /// </summary>
    public static class GP_OverlayPrefabBuilder
    {
        const string PluginFolder = "Assets/Plugins/GamePush";
        const string ResourcesFolder = PluginFolder + "/Resources/GamePush";
        const string OverlaysFolder = ResourcesFolder + "/Overlays";
        const string RowsFolder = OverlaysFolder + "/Rows";
        const string SkinPath = ResourcesFolder + "/GP_OverlaySkin.asset";

        [MenuItem("GamePush/Overlays/Rebuild Default Prefabs", false, 200)]
        public static void RebuildMenu()
        {
            if (!EnsureTextMeshPro())
                return;
            Rebuild();
            EditorUtility.DisplayDialog("GamePush",
                "Default overlay prefabs rebuilt in\n" + OverlaysFolder, "OK");
        }

        /// <summary>Rebuilds silently; safe to call from the setup window.</summary>
        public static void Rebuild()
        {
            EnsureFolders();
            var skin = LoadOrCreateSkin();

            skin.achievementRow = Save(BuildAchievementRow(), RowsFolder + "/AchievementRow.prefab");
            skin.leaderboardRow = Save(BuildLeaderboardRow(), RowsFolder + "/LeaderboardRow.prefab");
            skin.messageRow = Save(BuildMessageRow(), RowsFolder + "/MessageRow.prefab");
            skin.memberRow = Save(BuildMemberRow(), RowsFolder + "/MemberRow.prefab");
            skin.gameCard = Save(BuildGameCard(), RowsFolder + "/GameCard.prefab");
            skin.feedbackRow = Save(BuildFeedbackRow(), RowsFolder + "/FeedbackRow.prefab");

            SetScreen(skin, GP_OverlayKind.Confirm, Save(BuildConfirm(), OverlaysFolder + "/Confirm.prefab"),
                1.6f, new Vector2(420f, 260f), new Vector2(820f, 620f));
            SetScreen(skin, GP_OverlayKind.Achievements,
                Save(BuildAchievements(), OverlaysFolder + "/Achievements.prefab"),
                1f, new Vector2(360f, 400f), new Vector2(1400f, 1800f));
            SetScreen(skin, GP_OverlayKind.Leaderboard,
                Save(BuildLeaderboard(), OverlaysFolder + "/Leaderboard.prefab"),
                1.2f, new Vector2(360f, 400f), new Vector2(1400f, 1800f));
            SetScreen(skin, GP_OverlayKind.Chat, Save(BuildChat(), OverlaysFolder + "/Chat.prefab"),
                1.4f, new Vector2(360f, 400f), new Vector2(1600f, 1800f));
            SetScreen(skin, GP_OverlayKind.Document, Save(BuildDocument(), OverlaysFolder + "/Document.prefab"),
                1.1f, new Vector2(360f, 400f), new Vector2(1100f, 1800f));
            SetScreen(skin, GP_OverlayKind.GamesCollections,
                Save(BuildGamesCollections(), OverlaysFolder + "/GamesCollections.prefab"),
                1f, new Vector2(360f, 400f), new Vector2(1400f, 1800f));
            SetScreen(skin, GP_OverlayKind.Feedbacks, Save(BuildFeedbacks(), OverlaysFolder + "/Feedbacks.prefab"),
                1.4f, new Vector2(360f, 400f), new Vector2(1600f, 1800f));
            SetScreen(skin, GP_OverlayKind.AdCountdown,
                Save(BuildAdCountdown(), OverlaysFolder + "/AdCountdown.prefab"),
                1.6f, new Vector2(360f, 220f), new Vector2(720f, 480f));
            SetScreen(skin, GP_OverlayKind.AdFailed, Save(BuildAdFailed(), OverlaysFolder + "/AdFailed.prefab"),
                1.6f, new Vector2(360f, 220f), new Vector2(760f, 520f));

            EditorUtility.SetDirty(skin);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// TextMeshPro renders nothing without its Essential Resources, so offer the import
        /// instead of producing prefabs full of missing fonts.
        /// </summary>
        public static bool EnsureTextMeshPro()
        {
            if (TMP_Settings.instance != null)
                return true;

            var import = EditorUtility.DisplayDialog("GamePush",
                "TextMeshPro Essential Resources are not imported yet. The overlay prefabs need them.\n\n" +
                "Import now?", "Import", "Cancel");
            if (!import)
                return false;

            EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
            return TMP_Settings.instance != null;
        }

        public static bool IsTextMeshProReady => TMP_Settings.instance != null;

        #region Assets

        static void EnsureFolders()
        {
            EnsureFolder(PluginFolder + "/Resources");
            EnsureFolder(ResourcesFolder);
            EnsureFolder(OverlaysFolder);
            EnsureFolder(RowsFolder);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var leaf = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf))
                return;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        static GP_OverlaySkin LoadOrCreateSkin()
        {
            var skin = AssetDatabase.LoadAssetAtPath<GP_OverlaySkin>(SkinPath);
            if (skin != null)
                return skin;
            skin = ScriptableObject.CreateInstance<GP_OverlaySkin>();
            AssetDatabase.CreateAsset(skin, SkinPath);
            return skin;
        }

        static void SetScreen(GP_OverlaySkin skin, GP_OverlayKind kind, GameObject prefab, float maxAspect,
            Vector2 minSize, Vector2 maxSize)
        {
            var entry = skin.screens.Find(item => item != null && item.kind == kind);
            if (entry == null)
            {
                entry = new GP_OverlayPrefabEntry { kind = kind };
                skin.screens.Add(entry);
            }
            entry.prefab = prefab;
            entry.maxAspect = maxAspect;
            entry.minSize = minSize;
            entry.maxSize = maxSize;
        }

        static GameObject Save(GameObject instance, string path)
        {
            var asset = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return asset;
        }

        #endregion

        #region Screen chrome

        struct Shell
        {
            public GameObject root;
            public RectTransform panel;
            public RectTransform header;
            public RectTransform body;
            public TMP_Text title;
            public TMP_Text status;
            public Button close;
            public Image backdrop;
            public Button backdropButton;
        }

        /// <summary>Backdrop + responsive panel + header + status label, shared by every screen.</summary>
        static Shell BuildShell(string name, bool withLayoutMode = true)
        {
            var shell = new Shell();

            var root = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            Stretch((RectTransform)root.transform);
            shell.root = root;

            var backdrop = Panel("Backdrop", root.transform, Skin.backdrop);
            shell.backdrop = backdrop;
            shell.backdropButton = backdrop.gameObject.AddComponent<Button>();
            shell.backdropButton.transition = Selectable.Transition.None;
            shell.backdropButton.targetGraphic = backdrop;

            var panel = Panel("Panel", root.transform, Skin.panel, Skin.panelSprite);
            shell.panel = (RectTransform)panel.transform;
            panel.gameObject.AddComponent<GP_OverlayResponsive>();
            if (withLayoutMode)
                panel.gameObject.AddComponent<GP_OverlayLayoutMode>();
            Vertical(panel.gameObject, new RectOffset(0, 0, 0, 0), 0f);

            var header = Panel("Header", panel.transform, Skin.header);
            shell.header = (RectTransform)header.transform;
            Horizontal(header.gameObject, new RectOffset(24, 16, 8, 8), 12f);
            Element(header.gameObject, minHeight: 96f, preferredHeight: 96f);

            shell.title = Text("Title", header.transform, "", Skin.titleSize, Skin.text,
                TextAlignmentOptions.MidlineLeft);
            Element(shell.title.gameObject, flexibleWidth: 1f);

            shell.close = IconButton("Close", header.transform, "✕", Skin.row);

            var body = Rect("Body", panel.transform);
            shell.body = body;
            Element(body.gameObject, flexibleHeight: 1f);

            // The status label floats over the body so an empty list still shows a message.
            shell.status = Text("Status", body, "", Skin.bodySize, Skin.textMuted, TextAlignmentOptions.Center);
            Stretch((RectTransform)shell.status.transform);
            var ignore = shell.status.gameObject.AddComponent<LayoutElement>();
            ignore.ignoreLayout = true;

            return shell;
        }

        static void Wire(GP_OverlayView view, Shell shell)
        {
            view.panel = shell.panel;
            view.backdrop = shell.backdrop;
            view.backdropButton = shell.backdropButton;
            view.closeButton = shell.close;
            view.titleLabel = shell.title;
            view.statusLabel = shell.status;
            view.canvasGroup = shell.root.GetComponent<CanvasGroup>();
        }

        #endregion

        #region Screens

        static GameObject BuildConfirm()
        {
            var shell = BuildShell("Confirm", false);
            var view = shell.root.AddComponent<GP_ConfirmView>();
            Wire(view, shell);

            Vertical(shell.body.gameObject, new RectOffset(32, 32, 24, 24), 24f);

            view.messageLabel = Text("Message", shell.body, "", Skin.bodySize, Skin.text,
                TextAlignmentOptions.Center);
            Element(view.messageLabel.gameObject, flexibleHeight: 1f, minHeight: 96f);

            var buttons = Rect("Buttons", shell.body);
            Horizontal(buttons.gameObject, new RectOffset(0, 0, 0, 0), 16f).childForceExpandWidth = true;
            Element(buttons.gameObject, minHeight: 96f, preferredHeight: 96f);

            view.cancelButton = Button("Cancel", buttons, "", Skin.row, out var cancelLabel);
            view.cancelLabel = cancelLabel;
            view.cancelBackground = view.cancelButton.GetComponent<Image>();
            Element(view.cancelButton.gameObject, flexibleWidth: 1f);

            view.confirmButton = Button("Confirm", buttons, "", Skin.accent, out var confirmLabel);
            view.confirmLabel = confirmLabel;
            view.confirmBackground = view.confirmButton.GetComponent<Image>();
            Element(view.confirmButton.gameObject, flexibleWidth: 1f);

            return shell.root;
        }

        static GameObject BuildAchievements()
        {
            var shell = BuildShell("Achievements");
            var view = shell.root.AddComponent<GP_AchievementsView>();
            Wire(view, shell);
            view.layoutMode = shell.panel.GetComponent<GP_OverlayLayoutMode>();

            view.counterLabel = Text("Counter", shell.header, "", Skin.captionSize, Skin.textMuted,
                TextAlignmentOptions.MidlineRight);
            view.counterLabel.transform.SetSiblingIndex(1);
            Element(view.counterLabel.gameObject, minWidth: 120f, preferredWidth: 140f);

            // Wide puts the group filter in a left rail; Compact keeps it as a chip strip on top.
            var columns = Rect("Columns", shell.body);
            Horizontal(columns.gameObject, new RectOffset(0, 0, 0, 0), 0f);

            var rail = Rect("GroupRail", columns);
            Vertical(rail.gameObject, new RectOffset(12, 12, 12, 12), 8f);
            Element(rail.gameObject, minWidth: 240f, preferredWidth: 260f);
            var railFitter = rail.gameObject.AddComponent<ContentSizeFitter>();
            railFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            view.groupRail = rail;

            var template = Button("GroupButtonTemplate", rail, "", Skin.row, out _);
            Element(template.gameObject, minHeight: 72f, preferredHeight: 72f);
            template.gameObject.SetActive(false);
            view.groupButtonTemplate = template;

            var list = ScrollList("List", columns, true, 12f);
            view.list = list;

            return shell.root;
        }

        static GameObject BuildLeaderboard()
        {
            var shell = BuildShell("Leaderboard");
            var view = shell.root.AddComponent<GP_LeaderboardView>();
            Wire(view, shell);
            view.layoutMode = shell.panel.GetComponent<GP_OverlayLayoutMode>();

            view.subtitleLabel = Text("Subtitle", shell.header, "", Skin.captionSize, Skin.textMuted,
                TextAlignmentOptions.MidlineRight);
            view.subtitleLabel.transform.SetSiblingIndex(1);
            Element(view.subtitleLabel.gameObject, minWidth: 160f, preferredWidth: 220f);

            Vertical(shell.body.gameObject, new RectOffset(0, 0, 0, 0), 0f);
            view.list = ScrollList("List", shell.body, false);

            var holder = Rect("SelfRow", shell.body);
            Element(holder.gameObject, minHeight: 108f, preferredHeight: 108f);
            view.selfRowHolder = holder.gameObject;
            var self = LeaderboardRowHierarchy(holder);
            Stretch((RectTransform)self.transform);
            view.selfRow = self;
            holder.gameObject.SetActive(false);

            return shell.root;
        }

        static GameObject BuildChat()
        {
            var shell = BuildShell("Chat");
            var view = shell.root.AddComponent<GP_ChatView>();
            Wire(view, shell);
            var mode = shell.panel.GetComponent<GP_OverlayLayoutMode>();
            view.layoutMode = mode;

            view.membersToggle = IconButton("MembersToggle", shell.header, "☰", Skin.row);
            view.membersToggle.transform.SetSiblingIndex(1);

            Vertical(shell.body.gameObject, new RectOffset(0, 0, 0, 0), 0f);

            var columns = Rect("Columns", shell.body);
            Horizontal(columns.gameObject, new RectOffset(0, 0, 0, 0), 0f);
            Element(columns.gameObject, flexibleHeight: 1f);

            view.messageList = ScrollList("Messages", columns, false, 10f);

            var members = Rect("Members", columns);
            Background(members, Skin.header);
            Vertical(members.gameObject, new RectOffset(0, 0, 0, 0), 0f);
            Element(members.gameObject, minWidth: 300f, preferredWidth: 340f);
            view.membersPanel = members.gameObject;
            view.memberList = ScrollList("MemberList", members, false);
            mode.wideOnly.Add(members.gameObject);

            var composer = Rect("Composer", shell.body);
            Background(composer, Skin.header);
            Horizontal(composer.gameObject, new RectOffset(16, 16, 12, 12), 12f);
            Element(composer.gameObject, minHeight: 104f, preferredHeight: 104f);
            view.composer = composer;

            view.input = InputField("Input", composer);
            Element(view.input.gameObject, flexibleWidth: 1f);

            view.sendButton = Button("Send", composer, GP_OverlayStrings.Send, Skin.accent, out _);
            Element(view.sendButton.gameObject, minWidth: 180f, preferredWidth: 200f);

            return shell.root;
        }

        static GameObject BuildDocument()
        {
            var shell = BuildShell("Document", false);
            var view = shell.root.AddComponent<GP_DocumentView>();
            Wire(view, shell);

            Horizontal(shell.body.gameObject, new RectOffset(0, 0, 0, 0), 0f).childAlignment =
                TextAnchor.UpperCenter;

            var scrollRect = Rect("Scroll", shell.body);
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            Element(scrollRect.gameObject, flexibleWidth: 1f, flexibleHeight: 1f);

            var viewport = Rect("Viewport", scrollRect);
            viewport.gameObject.AddComponent<RectMask2D>();
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0f);

            var content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            Horizontal(content.gameObject, new RectOffset(32, 32, 24, 24), 0f).childAlignment =
                TextAnchor.UpperCenter;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var body = Text("Text", content, "", Skin.bodySize, Skin.text, TextAlignmentOptions.TopLeft);
            view.contentLabel = body;
            view.contentLayout = body.gameObject.AddComponent<LayoutElement>();
            view.contentLayout.preferredWidth = view.maxTextWidth;

            scroll.viewport = viewport;
            scroll.content = content;
            view.scrollRect = scroll;

            return shell.root;
        }

        static GameObject BuildGamesCollections()
        {
            var shell = BuildShell("GamesCollections");
            var view = shell.root.AddComponent<GP_GamesCollectionsView>();
            Wire(view, shell);

            Vertical(shell.body.gameObject, new RectOffset(0, 0, 0, 0), 0f);
            var list = ScrollList("List", shell.body, true, 16f);
            view.list = list;
            view.grid = list.content.GetComponent<GP_FlexibleGrid>();
            view.grid.cellRatio = 1.25f;

            return shell.root;
        }

        static GameObject BuildFeedbacks()
        {
            var shell = BuildShell("Feedbacks");
            var view = shell.root.AddComponent<GP_FeedbacksView>();
            Wire(view, shell);
            var mode = shell.panel.GetComponent<GP_OverlayLayoutMode>();
            view.layoutMode = mode;

            view.backButton = IconButton("Back", shell.header, "‹", Skin.row);
            view.backButton.transform.SetSiblingIndex(0);
            view.newButton = IconButton("New", shell.header, "+", Skin.accent);
            view.newButton.transform.SetSiblingIndex(2);

            var columns = Rect("Columns", shell.body);
            Horizontal(columns.gameObject, new RectOffset(0, 0, 0, 0), 0f);

            var listPanel = Rect("ListPanel", columns);
            Vertical(listPanel.gameObject, new RectOffset(0, 0, 0, 0), 0f);
            Element(listPanel.gameObject, minWidth: 340f, preferredWidth: 420f, flexibleWidth: 1f);
            view.listPanel = listPanel.gameObject;
            view.feedbackList = ScrollList("List", listPanel, false);

            var threadPanel = Rect("ThreadPanel", columns);
            Background(threadPanel, Skin.header);
            Vertical(threadPanel.gameObject, new RectOffset(0, 0, 0, 0), 0f);
            Element(threadPanel.gameObject, flexibleWidth: 2f);
            view.threadPanel = threadPanel.gameObject;

            view.threadTitle = Text("ThreadTitle", threadPanel, "", Skin.bodySize, Skin.text,
                TextAlignmentOptions.MidlineLeft);
            Element(view.threadTitle.gameObject, minHeight: 72f, preferredHeight: 72f);

            view.threadList = ScrollList("Thread", threadPanel, false, 10f);

            var composer = Rect("Composer", threadPanel);
            Horizontal(composer.gameObject, new RectOffset(16, 16, 12, 12), 12f);
            Element(composer.gameObject, minHeight: 104f, preferredHeight: 104f);

            view.input = InputField("Input", composer);
            Element(view.input.gameObject, flexibleWidth: 1f);

            view.sendButton = Button("Send", composer, GP_OverlayStrings.Send, Skin.accent, out _);
            Element(view.sendButton.gameObject, minWidth: 180f, preferredWidth: 200f);

            return shell.root;
        }

        static GameObject BuildAdCountdown()
        {
            var shell = BuildShell("AdCountdown", false);
            var view = shell.root.AddComponent<GP_AdCountdownView>();
            Wire(view, shell);
            shell.close.gameObject.SetActive(false);
            shell.header.gameObject.SetActive(false);

            Vertical(shell.body.gameObject, new RectOffset(32, 32, 32, 32), 16f).childAlignment =
                TextAnchor.MiddleCenter;

            view.captionLabel = Text("Caption", shell.body, "", Skin.bodySize, Skin.text,
                TextAlignmentOptions.Center);
            Element(view.captionLabel.gameObject, minHeight: 56f);

            view.countdownLabel = Text("Countdown", shell.body, "", Skin.titleSize * 2f, Skin.accent,
                TextAlignmentOptions.Center);
            Element(view.countdownLabel.gameObject, minHeight: 140f, flexibleHeight: 1f);

            view.skipButton = Button("Skip", shell.body, "", Skin.row, out _);
            Element(view.skipButton.gameObject, minHeight: 88f, preferredHeight: 88f);

            return shell.root;
        }

        static GameObject BuildAdFailed()
        {
            var shell = BuildShell("AdFailed", false);
            var view = shell.root.AddComponent<GP_AdFailedView>();
            Wire(view, shell);
            shell.header.gameObject.SetActive(false);

            Vertical(shell.body.gameObject, new RectOffset(32, 32, 32, 32), 24f).childAlignment =
                TextAnchor.MiddleCenter;

            view.textLabel = Text("Text", shell.body, "", Skin.bodySize, Skin.text, TextAlignmentOptions.Center);
            Element(view.textLabel.gameObject, flexibleHeight: 1f, minHeight: 96f);

            view.okButton = Button("Ok", shell.body, "", Skin.accent, out _);
            Element(view.okButton.gameObject, minHeight: 96f, preferredHeight: 96f);

            return shell.root;
        }

        #endregion

        #region Rows

        static GameObject BuildAchievementRow()
        {
            var root = new GameObject("AchievementRow", typeof(RectTransform));
            var rect = (RectTransform)root.transform;
            Size(rect, new Vector2(480f, 200f));

            var row = root.AddComponent<GP_AchievementRow>();
            row.background = root.AddComponent<Image>();
            row.background.color = Skin.row;
            row.background.sprite = Skin.rowSprite;

            Horizontal(root, new RectOffset(16, 16, 16, 16), 16f);

            row.icon = Avatar("Icon", rect, 112f);

            var text = Rect("Text", rect);
            Vertical(text.gameObject, new RectOffset(0, 0, 0, 0), 4f).childAlignment = TextAnchor.MiddleLeft;
            Element(text.gameObject, flexibleWidth: 1f);

            row.titleLabel = Text("Title", text, "", Skin.bodySize, Skin.text);
            row.descriptionLabel = Text("Description", text, "", Skin.captionSize, Skin.textMuted);

            var bar = Panel("ProgressBar", text, Skin.rowAlt);
            Element(bar.gameObject, minHeight: 16f, preferredHeight: 16f);
            var fill = Panel("Fill", bar.transform, Skin.accent);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            row.progressFill = fill;

            row.progressLabel = Text("Progress", text, "", Skin.captionSize, Skin.textMuted,
                TextAlignmentOptions.MidlineRight);

            row.lockedBadge = Text("Locked", rect, "🔒", Skin.captionSize, Skin.textMuted,
                TextAlignmentOptions.Center).gameObject;
            Element(row.lockedBadge, minWidth: 48f, preferredWidth: 48f);
            row.unlockedBadge = Text("Unlocked", rect, "✓", Skin.captionSize, Skin.accent,
                TextAlignmentOptions.Center).gameObject;
            Element(row.unlockedBadge, minWidth: 48f, preferredWidth: 48f);

            return root;
        }

        static GameObject BuildLeaderboardRow()
        {
            var holder = new GameObject("LeaderboardRow", typeof(RectTransform));
            var row = LeaderboardRowHierarchy((RectTransform)holder.transform);
            // The standalone prefab is just the row itself.
            var result = row.gameObject;
            result.transform.SetParent(null, false);
            Object.DestroyImmediate(holder);
            Size((RectTransform)result.transform, new Vector2(900f, 108f));
            return result;
        }

        static GP_LeaderboardRow LeaderboardRowHierarchy(RectTransform parent)
        {
            var rect = Rect("Row", parent);
            Size(rect, new Vector2(900f, 108f));
            var row = rect.gameObject.AddComponent<GP_LeaderboardRow>();
            row.background = rect.gameObject.AddComponent<Image>();
            row.background.color = Skin.row;
            row.background.sprite = Skin.rowSprite;

            Horizontal(rect.gameObject, new RectOffset(16, 16, 8, 8), 16f);

            row.positionLabel = Text("Position", rect, "", Skin.bodySize, Skin.text, TextAlignmentOptions.Center);
            Element(row.positionLabel.gameObject, minWidth: 80f, preferredWidth: 80f);

            row.avatar = Avatar("Avatar", rect, 72f);

            row.nameLabel = Text("Name", rect, "", Skin.bodySize, Skin.text, TextAlignmentOptions.MidlineLeft);
            Element(row.nameLabel.gameObject, flexibleWidth: 1f);

            var extras = Rect("Extras", rect);
            Horizontal(extras.gameObject, new RectOffset(0, 0, 0, 0), 12f);
            Element(extras.gameObject, flexibleWidth: 1f);
            row.extraColumns = extras;

            var template = Text("ExtraTemplate", extras, "", Skin.captionSize, Skin.textMuted,
                TextAlignmentOptions.Center);
            Element(template.gameObject, minWidth: 120f, preferredWidth: 140f);
            template.gameObject.SetActive(false);
            row.extraColumnTemplate = template;

            row.scoreLabel = Text("Score", rect, "", Skin.bodySize, Skin.text, TextAlignmentOptions.MidlineRight);
            Element(row.scoreLabel.gameObject, minWidth: 160f, preferredWidth: 180f);

            return row;
        }

        static GameObject BuildMessageRow()
        {
            var root = new GameObject("MessageRow", typeof(RectTransform));
            var rect = (RectTransform)root.transform;
            Size(rect, new Vector2(900f, 140f));

            var row = root.AddComponent<GP_MessageRow>();
            row.layout = Horizontal(root, new RectOffset(12, 12, 8, 8), 12f);
            row.layout.childAlignment = TextAnchor.UpperLeft;

            var fitter = root.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            row.avatar = Avatar("Avatar", rect, 72f);

            var bubbleImage = Panel("Bubble", rect, Skin.row, Skin.rowSprite);
            row.bubble = bubbleImage;
            Vertical(bubbleImage.gameObject, new RectOffset(16, 16, 12, 12), 4f);
            Element(bubbleImage.gameObject, flexibleWidth: 1f);
            var bubbleFitter = bubbleImage.gameObject.AddComponent<ContentSizeFitter>();
            bubbleFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var head = Rect("Head", bubbleImage.transform);
            Horizontal(head.gameObject, new RectOffset(0, 0, 0, 0), 8f);
            Element(head.gameObject, minHeight: 32f, preferredHeight: 32f);

            row.authorLabel = Text("Author", head, "", Skin.captionSize, Skin.textMuted);
            Element(row.authorLabel.gameObject, flexibleWidth: 1f);
            row.timeLabel = Text("Time", head, "", Skin.captionSize, Skin.textMuted,
                TextAlignmentOptions.MidlineRight);
            Element(row.timeLabel.gameObject, minWidth: 100f, preferredWidth: 110f);

            row.textLabel = Text("Text", bubbleImage.transform, "", Skin.bodySize, Skin.text);

            row.deleteButton = IconButton("Delete", rect, "✕", Skin.rowAlt);
            row.deleteButton.gameObject.SetActive(false);

            return root;
        }

        static GameObject BuildMemberRow()
        {
            var root = new GameObject("MemberRow", typeof(RectTransform));
            var rect = (RectTransform)root.transform;
            Size(rect, new Vector2(400f, 96f));

            var row = root.AddComponent<GP_MemberRow>();
            row.background = root.AddComponent<Image>();
            row.background.color = Skin.row;
            row.background.sprite = Skin.rowSprite;
            Horizontal(root, new RectOffset(12, 12, 8, 8), 12f);

            row.onlineDot = Panel("Online", rect, Skin.textMuted);
            Size((RectTransform)row.onlineDot.transform, new Vector2(16f, 16f));
            Element(row.onlineDot.gameObject, minWidth: 16f, preferredWidth: 16f, minHeight: 16f,
                preferredHeight: 16f);

            row.avatar = Avatar("Avatar", rect, 64f);

            row.nameLabel = Text("Name", rect, "", Skin.bodySize, Skin.text, TextAlignmentOptions.MidlineLeft);
            Element(row.nameLabel.gameObject, flexibleWidth: 1f);

            row.stateLabel = Text("State", rect, "", Skin.captionSize, Skin.textMuted,
                TextAlignmentOptions.Center);
            Element(row.stateLabel.gameObject, minWidth: 48f, preferredWidth: 48f);

            row.muteButton = IconButton("Mute", rect, "🔇", Skin.rowAlt);
            row.kickButton = IconButton("Kick", rect, "⨯", Skin.danger);

            return root;
        }

        static GameObject BuildGameCard()
        {
            var root = new GameObject("GameCard", typeof(RectTransform));
            var rect = (RectTransform)root.transform;
            Size(rect, new Vector2(320f, 400f));

            var card = root.AddComponent<GP_GameCard>();
            card.background = root.AddComponent<Image>();
            card.background.color = Skin.row;
            card.background.sprite = Skin.rowSprite;
            card.button = root.AddComponent<Button>();
            card.button.targetGraphic = card.background;

            Vertical(root, new RectOffset(12, 12, 12, 12), 8f).childAlignment = TextAnchor.UpperCenter;

            card.icon = Avatar("Icon", rect, 200f);
            Element(card.icon.gameObject, flexibleHeight: 1f, minHeight: 160f);

            card.nameLabel = Text("Name", rect, "", Skin.captionSize, Skin.text, TextAlignmentOptions.Center);
            Element(card.nameLabel.gameObject, minHeight: 56f);

            card.playLabel = Text("Play", rect, "", Skin.captionSize, Skin.accent, TextAlignmentOptions.Center);
            Element(card.playLabel.gameObject, minHeight: 44f);

            return root;
        }

        static GameObject BuildFeedbackRow()
        {
            var root = new GameObject("FeedbackRow", typeof(RectTransform));
            var rect = (RectTransform)root.transform;
            Size(rect, new Vector2(420f, 140f));

            var row = root.AddComponent<GP_FeedbackRow>();
            row.background = root.AddComponent<Image>();
            row.background.color = Skin.row;
            row.background.sprite = Skin.rowSprite;
            row.button = root.AddComponent<Button>();
            row.button.targetGraphic = row.background;

            Vertical(root, new RectOffset(16, 16, 12, 12), 6f);

            row.textLabel = Text("Text", rect, "", Skin.bodySize, Skin.text);
            Element(row.textLabel.gameObject, flexibleHeight: 1f, minHeight: 56f);

            var footer = Rect("Footer", rect);
            Horizontal(footer.gameObject, new RectOffset(0, 0, 0, 0), 8f);
            Element(footer.gameObject, minHeight: 36f, preferredHeight: 36f);

            row.statusLabel = Text("Status", footer, "", Skin.captionSize, Skin.textMuted);
            Element(row.statusLabel.gameObject, flexibleWidth: 1f);
            row.dateLabel = Text("Date", footer, "", Skin.captionSize, Skin.textMuted,
                TextAlignmentOptions.MidlineRight);
            Element(row.dateLabel.gameObject, minWidth: 160f, preferredWidth: 170f);

            return root;
        }

        #endregion

        static TMP_InputField InputField(string name, Transform parent)
        {
            var background = Panel(name, parent, Skin.row, Skin.rowSprite);
            var field = background.gameObject.AddComponent<TMP_InputField>();
            field.targetGraphic = background;
            field.lineType = TMP_InputField.LineType.SingleLine;

            var viewport = Rect("TextArea", background.transform);
            viewport.offsetMin = new Vector2(16f, 8f);
            viewport.offsetMax = new Vector2(-16f, -8f);
            viewport.gameObject.AddComponent<RectMask2D>();

            var placeholder = Text("Placeholder", viewport, "", Skin.bodySize, Skin.textMuted,
                TextAlignmentOptions.MidlineLeft);
            var text = Text("Text", viewport, "", Skin.bodySize, Skin.text, TextAlignmentOptions.MidlineLeft);

            field.textViewport = viewport;
            field.textComponent = text;
            field.placeholder = placeholder;
            field.fontAsset = Skin.font;
            field.pointSize = Skin.bodySize;
            return field;
        }
    }
}
