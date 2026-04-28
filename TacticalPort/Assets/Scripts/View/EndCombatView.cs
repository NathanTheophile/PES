#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TacticalPort.View
{
    public sealed class EndCombatView : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private GameObject _WinPanel;
        [SerializeField] private GameObject _LosePanel;
        [SerializeField] private string _MainMenuSceneName = "S_MainMenu";

        private bool _IsConfigured;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            Configure();
            Hide();
        }

        private void OnDestroy()
        {
            UnhookButtons();
        }

        #endregion

        #region _____________________________| DISPLAY

        public void Show(BattleOutcome pOutcome)
        {
            Configure();

            bool lIsPlayerVictory = pOutcome == BattleOutcome.PlayerVictory;
            bool lIsDefeat = pOutcome == BattleOutcome.EnemyVictory || pOutcome == BattleOutcome.Draw;

            gameObject.SetActive(lIsPlayerVictory || lIsDefeat);
            SetPanelActive(_WinPanel, lIsPlayerVictory);
            SetPanelActive(_LosePanel, lIsDefeat);
        }

        public void Hide()
        {
            Configure();
            SetPanelActive(_WinPanel, false);
            SetPanelActive(_LosePanel, false);
        }

        #endregion

        #region _____________________________| HELPERS

        private void Configure()
        {
            if (_IsConfigured)
                return;

            _WinPanel ??= FindChildGameObject("UI_Panel_Win");
            _LosePanel ??= FindChildGameObject("UI_Panel_Lose");
            HookButtons();
            _IsConfigured = true;
        }

        private void HookButtons()
        {
            Button[] lButtons = GetComponentsInChildren<Button>(true);
            for (int lIndex = 0; lIndex < lButtons.Length; lIndex++)
            {
                Button lButton = lButtons[lIndex];
                if (lButton == null || lButton.gameObject.name != "Btn_MainMenu")
                    continue;

                lButton.onClick.RemoveListener(LoadMainMenu);
                lButton.onClick.AddListener(LoadMainMenu);
            }
        }

        private void UnhookButtons()
        {
            Button[] lButtons = GetComponentsInChildren<Button>(true);
            for (int lIndex = 0; lIndex < lButtons.Length; lIndex++)
            {
                Button lButton = lButtons[lIndex];
                if (lButton != null)
                    lButton.onClick.RemoveListener(LoadMainMenu);
            }
        }

        private void LoadMainMenu()
        {
            if (!string.IsNullOrWhiteSpace(_MainMenuSceneName))
                SceneManager.LoadScene(_MainMenuSceneName, LoadSceneMode.Single);
        }

        private GameObject FindChildGameObject(string pName)
        {
            if (string.IsNullOrWhiteSpace(pName))
                return null;

            Transform[] lChildren = GetComponentsInChildren<Transform>(true);
            for (int lIndex = 0; lIndex < lChildren.Length; lIndex++)
            {
                if (lChildren[lIndex] != null && lChildren[lIndex].gameObject.name == pName)
                    return lChildren[lIndex].gameObject;
            }

            return null;
        }

        private static void SetPanelActive(GameObject pPanel, bool pIsActive)
        {
            if (pPanel != null && pPanel.activeSelf != pIsActive)
                pPanel.SetActive(pIsActive);
        }

        #endregion
    }
}
