using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TowerDefense.UI
{
    public class MenuController : MonoBehaviour
    {
        [SerializeField] private TMP_InputField playerNameInput;
        [SerializeField] private Button         startButton;
        [SerializeField] private TextMeshProUGUI errorLabel;

        private void Start()
        {
            errorLabel.text = "";
            startButton.onClick.AddListener(OnStartClicked);

            string saved = PlayerPrefs.GetString("PlayerName", "");
            if (!string.IsNullOrEmpty(saved))
                playerNameInput.text = saved;
        }

        private void OnStartClicked()
        {
            string name = playerNameInput.text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                errorLabel.text = "⚠ Ingresa tu nombre para continuar.";
                return;
            }

            PlayerPrefs.SetString("PlayerName", name);
            SceneManager.LoadScene("Game");
        }
    }
}
