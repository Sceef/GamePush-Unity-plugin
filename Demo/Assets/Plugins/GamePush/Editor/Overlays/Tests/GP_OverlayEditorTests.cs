using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using GamePush.Overlays;
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
