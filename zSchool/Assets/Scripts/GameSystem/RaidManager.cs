using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaidManager : MonoBehaviour
{
    [System.Serializable]
    public class WaveData
    {
        public string waveName = "Round";
        public int meleeCount;      // 근접 몬스터 수
        public int rangedCount;     // 원거리 몬스터 수
        public int bossCount;       // 보스 몬스터 수
    }

    [Header("Monster Prefabs")]
    public GameObject meleeMonsterPrefab;
    public GameObject rangedMonsterPrefab;
    public GameObject bossMonsterPrefab;

    [Header("Spawners")]
    public MonsterSpawner[] spawners;

    [Header("Wave Settings")]
    public float timeBetweenWaves = 3f;
    public List<WaveData> waves = new List<WaveData>();

    private int currentWaveIndex = 0;
    private int aliveMonsterCount = 0;
    private bool isWaveSpawning = false;

    void Start()
    {
        // 1~4 라운드 기본 설정
        if (waves.Count == 0)
        {
            waves.Add(new WaveData { waveName = "ROUND 1", meleeCount = 5, rangedCount = 0, bossCount = 0 });
            waves.Add(new WaveData { waveName = "ROUND 2", meleeCount = 5, rangedCount = 3, bossCount = 0 });
            waves.Add(new WaveData { waveName = "ROUND 3", meleeCount = 7, rangedCount = 5, bossCount = 0 });
            waves.Add(new WaveData { waveName = "FINAL BOSS ROUND", meleeCount = 4, rangedCount = 2, bossCount = 1 });
        }

        StartCoroutine(StartNextWave());
    }

    void Update()
    {
        if (!isWaveSpawning && aliveMonsterCount <= 0 && currentWaveIndex < waves.Count)
        {
            StartCoroutine(StartNextWave());
        }
    }

    private IEnumerator StartNextWave()
    {
        isWaveSpawning = true;

        if (currentWaveIndex > 0)
        {
            if (RaidUI.Instance != null)
                RaidUI.Instance.ShowNotice("ROUND CLEAR!\n다음 라운드 준비 중...", true);

            yield return new WaitForSeconds(timeBetweenWaves);

            if (RaidUI.Instance != null)
                RaidUI.Instance.ShowNotice("", false);
        }

        if (currentWaveIndex >= waves.Count)
        {
            if (RaidUI.Instance != null)
                RaidUI.Instance.ShowNotice("STAGE CLEAR!", true);
            yield break;
        }

        WaveData currentWave = waves[currentWaveIndex];

        if (RaidUI.Instance != null)
        {
            RaidUI.Instance.UpdateWaveText(currentWave.waveName);
        }

        List<GameObject> spawnList = new List<GameObject>();
        for (int i = 0; i < currentWave.meleeCount; i++) spawnList.Add(meleeMonsterPrefab);
        for (int i = 0; i < currentWave.rangedCount; i++) spawnList.Add(rangedMonsterPrefab);
        for (int i = 0; i < currentWave.bossCount; i++) spawnList.Add(bossMonsterPrefab);

        aliveMonsterCount = spawnList.Count;
        if (RaidUI.Instance != null) RaidUI.Instance.UpdateMonsterCount(aliveMonsterCount);

        for (int i = 0; i < spawnList.Count; i++)
        {
            if (spawners.Length == 0) break;

            MonsterSpawner spawner = spawners[i % spawners.Length];
            GameObject monster = spawner.SpawnMonster(spawnList[i]);

            if (monster != null)
            {
                MonsterTracker tracker = monster.AddComponent<MonsterTracker>();
                tracker.onDestroyed = OnMonsterKilled;
            }

            yield return new WaitForSeconds(0.2f);
        }

        currentWaveIndex++;
        isWaveSpawning = false;
    }

    private void OnMonsterKilled()
    {
        aliveMonsterCount--;
        if (aliveMonsterCount < 0) aliveMonsterCount = 0;

        if (RaidUI.Instance != null)
        {
            RaidUI.Instance.UpdateMonsterCount(aliveMonsterCount);
        }
    }
}

// 몬스터 사망 추적용 클래스 (파일 맨 아래 배치)
public class MonsterTracker : MonoBehaviour
{
    public System.Action onDestroyed;

    private void OnDestroy()
    {
        if (gameObject.scene.isLoaded)
        {
            onDestroyed?.Invoke();
        }
    }
}