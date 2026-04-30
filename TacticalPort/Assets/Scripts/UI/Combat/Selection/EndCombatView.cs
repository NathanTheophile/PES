#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System.Collections.Generic;
using TacticalPort.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TacticalPort.UI
{
    public sealed class EndCombatView : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private GameObject _WinPanel;
        [SerializeField] private GameObject _LosePanel;
        [SerializeField] private Button[] _MainMenuButtons = new Button[0];
        [SerializeField] private string _MainMenuSceneName = "S_MainMenu";

        private readonly List<Button> _ResolvedMainMenuButtons = new List<Button>();
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

            ValidateReferences();
            HookButtons();
            _IsConfigured = true;
        }

        private void HookButtons()
        {
            ResolveMainMenuButtons();
            for (int lIndex = 0; lIndex < _ResolvedMainMenuButtons.Count; lIndex++)
            {
                Button lButton = _ResolvedMainMenuButtons[lIndex];
                if (lButton == null)
                    continue;

                lButton.onClick.RemoveListener(LoadMainMenu);
                lButton.onClick.AddListener(LoadMainMenu);
            }
        }

        private void UnhookButtons()
        {
            ResolveMainMenuButtons();
            for (int lIndex = 0; lIndex < _ResolvedMainMenuButtons.Count; lIndex++)
            {
                Button lButton = _ResolvedMainMenuButtons[lIndex];
                if (lButton != null)
                    lButton.onClick.RemoveListener(LoadMainMenu);
            }
        }

        private void LoadMainMenu()
        {
            if (!string.IsNullOrWhiteSpace(_MainMenuSceneName))
                SceneManager.LoadScene(_MainMenuSceneName, LoadSceneMode.Single);
        }

        private void ResolveMainMenuButtons()
        {
            _ResolvedMainMenuButtons.Clear();

            if (_MainMenuButtons != null)
            {
                for (int lIndex = 0; lIndex < _MainMenuButtons.Length; lIndex++)
                    AddResolvedMainMenuButton(_MainMenuButtons[lIndex]);
            }

            Button[] lChildButtons = GetComponentsInChildren<Button>(true);
            for (int lIndex = 0; lIndex < lChildButtons.Length; lIndex++)
                AddResolvedMainMenuButton(lChildButtons[lIndex]);
        }

        private void AddResolvedMainMenuButton(Button pButton)
        {
            if (pButton != null && !_ResolvedMainMenuButtons.Contains(pButton))
                _ResolvedMainMenuButtons.Add(pButton);
        }

        private void ValidateReferences()
        {
            LogMissingReference(_WinPanel, nameof(_WinPanel));
            LogMissingReference(_LosePanel, nameof(_LosePanel));
            ResolveMainMenuButtons();
            if (_ResolvedMainMenuButtons.Count == 0)
                Debug.LogWarning($"{nameof(EndCombatView)} is missing main menu button references.", this);
        }

        private void LogMissingReference(Object pReference, string pFieldName)
        {
            if (pReference == null)
                Debug.LogWarning($"{nameof(EndCombatView)} is missing reference '{pFieldName}'.", this);
        }

        private static void SetPanelActive(GameObject pPanel, bool pIsActive)
        {
            if (pPanel != null && pPanel.activeSelf != pIsActive)
                pPanel.SetActive(pIsActive);
        }

        #endregion
    }
}
