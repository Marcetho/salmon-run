using UnityEngine;

public class LevelTransition : MonoBehaviour
{
    public enum PointType { Start, End }

    [Header("Transition Settings")]
    [SerializeField] private int levelNumber = 1;
    [SerializeField] private PointType pointType = PointType.Start;
    [SerializeField] private float detectionRadius = 5f;

    [Header("Visual Settings")]
    [SerializeField] private Color gizmoColor = Color.green;
    [SerializeField] private bool showGizmo = true;

    private GameController gameController;

    private void Start()
    {
        gameController = FindAnyObjectByType<GameController>();
        if (gameController == null)
        {
            Debug.LogError("LevelTransition: GameController not found in scene!");
        }
    }

    private void Update()
    {
        if (pointType == PointType.End && GameController.currentPlayer != null)
        {
            float distance = Vector3.Distance(transform.position, GameController.currentPlayer.transform.position);
            if (distance < detectionRadius)
            {
                // Player has reached the end point
                if (gameController != null)
                {
                    // Use CheckForVictory instead of directly transitioning
                    gameController.CheckForVictory(this);

                    // Disable this end point temporarily to prevent multiple transitions
                    this.enabled = false;
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmo) return;

        // Draw different colors for start and end points
        Gizmos.color = pointType == PointType.Start ? Color.green : Color.red;

        // Draw sphere to represent detection radius
        if (pointType == PointType.End)
        {
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }

        // Draw icon for the point
        Gizmos.DrawSphere(transform.position, 1f);

        // Label the point in scene view
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
            $"Level {levelNumber} {pointType.ToString()} Point");
#endif
    }

    // Public getter for level information
    public int LevelNumber => levelNumber;
    public PointType GetPointType => pointType;
}
