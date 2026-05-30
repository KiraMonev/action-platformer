using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DeathScreen : MonoBehaviour
{
    [SerializeField] private GameObject deathScreenUI;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button exitButton;

    private Player _player;

    private void Start()
    {
        _player = FindAnyObjectByType<Player>();
        if (_player != null)
        {
            _player.OnDeath += HandlePlayerDeath;
        }
        else
        {
            Debug.LogError("[DeathScreen] Player not found in scene!");
        }

        // Set up button listeners
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartLevel);
        }
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ExitGameMethod);
        }

        // Ensure the death screen UI is hidden on start
        if (deathScreenUI != null)
        {
            deathScreenUI.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (_player != null)
        {
            _player.OnDeath -= HandlePlayerDeath;
        }
    }

    private void HandlePlayerDeath()
    {
        // Disable PauseMenu to prevent pausing while dead
        if (TryGetComponent<PauseMenu>(out var pauseMenu))
        {
            pauseMenu.enabled = false;
        }
    }

    private void Update()
    {
        // Once the player is dead and the game freezes (completing the death animation), show the UI
        if (_player != null && _player.IsDead && Time.timeScale == 0f && deathScreenUI != null && !deathScreenUI.activeSelf)
        {
            deathScreenUI.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void RestartLevel()
    {
        Time.timeScale = 1.0f; // Reset game speed before reloading
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ExitGameMethod()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
