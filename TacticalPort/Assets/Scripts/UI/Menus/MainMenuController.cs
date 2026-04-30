#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TacticalPort.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private string _TeamSelectionSceneName = "S_TeamSelection";
        [SerializeField] private Button _PlayTestButton;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            ValidateReferences();
            TeamSelectionState.Clear();

            if (_PlayTestButton != null)
            {
                _PlayTestButton.onClick.RemoveListener(LoadTeamSelectionScene);
                _PlayTestButton.onClick.AddListener(LoadTeamSelectionScene);
            }
        }

        #endregion

        #region _____________________________| HELPERS

        private void ValidateReferences()
        {
            if (_PlayTestButton == null)
                Debug.LogWarning($"{nameof(MainMenuController)} is missing reference '{nameof(_PlayTestButton)}'.", this);
        }

        private void LoadTeamSelectionScene()
        {
            if (!string.IsNullOrWhiteSpace(_TeamSelectionSceneName))
                SceneManager.LoadScene(_TeamSelectionSceneName);
        }

        #endregion
    }
}
