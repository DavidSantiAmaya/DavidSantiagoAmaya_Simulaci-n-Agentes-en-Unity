using UnityEngine;

public class Bunny : MonoBehaviour
{
    [Header("Bunny Settings")]
    public float energy = 10f;
    public float age = 0f;
    public float maxAge = 20f;
    public float speed = 1f;
    public float visionRange = 5f;

    [Header("Sleep Settings")]
    public float sleepWakeEnergy = 100f;   // despierta al llegar a 100

    [Header("Bunny States")]
    public bool isAlive = true;
    public BunnyState currentState = BunnyState.Exploring;

    private Vector3 destination;
    private float h;

    private void Start()
    {
        destination = transform.position;
    }

    public void Simulate(float h)
    {
        if (!isAlive) return;

        this.h = h;

        EvaluateState();

        switch (currentState)
        {
            case BunnyState.Exploring:
                Explore();
                break;
            case BunnyState.SearchingFood:
                SearchFood();
                break;
            case BunnyState.Eating:
                Eat();
                break;
            case BunnyState.Fleeing:
                Flee();
                break;
            case BunnyState.Sleeping:
                Sleep();
                break;
        }

        Move();
        Age();
        CheckState();
    }

    void EvaluateState()
    {
        // Si está durmiendo, solo se despierta si ya tiene suficiente energía
        // o se interrumpe si hay un depredador cerca
        if (currentState == BunnyState.Sleeping)
        {
            if (PredatorInRange())
            {
                currentState = BunnyState.Fleeing;
                return;
            }

            if (energy < sleepWakeEnergy)
            {
                return;
            }

            currentState = BunnyState.Exploring;
        }

        // 1. Si hay un depredador cerca -> huir
        if (PredatorInRange())
        {
            currentState = BunnyState.Fleeing;
            return;
        }

        // 2. Si la energía está en 0 -> dormir
        if (energy <= 0f)
        {
            currentState = BunnyState.Sleeping;
            return;
        }

        // 3. Si la energía está baja -> buscar comida
        if (energy < 500f)
        {
            Food nearestFood = FindNearestFood();
            if (nearestFood != null)
            {
                currentState = BunnyState.SearchingFood;
                destination = nearestFood.transform.position;
            }
        }

        // 4. Si está encima de la comida -> comer
        Collider2D foodHit = Physics2D.OverlapCircle(transform.position, 0.2f, LayerMask.GetMask("Food"));
        if (foodHit != null)
        {
            Food food = foodHit.GetComponent<Food>();
            if (food != null)
            {
                currentState = BunnyState.Eating;
                return;
            }
        }

        // 5. Si no pasa nada -> explorar
        if (currentState != BunnyState.Eating)
        {
            currentState = BunnyState.Exploring;
        }
    }

    void Explore()
    {
        Food nearestFood = FindNearestFood();
        if (nearestFood != null)
        {
            currentState = BunnyState.SearchingFood;
            destination = nearestFood.transform.position;
            return;
        }

        if (Vector3.Distance(transform.position, destination) < 0.1f)
        {
            SelectNewDestination();
        }
    }

    void SearchFood()
    {
        Food nearestFood = FindNearestFood();
        if (nearestFood == null)
        {
            currentState = BunnyState.Exploring;
            return;
        }

        destination = nearestFood.transform.position;

        if (Vector3.Distance(transform.position, nearestFood.transform.position) < 0.2f)
        {
            currentState = BunnyState.Eating;
        }
    }

    void Eat()
    {
        Collider2D foodHit = Physics2D.OverlapCircle(transform.position, 0.2f, LayerMask.GetMask("Food"));
        if (foodHit != null)
        {
            Food food = foodHit.GetComponent<Food>();
            if (food != null)
            {
                energy += food.nutrition;
                Destroy(food.gameObject);
            }
        }

        currentState = BunnyState.Exploring;
    }

    void Flee()
    {
        Vector3 fleeDir = (transform.position - GetNearestPredatorPosition()).normalized;

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            fleeDir,
            visionRange,
            LayerMask.GetMask("Obstacles", "Water")
        );

        if (hit.collider != null)
        {
            float offset = transform.localScale.magnitude * 0.5f;
            destination = hit.point - (Vector2)fleeDir * offset;
        }
        else
        {
            destination = transform.position + fleeDir * visionRange;
        }

        // Se mantiene en estado de huida mientras el depredador siga cerca
        // y se reevaluará en la siguiente simulación.
    }

    void Sleep()
    {
        destination = transform.position;

        // Recupera 10 por segundo de simulación
        energy = Mathf.Min(sleepWakeEnergy, energy + 10f * h);

        // Se despierta al llegar a 100
        if (energy >= sleepWakeEnergy)
        {
            currentState = BunnyState.Exploring;
        }
    }

    void SelectNewDestination()
    {
        Vector3 direction = new Vector3(
            Random.Range(-visionRange, visionRange),
            Random.Range(-visionRange, visionRange),
            0f
        );

        Vector3 targetPoint = transform.position + direction;

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            direction.normalized,
            visionRange,
            LayerMask.GetMask("Obstacles", "Water")
        );

        if (hit.collider != null)
        {
            float offset = transform.localScale.magnitude * 0.5f;
            destination = hit.point - (Vector2)direction.normalized * offset;
        }
        else
        {
            destination = targetPoint;
        }
    }

    void Move()
    {
        if (currentState == BunnyState.Sleeping)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            destination,
            speed * h
        );

        energy = Mathf.Max(0f, energy - speed * h);
    }

    void Age()
    {
        age += h;
    }

    void CheckState()
    {
        if (age > maxAge)
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

    bool PredatorInRange()
    {
        Collider2D predator = Physics2D.OverlapCircle(
            transform.position,
            visionRange,
            LayerMask.GetMask("Foxes")
        );

        return predator != null;
    }

    Vector3 GetNearestPredatorPosition()
    {
        Collider2D[] predators = Physics2D.OverlapCircleAll(
            transform.position,
            visionRange,
            LayerMask.GetMask("Foxes")
        );

        float minDist = Mathf.Infinity;
        Vector3 pos = transform.position;

        foreach (var p in predators)
        {
            float dist = Vector2.Distance(transform.position, p.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                pos = p.transform.position;
            }
        }

        return pos;
    }

    Food FindNearestFood()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            visionRange,
            LayerMask.GetMask("Food")
        );

        Food nearest = null;
        float minDist = Mathf.Infinity;

        foreach (Collider2D hit in hits)
        {
            Food food = hit.GetComponent<Food>();
            if (food == null) continue;

            Vector3 direction = food.transform.position - transform.position;
            float distanceToFood = direction.magnitude;

            RaycastHit2D obstacleHit = Physics2D.Raycast(
                transform.position,
                direction.normalized,
                distanceToFood,
                LayerMask.GetMask("Obstacles", "Water")
            );

            if (obstacleHit.collider != null)
                continue;

            if (distanceToFood < minDist)
            {
                minDist = distanceToFood;
                nearest = food;
            }
        }

        return nearest;
    }
}