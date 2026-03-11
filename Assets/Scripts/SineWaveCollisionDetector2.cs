using UnityEngine;

/// <summary>
/// Detects collisions with the start and end sine wave points.
/// Supports reversed direction based on StaticValsReach8.currentMoveDir.
/// 
/// MoveDir=1 (Positive): go0 = start (Hit), go251 = end (Finished)
/// MoveDir=2 (Negative): go251 = start (Hit), go0 = end (Finished)
/// 
/// Attach this to the grabbable sphere.
/// </summary>
public class SineWaveCollisionDetector2 : MonoBehaviour
{
    [Header("Collision State")]
    [Tooltip("1 if sphere has collided with start point, else 0")]
    public int Hit = 0;

    [Tooltip("1 if sphere has collided with end point, else 0")]
    public int Finished = 0;

    [Header("Position & Time Data")]
    public Vector3 hitPosition = Vector3.zero;
    public float hitTime = 0f;
    public Vector3 finishedPosition = Vector3.zero;
    public float finishedTime = 0f;

    [Header("Debug")]
    public bool logCollisions = true;

    // Determined by MoveDir
    private string startPointName;
    private string endPointName;
    private bool hasHitStart = false;
    private bool hasHitEnd = false;

    private void Start()
    {
        int moveDir = StaticValsReach8.currentMoveDir;

        if (moveDir == 1) // Positive: go0 → go251
        {
            startPointName = "go0";
            endPointName = "go251";
        }
        else // Negative: go251 → go0
        {
            startPointName = "go251";
            endPointName = "go0";
        }

        if (logCollisions)
        {
            Debug.Log($"SineWaveCollisionDetector2: MoveDir={moveDir}, Start={startPointName}, End={endPointName}");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check for collision with START point
        if (!hasHitStart && other.gameObject.name == startPointName)
        {
            Hit = 1;
            hitPosition = transform.position;
            hitTime = Time.time;
            hasHitStart = true;

            if (logCollisions)
            {
                Debug.Log($"✓ HIT (start) at {startPointName}! Time={hitTime:F3}s, Position={hitPosition}");
            }
        }

        // Check for collision with END point
        if (!hasHitEnd && other.gameObject.name == endPointName)
        {
            Finished = 1;
            finishedPosition = transform.position;
            finishedTime = Time.time;
            hasHitEnd = true;

            if (logCollisions)
            {
                Debug.Log($"✓ FINISHED (end) at {endPointName}! Time={finishedTime:F3}s, Position={finishedPosition}");

                if (hasHitStart)
                {
                    float duration = finishedTime - hitTime;
                    Debug.Log($"✓ Total duration from {startPointName} to {endPointName}: {duration:F3}s");
                }
            }
        }
    }

    /// <summary>
    /// Reset the collision state (useful for new trials).
    /// </summary>
    public void ResetState()
    {
        Hit = 0;
        Finished = 0;
        hasHitStart = false;
        hasHitEnd = false;
        hitPosition = Vector3.zero;
        hitTime = 0f;
        finishedPosition = Vector3.zero;
        finishedTime = 0f;

        if (logCollisions)
        {
            Debug.Log("SineWaveCollisionDetector2: State reset");
        }
    }
}
