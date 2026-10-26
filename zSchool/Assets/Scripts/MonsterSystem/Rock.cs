using UnityEngine;

public class Rock : MonoBehaviour
{
    public int damage = 2;          // 돌의 공격력
    public float lifeTime = 5f;     // 자동 파괴 시간

    void Start()
    {
        // 5초 후 자동 파괴
        Destroy(gameObject, lifeTime);
    }

    // Is Trigger가 체크된 상태에서 감지
    private void OnTriggerEnter(Collider other)
    {
        // 몬스터 자신이나 몬스터 자식 오브젝트면 무시
        if (other.CompareTag("Monster") || other.transform.root.CompareTag("Monster"))
        {
            return;
        }

        // 플레이어와 부딪힌 경우
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damage);
                Debug.Log($"플레이어가 돌에 맞아 {damage} 데미지를 입었습니다!");
            }

            Destroy(gameObject);
            return;
        }

        // 지형(바닥/벽 등)에 부딪히면 파괴
        Destroy(gameObject);
    }
}