using UnityEngine;

public class Rock : MonoBehaviour
{
    public int damage = 2;          // 돌의 공격력
    public float lifeTime = 5f;     // 허공에 날아갔을 때 자동 파괴 시간

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    // 돌(Stone)의 Collider에서 'Is Trigger'가 체크되어 있을 때 실행됩니다.
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
            // PlayerController 컴포넌트를 가져와 TakeDamage 호출!
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damage);
                Debug.Log($"플레이어가 돌에 맞아 {damage} 데미지를 입었습니다!");
            }

            Destroy(gameObject);
            return;
        }

        // 벽이나 바닥 등 지형에 부딪히면 파괴
        Destroy(gameObject);
    }
}