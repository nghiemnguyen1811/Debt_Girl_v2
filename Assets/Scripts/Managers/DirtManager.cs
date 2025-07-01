using UnityEngine;
using System.Collections;

public class DirtManager : MonoBehaviour
{
    [Header("Floor Settings")]
    [SerializeField] private BoxCollider floorCollider;

    [Header("Spawn Settings")]
    [SerializeField] private GameObject dirtPrefab;
    [SerializeField] private int maxAttemptsPerSpawn = 20;
    [SerializeField] private LayerMask detectionLayer;

    [Header("Timing")]
    [SerializeField] private float minSpawnInterval = 30f;
    [SerializeField] private float maxSpawnInterval = 60f;
    [SerializeField] private int minDirtPerCycle = 1;
    [SerializeField] private int maxDirtPerCycle = 3;

    private void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);

            int dirtToSpawn = Random.Range(minDirtPerCycle, maxDirtPerCycle + 1);

            for (int i = 0; i < dirtToSpawn; i++)
            {
                TrySpawnDirt();
            }
        }
    }

    public void TrySpawnDirt()
    {
        if (floorCollider == null || dirtPrefab == null)
        {
            Debug.LogWarning("[DirtManager] Missing floorCollider or dirtPrefab.");
            return;
        }

        Bounds bounds = floorCollider.bounds;

        for (int attempt = 0; attempt < maxAttemptsPerSpawn; attempt++)
        {
            Vector3 randomPoint = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                0f,
                Random.Range(bounds.min.z, bounds.max.z)
            );

            // Tạm thời tạo 1 Dirt giả để lấy radius
            float radius = dirtPrefab.GetComponent<Dirt>().GetDetectionRadius();

            // Kiểm tra xem có vật thể nào gần đó không
            Collider[] overlaps = Physics.OverlapSphere(randomPoint, radius, detectionLayer);

            if (overlaps.Length == 0)
            {
                // Nếu khu vực trống, mới tạo Dirt thực tế từ pool
                Dirt dirt = (Dirt)PoolManager.Instance.ReuseComponent(dirtPrefab, randomPoint, Quaternion.identity);

                if (dirt == null)
                {
                    Debug.LogWarning("[DirtManager] Spawned object has no Dirt component.");
                    return;
                }

                dirt.gameObject.SetActive(true);
                Debug.Log($"[DirtManager] Dirt spawned at {randomPoint} after {attempt + 1} attempts.");
                return;
            }
        }

        Debug.LogWarning("[DirtManager] Failed to spawn dirt in a valid location.");
    }

}
