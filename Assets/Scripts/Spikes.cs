using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spikes : MonoBehaviour {

    private SpriteRenderer m_sprite = null;
    private Color m_defaultColor = new Color();

    [Header("Damage Settings")]
    [SerializeField] private Vector2 m_knockbackForce = new Vector2(0.1f, 0.5f);

    void Start()
    {
        m_sprite = transform.GetComponent<SpriteRenderer>();
        m_defaultColor = m_sprite.color;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();
            if (player != null)
            {
                // Determine knockback direction
                float knockbackDirectionX;
                float playerVelocityX = player.GetVelocity().x;

                if (Mathf.Abs(playerVelocityX) > 0.01f)
                {
                    // Knockback in the opposite direction of horizontal movement
                    knockbackDirectionX = -Mathf.Sign(playerVelocityX);
                }
                else
                {
                    // If player is still, knockback based on which side of the spike they are on
                    knockbackDirectionX = (player.transform.position.x > transform.position.x) ? 1 : -1;
                }

                Vector2 finalKnockback = new Vector2(knockbackDirectionX * m_knockbackForce.x, m_knockbackForce.y);
                player.TakeDamage(finalKnockback);
            }

            // Visual feedback on the spike itself
            m_sprite.color = Color.red;
            StartCoroutine(ResetColorRoutine());
        }
    }

    private IEnumerator ResetColorRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        m_sprite.color = m_defaultColor;
    }
}
