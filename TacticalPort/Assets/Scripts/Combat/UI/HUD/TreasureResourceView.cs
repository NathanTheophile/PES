#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TacticalPort.UI
{
    public sealed class TreasureResourceView : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private TMP_Text _Label;
        [SerializeField] private Image[] _Slots;
        [SerializeField] private Color _FilledColor = new Color(1f, 0.72f, 0.18f, 1f);
        [SerializeField] private Color _EmptyColor = new Color(0.18f, 0.18f, 0.18f, 0.75f);
        [SerializeField] private string _LabelFormat = "Coffre {0}/{1}";

        #endregion

        #region _____________________________| DISPLAY

        public void Refresh(UnitRuntime pUnit)
        {
            bool lHasResource = pUnit != null && pUnit.HasTreasureResource;
            if (gameObject.activeSelf != lHasResource)
                gameObject.SetActive(lHasResource);

            if (!lHasResource)
                return;

            int lTreasureCount = pUnit.TreasureCount;
            int lMaxTreasure = pUnit.MaxTreasure;

            if (_Label != null)
                _Label.text = string.Format(_LabelFormat, lTreasureCount, lMaxTreasure);

            if (_Slots == null)
                return;

            for (int lIndex = 0; lIndex < _Slots.Length; lIndex++)
            {
                Image lSlot = _Slots[lIndex];
                if (lSlot == null)
                    continue;

                lSlot.gameObject.SetActive(lIndex < lMaxTreasure);
                lSlot.color = lIndex < lTreasureCount ? _FilledColor : _EmptyColor;
            }
        }

        #endregion
    }
}
