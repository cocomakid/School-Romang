using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [Header("스폰 위치 세부 조절 (Offset)")]
    public Vector3 spawnOffset = new Vector3(0f, 0f, 1f);

    // RaidManager에서 몬스터 프리팹을 받아 스폰하고, 생성된 오브젝트를 반환합니다.
    public GameObject SpawnMonster(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("스포너에 전달된 몬스터 프리팹이 null입니다!");
            return null;
        }

        Vector3 finalSpawnPosition = transform.position + spawnOffset;
        return Instantiate(prefab, finalSpawnPosition, Quaternion.identity);
    }
}