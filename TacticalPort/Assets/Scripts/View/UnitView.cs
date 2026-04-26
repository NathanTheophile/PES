using TacticalPort.Core.Runtime;
using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEngine;

namespace TacticalPort.View
{
    public sealed class UnitView : MonoBehaviour
    {
        #region _____________________________| VALUES

        [SerializeField] private SpriteRenderer _SpriteRenderer;
        [SerializeField] private Color _DefeatedTint = new Color(0.45f, 0.45f, 0.45f, 0.8f);
        [SerializeField] private int _BodySortingOrderOffset = 20;
        [SerializeField] private bool _ScaleWithFootprint = false;
        [SerializeField] private Vector2 _FootprintScaleMultiplier = new Vector2(1f, 1f);

        private BattleUnitRuntime _Runtime;
        private BattleUnitDefinition _Definition;
        private BoardView _BoardView;
        private Vector3 _BaseSpriteLocalScale = Vector3.one;

        #endregion

        #region _____________________________| ACCESSORS

        public BattleUnitId UnitId => _Runtime != null ? _Runtime.Id : BattleUnitId.None;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            CacheMissingReferences(true);
            CacheBaseSpriteScale();
        }

        private void OnValidate()
        {
            CacheMissingReferences(false);
            CacheBaseSpriteScale();
        }

        #endregion

        #region _____________________________| BINDING

        public void Bind(BattleUnitRuntime pRuntime, BattleUnitDefinition pDefinition, BoardView pBoardView)
        {
            _Runtime = pRuntime;
            _Definition = pDefinition;
            _BoardView = pBoardView;

            CacheMissingReferences(true);
            Refresh();
        }

        #endregion

        #region _____________________________| DISPLAY

        public void Refresh()
        {
            if (_Runtime == null || _BoardView == null)
                return;

            transform.position = ResolveWorldPosition();
            gameObject.name = $"UnitView_{_Runtime.Id}_{_Runtime.Definition.DisplayName}";

            if (_SpriteRenderer != null)
            {
                _SpriteRenderer.enabled = true;
                _SpriteRenderer.color = _Runtime.IsAlive ? (_Definition != null ? _Definition.Tint : Color.white) : _DefeatedTint;
                _SpriteRenderer.sortingOrder = ResolveSortingOrder(_BodySortingOrderOffset);
                _SpriteRenderer.transform.localScale = ResolveSpriteScale();
            }
        }

        #endregion

        #region _____________________________| HELPERS

        private void CacheMissingReferences(bool pLogError)
        {
            if (!IsOwnedRenderer(_SpriteRenderer))
                _SpriteRenderer = null;

            _SpriteRenderer ??= ResolveBodyRenderer();

            if (_SpriteRenderer == null && pLogError)
                Debug.LogError("UnitView requires a SpriteRenderer on the root, on a child named 'BodyRenderer', or somewhere under the UnitView hierarchy.", this);
        }

        private int ResolveSortingOrder(int pOffset)
        {
            if (_Runtime == null)
                return pOffset;

            return -((_Runtime.Position.X + _Runtime.Position.Y) * 10) + pOffset;
        }

        private Vector3 ResolveWorldPosition()
        {
            if (_Runtime == null || _BoardView == null)
                return transform.position;

            Vector3 lAccumulated = Vector3.zero;
            int lCellCount = 0;
            foreach (GridCoord lCell in _Runtime.EnumerateOccupiedCells())
            {
                lAccumulated += _BoardView.GetWorldPosition(lCell);
                lCellCount++;
            }

            if (lCellCount <= 0)
                return _BoardView.GetWorldPosition(_Runtime.Position);

            return lAccumulated / lCellCount;
        }

        private Vector3 ResolveSpriteScale()
        {
            if (!_ScaleWithFootprint || _Definition == null)
                return _BaseSpriteLocalScale;

            float lScaleX = Mathf.Max(1f, _Definition.FootprintWidth * Mathf.Max(0.1f, _FootprintScaleMultiplier.x));
            float lScaleY = Mathf.Max(1f, _Definition.FootprintHeight * Mathf.Max(0.1f, _FootprintScaleMultiplier.y));
            return new Vector3(
                _BaseSpriteLocalScale.x * lScaleX,
                _BaseSpriteLocalScale.y * lScaleY,
                _BaseSpriteLocalScale.z);
        }

        private SpriteRenderer ResolveBodyRenderer()
        {
            SpriteRenderer lRootRenderer = GetComponent<SpriteRenderer>();
            if (lRootRenderer != null)
                return lRootRenderer;

            foreach (SpriteRenderer lRenderer in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (lRenderer != null && lRenderer.gameObject.name == "BodyRenderer")
                    return lRenderer;
            }

            return GetComponentInChildren<SpriteRenderer>(true);
        }

        private void CacheBaseSpriteScale()
        {
            if (_SpriteRenderer != null && _SpriteRenderer.transform != null)
                _BaseSpriteLocalScale = _SpriteRenderer.transform.localScale;
        }

        private bool IsOwnedRenderer(SpriteRenderer pRenderer) =>
            pRenderer != null
            && pRenderer.transform != null
            && (pRenderer.transform == transform || pRenderer.transform.IsChildOf(transform));

        #endregion
    }
}
