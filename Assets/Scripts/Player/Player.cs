using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [Header("Здоровье и настройка сердец")]
    [SerializeField] private float health = 3.0f;
    [SerializeField] private int numOfHearts = 3;
    public Image[] hearts;
    public Sprite fullHeart;
    public Sprite emptyHeart; 
    private void FixedUpdate()
    {
        if(health > numOfHearts)
        {
            health = numOfHearts;
        }
        for(int i = 0; i < hearts.Length; i++)
        {
            if (i < Mathf.RoundToInt(health))
            {
                hearts[i].sprite = fullHeart;
            }
            else
            {
                hearts[i].sprite = emptyHeart;
            }
            if (i < numOfHearts)
            {
                hearts[i].enabled = true;
            }
            else
            {
                hearts[i].enabled = false;
            }
        }
    }
}