using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using UnityEngine.XR;
using System;

public class SaveDataTrainingXR1 : MonoBehaviour
{
    [Header("=== EXPERIMENT MODE CONDITION ===")]
    [Tooltip("Select the experiment mode condition (1-6). This will be applied when Play is pressed.\n" +
             "1. Proactive_VR - Behavior 1 (Proactive)\n" +
             "2. Reactive_VR - Behavior 2 (Reactive)\n" +
             "3. Baseline_VR - Behavior 0 (Normal)\n" +
             "4. Proactive_AR - Behavior 1 (Proactive)\n" +
             "5. Reactive_AR - Behavior 2 (Reactive)\n" +
             "6. Baseline_AR - Behavior 0 (Normal)")]
    public ExperimentModeCondition experimentMode = ExperimentModeCondition._3_Baseline_VR;
    //[Tooltip("1 = x, 2 = y")]
    //public int Axis;
    //[Tooltip("1 = positive, 2 = negative")]
    //public int MoveDirec;
    [Tooltip("1 = no shift, 2 = reactive")]
    public int condition;
    [Tooltip("1 = pre/post, 2 = training")]
    public int block;

    [Header("Main References")]
    public XRNode trackedHand = XRNode.RightHand;
    public GameObject pacer;
    public GameObject background;

    [Header("Grabbable Sphere")]
    [Tooltip("Sphere that has IsHoldingSphere + SineWaveCollisionDetector")]
    public GameObject sphere;
    private IsHoldingSphere holdingSphere;
    private SineWaveCollisionDetector collisionDetector;

    [Header("Coins")]
    public GameObject coin1;
    public GameObject coin2;
    public GameObject coin3;
    public GameObject sinusoid;

    [Header("Coin Scripts")]
    [Tooltip("CoinBombScript1 - handles coin placement")]
    public CoinBombScript1 coinBombScript;
    [Tooltip("CoinCollector - handles coin collection")]
    public CoinCollector coinCollectorScript;

    [Header("File Settings")]
    [Tooltip("Leave empty to use default location (My Documents/MetaXR_Logs). Or specify custom path.")]
    public string filepathpre = "";

    private const string DELIMITER = ",";
    private const string EXTENSION = ".csv";
    private string filepath;

    // Runtime state
    private InputDevice handDevice;
    private Vector3 handPos;
    private bool isHolding;
    private int hitt;
    private float pacerx;
    private float distance;
    private int finished;

    // Coin data
    private int CoinGet1, CoinGet2, CoinGet3;
    private int Coin1go, Coin2go, Coin3go;

    // Sine wave start/end positions
    private Vector3 go0Position = Vector3.zero;
    private Vector3 go251Position = Vector3.zero;
    private bool sineWavePositionsCached = false;

    // CSV buffer - write every N frames instead of all at end
    private readonly List<string> frameData = new List<string>();
    private const int WRITE_BUFFER_SIZE = 300;

    // Cached StringBuilder for Update loop optimization
    private StringBuilder lineBuilder;

    private void Awake()
    {
        // Apply the selected experiment mode to the static class
        StaticValsReach7.SetExperimentMode(experimentMode);

        Debug.Log("=== SaveDataTrainingXR: Awake() called ===");
        Debug.Log($"🎮 Experiment Mode Condition: {ExperimentModeConditionHelper.GetDisplayName(experimentMode)} ({(int)experimentMode})");
        Debug.Log($"📋 Mode Description: {ExperimentModeConditionHelper.GetModeDescription(experimentMode)}");
        Debug.Log($"🔧 Behavior Mode: {ExperimentModeConditionHelper.GetBehaviorMode(experimentMode)} (0=Baseline, 1=Proactive, 2=Reactive)");
        Debug.Log($"🌐 Environment: {ExperimentModeConditionHelper.GetEnvironment(experimentMode)}");

        hitt = 0;
        finished = 0;

        // Initialize StringBuilder for optimization
        lineBuilder = new StringBuilder(512);

        // Determine save location
        if (string.IsNullOrEmpty(filepathpre))
        {
            string userProfile = System.Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrEmpty(userProfile))
            {
                filepathpre = Path.Combine(userProfile, "Documents", "MetaXR_Logs");
            }
            else
            {
                string documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
                filepathpre = Path.Combine(documentsPath, "MetaXR_Logs");
            }
            Debug.Log($"Using default documents path: {filepathpre}");
        }
        else
        {
            Debug.Log($"Using custom path from inspector: {filepathpre}");
        }

