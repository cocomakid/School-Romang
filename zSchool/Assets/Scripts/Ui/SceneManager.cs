using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class SceneChanger : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject settingPanel;
    public GameObject closePanel;
    public void GameStart() => SceneManager.LoadScene("GameScene");
    public void Setting() => settingPanel.SetActive(true);
    public void Close() => closePanel.SetActive(false);
    public void GameExit() => Application.Quit();
}