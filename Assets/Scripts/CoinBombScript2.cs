using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Coin placement for the 2-mode system (Reactive / Baseline).
/// 
/// Baseline: No coins placed.
/// Reactive: Coins placed perpendicular to sine wave, hidden until user approaches.
///   - Horizontal wave (Axis=X): coins offset in Y (above/below)
///   - Vertical wave (Axis=Y): coins offset in X (left/right)
/// </summary>
public class CoinBombScript2 : MonoBehaviour
{
    [Header("Coin GameObjects")]
    public GameObject coin1;
    public GameObject coin2;
    public GameObject coin3;

    [Header("Visual Settings")]
    public Material orange;
    public Material blue;
    public Material greyLine;

    [Header("Reactive Mode Settings")]
    [Tooltip("Distance threshold to reveal coin (meters)")]
    public float reactiveRevealDistance = 0.05f;

    [Tooltip("Perpendicular offset for coin placement (meters)")]
    public float coinOffset = 0.1f;

    [Header("References")]
    public Transform userSphere;
    public SinusoidScript8 sinusoidScript;

    // Internal variables
    private GameObject Gobj1, Gobj2, Gobj3;
    private int[] ydirs;
    private Vector3 coin1Position, coin2Position, coin3Position;
    private int coin1Location, coin2Location, coin3Location;
    private bool isInitialized = false;
    private bool[] coinRevealed = new bool[3];
    private ExperimentCondition currentCondition;
    private int currentAxis;

    void Start()
    {
        currentCondition = StaticValsReach8.condition;
        currentAxis = StaticValsReach8.currentAxis;

        Debug.Log($"CoinBombScript2: Condition = {ExperimentConditionHelper.GetDisplayName(currentCondition)}");
        Debug.Log($"CoinBombScript2: Axis = {(currentAxis == 1 ? "X (Horizontal)" : "Y (Vertical)")}");

        if (currentCondition == ExperimentCondition.Baseline)
        {
            // Baseline: no coins
            Debug.Log("CoinBombScript2: Baseline mode - no coins placed.");
            HideAllCoins();
            return;
        }

        // Reactive mode: place coins
        if (StaticValsReach8.ran1 == 0 && StaticValsReach8.ran2 == 0 && StaticValsReach8.ran3 == 0)
        {
            Debug.LogWarning("CoinBombScript2: Coin locations not set! Picking coin set...");
            int[] set = StaticValsReach8.PickCoinSet();
            StaticValsReach8.ran1 = set[0];
            StaticValsReach8.ran2 = set[1];
            StaticValsReach8.ran3 = set[2];
        }

        coin1Location = StaticValsReach8.ran1;
        coin2Location = StaticValsReach8.ran2;
        coin3Location = StaticValsReach8.ran3;

        Debug.Log($"CoinBombScript2: Coin locations = [{coin1Location}, {coin2Location}, {coin3Location}]");

        StartCoroutine(InitializeCoins());
    }

    void Update()
    {
        // Reactive mode: reveal coins when user approaches
        if (currentCondition == ExperimentCondition.Reactive && userSphere != null && isInitialized)
        {
            CheckAndRevealCoin(0, coin1, coin1Position);
            CheckAndRevealCoin(1, coin2, coin2Position);
            CheckAndRevealCoin(2, coin3, coin3Position);
        }
    }

