#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Core.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TacticalPort.View
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

        private void Awake()
        {
            CacheMissingReferences();
        }

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
            CacheMissingReferences();

            if (_DisplayedUnit == null)
            {
                SetText(_NameText, "-");
                SetText(_HealthText, "HP: -");
                SetText(_ActionPointsText, "AP: -");
                SetText(_MovementText, "MP: -");
                SetPortrait(null);
                return;
            }

            SetPortrait(_DisplayedUnit.Definition.Portrait);
            SetText(_NameText, _DisplayedUnit.Definition.DisplayName);
            SetText(_HealthText, $"HP: {_DisplayedUnit.CurrentHealth}/{_DisplayedUnit.Definition.MaxHealth}");
            SetText(_ActionPointsText, $"AP: {_DisplayedUnit.RemainingActionPoints}/{_DisplayedUnit.Definition.ActionPointsPerTurn}");
            SetText(_MovementText, $"MP: {_DisplayedUnit.RemainingMovement}/{_DisplayedUnit.Definition.MoveRange}");
        }

        #endregion

        #region _____________________________| HELPERS

        private void CacheMissingReferences()
        {
            _PortraitImage ??= FindChildImage("Img_UnitPortrait");
            _NameText ??= FindChildText("Txt_UnitName");
            _HealthText ??= FindChildText("Txt_UnitHP");
            _ActionPointsText ??= FindChildText("Txt_UnitAP");
            _MovementText ??= FindChildText("Txt_UnitMP");
        }

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

        private Image FindChildImage(string pName)
        {
            Image[] lImages = GetComponentsInChildren<Image>(true);
            for (int lIndex = 0; lIndex < lImages.Length; lIndex++)
            {
                if (lImages[lIndex] != null && lImages[lIndex].gameObject.name == pName)
                    return lImages[lIndex];
            }

            return lImages.Length > 0 ? lImages[0] : null;
        }

        private TMP_Text FindChildText(string pName)
        {
            TMP_Text[] lTexts = GetComponentsInChildren<TMP_Text>(true);
            for (int lIndex = 0; lIndex < lTexts.Length; lIndex++)
            {
                if (lTexts[lIndex] != null && lTexts[lIndex].gameObject.name == pName)
                    return lTexts[lIndex];
            }

            return null;
        }

        #endregion
    }
}
