using UnityEngine;

/// <summary>
/// Simple coin collection system that detects when sphere touches coins.
/// Coins disappear when collected. Same logic as CoinCollector, compatible with CoinBombScript2.
/// </summary>
public class CoinCollector2 : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The sphere that can collect coins")]
    public GameObject sphere;

    [Header("Coins")]
    public GameObject coin1;
    public GameObject coin2;
    public GameObject coin3;

    [Header("Collection Settings")]
    [Tooltip("Distance threshold for collection (in meters)")]
    public float collectionRadius = 0.025f;

    // Collection status
    private bool coin1Collected = false;
    private bool coin2Collected = false;
    private bool coin3Collected = false;

    // Optional: Audio feedback
    [Header("Optional Feedback")]
    public AudioClip collectSound;
    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && collectSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        Debug.Log("=== CoinCollector2 initialized ===");
        Debug.Log($"Collection radius: {collectionRadius}m");
        Debug.Log($"Sphere assigned: {sphere != null}");
        Debug.Log($"Coins assigned - coin1:{coin1 != null}, coin2:{coin2 != null}, coin3:{coin3 != null}");
    }

    private void Update()
    {
        if (sphere == null) return;

        CheckCoinCollection(coin1, ref coin1Collected, "Coin1");
        CheckCoinCollection(coin2, ref coin2Collected, "Coin2");
        CheckCoinCollection(coin3, ref coin3Collected, "Coin3");
    }

    private void CheckCoinCollection(GameObject coin, ref bool collected, string coinName)
    {
        if (collected || coin == null || !coin.activeInHierarchy) return;

        float distance = Vector3.Distance(sphere.transform.position, coin.transform.position);

        if (distance < collectionRadius)
        {
            CollectCoin(coin, ref collected, coinName);
        }
    }

    private void CollectCoin(GameObject coin, ref bool collected, string coinName)
    {
        collected = true;
        coin.SetActive(false);

        Debug.Log($"✓✓✓ {coinName} COLLECTED! ✓✓✓");

        if (audioSource != null && collectSound != null)
        {
            audioSource.PlayOneShot(collectSound);
        }
    }

    public void ResetCoins()
    {
        coin1Collected = false;
        coin2Collected = false;
        coin3Collected = false;

        if (coin1 != null) coin1.SetActive(true);
        if (coin2 != null) coin2.SetActive(true);
        if (coin3 != null) coin3.SetActive(true);

        Debug.Log("CoinCollector2: Coins reset for new trial");
    }

    // Get collection status (0 or 1)
    public int GetCoin1Status() => coin1Collected ? 1 : 0;
    public int GetCoin2Status() => coin2Collected ? 1 : 0;
    public int GetCoin3Status() => coin3Collected ? 1 : 0;

    public bool IsCoin1Collected() => coin1Collected;
    public bool IsCoin2Collected() => coin2Collected;
    public bool IsCoin3Collected() => coin3Collected;

    public bool AllCoinsCollected() => coin1Collected && coin2Collected && coin3Collected;

    public int GetTotalCoinsCollected()
    {
        int total = 0;
        if (coin1Collected) total++;
        if (coin2Collected) total++;
        if (coin3Collected) total++;
        return total;
    }
}
