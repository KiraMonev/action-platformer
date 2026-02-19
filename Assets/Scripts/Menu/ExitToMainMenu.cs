using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitToMainMenu : MonoBehaviour
{
    public void ToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("DevMainMenuScene");
    }
}
