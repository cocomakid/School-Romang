using UnityEngine;
using TMPro;

public class RaidUI : MonoBehaviour
{
    [Header("UI Text Components")]
    public TMP_Text waveText;          // 예: "ROUND 1"
    public TMP_Text monsterCountText;  // 예: "남은 적: 5"
    public TMP_Text noticeText;        // 예: "다음 라운드 준비 중..."

    public static RaidUI Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void UpdateWaveText(string waveName)
    {
        if (waveText != null) waveText.text = waveName;
    }

    public void UpdateMonsterCount(int count)
    {
        if (monsterCountText != null) monsterCountText.text = $"남은 적: {count}마리";
    }

    public void ShowNotice(string message, bool isVisible)
    {
        if (noticeText != null)
        {
            noticeText.text = message;
            noticeText.gameObject.SetActive(isVisible);
        }
    }
}