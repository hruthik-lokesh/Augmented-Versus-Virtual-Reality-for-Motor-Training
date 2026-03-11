using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using UnityEngine.XR;
using System;

/// <summary>
/// Per-frame data logger for the 2-mode experiment system.
/// Logs to CSV with columns: Frame, Time, Condition, Axis, MoveDir, etc.
/// </summary>
public class SaveDataTrainingXR2 : MonoBehaviour
{
    [Header("=== EXPERIMENT CONDITION ===")]
    [Tooltip("Select the experiment condition:\n1. Reactive - Coins revealed on approach\n2. Baseline - No coins, sine wave only")]
    public ExperimentCondition experimentCondition = ExperimentCondition.Baseline;

    [Header("Trial Parameters (Read-Only at Runtime)")]
    [Tooltip("1 = X (horizontal), 2 = Y (vertical)")]
    [SerializeField] private int currentAxis;
    [Tooltip("1 = Positive (go0→go251), 2 = Negative (go251→go0)")]
    [SerializeField] private int currentMoveDir;
    [SerializeField] private int currentTrialNumber;
    [SerializeField] private string currentTrialInfo;

    [Header("Main References")]
    public XRNode trackedHand = XRNode.RightHand;
    public GameObject pacer;
    public GameObject background;

    [Header("Grabbable Sphere")]
    [Tooltip("Sphere that has IsHoldingSphere + SineWaveCollisionDetector2")]
    public GameObject sphere;
    private IsHoldingSphere holdingSphere;
    private SineWaveCollisionDetector2 collisionDetector;

    [Header("Coins")]
    public GameObject coin1;
    public GameObject coin2;
    public GameObject coin3;
    public GameObject sinusoid;

    [Header("Coin Scripts")]
    [Tooltip("CoinBombScript2 - handles coin placement")]
    public CoinBombScript2 coinBombScript;
    [Tooltip("CoinCollector2 - handles coin collection")]
    public CoinCollector2 coinCollectorScript;

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
    private Vector3 startPointPosition = Vector3.zero;
    private Vector3 endPointPosition = Vector3.zero;
    private bool sineWavePositionsCached = false;

    // CSV buffer
    private readonly List<string> frameData = new List<string>();
    private const int WRITE_BUFFER_SIZE = 300;

    // Cached StringBuilder
    private StringBuilder lineBuilder;

    private void Awake()
    {
        // Apply the selected condition to the static class
        StaticValsReach8.SetCondition(experimentCondition);

        // Initialize session if this is the first trial
        if (StaticValsReach8.curindex == 0)
        {
            StaticValsReach8.InitializeSession();
        }

        // Read current trial parameters
        TrialConfig trial = StaticValsReach8.GetCurrentTrial();
        currentAxis = trial.Axis;
        currentMoveDir = trial.MoveDir;
        currentTrialNumber = StaticValsReach8.curindex + 1;
        currentTrialInfo = $"Trial {currentTrialNumber}/12 - {trial}";

        Debug.Log("=== SaveDataTrainingXR2: Awake() called ===");
        Debug.Log($"🎮 Condition: {ExperimentConditionHelper.GetDisplayName(experimentCondition)}");
        Debug.Log($"📋 {currentTrialInfo}");
        Debug.Log($"🔧 Axis={trial.AxisName}, MoveDir={trial.DirectionName}, Start={trial.StartPointName}");

        hitt = 0;
        finished = 0;
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

        // Build filename
        int trialNumber = StaticValsReach8.curindex + 1;
        string filename = $"Trial{trialNumber}";
        filepath = Path.Combine(filepathpre, $"{filename}{EXTENSION}");

        Debug.Log($"📁 Saving to: {filepath}");

        // Write CSV header
        StringBuilder header = new StringBuilder();
        header.Append("Frame,Time,Condition,Axis,MoveDir,Distance,PosX,PosY,PosZ,PacerX,Holding,Hit,Finished,");
        header.Append("HitTime,HitPosX,HitPosY,HitPosZ,");
        header.Append("FinishedTime,FinishedPosX,FinishedPosY,FinishedPosZ,");
        header.Append("StartPoint_X,StartPoint_Y,StartPoint_Z,EndPoint_X,EndPoint_Y,EndPoint_Z,");
        header.Append("Coin1x,Coin1y,Coin1go,Collected1,");
        header.Append("Coin2x,Coin2y,Coin2go,Collected2,");
        header.AppendLine("Coin3x,Coin3y,Coin3go,Collected3");

        try
        {
            File.WriteAllText(filepath, header.ToString());
            Debug.Log($"✅ CSV file created successfully!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to create CSV file: {e.Message}");
        }
    }

