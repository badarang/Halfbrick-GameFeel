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
    [SerializeField] private float m_hitStopDuration = 0.05f; 

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
            m_sprite.color = m_defaultColor;
        }
    }

    private IEnumerator DamageRoutine(Player player)
    {
        while (true)
        {
            if (player != null)
            {
                float directionX = (m_knockbackDirection == KnockbackDirection.Right) ? 1f : -1f;
                Vector2 finalKnockback = new Vector2(directionX * m_knockbackForce.x, m_knockbackForce.y);
                
                if (player.TakeDamage(finalKnockback))
                {
                    QuestManager.Instance.CompleteQuest("touch_spikes");
                    
                    CameraController.Instance.Shake(0.2f, 0.5f);
                    
                    m_sprite.color = Color.red;
                    yield return StartCoroutine(HitStopRoutine());
                    m_sprite.color = m_defaultColor;
                }
            }
            
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator HitStopRoutine()
    {
        if (Time.timeScale > 0.01f)
        {
            float originalTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(m_hitStopDuration);
            Time.timeScale = originalTimeScale;
        }
    }
}