        // Create directory and verify write access
        try
        {
            Directory.CreateDirectory(filepathpre);

            string testFile = Path.Combine(filepathpre, ".test");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);

            Debug.Log($"✓ Directory ready with write access: {filepathpre}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"✗ Cannot use directory '{filepathpre}': {e.Message}");
            filepathpre = Application.persistentDataPath;
            Debug.LogWarning($"⚠️ Using Unity persistent data path: {filepathpre}");
        }

        // Build filename using curindex for trial number
        // Format: Trial{curindex + 1}.csv (starts at Trial1)
        // Example: Trial1.csv, Trial2.csv, etc.
        int trialNumber = StaticValsReach7.curindex + 1;
        string filename = $"Trial{trialNumber}";

        filepath = Path.Combine(filepathpre, $"{filename}{EXTENSION}"); 

        Debug.Log($"📁 Saving to: {filepath}");

        // Write CSV header with experiment mode columns
        StringBuilder header = new StringBuilder();
        header.Append("Frame,Time,ExperimentMode,BehaviorMode,Environment,Distance,PosX,PosY,PosZ,PacerX,Holding,Hit,Finished,");
        header.Append("HitTime,HitPosX,HitPosY,HitPosZ,");
        header.Append("FinishedTime,FinishedPosX,FinishedPosY,FinishedPosZ,");
        header.Append("Go0_X,Go0_Y,Go0_Z,Go251_X,Go251_Y,Go251_Z,");
        header.Append("Coin1x,Coin1y,Coin1go,Collected1,");
        header.Append("Coin2x,Coin2y,Coin2go,Collected2,");
        header.AppendLine("Coin3x,Coin3y,Coin3go,Collected3");

