using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using DG.Tweening;

public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    public float m_moveSpeed = (0.05f * 60.0f);
    public float m_changeSpeed = 0.2f * 60.0f;
    public float m_moveDuration = 3.0f;
    public float m_holdDuration = 0.5f;
    public float m_chargeCooldownDuration = 2.0f;
    public float m_chargeMinRange = 1.0f;
    
    [Header("Combat")]
    public float m_maxHealth = 3.0f;
    public Vector2 m_knockbackForce = new Vector2(0.1f, 0.5f);
    public float m_playerBounceForce = 5f;
    public float m_stompDamage = 1.0f;
    public float m_groundPoundDamage = 3.0f;
    public GameObject m_enemyDieFallPrefab; 

    public Player m_player = null;

    private Rigidbody2D m_rigidBody = null;
    private UnitRenderer m_unitRenderer = null;
    private float m_health = 3.0f;
    private float m_timer = 0.0f;
    private float m_lastPlayerDiff = 0.0f;
    private Vector2 m_vel = new Vector2(0, 0);
    private Vector3 m_originalScale;
    private bool m_isStunned = false;

    private enum WallCollision
    {
        None = 0,
        Left,
        Right
    };
    private WallCollision m_wallFlags = WallCollision.None;

    private enum State
    {
        Idle = 0,
        Walking,
        Charging,
        ChargingCooldown,
        Stunned
    };
    private State m_state = State.Idle;

    void Start()
    {
        m_health = m_maxHealth;
        m_rigidBody = transform.GetComponent<Rigidbody2D>();
        m_unitRenderer = GetComponentInChildren<UnitRenderer>();
        m_originalScale = transform.localScale;
    }

    void FixedUpdate()
    {
        if (m_isStunned) return;

        switch (m_state)
        {
            case State.Idle:
                Idle();
                break;
            case State.Walking:
                Walking();
                break;
            case State.Charging:
                Charging();
                break;
            case State.ChargingCooldown:
                ChargingCooldown();
                break;
            default:
                break;
        }

        m_wallFlags = WallCollision.None;
    }

    public void InflictDamage(float damageAmount)
    {
        if (m_isStunned) return;

        m_health -= damageAmount;
        
        if (m_unitRenderer != null)
        {
            StartCoroutine(m_unitRenderer.ApplyHitFlashEffectRoutine(0.2f));
        }

        if(m_health <= 0.0f)
        {
            Die();
        }
        else
        {
            StartCoroutine(StunRoutine());
        }
    }

    private IEnumerator StunRoutine()
    {
        m_isStunned = true;
        m_state = State.Stunned;
        m_vel = Vector2.zero;
        
        transform.DOKill();
        
        float squashYScale = 0.5f;
        float stretchXScale = 1.5f;
        
        // Squash
        transform.DOScale(new Vector3(stretchXScale, squashYScale, 1f), 0.1f);
        
        yield return new WaitForSeconds(2.0f);
        
        // Jump back to normal
        transform.DOScale(m_originalScale, 0.2f).SetEase(Ease.OutBack);
        transform.DOJump(transform.position, 0.5f, 1, 0.2f);

        m_isStunned = false;
        m_state = State.Idle;
    }

    private void Die()
    {
        if (m_enemyDieFallPrefab != null)
        {
            GameObject dieFallInstance = Instantiate(m_enemyDieFallPrefab, transform.position, Quaternion.identity);
            EnemyDieFall dieFallScript = dieFallInstance.GetComponent<EnemyDieFall>();
            if (dieFallScript != null)
            {
                dieFallScript.Initialize(GetComponent<SpriteRenderer>().sprite, transform.position);
            }
        }
        Destroy(gameObject);
    }

    void Idle()
    {
        m_vel = Vector2.zero;

        float yDiff = m_player.transform.position.y - transform.position.y;
        if(Mathf.Abs(yDiff) <= m_chargeMinRange)
        {
            m_lastPlayerDiff = m_player.transform.position.x - transform.position.x;
            m_vel.x = m_changeSpeed * Mathf.Sign(m_lastPlayerDiff);
            m_timer = 0;
            m_state = State.Charging;
            return;
        }

        m_timer += Time.deltaTime;
        if(m_timer >= m_holdDuration)
        {
            m_timer = 0;
            m_state = State.Walking;

            if(m_wallFlags == WallCollision.None)
            {
                m_vel.x = (Random.Range(0.0f, 100.0f) < 50.0f) ? m_moveSpeed : -m_moveSpeed;
            }
            else
            {
                m_vel.x = (m_wallFlags == WallCollision.Left) ? m_moveSpeed : -m_moveSpeed;
            }
            return;
        }
    }

    void Walking()
    {
        ApplyVelocity();

        float yDiff = m_player.transform.position.y - transform.position.y;
        if (Mathf.Abs(yDiff) <= m_chargeMinRange)
        {
            m_lastPlayerDiff = m_player.transform.position.x - transform.position.x;
            m_vel.x = m_changeSpeed * Mathf.Sign(m_lastPlayerDiff);
            m_timer = 0;
            m_state = State.Charging;
            return;
        }

        m_timer += Time.deltaTime;
        if (m_timer >= m_moveDuration)
        {
            m_timer = 0.0f;
            m_state = State.Idle;
            return;
        }
    }

    void Charging()
    {
        ApplyVelocity();

        float xDiff = m_player.transform.position.x - transform.position.x;
        if (Mathf.Sign(m_lastPlayerDiff) != Mathf.Sign(xDiff))
        {
            m_vel.x = 0.0f;
            m_timer = 0;
            m_state = State.ChargingCooldown;
            return;
        }
    }

    void ChargingCooldown()
    {
        m_timer += Time.deltaTime;
        if (m_timer >= m_chargeCooldownDuration)
        {
            m_timer = 0.0f;
            m_state = State.Idle;
            return;
        }
    }

    void ApplyVelocity()
    {
        Vector3 pos = m_rigidBody.transform.position;
        pos.x += m_vel.x * Time.fixedDeltaTime;
        pos.y += m_vel.y * Time.fixedDeltaTime;
        m_rigidBody.transform.position = pos;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Player player = collision.gameObject.GetComponent<Player>();
            ContactPoint2D[] contacts = new ContactPoint2D[1];
            collision.GetContacts(contacts);
            ContactPoint2D contact = contacts[0];

            // Check if player is on top
            if (contact.normal.y < -0.5f) 
            {
                // Check if player is ground pounding
                float damage = player.IsGroundPounding() ? m_groundPoundDamage : m_stompDamage;
                InflictDamage(damage);
                
                // Bounce player
                player.Bounce(new Vector2(player.GetVelocity().x, m_playerBounceForce));
            }
            else // Player hit from side or bottom
            {
                float knockbackDirectionX = (player.transform.position.x > transform.position.x) ? 1 : -1;
                Vector2 finalKnockback = new Vector2(knockbackDirectionX * m_knockbackForce.x, m_knockbackForce.y);
                player.TakeDamage(finalKnockback);
            }
        }
        else
        {
            ProcessCollision(collision);
        }
    }

    private void ProcessCollision(Collision2D collision)
    {
        Vector3 pos = m_rigidBody.transform.position;

        foreach (ContactPoint2D contact in collision.contacts)
        {
            Vector2 impulse = contact.normal * (contact.normalImpulse / Time.fixedDeltaTime);
            pos.x += impulse.x;
            pos.y += impulse.y;

            if (Mathf.Abs(contact.normal.y) < Mathf.Abs(contact.normal.x))
            {
                if ((contact.normal.x > 0 && m_vel.x < 0) || (contact.normal.x < 0 && m_vel.x > 0))
                {
                    m_vel.x = 0;
                    m_wallFlags = (contact.normal.x < 0) ? WallCollision.Left : WallCollision.Right;
                    m_state = State.Idle;
                    m_timer = 0;
                }
            }
        }
        m_rigidBody.transform.position = pos;
    }
}
