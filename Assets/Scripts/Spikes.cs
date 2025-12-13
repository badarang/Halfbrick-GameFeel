using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spikes : MonoBehaviour {

    private SpriteRenderer m_sprite = null;
    private Color m_defaultColor = new Color();
    private Coroutine m_damageCoroutine;

    public enum KnockbackDirection
    {
        Left,
        Right
    }

    [Header("Damage Settings")]
    [SerializeField] private Vector2 m_knockbackForce = new Vector2(0.1f, 0.5f);
    [SerializeField] private KnockbackDirection m_knockbackDirection = KnockbackDirection.Left;

    void Start()
    {
        m_sprite = transform.GetComponent<SpriteRenderer>();
        m_defaultColor = m_sprite.color;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (m_damageCoroutine != null) StopCoroutine(m_damageCoroutine);
            m_damageCoroutine = StartCoroutine(DamageRoutine(collision.GetComponent<Player>()));
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (m_damageCoroutine != null)
            {
                StopCoroutine(m_damageCoroutine);
                m_damageCoroutine = null;
            }
        }
    }

    private IEnumerator DamageRoutine(Player player)
    {
        while (true)
        {
            if (player != null)
            {
                // Determine knockback direction based on inspector setting
                float directionX = (m_knockbackDirection == KnockbackDirection.Right) ? 1f : -1f;
                Vector2 finalKnockback = new Vector2(directionX * m_knockbackForce.x, m_knockbackForce.y);
                
                // Try to take damage (Player will ignore if invincible)
                player.TakeDamage(finalKnockback);

                // Visual feedback on the spike itself
                m_sprite.color = Color.white;
                StartCoroutine(ResetColorRoutine());
            }
            
            // Wait a bit before trying to damage again. 
            // This interval should be shorter than invincibility duration to catch the moment it ends.
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator ResetColorRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        m_sprite.color = m_defaultColor;
    }
}
