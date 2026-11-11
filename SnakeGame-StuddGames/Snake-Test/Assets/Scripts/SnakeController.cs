
using System.Collections.Generic;
using UnityEngine;

public class SnakeController : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject headPrefab;
    [SerializeField] private GameObject nodePrefab;

    [Header("Snake Settings")]
    [SerializeField] private int initialNodeCount = 5;
    [SerializeField] private float nodeSpacing = 0.5f;

    [Header("Pooling Settings")]
    [SerializeField] private int initialPoolSize = 50;

    [Header("Speed Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float speedIncreasePerBonus = 0.5f;
    [SerializeField] private float maxSpeed = 7f;

    [Header("Turn Settings")]
    [SerializeField] private int stepsPerTurn = 1;
    [SerializeField] private int maxQueuedTurns = 2;

    private Transform head;

    private Vector2Int direction = Vector2Int.right;

    private List<Transform> nodes = new();
    private Queue<GameObject> nodePool = new();

    private SnakeControls controls;

    private Queue<Vector2Int> turnQueue = new();

    private float totalDistance;
    private float distanceSinceLastStep;

    private int stepsMovedSinceTurn;

    // Logical position of the head.
    private Vector2 logicalHeadPosition;

    // Smooth visual movement positions.
    private Vector2 previousHeadPosition;

    // Records the path travelled by the head.
    private struct PathPoint
    {
        public Vector2 position;
        public float distance;

        public PathPoint(Vector2 position, float distance)
        {
            this.position = position;
            this.distance = distance;
        }
    }

    private List<PathPoint> path = new();

    private void Awake()
    {
        controls = new SnakeControls();
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void Start()
    {
        // Spawn head.
        head = Instantiate(
            headPrefab,
            transform.position,
            Quaternion.identity,
            transform
        ).transform;

        previousHeadPosition = head.position;
        logicalHeadPosition = (Vector2)head.position + (Vector2)direction * nodeSpacing;
        totalDistance = nodeSpacing;
        stepsMovedSinceTurn = stepsPerTurn;

        InitializePath();

        InitializeNodePool();

        // Spawn initial body nodes from pool.
        for (int i = 0; i < initialNodeCount; i++)
        {
            GameObject node = GetNodeFromPool(transform.position);
            nodes.Add(node.transform);
        }

        UpdateNodePositions();
    }

    private void Update()
    {
        HandleInput();

        MoveHead();

        UpdateVisualHead();

        UpdateNodePositions();

        CleanOldPath();
    }

    // INPUT

    private void HandleInput()
    {
        Vector2 input = controls.Gameplay.Move.ReadValue<Vector2>();

        if (input.sqrMagnitude < 0.5f)
            return;

        Vector2Int newDirection;

        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
        {
            newDirection = input.x > 0
                ? Vector2Int.right
                : Vector2Int.left;
        }
        else
        {
            newDirection = input.y > 0
                ? Vector2Int.up
                : Vector2Int.down;
        }

        Vector2Int lastDirection = direction;

        foreach (Vector2Int queuedDirection in turnQueue)
        {
            lastDirection = queuedDirection;
        }

        // Ignore duplicate and opposite directions.
        if (newDirection == lastDirection ||
            newDirection == -lastDirection)
            return;

        // Queue the direction.
        if (turnQueue.Count < maxQueuedTurns)
        {
            turnQueue.Enqueue(newDirection);
        }
    }

    // HEAD MOVEMENT

    private void MoveHead()
    {
        float movement =
            moveSpeed * Time.deltaTime;

        distanceSinceLastStep += movement;

        while (distanceSinceLastStep >= nodeSpacing)
        {
            distanceSinceLastStep -= nodeSpacing;

            // Count completed movement steps.
            stepsMovedSinceTurn++;

            // Execute queued direction after N steps.
            if (stepsMovedSinceTurn >= stepsPerTurn &&
                turnQueue.Count > 0)
            {
                direction = turnQueue.Dequeue();

                stepsMovedSinceTurn = 0;

                RotateHead();
            }

            // Move one complete logical step.
            previousHeadPosition = logicalHeadPosition;

            logicalHeadPosition +=
                (Vector2)direction * nodeSpacing;

            totalDistance += nodeSpacing;

            // Record the head path.
            path.Add(new PathPoint(
                logicalHeadPosition,
                totalDistance
            ));

            // TODO: Check grid-based collision here.
        }
    }

    // VISUAL MOVEMENT

    private void UpdateVisualHead()
    {
        float interpolation = distanceSinceLastStep / nodeSpacing;

        head.position = Vector2.Lerp(
            previousHeadPosition,
            logicalHeadPosition,
            interpolation
        );
    }

    private void RotateHead()
    {
        float angle = Mathf.Atan2(
            direction.y,
            direction.x
        ) * Mathf.Rad2Deg;

        head.rotation = Quaternion.Euler(0, 0, angle);
    }

    // PATH INITIALIZATION

    private void InitializePath()
    {
        float initialDistance =
            (initialNodeCount + 2) * nodeSpacing;

        int steps = Mathf.CeilToInt(initialDistance / 0.05f);

        for (int i = 0; i <= steps; i++)
        {
            float distance = Mathf.Lerp(
                -initialDistance,
                0f,
                (float)i / steps
            );

            Vector2 position =
                previousHeadPosition +
                (Vector2)direction * distance;

            path.Add(new PathPoint(position, distance));
        }

        path.Add(new PathPoint(logicalHeadPosition, totalDistance));
    }

    // BODY FOLLOWING

    private void UpdateNodePositions()
    {
        float currentHeadDistance = (totalDistance - nodeSpacing) + distanceSinceLastStep;

        for (int i = 0; i < nodes.Count; i++)
        {
            float targetDistance =
                currentHeadDistance - ((i + 1) * nodeSpacing);

            Vector2 targetPosition =
                GetPositionAtDistance(targetDistance);

            nodes[i].position = targetPosition;
        }
    }

    private Vector2 GetPositionAtDistance(float targetDistance)
    {
        int low = 0;
        int high = path.Count - 1;

        while (low < high)
        {
            int mid = (low + high) / 2;

            if (path[mid].distance < targetDistance)
                low = mid + 1;
            else
                high = mid;
        }

        int nextIndex = low;

        int previousIndex = Mathf.Max(0, nextIndex - 1);

        PathPoint previous = path[previousIndex];
        PathPoint next = path[nextIndex];

        float range = next.distance - previous.distance;

        float t = range > 0f
            ? (targetDistance - previous.distance) / range
            : 0f;

        return Vector2.Lerp(
            previous.position,
            next.position,
            t
        );
    }

    // PATH CLEANUP

    private void CleanOldPath()
    {
        float minimumDistance =
            totalDistance - ((nodes.Count + 2) * nodeSpacing);

        while (path.Count > 2 &&
               path[1].distance < minimumDistance)
        {
            path.RemoveAt(0);
        }
    }

    // GROWTH & OBJECT POOLING

    private void InitializeNodePool()
    {
        int poolCount = Mathf.Max(initialPoolSize, initialNodeCount + 10);
        for (int i = 0; i < poolCount; i++)
        {
            CreatePooledNode();
        }
    }

    private GameObject CreatePooledNode()
    {
        GameObject node = Instantiate(nodePrefab, transform);
        node.SetActive(false);
        nodePool.Enqueue(node);
        return node;
    }

    private GameObject GetNodeFromPool(Vector2 spawnPosition)
    {
        GameObject node = nodePool.Count > 0 ? nodePool.Dequeue() : Instantiate(nodePrefab, transform);
        node.transform.position = spawnPosition;
        node.transform.rotation = Quaternion.identity;
        node.SetActive(true);
        return node;
    }

    /// Returns an active body node back to the pool.
    public void ReturnNodeToPool(GameObject node)
    {
        if (node == null) return;
        node.SetActive(false);
        nodePool.Enqueue(node);
    }

    public void Grow()
    {
        Vector2 spawnPosition = nodes.Count > 0 ? (Vector2)nodes[^1].position : (Vector2)transform.position;
        GameObject newNode = GetNodeFromPool(spawnPosition);

        nodes.Add(newNode.transform);

        UpdateNodePositions();
    }

    /// Checks if a node is among the immediate segments behind the head (neck)
    /// to prevent false-positive self-collision when making tight turns.
    public bool IsNeckNode(Transform nodeTransform)
    {
        if (nodes.Count > 0 && nodes[0] == nodeTransform) return true;
        if (nodes.Count > 1 && nodes[1] == nodeTransform) return true;
        return false;
    }

    // SPEED

    /// Increases movement speed by a specific amount, capped at maxSpeed.
    public void IncreaseSpeed(float amount)
    {
        moveSpeed = Mathf.Min(moveSpeed + Mathf.Max(0f, amount), maxSpeed);
    }

    /// Increases movement speed by the configured bonus increment (speedIncreasePerBonus), capped at maxSpeed.
    public void IncreaseSpeed()
    {
        IncreaseSpeed(speedIncreasePerBonus);
    }


    // OCCUPANCY CHECK


    public bool IsOccupyingPosition(Vector2 position, float tolerance = 0.5f)
    {
        if (head != null && Vector2.Distance((Vector2)head.position, position) < tolerance)
            return true;

        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] != null && Vector2.Distance((Vector2)nodes[i].position, position) < tolerance)
                return true;
        }

        return false;
    }
}
