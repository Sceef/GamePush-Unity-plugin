using UnityEngine;
using UnityEngine.UI;

namespace GamePush.Overlays.Widgets
{
    /// <summary>
    /// GridLayoutGroup with a cell size derived from the current width instead of a fixed value,
    /// so the same prefab gives two columns on a phone and four on a wide panel.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(GridLayoutGroup))]
    [DisallowMultipleComponent]
    public sealed class GP_FlexibleGrid : MonoBehaviour
    {
        public int compactColumns = 2;
        public int wideColumns = 4;

        [Tooltip("When positive, column count is derived from available width and clamped by compact/wide columns.")]
        public float minCellWidth = 0f;

        [Tooltip("Cell height divided by cell width.")]
        public float cellRatio = 1f;

        GridLayoutGroup _grid;
        RectTransform _rect;
        GP_OverlayLayoutMode _mode;
        float _lastWidth = -1f;
        int _lastColumns = -1;

        void OnEnable()
        {
            _grid = GetComponent<GridLayoutGroup>();
            _rect = (RectTransform)transform;
            _mode = GetComponentInParent<GP_OverlayLayoutMode>();
            if (_mode != null)
                _mode.ModeChanged += OnModeChanged;
            Rebuild();
        }

        void OnDisable()
        {
            if (_mode != null)
                _mode.ModeChanged -= OnModeChanged;
        }

        void OnRectTransformDimensionsChange() => Rebuild();

        void OnModeChanged(GP_LayoutMode mode) => Rebuild();

        public void Rebuild()
        {
            if (_grid == null || _rect == null)
                return;
            var width = _rect.rect.width;
            if (width <= 0f)
                return;

            var maxColumns = _mode != null && _mode.Mode == GP_LayoutMode.Wide ? wideColumns : compactColumns;
            var columns = maxColumns;
            if (minCellWidth > 0f)
            {
                var usable = width - _grid.padding.left - _grid.padding.right + _grid.spacing.x;
                columns = Mathf.FloorToInt(usable / (minCellWidth + _grid.spacing.x));
                columns = Mathf.Clamp(columns, 1, Mathf.Max(1, maxColumns));
            }
            columns = Mathf.Max(1, columns);
            if (Mathf.Approximately(width, _lastWidth) && columns == _lastColumns)
                return;
            _lastWidth = width;
            _lastColumns = columns;

            var padding = _grid.padding.left + _grid.padding.right;
            var spacing = _grid.spacing.x * (columns - 1);
            var cellWidth = Mathf.Max(1f, (width - padding - spacing) / columns);

            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _grid.constraintCount = columns;
            _grid.cellSize = new Vector2(cellWidth, cellWidth * Mathf.Max(0.1f, cellRatio));
        }
    }
}
