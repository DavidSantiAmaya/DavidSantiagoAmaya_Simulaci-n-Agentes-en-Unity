using UnityEngine;

public class Predator : MonoBehaviour
{
    [Header("Predator Settings")]
    public float energy = 10;
    public float age = 0;
    public float maxAge = 20;
    public float speed = 1f;
    public float visionRange = 5f;

    [Header("Water Settings")]
    [Range(0f, 100f)]
    public float waterCrossChance = 50f;

    [Header("Predator States")]
    public bool isAlive = true;
    public PredatorState currentState = PredatorState.Exploring;

    private Vector3 destination;
    private float h;

    private Bunny targetBunny;
    private bool waterDecisionMade = false;
    private bool canCrossWaterThisSearch = false;

    // Mientras este valor sea true, el zorro explora sin volver a detectar comida.
    private bool ignoreFoodUntilExplorePoint = false;

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
        if (ignoreFoodUntilExplorePoint)
        {
            if (Vector3.Distance(transform.position, destination) < 0.1f)
            {
                ignoreFoodUntilExplorePoint = false;
                SelectNewDestination();
            }
            return;
        }

        Bunny nearestBunny = FindNearestBunny();

        if (nearestBunny != null)
        {
            targetBunny = nearestBunny;
            currentState = PredatorState.SearchingFood;
            waterDecisionMade = false;
            canCrossWaterThisSearch = false;
            return;
        }

        if (Vector3.Distance(transform.position, destination) < 0.1f)
        {
            SelectNewDestination();
        }
    }

    void SearchFood()
    {
        if (targetBunny == null)
        {
            AbortHuntAndExplore();
            return;
        }

        Vector2 origin = transform.position;
        Vector2 target = targetBunny.transform.position;
        Vector2 dir = target - origin;
        float dist = dir.magnitude;

        if (dist <= 0.001f)
        {
            currentState = PredatorState.Eating;
            return;
        }

        Vector2 dirNorm = dir / dist;

        RaycastHit2D obstacleHit = Physics2D.Raycast(
            origin,
            dirNorm,
            dist,
            LayerMask.GetMask("Obstacles")
        );

        if (obstacleHit.collider != null)
        {
            AbortHuntAndExplore();
            return;
        }

        RaycastHit2D waterHit = Physics2D.Raycast(
            origin,
            dirNorm,
            dist,
            LayerMask.GetMask("Water")
        );

        if (waterHit.collider != null)
        {
            if (!waterDecisionMade)
            {
                canCrossWaterThisSearch = Random.Range(0f, 100f) < waterCrossChance;
                waterDecisionMade = true;
            }

            if (!canCrossWaterThisSearch)
            {
                AbortHuntAndExplore();
                return;
            }
        }

        destination = targetBunny.transform.position;

        if (Vector3.Distance(transform.position, targetBunny.transform.position) < 0.2f)
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
            Bunny food = foodHit.GetComponent<Bunny>();
            if (food != null)
            {
                energy += food.age;
                Destroy(food.gameObject);
            }
        }

        ResetSearch();
        currentState = PredatorState.Exploring;
    }

    void AbortHuntAndExplore()
    {
        ResetSearch();
        currentState = PredatorState.Exploring;
        ignoreFoodUntilExplorePoint = true;
        SelectNewDestination();
    }

    void ResetSearch()
    {
        targetBunny = null;
        waterDecisionMade = false;
        canCrossWaterThisSearch = false;
    }

    void SelectNewDestination()
    {
        Vector3 direction = new Vector3(
            Random.Range(-visionRange, visionRange),
            Random.Range(-visionRange, visionRange),
            0
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
        Vector3 nextPosition = Vector3.MoveTowards(
            transform.position,
            destination,
            speed * h
        );

        Vector3 moveDir = nextPosition - transform.position;
        float moveDist = moveDir.magnitude;

        if (moveDist > 0.0001f)
        {
            int mask = LayerMask.GetMask("Obstacles");

            bool allowWater =
                currentState == PredatorState.SearchingFood &&
                canCrossWaterThisSearch;

            if (!allowWater)
            {
                mask |= LayerMask.GetMask("Water");
            }

            RaycastHit2D hit = Physics2D.Raycast(
                transform.position,
                moveDir.normalized,
                moveDist,
                mask
            );

            if (hit.collider != null)
            {
                float offset = 0.05f;
                nextPosition = (Vector3)hit.point - moveDir.normalized * offset;
            }
        }

        transform.position = nextPosition;
        energy -= speed * h;
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
            Bunny bunny = hit.GetComponent<Bunny>();
            if (bunny == null) continue;

            Vector3 direction = bunny.transform.position - transform.position;
            float dist = direction.magnitude;

            RaycastHit2D obstacleHit = Physics2D.Raycast(
                transform.position,
                direction.normalized,
                dist,
                LayerMask.GetMask("Obstacles", "Water")
            );

            if (obstacleHit.collider != null)
                continue;

            if (dist < minDist)
            {
                minDist = dist;
                nearest = bunny;
            }
        }

        return nearest;
    }
}
