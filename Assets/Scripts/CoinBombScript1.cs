/*

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CoinBombScript1 : MonoBehaviour
{
    [Header("Coin GameObjects")]
    public GameObject coin1;
    public GameObject coin2;
    public GameObject coin3;

    [Header("Visual Settings")]
    public Material orange;
    public Material blue;

    [Header("Mode Settings")]
    [Tooltip("0 = Normal, 1 = Proactive, 2 = Reactive")]
    [Range(0, 2)]
    public int mode = 0;

    [Header("Proactive/Reactive Mode Settings")]
    [Tooltip("Distance offset (meters, default 0.1 = 10cm)")]
    public float proactiveOffset = 0.1f;

    [Header("Reactive Mode Settings")]
    public Transform userSphere;

    [Header("Sine Wave Settings")]
    public SinusoidScript7 sinusoidScript;
    public float verticalOffset = 0.0f;
    public bool useVerticalOffset = false;

    // Internal variables
    private GameObject[] coinarray;
    private GameObject Gobj1, Gobj2, Gobj3;
    private GameObject ProactiveGobj1, ProactiveGobj2, ProactiveGobj3;
    private int[] ydirs;
    private Vector3 coin1Position, coin2Position, coin3Position;
    private Vector3 proactiveCoin1Position, proactiveCoin2Position, proactiveCoin3Position;
    private int coin1Location, coin2Location, coin3Location;
    private int proactiveLocation1, proactiveLocation2, proactiveLocation3;
    private bool isInitialized = false;
    private bool[] coinActivated = new bool[3];

    void Start()
    {
        if (StaticValsReach7.ran1 == 0 && StaticValsReach7.ran2 == 0 && StaticValsReach7.ran3 == 0)
        {
            Debug.LogWarning("CoinBombScript1: StaticValsReach7 not set. Calling Set(0)...");
            StaticValsReach7.Set(0);
        }

        mode = StaticValsReach7.requestedMode;
        Debug.Log($"CoinBombScript1: MODE = {mode} (0=Normal, 1=Proactive, 2=Reactive)");

        GetRandomCoinLocations();

        Debug.Log($"CoinBombScript1: Coin locations = [{coin1Location}, {coin2Location}, {coin3Location}]");
        if (mode == 1 || mode == 2)
        {
            Debug.Log($"CoinBombScript1: Proactive locations = [{proactiveLocation1}, {proactiveLocation2}, {proactiveLocation3}]");
        }

        StartCoroutine(InitializeCoins());
    }

    void Update()
    {
        if (mode == 2 && userSphere != null && isInitialized)
        {
            CheckAndActivateCoin(coin1, proactiveCoin1Position, userSphere.position, 0);
            CheckAndActivateCoin(coin2, proactiveCoin2Position, userSphere.position, 1);
            CheckAndActivateCoin(coin3, proactiveCoin3Position, userSphere.position, 2);
        }
    }

    void CheckAndActivateCoin(GameObject coin, Vector3 target, Vector3 user, int index)
    {
        if (coin == null || coinActivated[index]) return;
        if (Vector3.Distance(target, user) < 0.05f)
        {
            coin.SetActive(true);
            coinActivated[index] = true;
        }
    }

    /// <summary>
    /// Get coin locations from StaticValsReach7 and calculate unique proactive locations
    /// </summary>
    void GetRandomCoinLocations()
    {
        coin1Location = StaticValsReach7.ran1;
        coin2Location = StaticValsReach7.ran2;
        coin3Location = StaticValsReach7.ran3;

        Debug.Log($"Coin locations from StaticValsReach7: [{coin1Location}, {coin2Location}, {coin3Location}]");

        int offsetPoints = Mathf.RoundToInt(proactiveOffset / 0.001992032f); // ~50 points = 10cm

        // Calculate unique proactive locations using HashSet
        HashSet<int> usedLocations = new HashSet<int>();

        proactiveLocation1 = GetUniqueProactiveLocation(coin1Location, offsetPoints, usedLocations);
        usedLocations.Add(proactiveLocation1);

        proactiveLocation2 = GetUniqueProactiveLocation(coin2Location, offsetPoints, usedLocations);
        usedLocations.Add(proactiveLocation2);

        proactiveLocation3 = GetUniqueProactiveLocation(coin3Location, offsetPoints, usedLocations);

        Debug.Log($"Proactive locations (unique): [{proactiveLocation1}, {proactiveLocation2}, {proactiveLocation3}]");
    }

    /// <summary>
    /// Calculate a unique proactive location that isn't already used
    /// Uses smart direction to avoid edge clamping when possible
    /// </summary>
    int GetUniqueProactiveLocation(int coinLocation, int offsetPoints, HashSet<int> usedLocations)
    {
        const int MIN_CLAMP = 30;
        const int MAX_CLAMP = 210;

        // Smart direction: choose direction that avoids clamping
        int dirPositive = coinLocation + offsetPoints;
        int dirNegative = coinLocation - offsetPoints;

        bool positiveWouldClamp = dirPositive > MAX_CLAMP;
        bool negativeWouldClamp = dirNegative < MIN_CLAMP;

        int dir;
        if (positiveWouldClamp && !negativeWouldClamp)
        {
            dir = -1;  // Go negative to avoid upper clamp
        }
        else if (negativeWouldClamp && !positiveWouldClamp)
        {
            dir = 1;   // Go positive to avoid lower clamp
        }
        else
        {
            // Both safe or both clamp - pick random
            System.Random random = new System.Random();
            dir = random.Next(0, 2) == 0 ? 1 : -1;
        }

        int result = Mathf.Clamp(coinLocation + (dir * offsetPoints), MIN_CLAMP, MAX_CLAMP);

        // If already used, try opposite direction
        if (usedLocations.Contains(result))
        {
            result = Mathf.Clamp(coinLocation + (-dir * offsetPoints), MIN_CLAMP, MAX_CLAMP);
        }

        // If still duplicate, add small offset until unique
        while (usedLocations.Contains(result))
        {
            result = Mathf.Clamp(result + 5, 30, 210);
        }

        return result;
    }

    public void RandomizeCoins()
    {
        GetRandomCoinLocations();

        // Reset state
        isInitialized = false;
        Gobj1 = Gobj2 = Gobj3 = null;
        ProactiveGobj1 = ProactiveGobj2 = ProactiveGobj3 = null;
        for (int i = 0; i < coinActivated.Length; i++) coinActivated[i] = false;

        StartCoroutine(InitializeCoins());
    }

    IEnumerator InitializeCoins()
    {
        GameObject sinusoidParent = null;
        int waitAttempts = 0;

        while (sinusoidParent == null && waitAttempts < 50)
        {
            sinusoidParent = GameObject.Find("Sinusoid_Fixed_World_Space");
            if (sinusoidParent == null)
            {
                waitAttempts++;
                yield return new WaitForSeconds(0.1f);
            }
        }

        if (sinusoidParent == null)
        {
            Debug.LogError("CoinBombScript1: Sine wave parent not found!");
            yield break;
        }

        GameObject testObject = null;
        waitAttempts = 0;
        Transform[] children = null;

        while (testObject == null && waitAttempts < 50)
        {
            children = sinusoidParent.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                if (child.name == "go0" || child.name == "go251")
                {
                    testObject = child.gameObject;
                    break;
                }
            }
            if (testObject == null)
            {
                waitAttempts++;
                yield return new WaitForSeconds(0.1f);
            }
        }

        if (testObject == null)
        {
            Debug.LogError("CoinBombScript1: Sine wave points not found!");
            yield break;
        }

        Transform[] allChildren = sinusoidParent.GetComponentsInChildren<Transform>();

        if (mode == 0)
            FindNormalModeLocations(allChildren);
        else if (mode == 1 || mode == 2)
            FindProactiveModeLocations(allChildren);

        if (Gobj1 == null || Gobj2 == null || Gobj3 == null)
        {
            Debug.LogError("CoinBombScript1: Could not find sine wave GameObjects!");
            yield break;
        }

        ydirs = new int[3];
        System.Random random = new System.Random();
        for (int i = 0; i < 3; i++)
            ydirs[i] = random.Next(0, 2) == 0 ? -1 : 1;

        CalculateCoinPositions();
        PlaceCoins();

        isInitialized = true;
        Debug.Log("CoinBombScript1: Initialization complete!");
    }

    void FindNormalModeLocations(Transform[] allChildren)
    {
        foreach (Transform child in allChildren)
        {
            if (child.name == "go" + coin1Location) Gobj1 = child.gameObject;
            if (child.name == "go" + coin2Location) Gobj2 = child.gameObject;
            if (child.name == "go" + coin3Location) Gobj3 = child.gameObject;
        }
    }

    void FindProactiveModeLocations(Transform[] allChildren)
    {
        foreach (Transform child in allChildren)
        {
            if (child.name == "go" + coin1Location) Gobj1 = child.gameObject;
            if (child.name == "go" + coin2Location) Gobj2 = child.gameObject;
            if (child.name == "go" + coin3Location) Gobj3 = child.gameObject;

            if (child.name == "go" + proactiveLocation1) ProactiveGobj1 = child.gameObject;
            if (child.name == "go" + proactiveLocation2) ProactiveGobj2 = child.gameObject;
            if (child.name == "go" + proactiveLocation3) ProactiveGobj3 = child.gameObject;
        }

        if (ProactiveGobj1 != null) proactiveCoin1Position = ProactiveGobj1.transform.position;
        if (ProactiveGobj2 != null) proactiveCoin2Position = ProactiveGobj2.transform.position;
        if (ProactiveGobj3 != null) proactiveCoin3Position = ProactiveGobj3.transform.position;
    }

    void CalculateCoinPositions()
    {
        if (mode == 1 || mode == 2)
        {
            coin1Position = proactiveCoin1Position + new Vector3(0, proactiveOffset * ydirs[0], 0);
            coin2Position = proactiveCoin2Position + new Vector3(0, proactiveOffset * ydirs[1], 0);
            coin3Position = proactiveCoin3Position + new Vector3(0, proactiveOffset * ydirs[2], 0);
        }
        else if (useVerticalOffset)
        {
            coin1Position = Gobj1.transform.position + new Vector3(0, verticalOffset * ydirs[0], 0);
            coin2Position = Gobj2.transform.position + new Vector3(0, verticalOffset * ydirs[1], 0);
            coin3Position = Gobj3.transform.position + new Vector3(0, verticalOffset * ydirs[2], 0);
        }
        else
        {
            coin1Position = Gobj1.transform.position;
            coin2Position = Gobj2.transform.position;
            coin3Position = Gobj3.transform.position;
        }
    }

    void PlaceCoins()
    {
        PlaceCoin(coin1, coin1Position);
        PlaceCoin(coin2, coin2Position);
        PlaceCoin(coin3, coin3Position);

        if (mode == 0)
        {
            coin1?.SetActive(false);
            coin2?.SetActive(false);
            coin3?.SetActive(false);
            return;
        }

        if (mode == 1 || mode == 2)
        {
            MarkSineWavePoint(Gobj1, orange);
            MarkSineWavePoint(Gobj2, orange);
            MarkSineWavePoint(Gobj3, orange);

            if (blue != null)
            {
                MarkSineWavePoint(ProactiveGobj1, blue);
                MarkSineWavePoint(ProactiveGobj2, blue);
                MarkSineWavePoint(ProactiveGobj3, blue);
            }
        }

        if (mode == 2)
        {
            coin1?.SetActive(false);
            coin2?.SetActive(false);
            coin3?.SetActive(false);
            for (int i = 0; i < coinActivated.Length; i++) coinActivated[i] = false;
        }

        coinarray = new GameObject[] { coin1, coin2, coin3 };
    }

    void PlaceCoin(GameObject coin, Vector3 position)
    {
        if (coin == null) return;
        coin.transform.position = position;
        coin.SetActive(true);
        var renderer = coin.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.enabled = true;
    }

    void MarkSineWavePoint(GameObject gobj, Material material)
    {
        if (gobj == null || material == null) return;
        var renderer = gobj.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = material;
            gobj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            gobj.tag = "step1";
        }
    }

    // Public getters
    public int GetCoin1Location() => coin1Location;
    public int GetCoin2Location() => coin2Location;
    public int GetCoin3Location() => coin3Location;
    public int GetProactiveLocation1() => proactiveLocation1;
    public int GetProactiveLocation2() => proactiveLocation2;
    public int GetProactiveLocation3() => proactiveLocation3;
    public Vector3 GetCoinPosition(int i) => i == 0 ? coin1Position : i == 1 ? coin2Position : coin3Position;
    public GameObject GetSineWavePoint(int i) => i == 0 ? Gobj1 : i == 1 ? Gobj2 : Gobj3;
    public GameObject GetCoin(int i) => i == 0 ? coin1 : i == 1 ? coin2 : coin3;
    public bool IsInitialized() => isInitialized && Gobj1 != null && Gobj2 != null && Gobj3 != null;
    public int GetMode() => mode;
}

*/





