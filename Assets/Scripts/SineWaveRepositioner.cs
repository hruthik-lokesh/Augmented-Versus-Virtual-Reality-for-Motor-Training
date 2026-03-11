using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

/// <summary>
/// SineWaveRepositioner - Recenters the XR Rig when J is pressed.
/// 
/// PURPOSE:
/// When using Meta Quest Link, the tracking origin may be incorrect.
/// Press J to recenter - your current forward direction becomes the new world forward.
/// All objects (sine wave, cube, marker) stay at their positions but now appear
/// correctly in front of you.
/// 
/// Based on the BeTheCamera pattern for VR recentering.
/// </summary>
public class SineWaveRepositioner : MonoBehaviour
{
    [Header("XR Rig Reference")]
    [Tooltip("The [BuildingBlock] Camera Rig GameObject")]
    public GameObject xrRig;
    
    [Tooltip("The Main Camera (XR headset camera)")]
    public Transform mainCamera;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // XR Head tracking device
    private UnityEngine.XR.InputDevice headDevice;

    void Start()
    {
        // Auto-find XR Rig if not assigned
        if (xrRig == null)
        {
            // Try Meta Building Block name first
            xrRig = GameObject.Find("[BuildingBlock] Camera Rig");
            if (xrRig == null) xrRig = GameObject.Find("BuildingBlock Camera Rig");
            if (xrRig == null) xrRig = GameObject.Find("XR Origin (XR Rig)");
            if (xrRig == null) xrRig = GameObject.Find("XR Origin");
            
            // Search by partial name if not found
            if (xrRig == null)
            {
                var allObjects = FindObjectsOfType<GameObject>();
                foreach (var obj in allObjects)
                {
                    if (obj.name.Contains("Camera Rig") || obj.name.Contains("CameraRig"))
                    {
                        xrRig = obj;
                        break;
                    }
                }
            }
            
            if (xrRig != null)
            {
                LogDebug($"Auto-found XR Rig: {xrRig.name}");
            }
            else
            {
                Debug.LogError("SineWaveRepositioner: Could not find XR Rig! Please assign it in the inspector.");
            }
        }

        // Auto-find Main Camera if not assigned
        if (mainCamera == null)
        {
            mainCamera = Camera.main?.transform;
            if (mainCamera != null)
            {
                LogDebug($"Auto-found Main Camera: {mainCamera.name}");
            }
            else
            {
                Debug.LogError("SineWaveRepositioner: Could not find Main Camera!");
            }
        }

        // Initialize the head tracking device
        headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        LogDebug($"Head device valid: {headDevice.isValid}");

        LogDebug("Initialized. Press 'J' to recenter XR Rig.");
    }

    void Update()
    {
        // Re-acquire head device if lost
        if (!headDevice.isValid)
        {
            headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        }

        // Check for 'J' key press (both new and legacy input)
        bool jPressed = false;
        
        if (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame)
        {
            jPressed = true;
        }
        
        if (UnityEngine.Input.GetKeyUp(KeyCode.J))
        {
            jPressed = true;
        }

        if (jPressed)
        {
            RecenterXRRig();
        }
    }

    /// <summary>
    /// Recenters the XR Rig so your current forward becomes world forward.
    /// Uses the XR InputDevice API to get actual headset rotation.
    /// </summary>
    public void RecenterXRRig()
    {
        LogDebug("=== RecenterXRRig() CALLED ===");
        
        if (xrRig == null)
        {
            Debug.LogError("SineWaveRepositioner: xrRig is NULL!");
            return;
        }

        // Try to get rotation from XR head device first
        Quaternion headRotation = Quaternion.identity;
        bool gotHeadRotation = false;

        // Method 1: Use XR InputDevice API (most reliable for actual headset)
        if (headDevice.isValid)
        {
            if (headDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out Quaternion deviceRotation))
            {
                headRotation = deviceRotation;
                gotHeadRotation = true;
                LogDebug($"Got rotation from XR InputDevice: {headRotation.eulerAngles}");
            }
        }

        // Method 2: Fall back to camera transform
        if (!gotHeadRotation && mainCamera != null)
        {
            headRotation = mainCamera.rotation;
            gotHeadRotation = true;
            LogDebug($"Got rotation from Camera transform: {headRotation.eulerAngles}");
        }

        if (!gotHeadRotation)
        {
            Debug.LogError("SineWaveRepositioner: Could not get head rotation from any source!");
            return;
        }

        LogDebug($"Before recenter:");
        LogDebug($"  Head rotation (euler): {headRotation.eulerAngles}");
        LogDebug($"  XR Rig rotation (euler): {xrRig.transform.eulerAngles}");

        // Apply the inverse rotation to the rig (BeTheCamera pattern)
        Quaternion inverseRotation = Quaternion.Inverse(headRotation);
        xrRig.transform.rotation = inverseRotation * xrRig.transform.rotation;

        LogDebug($"After recenter:");
        LogDebug($"  XR Rig rotation (euler): {xrRig.transform.eulerAngles}");
        LogDebug("=== RecenterXRRig() COMPLETE ===");
    }

    private void LogDebug(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[SineWaveRepositioner] {message}");
        }
    }
}
