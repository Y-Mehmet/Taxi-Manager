using UnityEngine;
using System.Collections.Generic;

public class UIParticleManager : MonoBehaviour
{
    public static UIParticleManager Instance { get; private set; }

    [Header("Pool Settings")]
    [SerializeField] private GameObject uiParticlePrefab; // UI Particle Prefab
    [SerializeField] private Canvas mainCanvas;             // Canvas where UI particles will be shown
    [SerializeField] private int initialPoolSize = 5; // Smaller pool size for UI particles

    private readonly Queue<GameObject> particlePool = new Queue<GameObject>();
    private int createdParticleCount = 0;

    void Awake()
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
    }

    void Start()
    {
        InitializePool();
    }

    private void InitializePool()
    {
        if (uiParticlePrefab == null || mainCanvas == null)
        {
            Debug.LogError("[UIParticleManager] Prefab or Canvas is missing. Cannot initialize pool.");
            return;
        }

        Debug.Log($"[UIParticleManager] Initializing pool with size: {initialPoolSize}");
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewParticleForPool();
        }
    }

    private void CreateNewParticleForPool()
    {
        GameObject particle = Instantiate(uiParticlePrefab, transform);
        createdParticleCount++;
        particle.gameObject.name = $"UIParticle_{createdParticleCount}";
        particle.SetActive(false);
        particlePool.Enqueue(particle);
    }

    /// <summary>
    /// Spawns a UI particle effect at the given screen position.
    /// </summary>
    /// <param name="screenPosition">The screen position (pixel) where the particle should appear.</param>
    public void SpawnParticle(Vector2 screenPosition)
    {
        if (mainCanvas == null) return;

        GameObject particleToPlay;

        if (particlePool.Count > 0)
        {
            particleToPlay = particlePool.Dequeue();
        }
        else
        {
            Debug.LogWarning("[UIParticleManager] Pool is empty. Creating a new particle on the fly.");
            CreateNewParticleForPool();
            particleToPlay = particlePool.Dequeue();
        }

        particleToPlay.transform.SetParent(mainCanvas.transform, false);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mainCanvas.GetComponent<RectTransform>() as RectTransform,
            screenPosition,
            mainCanvas.worldCamera,
            out Vector2 localPosition
        );
        
        particleToPlay.GetComponent<RectTransform>().anchoredPosition = localPosition;

        particleToPlay.SetActive(true);
        ParticleSystem ps = particleToPlay.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
            StartCoroutine(DespawnAfterDuration(particleToPlay, ps.main.duration));
        }
        else
        {
            Debug.LogWarning($"[UIParticleManager] UIParticle_{particleToPlay.name} does not have a ParticleSystem component. It will not despawn automatically.");
            // If no ParticleSystem, despawn after a default duration
            StartCoroutine(DespawnAfterDuration(particleToPlay, 2f)); 
        }
    }

    public void ReturnToPool(GameObject particle)
    {
        if (particle == null) return;
        
        ParticleSystem ps = particle.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        particle.transform.SetParent(transform, false);
        particle.SetActive(false);
        particlePool.Enqueue(particle);
    }

    private System.Collections.IEnumerator DespawnAfterDuration(GameObject particle, float duration)
    {
        yield return new WaitForSeconds(duration);
        ReturnToPool(particle);
    }
}