        try
        {
            File.WriteAllText(filepath, header.ToString());
            Debug.Log($"✅ CSV file created successfully!");
            Debug.Log($"✅ Header written to: {filepath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to create CSV file: {e.Message}");
            Debug.LogError($"❌ Attempted path: {filepath}");
        }
    }

    private void Start()
    {
        Debug.Log("=== SaveDataTrainingXR: Start() called ===");

        handDevice = InputDevices.GetDeviceAtXRNode(trackedHand);
        Debug.Log($"Hand device initialized for {trackedHand}");

        if (sphere != null)
        {
            holdingSphere = sphere.GetComponent<IsHoldingSphere>();
            if (holdingSphere == null)
                Debug.LogWarning("⚠️ Sphere missing IsHoldingSphere component!");
            else
                Debug.Log("✓ IsHoldingSphere found");

            collisionDetector = sphere.GetComponent<SineWaveCollisionDetector>();
            if (collisionDetector == null)
                Debug.LogWarning("⚠️ Sphere missing SineWaveCollisionDetector component!");
            else
                Debug.Log("✓ SineWaveCollisionDetector found");
        }
        else
        {
            Debug.LogError("✗ Sphere not assigned in inspector!");
        }

        // Auto-find CoinCollector if not assigned
        if (coinCollectorScript == null)
        {
            coinCollectorScript = FindObjectOfType<CoinCollector>();
            if (coinCollectorScript != null)
                Debug.Log("✓ CoinCollector auto-discovered");
            else
                Debug.LogWarning("⚠️ CoinCollector not found in scene! Coin collection will not be tracked.");
        }

        // Auto-find CoinBombScript1 if not assigned
        if (coinBombScript == null)
        {
            coinBombScript = FindObjectOfType<CoinBombScript1>();
            if (coinBombScript != null)
                Debug.Log("✓ CoinBombScript1 auto-discovered");
            else
                Debug.LogWarning("⚠️ CoinBombScript1 not found in scene! Coin locations will not be tracked.");
        }

        Debug.Log($"Coin references - coin1:{coin1 != null}, coin2:{coin2 != null}, coin3:{coin3 != null}");
        Debug.Log($"coinBombScript assigned: {coinBombScript != null}");
        Debug.Log($"coinCollectorScript assigned: {coinCollectorScript != null}");

        Invoke(nameof(CacheSineWavePositions), 0.5f);
    }

    private void CacheSineWavePositions()
    {
        GameObject go0 = GameObject.Find("go0");
        GameObject go251 = GameObject.Find("go251");

        if (go0 != null)
        {
            go0Position = go0.transform.position;
            Debug.Log($"✓ Cached go0 position: {go0Position}");
        }
        else
        {
            Debug.LogWarning("⚠️ Could not find go0 GameObject.");
        }

        if (go251 != null)
        {
            go251Position = go251.transform.position;
            Debug.Log($"✓ Cached go251 position: {go251Position}");
        }
        else
        {
            Debug.LogWarning("⚠️ Could not find go251 GameObject.");
        }

        sineWavePositionsCached = true;
    }

    private void OnEnable()
    {
        if (holdingSphere != null)
            holdingSphere.HoldingChanged += OnHoldingChanged;
    }

    private void OnDisable()
    {
        if (holdingSphere != null)
            holdingSphere.HoldingChanged -= OnHoldingChanged;

        WriteBufferedData();

        Debug.Log($"✅ Final data saved to: {filepath}");
    }

    private void Update()
    {
        if (!handDevice.isValid)
        {
            handDevice = InputDevices.GetDeviceAtXRNode(trackedHand);
        }

        if (handDevice.isValid && handDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position))
        {
            handPos = position;
        }

        distance = background != null ? Vector3.Distance(handPos, background.transform.position) : 0f;

        if (pacer != null && handPos.x > 0 && handPos.x < 0.55f && hitt == 1)
            pacerx = pacer.transform.position.x;
        else if (handPos.x < 0)
            pacerx = 0f;
        else if (handPos.x > 0.55f)
            pacerx = 0.55f;
        else
            pacerx = pacer != null ? pacer.transform.position.x : 0f;

        float coin1x = 0f, coin1y = 0f;
        float coin2x = 0f, coin2y = 0f;
        float coin3x = 0f, coin3y = 0f;

        if (coin1 != null && coin1.activeInHierarchy)
        {
            coin1x = coin1.transform.position.x;
            coin1y = coin1.transform.position.y;
        }

        if (coin2 != null && coin2.activeInHierarchy)
        {
            coin2x = coin2.transform.position.x;
            coin2y = coin2.transform.position.y;
        }

        if (coin3 != null && coin3.activeInHierarchy)
        {
            coin3x = coin3.transform.position.x;
            coin3y = coin3.transform.position.y;
        }

        if (coinBombScript != null && coinBombScript.IsInitialized())
        {
            Coin1go = coinBombScript.GetCoin1Location();
            Coin2go = coinBombScript.GetCoin2Location();
            Coin3go = coinBombScript.GetCoin3Location();
        }
        else
        {
            Coin1go = Coin2go = Coin3go = 0;
        }

        if (coinCollectorScript != null)
        {
            CoinGet1 = coinCollectorScript.GetCoin1Status();
            CoinGet2 = coinCollectorScript.GetCoin2Status();
            CoinGet3 = coinCollectorScript.GetCoin3Status();
        }
        else
        {
            CoinGet1 = CoinGet2 = CoinGet3 = 0;
        }

        if (collisionDetector != null)
        {
            hitt = collisionDetector.Hit;
            finished = collisionDetector.Finished;
        }
        else
        {
            hitt = 0;
            finished = 0;
        }

        float hitTime = 0f;
        Vector3 hitPos = Vector3.zero;
        float finishedTime = 0f;
        Vector3 finishedPos = Vector3.zero;

        if (collisionDetector != null)
        {
            hitTime = collisionDetector.hitTime;
            hitPos = collisionDetector.hitPosition;
            finishedTime = collisionDetector.finishedTime;
            finishedPos = collisionDetector.finishedPosition;
        }

        // Get mode info for CSV
        int behaviorMode = ExperimentModeConditionHelper.GetBehaviorMode(experimentMode);
        string environment = ExperimentModeConditionHelper.GetEnvironment(experimentMode);

        // Get sphere position (the [BuildingBlock] Cube that the user moves)
        Vector3 spherePos = Vector3.zero;
        if (sphere != null)
        {
            spherePos = sphere.transform.position;
        }

        // Build CSV line using reusable StringBuilder
        lineBuilder.Clear();
        lineBuilder.Append($"{Time.frameCount}{DELIMITER}{Time.time:F4}{DELIMITER}");
        lineBuilder.Append($"{(int)experimentMode}{DELIMITER}{behaviorMode}{DELIMITER}{environment}{DELIMITER}");
        lineBuilder.Append($"{distance:F4}{DELIMITER}");
        lineBuilder.Append($"{spherePos.x:F4}{DELIMITER}{spherePos.y:F4}{DELIMITER}{spherePos.z:F4}{DELIMITER}");
        lineBuilder.Append($"{pacerx:F4}{DELIMITER}{(isHolding ? 1 : 0)}{DELIMITER}{hitt}{DELIMITER}{finished}{DELIMITER}");
        lineBuilder.Append($"{hitTime:F4}{DELIMITER}{hitPos.x:F4}{DELIMITER}{hitPos.y:F4}{DELIMITER}{hitPos.z:F4}{DELIMITER}");
        lineBuilder.Append($"{finishedTime:F4}{DELIMITER}{finishedPos.x:F4}{DELIMITER}{finishedPos.y:F4}{DELIMITER}{finishedPos.z:F4}{DELIMITER}");
        lineBuilder.Append($"{go0Position.x:F4}{DELIMITER}{go0Position.y:F4}{DELIMITER}{go0Position.z:F4}{DELIMITER}");
        lineBuilder.Append($"{go251Position.x:F4}{DELIMITER}{go251Position.y:F4}{DELIMITER}{go251Position.z:F4}{DELIMITER}");
        lineBuilder.Append($"{coin1x:F4}{DELIMITER}{coin1y:F4}{DELIMITER}{Coin1go}{DELIMITER}{CoinGet1}{DELIMITER}");
        lineBuilder.Append($"{coin2x:F4}{DELIMITER}{coin2y:F4}{DELIMITER}{Coin2go}{DELIMITER}{CoinGet2}{DELIMITER}");
        lineBuilder.Append($"{coin3x:F4}{DELIMITER}{coin3y:F4}{DELIMITER}{Coin3go}{DELIMITER}{CoinGet3}");

        frameData.Add(lineBuilder.ToString());

        if (frameData.Count >= WRITE_BUFFER_SIZE)
        {
            WriteBufferedData();
        }

        if (Time.frameCount % 300 == 0)
        {
            Debug.Log($"Frame {Time.frameCount}: Mode={experimentMode}, Hit={hitt}, Finished={finished}, " +
                     $"Coins collected: [{CoinGet1},{CoinGet2},{CoinGet3}], " +
                     $"Buffered lines={frameData.Count}");
        }
    }

    private void WriteBufferedData()
    {
        if (frameData.Count == 0) return;

        try
        {
            File.AppendAllLines(filepath, frameData);
            Debug.Log($"✓ Wrote {frameData.Count} lines to CSV");
            frameData.Clear();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"✗ Failed to write CSV data: {e.Message}");
        }
    }

    private void OnHoldingChanged(bool holding)
    {
        isHolding = holding;
        Debug.Log($"Holding changed: {holding}");
    }
}