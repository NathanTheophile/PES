#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Core;
using TacticalPort.Data;
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
        [SerializeField] private TMP_Text _EnergyText;
        [SerializeField] private TMP_Text _MobilityText;

        private UnitRuntime _DisplayedUnit;

        #endregion

        #region _____________________________/ ACCESSORS

        public UnitRuntime Unit => _DisplayedUnit;
        public bool HasUnit => _DisplayedUnit != null;

        #endregion

        #region _____________________________| UNITY

        private void Awake() => LogMissingReferences();

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
                SetText(_HealthText, GameLocalization.Get(GameLocalization.UiTable, "stats.health_empty", "Health: -"));
                SetText(_EnergyText, GameLocalization.Get(GameLocalization.UiTable, "stats.energy_empty", "Energy: -"));
                SetText(_MobilityText, GameLocalization.Get(GameLocalization.UiTable, "stats.mobility_empty", "Mobility: -"));
                SetPortrait(null);
                return;
            }

            SetPortrait(_DisplayedUnit.Definition.DisplaySprite);
            SetText(_NameText, _DisplayedUnit.Definition.DisplayName);
            SetText(_HealthText, GameLocalization.Get(GameLocalization.UiTable, "stats.health_wear", "Health: {0}/{1} | Wear: {2}%",
                _DisplayedUnit.CurrentHealth, _DisplayedUnit.CurrentMaxHealth, _DisplayedUnit.EffectiveWearPercent));
            SetText(_EnergyText, GameLocalization.Get(GameLocalization.UiTable, "stats.energy", "Energy: {0}/{1}",
                _DisplayedUnit.RemainingEnergy, _DisplayedUnit.Definition.EnergyPerTurn));
            SetText(_MobilityText, GameLocalization.Get(GameLocalization.UiTable, "stats.mobility", "Mobility: {0}/{1}",
                _DisplayedUnit.RemainingMobility, _DisplayedUnit.Definition.MobilityPerTurn));
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

        private void LogMissingReferences()
        {
            LogMissingReference(_PortraitImage, nameof(_PortraitImage));
            LogMissingReference(_NameText, nameof(_NameText));
            LogMissingReference(_HealthText, nameof(_HealthText));
            LogMissingReference(_EnergyText, nameof(_EnergyText));
            LogMissingReference(_MobilityText, nameof(_MobilityText));
        }

        private void LogMissingReference(Object pReference, string pFieldName)
        {
            if (pReference == null)
                Debug.LogWarning($"{nameof(UnitInfoBoxView)} is missing reference '{pFieldName}'.", this);
        }

        #endregion
    }
}
