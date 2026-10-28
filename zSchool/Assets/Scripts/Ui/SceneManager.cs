using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneChanger : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject settingPanel;
    public GameObject closePanel;

    void Update()
    {
        // ⌨️ [ESC 키 입력 감지]
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingPanel != null)
            {
                bool willOpen = !settingPanel.activeSelf;
                settingPanel.SetActive(willOpen);

                if (willOpen)
                {
                    Time.timeScale = 0f;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    CloseSettingPanel();
                }
            }
        }
    }

    public void GameStart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameScene");
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    public void QuitToMain()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainScene");
    }

    public void Setting()
    {
        settingPanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // ❌ [X 버튼이나 닫기 버튼을 눌렀을 때 연결]
    public void Close()
    {
        CloseSettingPanel();
    }

    // 🛠️ [핵심] 씬에 따라 마우스 커서 상태를 다르게 처리하는 함수
    private void CloseSettingPanel()
    {
        if (settingPanel != null)
        {
            settingPanel.SetActive(false);
        }

        Time.timeScale = 1f;

        // 현재 씬 이름 가져오기
        string currentSceneName = SceneManager.GetActiveScene().name;

        if (currentSceneName == "MainScene")
        {
            // 🌟 메인 화면에서는 설정창을 닫아도 마우스가 자유롭게 보이게 유지
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // 🎮 게임 씬에서는 플레이어 시점 조작을 위해 마우스 잠금 및 숨김
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void GameExit() => Application.Quit();
}