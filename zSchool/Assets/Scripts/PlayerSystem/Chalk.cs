using UnityEngine;

public class Chalk : MonoBehaviour
{
    public float speed = 20f;
    public int damage = 4;

    // 👇 이 줄을 다시 추가해주시면 에러가 바로 해결됩니다!
    public GameObject chalkPrefab;

    [Header("분필 크기별 프리팹 설정")]
    public GameObject largeChalkPrefab;
    public GameObject mediumChalkPrefab;
    public GameObject smallChalkPrefab;

    public bool isThrown = true;
    private Rigidbody rb;
    private Collider col;
    private float spawnTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    void Start()
    {
        spawnTime = Time.time;

        if (isThrown)
        {
            if (col != null) col.isTrigger = true;

            if (rb != null)
            {
                rb.useGravity = false;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.linearVelocity = transform.forward * speed;
            }

            IgnorePlayerCollision();
        }
        else
        {
            DropToGround();
        }
    }

    private void IgnorePlayerCollision()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            Collider[] playerColliders = playerObj.GetComponentsInChildren<Collider>();
            if (col != null)
            {
                foreach (var pCol in playerColliders)
                {
                    Physics.IgnoreCollision(col, pCol, true);
                }
            }
        }
    }

    void Update()
    {
        if (isThrown && rb != null)
        {
            float moveDistance = rb.linearVelocity.magnitude * Time.deltaTime;

            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, moveDistance + 0.15f))
            {
                if (!hit.collider.CompareTag("Player") && hit.collider.GetComponent<Chalk>() == null)
                {
                    HandleImpact(hit.collider, hit.point);
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isThrown) return;
        if (other.CompareTag("Player") || other.GetComponent<Chalk>() != null) return;

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        HandleImpact(other, hitPoint);
    }

    private void HandleImpact(Collider other, Vector3 hitPoint)
    {
        if (!isThrown) return;

        if (other.CompareTag("Monster"))
        {
            Monster monster = other.GetComponent<Monster>();
            if (monster == null) monster = other.GetComponentInParent<Monster>();

            if (monster != null)
            {
                monster.TakeDamage(damage);
                Debug.Log($"몬스터 피격! 데미지: {damage}");
            }
        }

        SplitChalkAtPosition(hitPoint);
    }

    public void SplitChalkAtPosition(Vector3 spawnCenter)
    {
        if (damage == 4)
        {
            SpawnSplitChalks(spawnCenter, nextDamage: 2, count: 2);
        }
        else if (damage == 2)
        {
            SpawnSplitChalks(spawnCenter, nextDamage: 1, count: 1);
        }

        Destroy(gameObject);
    }

    private void SpawnSplitChalks(Vector3 spawnCenter, int nextDamage, int count)
    {
        GameObject prefabToUse = (nextDamage == 2) ? mediumChalkPrefab : smallChalkPrefab;
        if (prefabToUse == null) prefabToUse = largeChalkPrefab;

        for (int i = 0; i < count; i++)
        {
            float sideOffset = (i == 0) ? -0.1f : 0.1f;
            Vector3 spawnOffset = new Vector3(
                sideOffset + Random.Range(-0.02f, 0.02f),
                0.2f,
                Random.Range(-0.02f, 0.02f)
            );

            Vector3 targetSpawnPos = spawnCenter + spawnOffset;

            Collider[] wallColliders = Physics.OverlapSphere(targetSpawnPos, 0.15f, LayerMask.GetMask("Wall", "Default"));
            foreach (var wallCol in wallColliders)
            {
                if (wallCol.CompareTag("Wall"))
                {
                    Vector3 closestOnWall = wallCol.ClosestPoint(targetSpawnPos);
                    Vector3 pushOutDir = (targetSpawnPos - closestOnWall).normalized;
                    if (pushOutDir == Vector3.zero) pushOutDir = Vector3.up;

                    targetSpawnPos = closestOnWall + pushOutDir * 0.25f;
                }
            }

            GameObject newChalkObj = Instantiate(prefabToUse, targetSpawnPos, Quaternion.identity);

            Chalk newChalk = newChalkObj.GetComponent<Chalk>();
            if (newChalk != null)
            {
                newChalk.damage = nextDamage;
                newChalk.largeChalkPrefab = largeChalkPrefab;
                newChalk.mediumChalkPrefab = mediumChalkPrefab;
                newChalk.smallChalkPrefab = smallChalkPrefab;
                newChalk.isThrown = false;
            }

            Rigidbody newRb = newChalkObj.GetComponent<Rigidbody>();
            Collider newCol = newChalkObj.GetComponent<Collider>();

            if (newCol != null) newCol.isTrigger = false;

            if (newRb != null)
            {
                newRb.useGravity = true;
                newRb.collisionDetectionMode = CollisionDetectionMode.Continuous;

                float forceX = (i == 0) ? -0.4f : 0.4f;
                newRb.linearVelocity = new Vector3(
                    forceX,
                    Random.Range(1.2f, 1.8f),
                    Random.Range(-0.1f, 0.1f)
                );
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isThrown && collision.gameObject.CompareTag("Wall"))
        {
            if (rb != null)
            {
                rb.linearVelocity = new Vector3(0f, -1f, 0f);
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    public void DropToGround()
    {
        isThrown = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        if (col != null) col.isTrigger = false;
    }

    public bool CanPickUp() => !isThrown;

    private void OnDestroy()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.Invoke(nameof(PlayerController.CheckAndRefillChalks), 0.05f);
        }
    }
}