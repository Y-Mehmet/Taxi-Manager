using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Coin animasyonlarÄ± iÃ§in object pooling sistemi.
/// Coin sprite ve text prefab'larÄ±nÄ± pool'da tutar, performans iÃ§in.
/// </summary>
public class CoinObjectPool : MonoBehaviour
{
    public static CoinObjectPool Instance { get; private set; }

    [Header("Pool Settings")]
    [SerializeField] private GameObject coinSpritePrefab;
    [SerializeField] private GameObject floatingTextPrefab;
    [SerializeField] private int initialCoinSpritePoolSize = 20;
    [SerializeField] private int initialTextPoolSize = 10;
    [SerializeField] private Transform poolContainer;

    private Queue<GameObject> coinSpritePool = new Queue<GameObject>();
    private Queue<GameObject> floatingTextPool = new Queue<GameObject>();
    
    private List<GameObject> activeCoinSprites = new List<GameObject>();
    private List<GameObject> activeFloatingTexts = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Pool container oluÅŸtur
        if (poolContainer == null)
        {
            poolContainer = new GameObject("CoinObjectPool_Container").transform;
            poolContainer.SetParent(transform);
        }

        // Pool'larÄ± baÅŸlat
        InitializePools();
    }

    private void InitializePools()
    {
        // Coin sprite pool
        for (int i = 0; i < initialCoinSpritePoolSize; i++)
        {
            CreateNewCoinSprite();
        }

        // Floating text pool
        for (int i = 0; i < initialTextPoolSize; i++)
        {
            CreateNewFloatingText();
        }

        /* Debug.Log($"[CoinObjectPool] Initialized with {initialCoinSpritePoolSize} coin sprites and {initialTextPoolSize} texts"); */
    }

    private GameObject CreateNewCoinSprite()
    {
        if (coinSpritePrefab == null) return null;

        GameObject obj = Instantiate(coinSpritePrefab, poolContainer);
        obj.SetActive(false);
        coinSpritePool.Enqueue(obj);
        return obj;
    }

    private GameObject CreateNewFloatingText()
    {
        if (floatingTextPrefab == null) return null;

        GameObject obj = Instantiate(floatingTextPrefab, poolContainer);
        obj.SetActive(false);
        floatingTextPool.Enqueue(obj);
        return obj;
    }

    /// <summary>
    /// Pool'dan coin sprite alÄ±r
    /// </summary>
    public GameObject GetCoinSprite(Transform parent)
    {
        GameObject obj;

        if (coinSpritePool.Count > 0)
        {
            obj = coinSpritePool.Dequeue();
        }
        else
        {
            obj = CreateNewCoinSprite();
        }

        if (obj != null)
        {
            obj.transform.SetParent(parent);
            obj.SetActive(true);
            activeCoinSprites.Add(obj);
        }

        return obj;
    }

    /// <summary>
    /// Pool'dan floating text alÄ±r
    /// </summary>
    public GameObject GetFloatingText(Transform parent)
    {
        GameObject obj;

        if (floatingTextPool.Count > 0)
        {
            obj = floatingTextPool.Dequeue();
        }
        else
        {
            obj = CreateNewFloatingText();
        }

        if (obj != null)
        {
            obj.transform.SetParent(parent);
            obj.SetActive(true);
            activeFloatingTexts.Add(obj);
        }

        return obj;
    }

    /// <summary>
    /// Coin sprite'Ä± pool'a geri dÃ¶ndÃ¼rÃ¼r
    /// </summary>
    public void ReturnCoinSprite(GameObject obj)
    {
        if (obj == null) return;

        obj.SetActive(false);
        obj.transform.SetParent(poolContainer);
        activeCoinSprites.Remove(obj);
        coinSpritePool.Enqueue(obj);
    }

    /// <summary>
    /// Floating text'i pool'a geri dÃ¶ndÃ¼rÃ¼r
    /// </summary>
    public void ReturnFloatingText(GameObject obj)
    {
        if (obj == null) return;

        obj.SetActive(false);
        obj.transform.SetParent(poolContainer);
        activeFloatingTexts.Remove(obj);
        floatingTextPool.Enqueue(obj);
    }

    /// <summary>
    /// TÃ¼m aktif objeleri pool'a geri dÃ¶ndÃ¼rÃ¼r
    /// </summary>
    public void ReturnAllToPool()
    {
        // Coin sprite'larÄ± geri dÃ¶ndÃ¼r
        for (int i = activeCoinSprites.Count - 1; i >= 0; i--)
        {
            ReturnCoinSprite(activeCoinSprites[i]);
        }

        // Floating text'leri geri dÃ¶ndÃ¼r
        for (int i = activeFloatingTexts.Count - 1; i >= 0; i--)
        {
            ReturnFloatingText(activeFloatingTexts[i]);
        }
    }

    /// <summary>
    /// Pool istatistiklerini dÃ¶ndÃ¼rÃ¼r
    /// </summary>
    public string GetPoolStats()
    {
        return $"Coin Sprites - Pool: {coinSpritePool.Count}, Active: {activeCoinSprites.Count} | " +
               $"Floating Texts - Pool: {floatingTextPool.Count}, Active: {activeFloatingTexts.Count}";
    }

    private void OnDestroy()
    {
        ReturnAllToPool();
    }
}
