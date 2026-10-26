using UnityEngine;

public class RangedMonster : MonoBehaviour
{
    [Header("Monster Status")]
    public int maxHp = 10;
    private int currentHp;

    [Header("Player Tracking")]
    public Transform player;
    public float moveSpeed = 3f;
    public float attackRange = 7f;      // 공격 시도 거리
    public float stopDistance = 5f;      // 이동을 멈추는 거리

    [Header("Ranged Attack Settings")]
    public GameObject rockPrefab;        // 돌 프리팹
    public Transform attackPoint;        // 돌 생성 위치
    public float attackCooldown = 2f;    // 공격 간격
    public float rockForce = 20f;        // 돌 날아가는 속도/힘
    public float attackDelay = 0.5f;     // 공격 후 멈춰있는 시간 (선후딜)

    private float lastAttackTime;
    private bool isAttacking = false;    // 공격 중 이동 방지 플래그

    void Start()
    {
        currentHp = maxHp;

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }
    }

    void Update()
    {
        if (player == null) return;

        // 플레이어는 항상 바라봄
        LookAtPlayer();

        // 공격 중일 때는 이동하지 않음
        if (isAttacking) return;

        float distance = Vector3.Distance(transform.position, player.position);

        // 1. 공격 사거리 이내 진입 시 공격 수행
        if (distance <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            StartCoroutine(AttackRoutine());
            return;
        }

        // 2. 적정 거리보다 멀리 있다면 이동
        if (distance > stopDistance)
        {
            MoveToPlayer();
        }
    }

    private void LookAtPlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void MoveToPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0f;
        transform.position += direction * moveSpeed * Time.deltaTime;
    }

    // 공격 루틴 (멈춰서 돌 던지기)
    private System.Collections.IEnumerator AttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        // 돌 던지기
        ThrowRock();

        // 공격 후딜레이 동안 몬스터 이동 멈춤
        yield return new WaitForSeconds(attackDelay);

        isAttacking = false;
    }

    private void ThrowRock()
    {
        if (rockPrefab == null) return;

        // attackPoint가 있으면 그 위치, 없으면 몬스터 앞쪽
        Vector3 spawnPos = attackPoint != null
            ? attackPoint.position
            : transform.position + transform.forward * 1.2f + Vector3.up * 1.0f;

        GameObject rock = Instantiate(rockPrefab, spawnPos, Quaternion.identity);

        // Y축 높이를 동일하게 맞춰 하늘이나 바닥으로 꺾이지 않고 수평으로 조준
        Vector3 targetPosition = player.position;
        targetPosition.y = spawnPos.y;

        Vector3 throwDirection = (targetPosition - spawnPos).normalized;

        Rigidbody rockRb = rock.GetComponent<Rigidbody>();
        if (rockRb != null)
        {
            rockRb.linearVelocity = throwDirection * rockForce;
        }
    }

    public void TakeDamage(int damage)
    {
        currentHp -= damage;
        Debug.Log($"몬스터 HP: {currentHp}/{maxHp}");

        if (currentHp <= 0)
        {
            Destroy(gameObject);
        }
    }
}