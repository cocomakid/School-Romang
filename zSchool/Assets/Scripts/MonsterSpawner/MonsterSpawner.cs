using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [Header("스폰 설정")]
    public GameObject monsterPrefab;
    public float spawnInterval = 3f;

    [Header("스폰 위치 세부 조절 (Offset)")]
    // X(좌우), Y(위아래), Z(앞뒤) 위치를 조절할 수 있는 변수입니다.
    public Vector3 spawnOffset = new Vector3(0f, 0f, 1f);

    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            SpawnMonster();
            timer = 0f;
        }
    }

    void SpawnMonster()
    {
        if (monsterPrefab != null)
        {
            // 스포너의 원래 위치에 Offset 값을 더해 최종 스폰 위치를 결정합니다.
            Vector3 finalSpawnPosition = transform.position + spawnOffset;
            Instantiate(monsterPrefab, finalSpawnPosition, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("스포너에 몬스터 프리팹이 할당되지 않았습니다!");
        }
    }
}