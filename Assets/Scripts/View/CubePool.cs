using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Object pool for CubeView instances.
///
/// Instead of Instantiate/Destroy every time a cube appears or disappears,
/// we reuse deactivated GameObjects. Zero GC spikes during gameplay.
///
/// Only GridStateManager calls Get/Return. Nobody else touches the pool.
/// </summary>
public class CubePool
{
    private readonly Queue<CubeView> _available = new Queue<CubeView>();
    private readonly List<CubeView> _allInstances = new List<CubeView>();
    private readonly GameObject _prefab;
    private readonly Transform _parent;

    private const int GrowBatchSize = 10;

    public CubePool(GameObject prefab, Transform parent)
    {
        _prefab = prefab;
        _parent = parent;
    }

    /// <summary>
    /// Ensure at least 'count' instances exist in the pool.
    /// Only creates new ones if the pool doesn't have enough.
    /// Safe to call multiple times (e.g., on level reload).
    /// </summary>
    public void Prewarm(int count)
    {
        int needed = count - _available.Count;
        for (int i = 0; i < needed; i++)
            CreateInstance();
    }

    /// <summary>
    /// Get an inactive CubeView, activate it, and return it.
    /// Auto-grows if the pool is empty.
    /// </summary>
    public CubeView Get()
    {
        if (_available.Count == 0)
            Grow(GrowBatchSize);

        CubeView view = _available.Dequeue();
        view.gameObject.SetActive(true);
        return view;
    }

    /// <summary>
    /// Return a CubeView to the pool. Resets and deactivates it.
    /// Safe to call multiple times on the same view.
    /// </summary>
    public void Return(CubeView view)
    {
        if (view == null) return;
        if (!view.gameObject.activeSelf) return; // Already returned

        view.Reset();
        view.gameObject.SetActive(false);
        _available.Enqueue(view);
    }

    /// <summary>
    /// Return ALL active CubeViews to the pool. Called on level load.
    /// </summary>
    public void ReturnAll()
    {
        foreach (var view in _allInstances)
        {
            if (view != null && view.gameObject.activeSelf)
            {
                view.Reset();
                view.gameObject.SetActive(false);
                _available.Enqueue(view);
            }
        }
    }

    private void Grow(int count)
    {
        for (int i = 0; i < count; i++)
            CreateInstance();
    }

    private void CreateInstance()
    {
        GameObject go = Object.Instantiate(_prefab, _parent);
        CubeView view = go.GetComponent<CubeView>();
        go.SetActive(false);
        _available.Enqueue(view);
        _allInstances.Add(view);
    }
}
