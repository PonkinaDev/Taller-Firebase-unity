using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TowerDefense.UI
{
    /// <summary>
    /// Controlador de la escena "Menu".
    ///
    /// INSPECTOR ─ Escena "Menu" ──────────────────────────────────────────
    ///   • Adjuntar a GameObject "MenuController"
    ///   • playerNameInput → TMP_InputField "Ingresa tu nombre"
    ///   • startButton     → Button "JUGAR"
    ///   • errorLabel      → TextMeshProUGUI (pequeño, rojo, oculto)
    ///
    /// Jerarquía Canvas sugerida:
    ///   Canvas
    ///   └── Panel (fondo oscuro semitransparente)
    ///       ├── Title (TextMeshPro "⚔ DEFENSA DE TORRE ⚔")
    ///       ├── playerNameInput
    ///       ├── startButton
    ///       └── errorLabel
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        [SerializeField] private TMP_InputField playerNameInput;
        [SerializeField] private Button         startButton;
        [SerializeField] private TextMeshProUGUI errorLabel;

        private void Start()
        {
            errorLabel.text = "";
            startButton.onClick.AddListener(OnStartClicked);

            // Restaurar nombre anterior si existe
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
