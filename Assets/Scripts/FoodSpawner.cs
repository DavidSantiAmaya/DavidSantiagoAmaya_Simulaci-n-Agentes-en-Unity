using UnityEngine;

public class FoodSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject foodPrefab;
    public float spawnInterval = 2f;
    public int maxFood = 50;

    [Header("Spawn Area (Rectangular)")]
    public Vector2 areaSize = new Vector2(20, 20);

    private float time = 0f;

    public void Simulate(float h)
    {
        time += h;

        if (time >= spawnInterval)
        {
            time = 0f;

            if (CountFood() < maxFood)
            {
                SpawnFood();
            }
        }
    }

    void SpawnFood()
    {
        Vector2 spawnPos = new Vector2(
            Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
            Random.Range(-areaSize.y / 2f, areaSize.y / 2f)
        );

        spawnPos += (Vector2)transform.position;

        Instantiate(foodPrefab, spawnPos, Quaternion.identity);
    }

    int CountFood()
    {
        return FindObjectsByType<Food>(FindObjectsSortMode.InstanceID).Length;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(areaSize.x, areaSize.y, 1));
    }
}
