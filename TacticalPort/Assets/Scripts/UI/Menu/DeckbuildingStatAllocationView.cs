#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : Inspector-wired view for one deckbuilding stat allocation row.
//  Menu UI
#endregion

using System;
using TacticalPort.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TacticalPort.UI
{
    public sealed class DeckbuildingStatAllocationView : MonoBehaviour
    {
        [SerializeField] private UnitStatType _Stat;
        [SerializeField] private TMP_Text _Value;
        [SerializeField] private Button _DecreaseButton;
        [SerializeField] private Button _IncreaseButton;

        public UnitStatType Stat => _Stat;

        public void Bind(
            int pDisplayedValue,
            bool pCanDecrease,
            bool pCanIncrease,
            Action pDecrease,
            Action pIncrease)
        {
            if (_Value != null)
                _Value.text = pDisplayedValue.ToString();

            BindButton(_DecreaseButton, pCanDecrease, pDecrease);
            BindButton(_IncreaseButton, pCanIncrease, pIncrease);
        }

        private static void BindButton(Button pButton, bool pInteractable, Action pAction)
        {
            if (pButton == null)
                return;

            pButton.onClick.RemoveAllListeners();
            pButton.interactable = pInteractable && pAction != null;
            if (pAction != null)
                pButton.onClick.AddListener(() => pAction());
        }
    }
}
