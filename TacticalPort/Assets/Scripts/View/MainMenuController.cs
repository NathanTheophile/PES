using TacticalPort.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TacticalPort.View
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string _TeamSelectionSceneName = "S_TeamSelection";

        private Button _PlayTestButton;

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
    }
}
