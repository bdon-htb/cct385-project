using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ArrowPool : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] private ArrowProjectile arrowPrefab;
    [SerializeField] private int initialSize = 20;
    [SerializeField] private int maxArrows = 100;

    private ObjectPool<ArrowProjectile> pool;
    private readonly Queue<ArrowProjectile> activeArrows = new();

    private void Awake()
    {
        pool = new ObjectPool<ArrowProjectile>(
            createFunc: CreateArrow,
            actionOnGet: OnGetArrow,
            actionOnRelease: OnReleaseArrow,
            actionOnDestroy: OnDestroyArrow,
            collectionCheck: true,
            defaultCapacity: initialSize,
            maxSize: maxArrows
        );

        // Pre-create the initial arrows.
        for (int i = 0; i < initialSize; i++)
        {
            ArrowProjectile arrow = CreateArrow();
            pool.Release(arrow);
        }
    }

    private ArrowProjectile CreateArrow()
    {
        ArrowProjectile arrow = Instantiate(arrowPrefab, transform);
        arrow.gameObject.SetActive(false);
        return arrow;
    }

    private void OnGetArrow(ArrowProjectile arrow)
    {
        // Initialization is handled in GetArrow().
    }

    private void OnReleaseArrow(ArrowProjectile arrow)
    {
        arrow.gameObject.SetActive(false);
        arrow.transform.SetParent(transform);
    }

    private void OnDestroyArrow(ArrowProjectile arrow)
    {
        Destroy(arrow.gameObject);
    }

    public ArrowProjectile GetArrow(Vector3 position, Quaternion rotation)
    {
        if (activeArrows.Count >= maxArrows)
        {
            ArrowProjectile oldest = activeArrows.Dequeue();
            pool.Release(oldest);
        }

        ArrowProjectile arrow = pool.Get();

        arrow.transform.SetParent(null);
        arrow.transform.SetPositionAndRotation(position, rotation);

        arrow.ResetProjectile();
        arrow.gameObject.SetActive(true);

        activeArrows.Enqueue(arrow);

        return arrow;
    }
}