    private void Start()
    {
        Debug.Log("=== SaveDataTrainingXR2: Start() called ===");

        handDevice = InputDevices.GetDeviceAtXRNode(trackedHand);

        if (sphere != null)
        {
            holdingSphere = sphere.GetComponent<IsHoldingSphere>();
            if (holdingSphere == null)
                Debug.LogWarning("⚠️ Sphere missing IsHoldingSphere component!");
            else
                Debug.Log("✓ IsHoldingSphere found");

            collisionDetector = sphere.GetComponent<SineWaveCollisionDetector2>();
            if (collisionDetector == null)
                Debug.LogWarning("⚠️ Sphere missing SineWaveCollisionDetector2 component!");
            else
                Debug.Log("✓ SineWaveCollisionDetector2 found");
        }
        else
        {
            Debug.LogError("✗ Sphere not assigned in inspector!");
        }

        // Auto-find CoinCollector2 if not assigned
        if (coinCollectorScript == null)
        {
            coinCollectorScript = FindObjectOfType<CoinCollector2>();
            if (coinCollectorScript != null)
                Debug.Log("✓ CoinCollector2 auto-discovered");
            else
                Debug.LogWarning("⚠️ CoinCollector2 not found in scene!");
        }

        // Auto-find CoinBombScript2 if not assigned
        if (coinBombScript == null)
        {
            coinBombScript = FindObjectOfType<CoinBombScript2>();
            if (coinBombScript != null)
                Debug.Log("✓ CoinBombScript2 auto-discovered");
            else
                Debug.LogWarning("⚠️ CoinBombScript2 not found in scene!");
        }

        Invoke(nameof(CacheSineWavePositions), 0.5f);
    }

    private void CacheSineWavePositions()
    {
        TrialConfig trial = StaticValsReach8.GetCurrentTrial();
        string startName = trial.StartPointName;
        string endName = trial.EndPointName;

        GameObject startObj = GameObject.Find(startName);
        GameObject endObj = GameObject.Find(endName);

        if (startObj != null)
        {
            startPointPosition = startObj.transform.position;
            Debug.Log($"✓ Cached start point ({startName}) position: {startPointPosition}");
        }
        else
        {
            Debug.LogWarning($"⚠️ Could not find {startName} GameObject.");
        }

        if (endObj != null)
        {
            endPointPosition = endObj.transform.position;
            Debug.Log($"✓ Cached end point ({endName}) position: {endPointPosition}");
        }
        else
        {
            Debug.LogWarning($"⚠️ Could not find {endName} GameObject.");
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

        // Get sphere position
        Vector3 spherePos = Vector3.zero;
        if (sphere != null)
        {
            spherePos = sphere.transform.position;
        }

        // Build CSV line
        lineBuilder.Clear();
        lineBuilder.Append($"{Time.frameCount}{DELIMITER}{Time.time:F4}{DELIMITER}");
        lineBuilder.Append($"{(int)experimentCondition}{DELIMITER}{currentAxis}{DELIMITER}{currentMoveDir}{DELIMITER}");
        lineBuilder.Append($"{distance:F4}{DELIMITER}");
        lineBuilder.Append($"{spherePos.x:F4}{DELIMITER}{spherePos.y:F4}{DELIMITER}{spherePos.z:F4}{DELIMITER}");
        lineBuilder.Append($"{pacerx:F4}{DELIMITER}{(isHolding ? 1 : 0)}{DELIMITER}{hitt}{DELIMITER}{finished}{DELIMITER}");
        lineBuilder.Append($"{hitTime:F4}{DELIMITER}{hitPos.x:F4}{DELIMITER}{hitPos.y:F4}{DELIMITER}{hitPos.z:F4}{DELIMITER}");
        lineBuilder.Append($"{finishedTime:F4}{DELIMITER}{finishedPos.x:F4}{DELIMITER}{finishedPos.y:F4}{DELIMITER}{finishedPos.z:F4}{DELIMITER}");
        lineBuilder.Append($"{startPointPosition.x:F4}{DELIMITER}{startPointPosition.y:F4}{DELIMITER}{startPointPosition.z:F4}{DELIMITER}");
        lineBuilder.Append($"{endPointPosition.x:F4}{DELIMITER}{endPointPosition.y:F4}{DELIMITER}{endPointPosition.z:F4}{DELIMITER}");
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
            Debug.Log($"Frame {Time.frameCount}: Condition={experimentCondition}, Axis={currentAxis}, MoveDir={currentMoveDir}, " +
                     $"Hit={hitt}, Finished={finished}, Coins=[{CoinGet1},{CoinGet2},{CoinGet3}], Buffered={frameData.Count}");
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
