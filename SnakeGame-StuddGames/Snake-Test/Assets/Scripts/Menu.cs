using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button playBtn;
    [SerializeField] private Button quitBtn;

    [Header("Best Score Texts (Top 4)")]
    [Tooltip("Assign 4 TextMeshProUGUI elements here (Size 4)")]
    [SerializeField] private TextMeshProUGUI[] bestScoreTexts = new TextMeshProUGUI[4];

    [Header("Or Assign Individually")]
    [SerializeField] private TextMeshProUGUI firstScoreText;
    [SerializeField] private TextMeshProUGUI secondScoreText;
    [SerializeField] private TextMeshProUGUI thirdScoreText;
    [SerializeField] private TextMeshProUGUI fourthScoreText;

    [Header("Display Format")]
    [Tooltip("If true, formats as '1. 100'. If false, formats as '100'")]
    [SerializeField] private bool showRankPrefix = false;
    [Tooltip("Optional prefix before the score, e.g. 'Score: '")]
    [SerializeField] private string scorePrefix = "";

    private void Awake()
    { 
        if (playBtn != null)
            playBtn.onClick.AddListener(OnPlayButtonClicked);

        if (quitBtn != null)
            quitBtn.onClick.AddListener(OnQuitButtonClicked);
    }

    private void Start()
    {
        DisplayBestScores();
    }

    private void OnEnable()
    {
        DisplayBestScores();
    }

    /// <summary>
    /// Loads the 4 best scores from the persistent JSON file and applies them to the TextMeshPro elements.
    /// </summary>
    public void DisplayBestScores()
    {
        List<int> scores = ScoreManager.LoadScores();

        // 1. Apply to array elements if assigned
        if (bestScoreTexts != null)
        {
            for (int i = 0; i < bestScoreTexts.Length && i < scores.Count; i++)
            {
                if (bestScoreTexts[i] != null)
                {
                    bestScoreTexts[i].text = FormatScore(i, scores[i]);
                }
            }
        }

        // 2. Also apply to individual fields if assigned
        if (firstScoreText != null && scores.Count > 0)
            firstScoreText.text = FormatScore(0, scores[0]);

        if (secondScoreText != null && scores.Count > 1)
            secondScoreText.text = FormatScore(1, scores[1]);

        if (thirdScoreText != null && scores.Count > 2)
            thirdScoreText.text = FormatScore(2, scores[2]);

        if (fourthScoreText != null && scores.Count > 3)
            fourthScoreText.text = FormatScore(3, scores[3]);
    }

    private string FormatScore(int index, int score)
    {
        string rankStr = showRankPrefix ? $"{index + 1}. " : "";
        return $"{rankStr}{scorePrefix}{score}";
    }

    private void OnPlayButtonClicked()
    {
        // Load the game scene
        SceneManager.LoadScene(1);
    }

    private void OnQuitButtonClicked()
    {
        // Quit the application
        Application.Quit();
    }
}
