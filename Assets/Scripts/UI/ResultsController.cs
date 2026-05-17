using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TowerDefense.Data;
using TowerDefense.Services;

namespace TowerDefense.UI
{
    public class ResultsController : MonoBehaviour
    {
        [Header("Player Stats")]
        [SerializeField] private TextMeshProUGUI playerScoreLabel;
        [SerializeField] private TextMeshProUGUI wavesLabel;
        [SerializeField] private TextMeshProUGUI accuracyLabel;

        [Header("Leaderboard")]
        [SerializeField] private Transform rankingContainer;
        [SerializeField] private GameObject rankingRowPrefab;
        [SerializeField] private TextMeshProUGUI loadingLabel;

        [Header("Dashboard - Score Distribution")]
        [SerializeField] private RectTransform barChartParent;
        [SerializeField] private GameObject barPrefab;

        [Header("Dashboard - Average Accuracy")]
        [SerializeField] private RectTransform accuracyBarParent;

        [Header("Navigation")]
        [SerializeField] private Button replayButton;
        [SerializeField] private Button menuButton;

        private const float BAR_MAX_HEIGHT = 200f;

        private static readonly int[] SCORE_BUCKETS =
        {
            0, 100, 200, 300, 500, 750, 1000, 1500
        };

        private void Start()
        {
            int score = PlayerPrefs.GetInt("FinalScore", 0);
            int waves = PlayerPrefs.GetInt("WavesCompleted", 0);
            float accuracy = PlayerPrefs.GetFloat("Accuracy", 0f);

            playerScoreLabel.text = $"Your Score: <b>{score}</b>";
            wavesLabel.text = $"Waves Completed: {waves}";
            accuracyLabel.text = $"Shot Accuracy: {accuracy:F1}%";

            replayButton.onClick.AddListener(() =>
                SceneManager.LoadScene("Game"));

            menuButton.onClick.AddListener(() =>
                SceneManager.LoadScene("Menu"));

            StartCoroutine(LoadFromFirebase());
        }

        private IEnumerator LoadFromFirebase()
        {
            loadingLabel.text = "Loading leaderboard...";

            var highscoreTask =
                FirebaseService.Instance.GetHighscoresAsync(10);

            yield return new WaitUntil(() =>
                highscoreTask.IsCompleted);

            var sessionsTask =
                FirebaseService.Instance.GetRecentSessionsAsync(50);

            yield return new WaitUntil(() =>
                sessionsTask.IsCompleted);

            loadingLabel.gameObject.SetActive(false);

            BuildRankingTable(highscoreTask.Result);
            BuildScoreDistributionChart(sessionsTask.Result);
            BuildAccuracyChart(sessionsTask.Result);
        }

        private void BuildRankingTable(List<HighscoreEntry> entries)
        {
            foreach (Transform child in rankingContainer)
                Destroy(child.gameObject);

            string myName = PlayerPrefs.GetString("PlayerName", "");
            int rank = 1;

            foreach (var entry in entries)
            {
                var row = Instantiate(
                    rankingRowPrefab,
                    rankingContainer
                );

                var texts =
                    row.GetComponentsInChildren<TextMeshProUGUI>();

                if (texts.Length >= 3)
                {
                    texts[0].text = $"#{rank}";
                    texts[1].text = entry.playerName;
                    texts[2].text = entry.score.ToString();
                }

                bool isMe = entry.playerName == myName;

                var bg = row.GetComponent<Image>();

                if (bg != null)
                {
                    bg.color = isMe
                        ? new Color(1f, 0.9f, 0.2f, 0.3f)
                        : Color.clear;
                }

                rank++;
            }
        }

        private void BuildScoreDistributionChart(
            List<SessionData> sessions)
        {
            if (barChartParent == null ||
                barPrefab == null ||
                sessions.Count == 0)
            {
                return;
            }

            int bucketCount = SCORE_BUCKETS.Length;
            int[] counts = new int[bucketCount];

            foreach (var s in sessions)
            {
                for (int i = bucketCount - 1; i >= 0; i--)
                {
                    if (s.finalScore >= SCORE_BUCKETS[i])
                    {
                        counts[i]++;
                        break;
                    }
                }
            }

            int maxCount = Mathf.Max(1, Mathf.Max(counts));

            foreach (Transform child in barChartParent)
                Destroy(child.gameObject);

            for (int i = 0; i < bucketCount; i++)
            {
                var bar = Instantiate(barPrefab, barChartParent);

                float height =
                    BAR_MAX_HEIGHT *
                    ((float)counts[i] / maxCount);

                var rect =
                    bar.GetComponentInChildren<Image>().rectTransform;

                rect.sizeDelta = new Vector2(
                    rect.sizeDelta.x,
                    Mathf.Max(4f, height)
                );

                var label =
                    bar.GetComponentInChildren<TextMeshProUGUI>();

                if (label != null)
                {
                    label.text =
                        $"{SCORE_BUCKETS[i]}+\n({counts[i]})";
                }
            }
        }

        private void BuildAccuracyChart(
            List<SessionData> sessions)
        {
            if (accuracyBarParent == null ||
                barPrefab == null ||
                sessions.Count == 0)
            {
                return;
            }

            var totals =
                new Dictionary<string, (float sum, int count)>();

            foreach (var s in sessions)
            {
                if (!totals.ContainsKey(s.playerName))
                {
                    totals[s.playerName] = (0f, 0);
                }

                var (sum, count) = totals[s.playerName];

                totals[s.playerName] =
                    (sum + s.accuracy, count + 1);
            }

            var top5 =
                new List<(string name, float avg)>();

            foreach (var kv in totals)
            {
                top5.Add((
                    kv.Key,
                    kv.Value.sum / kv.Value.count
                ));
            }

            top5.Sort((a, b) =>
                b.avg.CompareTo(a.avg));

            if (top5.Count > 5)
            {
                top5.RemoveRange(5, top5.Count - 5);
            }

            foreach (Transform child in accuracyBarParent)
                Destroy(child.gameObject);

            foreach (var (name, avg) in top5)
            {
                var bar = Instantiate(
                    barPrefab,
                    accuracyBarParent
                );

                float height =
                    BAR_MAX_HEIGHT * (avg / 100f);

                var rect =
                    bar.GetComponentInChildren<Image>().rectTransform;

                rect.sizeDelta = new Vector2(
                    rect.sizeDelta.x,
                    Mathf.Max(4f, height)
                );

                var img =
                    bar.GetComponentInChildren<Image>();

                if (img != null)
                {
                    img.color =
                        new Color(0.2f, 0.8f, 0.4f);
                }

                var label =
                    bar.GetComponentInChildren<TextMeshProUGUI>();

                if (label != null)
                {
                    label.text =
                        $"{name}\n{avg:F0}%";
                }
            }
        }
    }
}