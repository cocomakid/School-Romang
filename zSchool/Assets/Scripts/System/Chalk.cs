using UnityEngine;

public class Chalk : MonoBehaviour
{
    public float speed = 20f;
    public int damage = 4;
    public GameObject chalkPrefab;

    private bool isThrown = true;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        if (isThrown && rb != null)
        {
            rb.linearVelocity = transform.forward * speed;
            rb.useGravity = false; // 공중에서 일자로 날아감
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isThrown) return;

        // 몬스터 피격 시
        if (other.CompareTag("Monster"))
        {
            // 몬스터 데미지 전달
            Monster monster = other.GetComponent<Monster>();
            if (monster != null)
            {
                monster.TakeDamage(damage);
            }

            // 부딪힌 지점에서 쪼개짐
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            SplitChalkAtPosition(hitPoint);
        }
        else if (other.CompareTag("Wall") || other.CompareTag("Ground"))
        {
            DropToGround();
        }
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
        else
        {
            Debug.Log("분필이 산산조각 났습니다!"); // 데미지 1은 완전 파괴
        }

        Destroy(gameObject);
    }

    private void SpawnSplitChalks(Vector3 spawnCenter, int nextDamage, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-0.3f, 0.3f), 0.2f, Random.Range(-0.3f, 0.3f));
            GameObject newChalkObj = Instantiate(chalkPrefab, spawnCenter + offset, Quaternion.identity);

            Chalk newChalk = newChalkObj.GetComponent<Chalk>();
            newChalk.damage = nextDamage;
            newChalk.isThrown = false; // 바닥에 떨어진 상태

            float scaleRatio = (float)nextDamage / 4f;
            newChalkObj.transform.localScale = Vector3.one * Mathf.Max(scaleRatio, 0.4f);

            Rigidbody newRb = newChalkObj.GetComponent<Rigidbody>();
            if (newRb != null)
            {
                newRb.useGravity = true;
                newRb.linearVelocity = new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(1f, 2f), Random.Range(-1.5f, 1.5f));
            }
        }
    }

    private void DropToGround()
    {
        isThrown = false;
        if (rb != null)
        {
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
        }
    }

    public bool CanPickUp() => !isThrown;
}