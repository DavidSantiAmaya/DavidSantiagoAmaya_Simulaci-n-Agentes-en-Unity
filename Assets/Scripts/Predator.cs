using UnityEngine;

public class Predator : MonoBehaviour
{
    [Header("Predator Settings")]
    public float energy = 10;
    public float age = 0;
    public float maxAge = 20;
    public float speed = 1f;
    public float visionRange = 5f;

    [Header("Predator States")]
    public bool isAlive = true;
    public PredatorState currentState = PredatorState.Exploring;

    private Vector3 destination;
    private float h;

    [Range(0f, 100f)]
    public float waterCrossChance = 50f;

    private void Start()
    {
        destination = transform.position;
    }

    public void Simulate(float h)
    {
        if (!isAlive) return;

        this.h = h;

        switch (currentState)
        {
            case PredatorState.Exploring:
                Explore();
                break;
            case PredatorState.SearchingFood:
                SearchFood();
                break;
            case PredatorState.Eating:
                Eat();
                break;
        }

        Move();
        Age();
        CheckState();
    }

    void Explore()
    {
        Bunny nearestBunny = FindNearestBunny();

        if (nearestBunny != null)
        {
            currentState = PredatorState.SearchingFood;
            destination = nearestBunny.transform.position;
            return;
        }

        if (Vector3.Distance(transform.position, destination) < 0.1f)
        {
            SelectNewDestination();
        }
    }

    void SearchFood()
    {
        Bunny nearestBunny = FindNearestBunny();

        if (nearestBunny == null)
        {
            currentState = PredatorState.Exploring;
            return;
        }

        destination = nearestBunny.transform.position;

        if (Vector3.Distance(transform.position, nearestBunny.transform.position) < 0.2f)
        {
            currentState = PredatorState.Eating;
        }
    }

    void Eat()
    {
        Collider2D foodHit = Physics2D.OverlapCircle(
            transform.position,
            0.2f,
            LayerMask.GetMask("Bunnies")
        );

        if (foodHit != null)
        {
            // 🔹 CORRECCIÓN: buscar en el padre por si el collider está en un hijo
            Bunny food = foodHit.GetComponentInParent<Bunny>();

            if (food != null)
            {
                energy += food.age;
                Destroy(food.gameObject);
            }
        }

        currentState = PredatorState.Exploring;
    }

    void SelectNewDestination()
    {
        Vector3 direction = new Vector3(
            Random.Range(-visionRange, visionRange),
            Random.Range(-visionRange, visionRange),
            0
        );

        // 🔹 CORRECCIÓN: evitar vector cero
        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Vector3 targetPoint = transform.position + direction;

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            direction.normalized,
            visionRange,
            LayerMask.GetMask("Obstacles", "Water")
        );

        if (hit.collider != null)
        {
            float offset = 0.5f; // 🔹 Mejor que usar scale
            destination = hit.point - (Vector2)direction.normalized * offset;
        }
        else
        {
            destination = targetPoint;
        }
    }

    void Move()
    {
        float distance = Vector3.Distance(transform.position, destination);

        if (distance > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                destination,
                speed * h
            );

            energy -= speed * h;
        }
    }

    void Age()
    {
        age += h;
    }

    void CheckState()
    {
        if (energy <= 0 || age > maxAge)
        {
            isAlive = false;
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(destination, 0.2f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, destination);
    }

    Bunny FindNearestBunny()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            visionRange,
            LayerMask.GetMask("Bunnies")
        );

        Bunny nearest = null;
        float minDist = Mathf.Infinity;

        foreach (Collider2D hit in hits)
        {
            // 🔹 CORRECCIÓN: usar GetComponentInParent
            Bunny bunny = hit.GetComponentInParent<Bunny>();

            if (bunny != null)
            {
                Vector3 direction = bunny.transform.position - transform.position;

                // 🔹 CORRECCIÓN: raycast solo hasta el conejo
                RaycastHit2D obstacleHit = Physics2D.Raycast(
                    transform.position,
                    direction.normalized,
                    direction.magnitude,
                    LayerMask.GetMask("Obstacles", "Water")
                );

                if (obstacleHit.collider != null)
                {
                    if (obstacleHit.collider.gameObject.layer == LayerMask.NameToLayer("Water"))
                    {
                        float random = Random.Range(0f, 100f);

                        if (random > waterCrossChance)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }

                float dist = Vector2.Distance(transform.position, bunny.transform.position);

                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = bunny;
                }
            }
        }

        return nearest;
    }
}