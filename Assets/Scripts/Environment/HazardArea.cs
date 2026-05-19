using UnityEngine;

public class HazardArea : MonoBehaviour
{
    [Header("Hazard Settings")]
    [SerializeField] private int damageAmount = 1;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (collision.TryGetComponent<Player>(out var player))
            {
                player.RespawnAtLastSafeGround(damageAmount);
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        // Fallback in case the player somehow stays inside the hazard (e.g. paused or during loading)
        if (collision.CompareTag("Player"))
        {
            if (collision.TryGetComponent<Player>(out var player))
            {
                player.RespawnAtLastSafeGround(damageAmount);
            }
        }
    }
}
