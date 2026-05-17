using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace TowerDefense.Core
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("HUD")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private TextMeshProUGUI messageText;

        [Header("End Screen")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI gameOverLabel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            UpdateScore(0);

            waveText.text = "Wave: 1";

            messageText.text = "";

            gameOverPanel.SetActive(false);
        }

        public void UpdateScore(int score)
        {
            scoreText.text = $"Score: {score}";
        }

        public void ShowWaveMessage(int waveCompleted)
        {
            StartCoroutine(
                ShowMessage($"Wave {waveCompleted} completed!")
            );
        }

        private IEnumerator ShowMessage(string msg)
        {
            messageText.text = msg;

            yield return new WaitForSeconds(2f);

            messageText.text = "";
        }

        public void ShowGameOver(bool playerWon)
        {
            gameOverPanel.SetActive(true);

            gameOverLabel.text =
                playerWon
                    ? "VICTORY!"
                    : "GAME OVER!";
        }
    }
}