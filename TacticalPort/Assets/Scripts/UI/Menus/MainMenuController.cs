#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TacticalPort.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private string _TeamSelectionSceneName = "S_TeamSelection";
        [SerializeField] private Button _TeamButton;
        [SerializeField, HideInInspector] private Button _PlayTestButton;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            Button lTeamButton = _TeamButton != null ? _TeamButton : _PlayTestButton;

            if (lTeamButton == null)
            {
                Debug.LogWarning($"{nameof(MainMenuController)} is missing a Team button reference.", this);
                return;
            }

            lTeamButton.onClick.RemoveListener(OpenTeamSelection);
            lTeamButton.onClick.AddListener(OpenTeamSelection);
        }

        #endregion

        #region _____________________________| HELPERS

        public void OpenTeamSelection()
        {
            if (!string.IsNullOrWhiteSpace(_TeamSelectionSceneName))
                SceneManager.LoadScene(_TeamSelectionSceneName);
        }

        #endregion
    }
}
