using UnityEngine;

public class CollisionHandler : MonoBehaviour
{
    private SnakeController snakeController;

    private void Awake()
    {
        snakeController = GetComponentInParent<SnakeController>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Food"))
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnFoodAte(false);

            if (snakeController != null)
                snakeController.Grow();
        }
        else if (collision.gameObject.CompareTag("BonusFood"))
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnFoodAte(true);

            if (snakeController != null)
            {
                snakeController.Grow();
                snakeController.IncreaseSpeed();
            }
        }
        else if (collision.gameObject.CompareTag("Node"))
        {
            // Ignore the immediate neck segments right behind the head to prevent false self-collision when turning
            if (snakeController != null && snakeController.IsNeckNode(collision.transform))
                return;

            if (GameManager.Instance != null)
                GameManager.Instance.GameOver();
        }
        else if (collision.gameObject.CompareTag("Border"))
        {
            if (GameManager.Instance != null)
                GameManager.Instance.GameOver();
        }
    }
}
