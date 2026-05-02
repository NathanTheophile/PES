#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TacticalPort.UI
{
    public sealed class UnitInfoBoxView : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private Image _PortraitImage;
        [SerializeField] private TMP_Text _NameText;
        [SerializeField] private TMP_Text _HealthText;
        [SerializeField] private TMP_Text _ActionPointsText;
        [SerializeField] private TMP_Text _MovementText;

        private UnitRuntime _DisplayedUnit;

        #endregion

        #region _____________________________/ ACCESSORS

        public UnitRuntime Unit => _DisplayedUnit;
        public bool HasUnit => _DisplayedUnit != null;

        #endregion

        #region _____________________________| UNITY

        private void Awake() => ValidateReferences();

        #endregion

        #region _____________________________| DISPLAY

        public void Show(UnitRuntime pUnit)
        {
            _DisplayedUnit = pUnit;
            Refresh();

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
        }

        public void Hide()
        {
            _DisplayedUnit = null;

            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        public void Refresh()
        {
            if (_DisplayedUnit == null)
            {
                SetText(_NameText, "-");
                SetText(_HealthText, "HP: -");
                SetText(_ActionPointsText, "AP: -");
                SetText(_MovementText, "MP: -");
                SetPortrait(null);
                return;
            }

            SetPortrait(_DisplayedUnit.Definition.DisplaySprite);
            SetText(_NameText, _DisplayedUnit.Definition.DisplayName);
            SetText(_HealthText, $"HP: {_DisplayedUnit.CurrentHealth}/{_DisplayedUnit.Definition.MaxHealth}");
            SetText(_ActionPointsText, $"AP: {_DisplayedUnit.RemainingActionPoints}/{_DisplayedUnit.Definition.ActionPointsPerTurn}");
            SetText(_MovementText, $"MP: {_DisplayedUnit.RemainingMovement}/{_DisplayedUnit.Definition.MoveRange}");
        }

        #endregion

        #region _____________________________| HELPERS

        private void SetPortrait(Sprite pPortrait)
        {
            if (_PortraitImage == null)
                return;

            _PortraitImage.sprite = pPortrait;
            _PortraitImage.enabled = pPortrait != null;
            _PortraitImage.preserveAspect = true;
        }

        private static void SetText(TMP_Text pTarget, string pValue)
        {
            if (pTarget != null)
                pTarget.text = pValue ?? string.Empty;
        }

        private void ValidateReferences()
        {
            LogMissingReference(_PortraitImage, nameof(_PortraitImage));
            LogMissingReference(_NameText, nameof(_NameText));
            LogMissingReference(_HealthText, nameof(_HealthText));
            LogMissingReference(_ActionPointsText, nameof(_ActionPointsText));
            LogMissingReference(_MovementText, nameof(_MovementText));
        }

        private void LogMissingReference(Object pReference, string pFieldName)
        {
            if (pReference == null)
                Debug.LogWarning($"{nameof(UnitInfoBoxView)} is missing reference '{pFieldName}'.", this);
        }

        #endregion
    }
}