    void CheckAndRevealCoin(int index, GameObject coin, Vector3 coinPos)
    {
        if (coinRevealed[index] || coin == null) return;

        // Check distance from user sphere to the sine wave point where the coin is anchored
        GameObject sinePoint = index == 0 ? Gobj1 : index == 1 ? Gobj2 : Gobj3;
        if (sinePoint == null) return;

        float dist = Vector3.Distance(sinePoint.transform.position, userSphere.position);

        if (dist < reactiveRevealDistance)
        {
            coin.SetActive(true);
            coinRevealed[index] = true;

            // Also highlight the sine wave point blue
            if (blue != null)
            {
                var renderer = sinePoint.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.material = blue;
                    sinePoint.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                }
            }

            Debug.Log($"CoinBombScript2: ✨ Revealed coin {index + 1} at distance {dist:F3}m");
        }
    }

    IEnumerator InitializeCoins()
    {
        // Wait for sine wave parent to be created
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
            Debug.LogError("CoinBombScript2: Sine wave parent not found!");
            yield break;
        }

        // Auto-get grey material from SinusoidScript8 if not assigned
        if (greyLine == null && sinusoidScript != null)
        {
            greyLine = sinusoidScript.greyLine;
        }
        if (greyLine == null)
        {
            Transform firstChild = sinusoidParent.transform.Find("go0");
            if (firstChild != null)
            {
                var renderer = firstChild.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    greyLine = renderer.material;
                    Debug.Log("CoinBombScript2: Auto-acquired grey material from go0");
                }
            }
        }

        // Wait for sine wave points to be ready
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
            Debug.LogError("CoinBombScript2: Sine wave points not found!");
            yield break;
        }

        // Find the sine wave points for our coin locations
        Transform[] allChildren = sinusoidParent.GetComponentsInChildren<Transform>();
        foreach (Transform child in allChildren)
        {
            if (child.name == "go" + coin1Location) Gobj1 = child.gameObject;
            if (child.name == "go" + coin2Location) Gobj2 = child.gameObject;
            if (child.name == "go" + coin3Location) Gobj3 = child.gameObject;
        }

        if (Gobj1 == null || Gobj2 == null || Gobj3 == null)
        {
            Debug.LogError($"CoinBombScript2: Could not find sine wave points go{coin1Location}, go{coin2Location}, go{coin3Location}!");
            yield break;
        }

        // Randomize offset direction (positive or negative perpendicular)
        ydirs = new int[3];
        System.Random random = new System.Random();
        for (int i = 0; i < 3; i++)
            ydirs[i] = random.Next(0, 2) == 0 ? -1 : 1;

        CalculateCoinPositions();
        PlaceCoins();

        isInitialized = true;
        Debug.Log("CoinBombScript2: Reactive mode initialization complete!");
    }

    void CalculateCoinPositions()
    {
        if (currentAxis == 1)
        {
            // Horizontal sine wave (Axis=X): coins offset perpendicular in Y (above/below)
            coin1Position = Gobj1.transform.position + new Vector3(0, coinOffset * ydirs[0], 0);
            coin2Position = Gobj2.transform.position + new Vector3(0, coinOffset * ydirs[1], 0);
            coin3Position = Gobj3.transform.position + new Vector3(0, coinOffset * ydirs[2], 0);
            Debug.Log("CoinBombScript2: Horizontal wave - coins offset vertically (Y)");
        }
        else
        {
            // Vertical sine wave (Axis=Y): coins offset perpendicular in X (left/right)
            coin1Position = Gobj1.transform.position + new Vector3(coinOffset * ydirs[0], 0, 0);
            coin2Position = Gobj2.transform.position + new Vector3(coinOffset * ydirs[1], 0, 0);
            coin3Position = Gobj3.transform.position + new Vector3(coinOffset * ydirs[2], 0, 0);
            Debug.Log("CoinBombScript2: Vertical wave - coins offset horizontally (X)");
        }

        Debug.Log($"CoinBombScript2: Coin1 at {coin1Position}, Coin2 at {coin2Position}, Coin3 at {coin3Position}");
    }

    void PlaceCoins()
    {
        // Mark sine wave points with orange
        MarkSineWavePoint(Gobj1, orange);
        MarkSineWavePoint(Gobj2, orange);
        MarkSineWavePoint(Gobj3, orange);

        // Place coins but hide them (reactive mode)
        PlaceCoin(coin1, coin1Position);
        PlaceCoin(coin2, coin2Position);
        PlaceCoin(coin3, coin3Position);

        // Hide coins initially for reactive reveal
        coin1?.SetActive(false);
        coin2?.SetActive(false);
        coin3?.SetActive(false);

        // Reset reveal state
        for (int i = 0; i < coinRevealed.Length; i++) coinRevealed[i] = false;

        Debug.Log("CoinBombScript2: Reactive mode - coins hidden until user approaches orange markers");
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
        }
    }

    void HideAllCoins()
    {
        if (coin1 != null) coin1.SetActive(false);
        if (coin2 != null) coin2.SetActive(false);
        if (coin3 != null) coin3.SetActive(false);
    }

    // Public getters
    public int GetCoin1Location() => coin1Location;
    public int GetCoin2Location() => coin2Location;
    public int GetCoin3Location() => coin3Location;
    public Vector3 GetCoinPosition(int i) => i == 0 ? coin1Position : i == 1 ? coin2Position : coin3Position;
    public GameObject GetSineWavePoint(int i) => i == 0 ? Gobj1 : i == 1 ? Gobj2 : Gobj3;
    public GameObject GetCoin(int i) => i == 0 ? coin1 : i == 1 ? coin2 : coin3;
    public bool IsInitialized() => isInitialized && Gobj1 != null && Gobj2 != null && Gobj3 != null;
    public ExperimentCondition GetCondition() => currentCondition;
}