using EditorAttributes;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ObjectPool
{
    [Tooltip("The prefab to pool.")]
    public GameObject prefabObject;
    [Tooltip("The number of prefabs to create on startup.")]
    public int defaultPoolDepth = 1;
    [Tooltip("Whether or not more prefabs can be created beyond the initial Pool Depth.")]
    public bool canGrow = true;
    [Tooltip("The transform the Objects will be parented under. (Defaults to the scene.)")]
    public Transform parent = null;
    [Tooltip("How long an instance will last until it is automatically disabled, set to -1 to never auto disable.")]
    public float autoDisableTime = -1;
    [Tooltip("The point where the object will appear when pumped.")]
    public Transform spawnPoint;
    [Tooltip("Whether the object will match the rotation of its spawnPoint.")]
    public bool rotate;
    [Tooltip("Whether or not and at what rate the Pool will automatically spawn its charactetrs.")]
    public float autoSpawnRate;

    private readonly List<PoolableObject> poolList = new();
    private int currentActiveObjects = 0;
    private int currentPooledObjects = 0;
    private int currentSelection = 0;
    private bool initialized;
    private float autoSpawnTimer;

    public int ActiveObjects() => currentActiveObjects;

    public void Update()
    {
        if(autoSpawnRate > 0)
        {
            autoSpawnTimer += Time.deltaTime;
            if(autoSpawnTimer >= autoSpawnRate)
            {
                autoSpawnTimer %= autoSpawnRate;
                Pump();
            }
        }
        if (autoDisableTime > 0) for (int i = 0; i < poolList.Count; i++)
            {
                if (poolList[i].Active) poolList[i].timeExisting += Time.deltaTime;
                if (poolList[i].timeExisting >= autoDisableTime) poolList[i].gameObject.SetActive(false);
            }
    }

    public void Initialize()
    {
        if (initialized) return;
        for (int i = 0; i < defaultPoolDepth; i++) NewInstance();
        initialized = true;
    }


    public PoolableObject Pump()
    {
        if (!initialized) Initialize();
        if (!FindNextInstance()) return null;
        PoolableObject instance = ActivateInstance(poolList[currentSelection]);
        IncrementSelection();
        return instance;
    }

    private void NewInstance()
    {
        GameObject pooledObject = Object.Instantiate(prefabObject);

        PoolableObject poolable;
        if (!pooledObject.TryGetComponent(out poolable)) poolable = pooledObject.AddComponent<PoolableObject>();
        poolable.transform.parent = parent;
        poolable.pool = this;
        poolable.onDeactivate += OnDeActivate;
        poolList.Add(poolable);
        currentPooledObjects++;
        //currentActiveObjects++;
        pooledObject.SetActive(false);
    }

    private bool FindNextInstance()
    {
        if (!poolList[currentSelection].Active) return true;
        if (currentActiveObjects >= currentPooledObjects)
        {
            if (!canGrow) return false;

            NewInstance();
            currentSelection = currentPooledObjects - 1;
        }
        int safetyCounter = 0;
        while (poolList[currentSelection].Active)
        {
            IncrementSelection();
            safetyCounter++;
            if (safetyCounter > defaultPoolDepth * 1000) return false;
        }
        return true;
    }

    private void IncrementSelection() => currentSelection = (currentSelection == currentPooledObjects - 1) ? 0 : currentSelection + 1;

    private PoolableObject ActivateInstance(PoolableObject instance)
    {
        instance.gameObject.SetActive(true);
        instance.Active = true;
        currentActiveObjects++;
        instance.timeExisting = 0;

        if(spawnPoint != null)
        {
            if (rotate) instance.PlaceAtMuzzle(spawnPoint);
            else
            {
                instance.SetPosition(spawnPoint.position);
                instance.SetRotation(Vector3.zero);
            }
        }

        return instance;
    }

    private void OnDeActivate(PoolableObject instance)
    {
        currentActiveObjects--;
        instance.Active = false;
    }

    
    private void Destroy()
    {
        for (int i = 0; i < poolList.Count; i++)
        {
            poolList[i].Disable(false);
            poolList[i].onDeactivate -= OnDeActivate;
            Object.Destroy(poolList[i].gameObject);
        }
        poolList.Clear();
    }
    

}