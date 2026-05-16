using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace TowerDefense.Core
{
    /// <summary>
    /// Gestiona el HUD durante la partida.
    ///
    /// INSPECTOR ─ Escena "Game" ──────────────────────────────────────────
    ///   • Crear Canvas (Screen Space - Overlay)
    ///   • Adjuntar UIManager al Canvas o a un GameObject vacío "UIManager"
    ///   • scoreText     → TextMeshProUGUI "Score: 0"  (esquina superior)
    ///   • waveText      → TextMeshProUGUI "Wave 1"    (esquina superior)
    ///   • messageText   → TextMeshProUGUI centrado (inicialmente invisible)
    ///   • gameOverPanel → Panel oscuro con texto "¡GAME OVER!" / "¡VICTORIA!"
    ///                     (inicialmente SetActive(false))
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private TextMeshProUGUI messageText;

        [Header("Pantalla de fin")]
        [SerializeField] private GameObject      gameOverPanel;
        [SerializeField] private TextMeshProUGUI gameOverLabel;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            UpdateScore(0);
            waveText.text        = "Oleada 1";
            messageText.text     = "";
            gameOverPanel.SetActive(false);
        }

        public void UpdateScore(int score)
            => scoreText.text = $"Puntaje: {score}";

        public void ShowWaveMessage(int waveCompleted)
            => StartCoroutine(ShowMessage($"¡Oleada {waveCompleted} superada!"));

        private IEnumerator ShowMessage(string msg)
        {
            messageText.text = msg;
            yield return new WaitForSeconds(2f);
            messageText.text = "";
        }

        public void ShowGameOver(bool playerWon)
        {
            gameOverPanel.SetActive(true);
            gameOverLabel.text = playerWon ? "¡VICTORIA!" : "¡GAME OVER!";
        }
    }
}
