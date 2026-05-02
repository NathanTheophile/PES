#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Shared;
using UnityEngine;

namespace TacticalPort.View
{
    public sealed class BoardCursorView : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private BoardView _BoardView;
        [SerializeField] private Transform _CursorRoot;
        [SerializeField] private GameObject _HoverCursorPrefab;
        [SerializeField] private GameObject _SelectionCursorPrefab;
        [SerializeField] private GameObject _TargetCursorPrefab;

        private GameObject _HoverCursor;
        private GameObject _SelectionCursor;
        private GameObject _TargetCursor;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            CacheMissingReferences();
            EnsureCursors();
            ClearAll();
        }

        private void OnValidate() => CacheMissingReferences();

        #endregion

        #region _____________________________| CURSORS

        public void SetHover(GridCoord pCoord, bool pVisible)
        {
            SetCursor(_HoverCursor, pCoord, pVisible, nameof(_HoverCursorPrefab));
        }

        public void SetSelection(GridCoord pCoord, bool pVisible)
        {
            SetCursor(_SelectionCursor, pCoord, pVisible, nameof(_SelectionCursorPrefab));
        }

        public void SetTarget(GridCoord pCoord, bool pVisible)
        {
            SetCursor(_TargetCursor, pCoord, pVisible, nameof(_TargetCursorPrefab));
        }

        public void ClearAll()
        {
            SetActive(_HoverCursor, false);
            SetActive(_SelectionCursor, false);
            SetActive(_TargetCursor, false);
        }

        #endregion

        #region _____________________________| HELPERS

        private void CacheMissingReferences()
        {
            if (_BoardView == null)
                _BoardView = GetComponent<BoardView>();
        }

        private void EnsureCursors()
        {
            Transform lParent = _CursorRoot != null ? _CursorRoot : transform;

            _HoverCursor ??= InstantiateCursor(_HoverCursorPrefab, lParent, "HoverCursor");
            _SelectionCursor ??= InstantiateCursor(_SelectionCursorPrefab, lParent, "SelectionCursor");
            _TargetCursor ??= InstantiateCursor(_TargetCursorPrefab, lParent, "TargetCursor");
        }

        private GameObject InstantiateCursor(GameObject pPrefab, Transform pParent, string pName)
        {
            if (pPrefab == null)
            {
                Debug.LogWarning($"BoardCursorView is missing prefab reference for '{pName}'.", this);
                return null;
            }

            GameObject lInstance = Instantiate(pPrefab, pParent);
            lInstance.name = pName;
            lInstance.SetActive(false);
            return lInstance;
        }

        private void SetCursor(GameObject pCursor, GridCoord pCoord, bool pVisible, string pFieldName)
        {
            if (!pVisible)
            {
                SetActive(pCursor, false);
                return;
            }

            if (_BoardView == null)
            {
                Debug.LogWarning("BoardCursorView is missing reference '_BoardView'.", this);
                SetActive(pCursor, false);
                return;
            }

            if (pCursor == null)
            {
                Debug.LogWarning($"BoardCursorView cannot show cursor because '{pFieldName}' was not created.", this);
                return;
            }

            pCursor.transform.position = _BoardView.GetMarkerWorldPosition(pCoord);
            SetActive(pCursor, true);
        }

        private static void SetActive(GameObject pCursor, bool pValue)
        {
            if (pCursor != null && pCursor.activeSelf != pValue)
                pCursor.SetActive(pValue);
        }

        #endregion
    }
}
