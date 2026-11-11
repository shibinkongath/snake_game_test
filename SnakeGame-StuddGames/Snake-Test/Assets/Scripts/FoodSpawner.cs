using UnityEngine;
using UnityEngine.UI;

public class FoodSpawner : MonoBehaviour
{
    [Header("Food Objects")]
    [SerializeField] private GameObject normalFood;
    [SerializeField] private GameObject bonusFood;

    [Header("Play Area Anchors")]
    [SerializeField] private Transform leftAnchor;
    [SerializeField] private Transform rightAnchor;
    [SerializeField] private Transform topAnchor;
    [SerializeField] private Transform bottomAnchor;

    [Header("Grid Settings")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private float boundaryPadding = 1f;
    [SerializeField] private LayerMask obstacleLayers;

    [Header("Bonus Food Settings")]
    [SerializeField] private int foodsBeforeBonus = 5;
    [SerializeField] private float bonusDuration = 5f;

    [Header("Bonus Progress Bar")]
    [Tooltip("The parent/root GameObject of the progress bar to show/hide")]
    [SerializeField] private GameObject bonusProgressBar;
    [Tooltip("The UI Image (Image Type = Filled) whose fillAmount decreases over time")]
    [SerializeField] private Image bonusProgressBarFill;

    [Header("References")]
    [SerializeField] private SnakeController snakeController;

    private int normalFoodEatenCount;
    private float bonusTimer;
    private bool bonusActive;

    private void Awake()
    {
        if (snakeController == null)
        {
            snakeController = FindFirstObjectByType<SnakeController>();
        }
    }

    private void Start()
    {
        if (normalFood != null) normalFood.SetActive(false);
        if (bonusFood != null) bonusFood.SetActive(false);

        bonusActive = false;
        normalFoodEatenCount = 0;

        // Ensure progress bar is hidden initially
        if (bonusProgressBar != null)
        {
            bonusProgressBar.SetActive(false);
        }
        else if (bonusProgressBarFill != null)
        {
            bonusProgressBarFill.gameObject.SetActive(false);
        }

        SpawnNormalFood();
    }

    private void Update()
    {
        if (!bonusActive)
            return;

        bonusTimer -= Time.deltaTime;

        // Decrease the fill amount of the progress bar image based on remaining timer
        if (bonusProgressBarFill != null && bonusDuration > 0f)
        {
            bonusProgressBarFill.fillAmount = Mathf.Clamp01(bonusTimer / bonusDuration);
        }

        if (bonusTimer <= 0f)
        {
            EndBonusFood();
            SpawnNormalFood();
        }
    }

    /// <summary>
    /// Called when the snake consumes normal or bonus food.
    /// </summary>
    public void OnFoodConsumed(bool isBonus)
    {
        if (isBonus)
        {
            EndBonusFood();
            SpawnNormalFood();
        }
        else
        {
            normalFoodEatenCount++;

            if (normalFoodEatenCount >= Mathf.Max(1, foodsBeforeBonus))
            {
                normalFoodEatenCount = 0;
                SpawnBonusFood();
            }
            else
            {
                SpawnNormalFood();
            }
        }
    }

    /// <summary>
    /// Kept for backward compatibility if called from legacy triggers or inspector events.
    /// </summary>
    public void SpawnNextFood()
    {
        OnFoodConsumed(bonusActive);
    }

    /// <summary>
    /// Kept for backward compatibility.
    /// </summary>
    public void SpawnAfterBonus()
    {
        EndBonusFood();
        SpawnNormalFood();
    }

    private void SpawnNormalFood()
    {
        EndBonusFood();

        if (normalFood == null)
        {
            Debug.LogWarning("FoodSpawner: Normal Food reference is missing!");
            return;
        }

        normalFood.transform.position = GetValidSpawnPosition();
        normalFood.SetActive(true);
    }

    private void SpawnBonusFood()
    {
        if (normalFood != null) normalFood.SetActive(false);

        if (bonusFood == null)
        {
            Debug.LogWarning("FoodSpawner: Bonus Food reference is missing!");
            return;
        }

        bonusFood.transform.position = GetValidSpawnPosition();
        bonusFood.SetActive(true);

        bonusTimer = bonusDuration;
        bonusActive = true;

        // Show progress bar and reset fill amount to 100%
        if (bonusProgressBar != null)
        {
            bonusProgressBar.SetActive(true);
        }
        else if (bonusProgressBarFill != null)
        {
            bonusProgressBarFill.gameObject.SetActive(true);
        }

        if (bonusProgressBarFill != null)
        {
            bonusProgressBarFill.fillAmount = 1f;
        }
    }

    private void EndBonusFood()
    {
        bonusActive = false;
        bonusTimer = 0f;

        if (bonusFood != null)
            bonusFood.SetActive(false);

        // Hide progress bar and reset fill amount when collected or timed out
        if (bonusProgressBar != null)
        {
            bonusProgressBar.SetActive(false);
        }
        else if (bonusProgressBarFill != null)
        {
            bonusProgressBarFill.gameObject.SetActive(false);
        }

        if (bonusProgressBarFill != null)
        {
            bonusProgressBarFill.fillAmount = 0f;
        }
    }

    private Vector2 GetValidSpawnPosition()
    {
        const int maxAttempts = 100;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 candidate = GetRandomPosition();

            // 1. Verify candidate does not overlap snake head or body segments
            if (snakeController != null && snakeController.IsOccupyingPosition(candidate, cellSize * 0.5f))
            {
                continue;
            }

            // 2. Verify candidate does not overlap configured obstacle layers
            if (obstacleLayers.value != 0)
            {
                Collider2D hit = Physics2D.OverlapCircle(candidate, cellSize * 0.4f, obstacleLayers);
                if (hit != null)
                {
                    continue;
                }
            }

            return candidate;
        }

        // Fallback if the board is mostly filled
        return GetRandomPosition();
    }

    private Vector2 GetRandomPosition()
    {
        if (leftAnchor == null || rightAnchor == null || topAnchor == null || bottomAnchor == null)
        {
            Debug.LogWarning("FoodSpawner: One or more play area anchors are not assigned!");
            return Vector2.zero;
        }

        float minX = Mathf.Min(leftAnchor.position.x, rightAnchor.position.x) + boundaryPadding;
        float maxX = Mathf.Max(leftAnchor.position.x, rightAnchor.position.x) - boundaryPadding;

        float minY = Mathf.Min(bottomAnchor.position.y, topAnchor.position.y) + boundaryPadding;
        float maxY = Mathf.Max(bottomAnchor.position.y, topAnchor.position.y) - boundaryPadding;

        float width = maxX - minX;
        float height = maxY - minY;

        if (width <= 0f || height <= 0f || cellSize <= 0f)
        {
            return new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        }

        int columns = Mathf.Max(1, Mathf.FloorToInt(width / cellSize));
        int rows = Mathf.Max(1, Mathf.FloorToInt(height / cellSize));

        float totalGridWidth = columns * cellSize;
        float totalGridHeight = rows * cellSize;

        // Center grid cells within the play area
        float startX = minX + (width - totalGridWidth) * 0.5f + (cellSize * 0.5f);
        float startY = minY + (height - totalGridHeight) * 0.5f + (cellSize * 0.5f);

        int x = Random.Range(0, columns);
        int y = Random.Range(0, rows);

        return new Vector2(
            startX + x * cellSize,
            startY + y * cellSize
        );
    }
}
