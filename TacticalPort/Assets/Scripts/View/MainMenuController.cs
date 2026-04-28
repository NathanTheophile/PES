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
    public sealed class MainMenuController : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private string _TeamSelectionSceneName = "S_TeamSelection";

        private Button _PlayTestButton;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            CacheReferences();
            TeamSelectionState.Clear();

            if (_PlayTestButton != null)
            {
                _PlayTestButton.onClick.RemoveListener(LoadTeamSelectionScene);
                _PlayTestButton.onClick.AddListener(LoadTeamSelectionScene);
            }
        }

        #endregion

        #region _____________________________| HELPERS

        private void CacheReferences()
        {
            if (_PlayTestButton != null)
                return;

            Transform lButtonTransform = transform.Find("VBox_LeftButtons/Btn_PlayTest");
            if (lButtonTransform != null)
                _PlayTestButton = lButtonTransform.GetComponent<Button>();
        }

        private void LoadTeamSelectionScene()
        {
            if (!string.IsNullOrWhiteSpace(_TeamSelectionSceneName))
                SceneManager.LoadScene(_TeamSelectionSceneName);
        }

        #endregion
    }
}
