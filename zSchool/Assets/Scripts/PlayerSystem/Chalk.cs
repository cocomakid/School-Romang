using UnityEngine;

public class Chalk : MonoBehaviour
{
    public float speed = 20f;
    public int damage = 4;
    public GameObject chalkPrefab;

    public bool isThrown = true;
    private Rigidbody rb;
    private Collider col;
    private float spawnTime;

    private readonly Vector3 baseChalkScale = new Vector3(0.15f, 0.2f, 0.15f);

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
            if (col != null)
            {
                col.isTrigger = true;
            }

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
        // 💡 [핵심] 고속으로 날아갈 때 Raycast로 몬스터 / 벽 / 바닥 충돌을 모두 정확히 예측 판정
        if (isThrown && rb != null)
        {
            float moveDistance = rb.linearVelocity.magnitude * Time.deltaTime;

            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, moveDistance + 0.15f))
            {
                // 플레이어나 다른 날아가는 분필과의 충돌만 무시
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

        // 플레이어 본인 및 다른 Chalk 충돌 무시
        if (other.CompareTag("Player") || other.GetComponent<Chalk>() != null) return;

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        HandleImpact(other, hitPoint);
    }

    // 💡 몬스터, 벽, 바닥 등 무엇에 부딪혀도 통과하지 않고 즉시 처리
    private void HandleImpact(Collider other, Vector3 hitPoint)
    {
        if (!isThrown) return;

        // 1. 몬스터 피격 시 즉시 데미지 전달
        if (other.CompareTag("Monster"))
        {
            Monster monster = other.GetComponent<Monster>();
            if (monster == null)
            {
                // 부모 오브젝트에 Monster 컴포넌트가 있는 경우 체크 (몬스터 자식 콜라이더 대응)
                monster = other.GetComponentInParent<Monster>();
            }

            if (monster != null)
            {
                monster.TakeDamage(damage);
                Debug.Log($"몬스터 피격! 데미지: {damage}");
            }
        }

        // 2. 부딪힌 즉시 쪼개지고 현재 분필 소멸 (통과 절대 불가)
        SplitChalkAtPosition(hitPoint);
    }

    public void SplitChalkAtPosition(Vector3 spawnCenter)
    {
        // 쪼개짐 처리 (4 -> 2 -> 1 -> 삭제)
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
        for (int i = 0; i < count; i++)
        {
            float sideOffset = (i == 0) ? -0.1f : 0.1f;
            Vector3 spawnOffset = new Vector3(
                sideOffset + Random.Range(-0.02f, 0.02f),
                0.2f,
                Random.Range(-0.02f, 0.02f)
            );

            Vector3 targetSpawnPos = spawnCenter + spawnOffset;

            // 벽 내부 스폰 차단 보정
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

            GameObject newChalkObj = Instantiate(chalkPrefab, targetSpawnPos, Quaternion.identity);

            Chalk newChalk = newChalkObj.GetComponent<Chalk>();
            if (newChalk != null)
            {
                newChalk.damage = nextDamage;
                newChalk.chalkPrefab = chalkPrefab;
                newChalk.isThrown = false; // 떨어진 잔해 상태
            }

            float scaleRatio = (float)nextDamage / 4f;
            float finalScaleMultiplier = Mathf.Max(scaleRatio, 0.45f);
            newChalkObj.transform.localScale = baseChalkScale * finalScaleMultiplier;

            Rigidbody newRb = newChalkObj.GetComponent<Rigidbody>();
            Collider newCol = newChalkObj.GetComponent<Collider>();

            if (newCol != null)
            {
                newCol.isTrigger = false;
            }

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
        // 바닥에 떨어진 쪼개진 분필이 벽에 비벼질 때 속도 초기화
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

        if (col != null)
        {
            col.isTrigger = false;
        }
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