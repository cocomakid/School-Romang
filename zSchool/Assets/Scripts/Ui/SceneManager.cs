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
                    Time.timeScale = 1f;
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }
    }

    public void GameStart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameScene");
    }

    // 🔄 [Retry 버튼 기능] 현재 씬(게임 씬)을 다시 로드하여 처음부터 재시작
    public void Retry()
    {
        Time.timeScale = 1f; // 시간 정지 해제

        // 현재 활성화된 씬의 이름을 가져와서 다시 로드합니다.
        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);

        // 혹은 씬 이름이 확실하다면 직접 적어도 됩니다: SceneManager.LoadScene("GameScene");
    }

    // 🚪 [Quit 버튼 기능] 메인 화면 씬으로 이동
    public void QuitToMain()
    {
        Time.timeScale = 1f; // 시간 정지 해제
        SceneManager.LoadScene("MainScene"); // 메인 화면 씬 이름 (실제 메인 씬 이름으로 변경하세요)
    }

    public void Setting()
    {
        settingPanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        settingPanel.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void GameExit() => Application.Quit();
}