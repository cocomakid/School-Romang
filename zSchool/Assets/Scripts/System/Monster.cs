using UnityEngine;

public class Monster : MonoBehaviour
{
    [Header("Monster Status")]
    public int maxHp = 10;
    private int currentHp;

    void Start()
    {
        currentHp = maxHp;
    }

    // Chalk.cs에서 부딪혔을 때 호출하는 데미지 처리 함수
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