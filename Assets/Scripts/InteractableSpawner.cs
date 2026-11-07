using System.Collections.Generic;
using UnityEngine;

public class InteractableSpawner : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private InteractablePool stationaryCarPool;
    [SerializeField] private InteractablePool movingCarPool;
    [SerializeField] private InteractablePool coinPool;
    [SerializeField] private InteractablePool fuelCanPool;
    [SerializeField] private float laneSpacing = 3.5f;
    [SerializeField] private float spawnDistance = 60f;
    [SerializeField] private float spawnHeight = 0.5f;
    [SerializeField] private float spawnInterval = 2.5f;
    [SerializeField] private float recycleBehindDistance = 45f;
    [SerializeField, Range(0f, 1f)] private float obstacleSpawnChance = 0.5f;

    private readonly List<Interactable> activeItems = new List<Interactable>();
    private float currentSpawnInterval;
    private float currentObstacleSpawnChance;
    private float timeUntilNextSpawn;

    public float BaseSpawnInterval => spawnInterval;
    public float BaseObstacleSpawnChance => obstacleSpawnChance;

    private void Start()
    {
        if (player == null
            || stationaryCarPool == null
            || movingCarPool == null
            || coinPool == null
            || fuelCanPool == null
            || laneSpacing <= 0f
            || spawnDistance <= 0f
            || spawnInterval <= 0f
            || recycleBehindDistance < 0f)
        {
            Debug.LogError("Assign the player and all four pools, and use valid spawn settings.", this);
            enabled = false;
            return;
        }

        ResetForNewRun();
    }

    private void Update()
    {
        RecyclePassedItems();

        timeUntilNextSpawn -= Time.deltaTime;
        if (timeUntilNextSpawn <= 0f)
        {
            SpawnRandomItem();
            timeUntilNextSpawn = currentSpawnInterval;
        }
    }

    public void SetDifficulty(float newSpawnInterval, float newObstacleSpawnChance)
    {
        currentSpawnInterval = Mathf.Max(0.1f, newSpawnInterval);
        currentObstacleSpawnChance = Mathf.Clamp01(newObstacleSpawnChance);
        timeUntilNextSpawn = Mathf.Min(timeUntilNextSpawn, currentSpawnInterval);
    }

    public void ResetForNewRun()
    {
        for (int i = 0; i < activeItems.Count; i++)
        {
            if (activeItems[i] != null)
            {
                activeItems[i].gameObject.SetActive(false);
            }
        }

        activeItems.Clear();
        currentSpawnInterval = spawnInterval;
        currentObstacleSpawnChance = obstacleSpawnChance;
        timeUntilNextSpawn = currentSpawnInterval;
    }

    private void RecyclePassedItems()
    {
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            Interactable item = activeItems[i];

            if (item == null || !item.gameObject.activeSelf)
            {
                activeItems.RemoveAt(i);
                continue;
            }

            if (item.transform.position.z < player.position.z - recycleBehindDistance)
            {
                item.gameObject.SetActive(false);
                activeItems.RemoveAt(i);
            }
        }
    }

    private void SpawnRandomItem()
    {
        InteractablePool pool;
        if (Random.value < currentObstacleSpawnChance)
        {
            pool = Random.value < 0.5f ? stationaryCarPool : movingCarPool;
        }
        else
        {
            pool = Random.value < 0.5f ? coinPool : fuelCanPool;
        }

        Interactable item = pool.Get();
        if (item == null)
        {
            return;
        }

        int lane = Random.Range(0, 3);
        item.transform.position = new Vector3(
            (lane - 1) * laneSpacing,
            spawnHeight,
            player.position.z + spawnDistance);
        item.transform.rotation = Quaternion.identity;
        activeItems.Add(item);
    }
}
