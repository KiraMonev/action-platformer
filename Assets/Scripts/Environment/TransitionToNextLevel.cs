using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionToNextLevel : MonoBehaviour
{    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if(SceneManager.GetActiveScene().buildIndex < 3) { 
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            }
        }
    }
}