using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CoinBombScript1 : MonoBehaviour
{
    [Header("Coin GameObjects")]
    public GameObject coin1;
    public GameObject coin2;
    public GameObject coin3;

    [Header("Visual Settings")]
    public Material orange;
    public Material blue;
    public Material greyLine; // Reference to the grey sine wave material

    [Header("Mode Settings (Read from StaticValsReach7)")]
    [Tooltip("Behavior mode derived from experiment mode: 0=Baseline, 1=Proactive, 2=Reactive")]
    [SerializeField] private int behaviorMode = 0;

    [Header("Proactive/Reactive Mode Settings")]
    [Tooltip("Distance offset (meters, default 0.1 = 10cm)")]
    public float proactiveOffset = 0.1f;

    [Header("Reactive Mode Settings")]
    public Transform userSphere;

    [Tooltip("Distance threshold to reveal point and coin (meters)")]
    public float reactiveRevealDistance = 0.05f;

    [Header("Sine Wave Settings")]
    public SinusoidScript7 sinusoidScript;
    public float verticalOffset = 0.0f;
    public bool useVerticalOffset = false;

    // Internal variables
    private GameObject[] coinarray;
    private GameObject Gobj1, Gobj2, Gobj3;
    private GameObject ProactiveGobj1, ProactiveGobj2, ProactiveGobj3;
    private int[] ydirs;
    private Vector3 coin1Position, coin2Position, coin3Position;
    private Vector3 proactiveCoin1Position, proactiveCoin2Position, proactiveCoin3Position;
    private int coin1Location, coin2Location, coin3Location;
    private int proactiveLocation1, proactiveLocation2, proactiveLocation3;
    private bool isInitialized = false;
    private bool[] coinActivated = new bool[3];
    private ExperimentModeCondition currentExperimentMode;

    void Start()
    {
        if (StaticValsReach7.ran1 == 0 && StaticValsReach7.ran2 == 0 && StaticValsReach7.ran3 == 0)
        {
            Debug.LogWarning("CoinBombScript1: StaticValsReach7 not set. Calling Set(0)...");
            StaticValsReach7.Set(0);
        }

        // Get the full experiment mode and behavior mode
        currentExperimentMode = StaticValsReach7.requestedExperimentMode;
        behaviorMode = StaticValsReach7.requestedMode; // Uses the property that converts to 0/1/2

        Debug.Log($"CoinBombScript1: Experiment Mode Condition = {ExperimentModeConditionHelper.GetDisplayName(currentExperimentMode)} ({(int)currentExperimentMode})");
        Debug.Log($"CoinBombScript1: Behavior Mode = {behaviorMode} (0=Baseline, 1=Proactive, 2=Reactive)");
        Debug.Log($"CoinBombScript1: Environment = {ExperimentModeConditionHelper.GetEnvironment(currentExperimentMode)}");

        GetRandomCoinLocations();

        Debug.Log($"CoinBombScript1: Coin locations = [{coin1Location}, {coin2Location}, {coin3Location}]");
        if (behaviorMode == 1 || behaviorMode == 2)
        {
            Debug.Log($"CoinBombScript1: Proactive locations = [{proactiveLocation1}, {proactiveLocation2}, {proactiveLocation3}]");
        }

        StartCoroutine(InitializeCoins());
    }

    void Update()
    {
        // Reactive behavior mode (behaviorMode == 2)
        if (behaviorMode == 2 && userSphere != null && isInitialized)
        {
            // Check and reveal BOTH point AND coin when user is close
            CheckAndRevealPointAndCoin(0, ProactiveGobj1, coin1, proactiveCoin1Position);
            CheckAndRevealPointAndCoin(1, ProactiveGobj2, coin2, proactiveCoin2Position);
            CheckAndRevealPointAndCoin(2, ProactiveGobj3, coin3, proactiveCoin3Position);
        }
    }

    /// <summary>
    /// In reactive mode: reveal both the blue point indicator AND the coin when user is close
    /// </summary>
    void CheckAndRevealPointAndCoin(int index, GameObject pointObj, GameObject coin, Vector3 targetPosition)
    {
        if (coinActivated[index]) return;

        float dist = Vector3.Distance(targetPosition, userSphere.position);

        if (dist < reactiveRevealDistance)
        {
            // Reveal the blue point on sine wave (change from grey to blue)
            if (pointObj != null && blue != null)
            {
                var renderer = pointObj.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.material = blue;
                    pointObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                }
                Debug.Log($"CoinBombScript1: ✨ Revealed blue point {pointObj.name}");
            }

            // Reveal the coin
            if (coin != null)
            {
                coin.SetActive(true);
                Debug.Log($"CoinBombScript1: ✨ Revealed coin at index {index}");
            }

            coinActivated[index] = true;
        }
    }

    /// <summary>
    /// Get coin locations from StaticValsReach7 and calculate proactive locations using lookup table
    /// This avoids clamping issues that could cause duplicate locations
    /// </summary>
    void GetRandomCoinLocations()
    {
        coin1Location = StaticValsReach7.ran1;
        coin2Location = StaticValsReach7.ran2;
        coin3Location = StaticValsReach7.ran3;

        Debug.Log($"Coin locations from StaticValsReach7: [{coin1Location}, {coin2Location}, {coin3Location}]");

        // Use lookup table for proactive locations - guarantees no duplicates and proper spacing
        proactiveLocation1 = GetProactiveLocationFromLookup(coin1Location);
        proactiveLocation2 = GetProactiveLocationFromLookup(coin2Location);
        proactiveLocation3 = GetProactiveLocationFromLookup(coin3Location);

        Debug.Log($"Proactive locations (from lookup): [{proactiveLocation1}, {proactiveLocation2}, {proactiveLocation3}]");

        // Verify no duplicates
        if (proactiveLocation1 == proactiveLocation2 || proactiveLocation1 == proactiveLocation3 || proactiveLocation2 == proactiveLocation3)
        {
            Debug.LogError($"CoinBombScript1: DUPLICATE proactive locations detected! This should not happen with properly spaced coin locations.");
        }
    }

    /// <summary>
    /// Lookup table for proactive locations - each coin location maps to a specific proactive point
    /// This ensures no clamping issues and guarantees unique, well-spaced proactive locations
    /// Offset is ~50 points (10cm) in a direction that stays within valid range (30-210)
    /// </summary>
    int GetProactiveLocationFromLookup(int coinLocation)
    {
        // Predefined mapping: coinLocation -> proactiveLocation
        // Each proactive location is ~50 points away from coin, staying within 30-210 range
        // Direction chosen to avoid edge clamping
        switch (coinLocation)
        {
            // Left group (52, 63, 74) - offset goes POSITIVE (toward middle)
            case 52: return 102;  // 52 + 50 = 102
            case 63: return 113;  // 63 + 50 = 113
            case 74: return 124;  // 74 + 50 = 124

            // Middle group (115, 125, 136) - offset goes NEGATIVE (toward left)
            case 115: return 65;  // 115 - 50 = 65
            case 125: return 75;  // 125 - 50 = 75
            case 136: return 86;  // 136 - 50 = 86

            // Right group (178, 188, 199) - offset goes NEGATIVE (toward middle)
            case 178: return 128; // 178 - 50 = 128
            case 188: return 138; // 188 - 50 = 138
            case 199: return 149; // 199 - 50 = 149

            default:
                // Fallback for unexpected values
                Debug.LogWarning($"CoinBombScript1: Unexpected coin location {coinLocation}, using fallback calculation");
                int offset = Mathf.RoundToInt(proactiveOffset / 0.001992032f);
                if (coinLocation < 120)
                    return Mathf.Clamp(coinLocation + offset, 30, 210);
                else
                    return Mathf.Clamp(coinLocation - offset, 30, 210);
        }
    }

    public void RandomizeCoins()
    {
        GetRandomCoinLocations();

        isInitialized = false;
        Gobj1 = Gobj2 = Gobj3 = null;
        ProactiveGobj1 = ProactiveGobj2 = ProactiveGobj3 = null;
        for (int i = 0; i < coinActivated.Length; i++) coinActivated[i] = false;

        StartCoroutine(InitializeCoins());
    }

    IEnumerator InitializeCoins()
    {
        GameObject sinusoidParent = null;
        int waitAttempts = 0;

        while (sinusoidParent == null && waitAttempts < 50)
        {
            sinusoidParent = GameObject.Find("Sinusoid_Fixed_World_Space");
            if (sinusoidParent == null)
            {
                waitAttempts++;
                yield return new WaitForSeconds(0.1f);
            }
        }

        if (sinusoidParent == null)
        {
            Debug.LogError("CoinBombScript1: Sine wave parent not found!");
            yield break;
        }

        // Auto-get grey material from SinusoidScript7 if not assigned
        if (greyLine == null && sinusoidScript != null)
        {
            greyLine = sinusoidScript.greyLine;
        }

        // Fallback: get grey material from first sine wave point
        if (greyLine == null)
        {
            Transform firstChild = sinusoidParent.transform.Find("go0");
            if (firstChild != null)
            {
                var renderer = firstChild.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    greyLine = renderer.material;
                    Debug.Log("CoinBombScript1: Auto-acquired grey material from go0");
                }
            }
        }

        GameObject testObject = null;
        waitAttempts = 0;

        while (testObject == null && waitAttempts < 50)
        {
            Transform[] children = sinusoidParent.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                if (child.name == "go0" || child.name == "go251")
                {
                    testObject = child.gameObject;
                    break;
                }
            }
            if (testObject == null)
            {
                waitAttempts++;
                yield return new WaitForSeconds(0.1f);
            }
        }

        if (testObject == null)
        {
            Debug.LogError("CoinBombScript1: Sine wave points not found!");
            yield break;
        }

        Transform[] allChildren = sinusoidParent.GetComponentsInChildren<Transform>();

        if (behaviorMode == 0) // Baseline
            FindNormalModeLocations(allChildren);
        else if (behaviorMode == 1 || behaviorMode == 2) // Proactive or Reactive
            FindProactiveModeLocations(allChildren);

        if (Gobj1 == null || Gobj2 == null || Gobj3 == null)
        {
            Debug.LogError("CoinBombScript1: Could not find sine wave GameObjects!");
            yield break;
        }

        ydirs = new int[3];
        System.Random random = new System.Random();
        for (int i = 0; i < 3; i++)
            ydirs[i] = random.Next(0, 2) == 0 ? -1 : 1;

        CalculateCoinPositions();
        PlaceCoins();

        isInitialized = true;
        Debug.Log($"CoinBombScript1: Initialization complete! Mode: {ExperimentModeConditionHelper.GetDisplayName(currentExperimentMode)}");
    }

    void FindNormalModeLocations(Transform[] allChildren)
    {
        foreach (Transform child in allChildren)
        {
            if (child.name == "go" + coin1Location) Gobj1 = child.gameObject;
            if (child.name == "go" + coin2Location) Gobj2 = child.gameObject;
            if (child.name == "go" + coin3Location) Gobj3 = child.gameObject;
        }
    }

    void FindProactiveModeLocations(Transform[] allChildren)
    {
        foreach (Transform child in allChildren)
        {
            if (child.name == "go" + coin1Location) Gobj1 = child.gameObject;
            if (child.name == "go" + coin2Location) Gobj2 = child.gameObject;
            if (child.name == "go" + coin3Location) Gobj3 = child.gameObject;

            if (child.name == "go" + proactiveLocation1) ProactiveGobj1 = child.gameObject;
            if (child.name == "go" + proactiveLocation2) ProactiveGobj2 = child.gameObject;
            if (child.name == "go" + proactiveLocation3) ProactiveGobj3 = child.gameObject;
        }

        if (ProactiveGobj1 != null) proactiveCoin1Position = ProactiveGobj1.transform.position;
        if (ProactiveGobj2 != null) proactiveCoin2Position = ProactiveGobj2.transform.position;
        if (ProactiveGobj3 != null) proactiveCoin3Position = ProactiveGobj3.transform.position;
    }

    void CalculateCoinPositions()
    {
        if (behaviorMode == 1 || behaviorMode == 2) // Proactive or Reactive
        {
            coin1Position = proactiveCoin1Position + new Vector3(0, proactiveOffset * ydirs[0], 0);
            coin2Position = proactiveCoin2Position + new Vector3(0, proactiveOffset * ydirs[1], 0);
            coin3Position = proactiveCoin3Position + new Vector3(0, proactiveOffset * ydirs[2], 0);
        }
        else if (useVerticalOffset)
        {
            coin1Position = Gobj1.transform.position + new Vector3(0, verticalOffset * ydirs[0], 0);
            coin2Position = Gobj2.transform.position + new Vector3(0, verticalOffset * ydirs[1], 0);
            coin3Position = Gobj3.transform.position + new Vector3(0, verticalOffset * ydirs[2], 0);
        }
        else
        {
            coin1Position = Gobj1.transform.position;
            coin2Position = Gobj2.transform.position;
            coin3Position = Gobj3.transform.position;
        }
    }

    void PlaceCoins()
    {
        PlaceCoin(coin1, coin1Position);
        PlaceCoin(coin2, coin2Position);
        PlaceCoin(coin3, coin3Position);

        if (behaviorMode == 0) // Baseline
        {
            coin1?.SetActive(false);
            coin2?.SetActive(false);
            coin3?.SetActive(false);
            return;
        }

        if (behaviorMode == 1) // Proactive
        {
            // Proactive mode: show orange coin markers and blue target points immediately
            MarkSineWavePoint(Gobj1, orange);
            MarkSineWavePoint(Gobj2, orange);
            MarkSineWavePoint(Gobj3, orange);

            if (blue != null)
            {
                MarkSineWavePoint(ProactiveGobj1, blue);
                MarkSineWavePoint(ProactiveGobj2, blue);
                MarkSineWavePoint(ProactiveGobj3, blue);
            }
        }
        else if (behaviorMode == 2) // Reactive
        {
            // Reactive mode: Orange markers for coin locations
            MarkSineWavePoint(Gobj1, orange);
            MarkSineWavePoint(Gobj2, orange);
            MarkSineWavePoint(Gobj3, orange);

            // Keep blue points as GREY (same as sine wave) - NOT hidden!
            // They will change to blue when user approaches
            KeepSineWavePointAsGrey(ProactiveGobj1);
            KeepSineWavePointAsGrey(ProactiveGobj2);
            KeepSineWavePointAsGrey(ProactiveGobj3);

            // Hide coins initially
            coin1?.SetActive(false);
            coin2?.SetActive(false);
            coin3?.SetActive(false);

            // Reset activation flags
            for (int i = 0; i < coinActivated.Length; i++) coinActivated[i] = false;

            Debug.Log("CoinBombScript1: Reactive mode - blue points kept as grey, coins hidden until user approaches");
        }

        coinarray = new GameObject[] { coin1, coin2, coin3 };
    }

    void PlaceCoin(GameObject coin, Vector3 position)
    {
        if (coin == null) return;
        coin.transform.position = position;
        coin.SetActive(true);
        var renderer = coin.GetComponent<MeshRenderer>();
        if (renderer != null) renderer.enabled = true;
    }

    void MarkSineWavePoint(GameObject gobj, Material material)
    {
        if (gobj == null || material == null) return;
        var renderer = gobj.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = material;
            renderer.enabled = true;
            gobj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            gobj.tag = "step1";
        }
    }

    /// <summary>
    /// Keep sine wave point visible with grey material (for reactive mode)
    /// This ensures the sine wave remains continuous without gaps
    /// </summary>
    void KeepSineWavePointAsGrey(GameObject gobj)
    {
        if (gobj == null) return;
        var renderer = gobj.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            // Keep the original grey material - don't change anything
            // Just ensure it's visible
            renderer.enabled = true;

            // Optionally restore grey material if it was changed
            if (greyLine != null)
            {
                renderer.material = greyLine;
            }

            // Keep original sine wave scale
            gobj.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);
        }
    }

    // Public getters
    public int GetCoin1Location() => coin1Location;
    public int GetCoin2Location() => coin2Location;
    public int GetCoin3Location() => coin3Location;
    public int GetProactiveLocation1() => proactiveLocation1;
    public int GetProactiveLocation2() => proactiveLocation2;
    public int GetProactiveLocation3() => proactiveLocation3;
    public Vector3 GetCoinPosition(int i) => i == 0 ? coin1Position : i == 1 ? coin2Position : coin3Position;
    public GameObject GetSineWavePoint(int i) => i == 0 ? Gobj1 : i == 1 ? Gobj2 : Gobj3;
    public GameObject GetCoin(int i) => i == 0 ? coin1 : i == 1 ? coin2 : coin3;
    public bool IsInitialized() => isInitialized && Gobj1 != null && Gobj2 != null && Gobj3 != null;
    public int GetBehaviorMode() => behaviorMode;
    public ExperimentModeCondition GetExperimentMode() => currentExperimentMode;
}