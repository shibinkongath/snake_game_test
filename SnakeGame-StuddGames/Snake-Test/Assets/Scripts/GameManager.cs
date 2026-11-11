using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;

public class GameManager : MonoBehaviour
{
    private int score = 0;
    [SerializeField] private int foodScore = 10;
    [SerializeField] private int bonusFoodScore = 20;

    [SerializeField] private TextMeshProUGUI scoreTxt;
    [SerializeField] private TextMeshProUGUI gameoverScoreTxt;
    [SerializeField] private GameObject gameoverScreen;
    [SerializeField] private Button retryBtn;
    [SerializeField] private Button menuBtn;
    [SerializeField] private GameObject foodSpawner; 
    private FoodSpawner foodSpawnerScript;
    public static GameManager Instance { get; private set; }

    private bool isGameOver = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (foodSpawner != null)
        {
            foodSpawnerScript = foodSpawner.GetComponent<FoodSpawner>();
        }
        else
        {
            foodSpawnerScript = FindFirstObjectByType<FoodSpawner>();
        }

        if (gameoverScreen != null)
        {
            gameoverScreen.SetActive(false);
        }

        // Button listeners
        if (retryBtn != null)
            retryBtn.onClick.AddListener(Retry);

        if (menuBtn != null)
            menuBtn.onClick.AddListener(BancktoMenu);

        Time.timeScale = 1f; // Ensure the game runs at normal speed when starting
    }

    public void OnFoodAte(bool isBonus)
    {
        if (isBonus)
        {
            score += bonusFoodScore;
        }
        else
        {
            score += foodScore;
        }

        UpdateScoreText();

        if (foodSpawnerScript != null)
        {
            foodSpawnerScript.OnFoodConsumed(isBonus);
        }
        else
        {
            Debug.LogWarning("GameManager: FoodSpawner reference is missing!");
        }
    }

    private void UpdateScoreText()
    {
        if (scoreTxt != null)
        {
            scoreTxt.text = "Score: " + score.ToString();
        }
    }

    public void GameOver()
    {
        if (isGameOver)
            return;

        isGameOver = true;

        // Check if the current score qualifies for the top 4 and save to JSON
        ScoreManager.TryAddScore(score);

        if (gameoverScreen != null)
        {
            gameoverScreen.SetActive(true);
        }
        if (gameoverScoreTxt != null)
        {
            gameoverScoreTxt.text = "Total Score: " + score.ToString();
        }   
        Time.timeScale = 0f;
    }

    private void Retry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void BancktoMenu()
    {
        SceneManager.LoadScene(0);
    }
}
