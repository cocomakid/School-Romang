
using UnityEngine;

public class Monster : MonoBehaviour
{
    [Header("Monster Status")]
    public int maxHp = 10;
    private int currentHp;

    [Header("Player Tracking")]
    public Transform player;
    public float moveSpeed = 3f;
    public float stopDistance = 1.5f;

    void Start()
    {
        currentHp = maxHp;

        // Player 태그를 가진 오브젝트 찾기
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
        FollowPlayer();
    }

    // 플레이어 추적
    private void FollowPlayer()
    {
        if (player == null)
            return;

        // 플레이어와의 거리 계산
        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        // 일정 거리 안에 들어오면 이동 중지
        if (distance <= stopDistance)
            return;

        // 플레이어 방향 계산 (Y축 회전 제외)
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            direction.Normalize();

            // 플레이어 방향 바라보기
            transform.rotation = Quaternion.LookRotation(direction);

            // 플레이어를 향해 이동
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
    }

    // 분필에 맞았을 때 호출
    public void TakeDamage(int damage)
    {
        currentHp -= damage;

        Debug.Log($"몬스터가 {damage} 데미지를 받았습니다! (남은 HP: {currentHp}/{maxHp})");

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("몬스터 처치 완료!");
        Destroy(gameObject);
    }
}
