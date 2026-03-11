using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Creates a sine wave from 252 small spheres, supporting both horizontal (Axis=X) and vertical (Axis=Y) orientations.
/// The starting sphere (based on MoveDir) is highlighted green and made slightly larger.
/// Reads Axis and MoveDir from StaticValsReach8.
/// </summary>
public class SinusoidScript8 : MonoBehaviour
{
    // Public variables - set in Inspector
    public Vector3[] Sinusoid;
    public float[] zTarget;
    public float[] yTarget;
    public Mesh Sphere;
    public Material greyLine;
    public Material transparent;
    public GameObject runwayend;
    public GameObject parms;

    [Header("Start Sphere Highlight")]
    [Tooltip("Material for the starting sphere (green). Auto-created if not assigned.")]
    public Material greenMaterial;

    [Header("Camera Reference")]
    [Tooltip("Reference to the Main Camera Transform (XR Origin's Main Camera)")]
    public Transform mainCamera;

    // Hidden or internal variables
    private int trial;
    [HideInInspector]
    public int breakpoint;
    public int[] rval;
    [HideInInspector]
    public int dir;
    [HideInInspector]
    public int cnd;
    [HideInInspector]
    public int block;

    void Start()
    {
        Sinusoid = new Vector3[158];
        zTarget = new float[158];
        yTarget = new float[158];

        // Auto-find the main camera if not set
        if (mainCamera == null)
        {
            mainCamera = Camera.main.transform;
            if (mainCamera != null)
                Debug.Log("SinusoidScript8: Auto-found Main Camera");
            else
            {
                Debug.LogError("SinusoidScript8: Could not find Main Camera! Please assign it in the inspector.");
                return;
            }
        }

        // Auto-create green material if not assigned
        if (greenMaterial == null)
        {
            greenMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (greenMaterial != null)
            {
                greenMaterial.SetColor("_BaseColor", new Color(0.2f, 0.9f, 0.3f, 1f)); // Bright green
                greenMaterial.EnableKeyword("_EMISSION");
                greenMaterial.SetColor("_EmissionColor", new Color(0.2f, 0.9f, 0.3f) * 1.5f);
                Debug.Log("SinusoidScript8: Auto-created green material for starting sphere");
            }
        }

        MakeObjects();
    }

    void MakeObjects()
    {
        int axis = StaticValsReach8.currentAxis;
        int moveDir = StaticValsReach8.currentMoveDir;

        bool isHorizontal = (axis == 1);
        bool isPositiveDir = (moveDir == 1);

        // Determine which sphere is the starting point
        int startIndex = isPositiveDir ? 0 : 251;

        Debug.Log($"SinusoidScript8: Creating {(isHorizontal ? "HORIZONTAL" : "VERTICAL")} sine wave");
        Debug.Log($"SinusoidScript8: Direction={( isPositiveDir ? "Positive (go0→go251)" : "Negative (go251→go0)")}");
        Debug.Log($"SinusoidScript8: Starting sphere = go{startIndex}");

        // --- Placement Parameters ---
        const float DISTANCE_IN_FRONT = 0.4f;
        float verticalOffset = isHorizontal ? 1.0f : 0.75f;
        float horizontalOffset = isHorizontal ? -0.25f : 0f;

        // Calculate World Spawn Point
        Vector3 startPosition = mainCamera.position +
                                mainCamera.forward * DISTANCE_IN_FRONT +
                                mainCamera.up * verticalOffset +
                                mainCamera.right * horizontalOffset;

        Quaternion cameraRotation = mainCamera.rotation;

        // Create parent object for organization
        GameObject sinusoidParent = new GameObject("Sinusoid_Fixed_World_Space");

        int i = 0;
        while (i < 252)
        {
            Vector3 pos;

            if (isHorizontal)
            {
                // HORIZONTAL (Axis=X): wave runs along X, oscillates in Y
                // Same as original SinusoidScript7
                pos = new Vector3(i * 0.001992032f, Mathf.Sin(i * 0.05f) * 0.1f, 0);
            }
            else
            {
                // VERTICAL (Axis=Y): wave runs along Y, oscillates in X
                pos = new Vector3(Mathf.Sin(i * 0.05f) * 0.1f, i * 0.001992032f, 0);
            }

            // Transform to World Space
            Vector3 finalWorldPosition = startPosition + (cameraRotation * pos);

            // Create object
            GameObject go1 = new GameObject();
            go1.name = "go" + i;

            // Add shape, material, and collider
            go1.AddComponent<MeshFilter>().mesh = Sphere;
            go1.AddComponent<MeshRenderer>().material = greyLine;

            // Add sphere collider as trigger for collision detection
            SphereCollider sc = go1.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.015f;

            go1.layer = 5;
            go1.transform.position = finalWorldPosition;
            go1.transform.localScale = new Vector3(.005f, .005f, .005f);
            go1.transform.SetParent(sinusoidParent.transform);

            // Highlight the STARTING sphere green and slightly larger
            if (i == startIndex)
            {
                if (greenMaterial != null)
                {
                    go1.GetComponent<MeshRenderer>().material = greenMaterial;
                }
                go1.transform.localScale = new Vector3(0.008f, 0.008f, 0.008f); // Slightly bigger
                Debug.Log($"✓ Starting sphere go{i} highlighted GREEN at {finalWorldPosition}");
            }

            // Debug logs for important points
            if (i == 0)
            {
                Debug.Log($"✓ Created go0 at {finalWorldPosition} with trigger collider");
            }
            else if (i == 251)
            {
                Debug.Log($"✓ Created go251 at {finalWorldPosition} with trigger collider");
            }

            i++;
        }

        Debug.Log($"✓ SinusoidScript8: Created 252 {(isHorizontal ? "horizontal" : "vertical")} sine wave points");
    }
}
