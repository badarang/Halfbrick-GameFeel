using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Weapon : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer m_spriteRenderer;
    [SerializeField] private Transform m_firePoint;
    [SerializeField] private Transform m_casingEjectPoint;
    [SerializeField] private GameObject m_shellCasingPrefab;

    [Header("Settings")]
    [SerializeField] private Vector3 m_rightPosition = new Vector3(0.5f, 0, 0);
    [SerializeField] private Vector3 m_leftPosition = new Vector3(-0.5f, 0, 0);
    [SerializeField] private float m_recoilAngle = -15f;
    [SerializeField] private float m_recoilDuration = 0.1f;
    [SerializeField] private Vector2 m_casingForce = new Vector2(2, 2);
    [SerializeField] private float m_casingTorque = 5f;

    private Player m_player;
    private bool m_isFiring = false;
    private Vector3 m_originalScale;

    void Start()
    {
        m_player = GetComponentInParent<Player>();
        if (m_spriteRenderer == null)
        {
            m_spriteRenderer = GetComponent<SpriteRenderer>();
        }
        m_originalScale = transform.localScale;
    }

    void Update()
    {
        if (m_player != null && !m_isFiring)
        {
            UpdateOrientation();
        }
    }

    private void UpdateOrientation()
    {
        if (m_player.IsFacingRight())
        {
            transform.localPosition = m_rightPosition;
            transform.localScale = m_originalScale;
        }
        else
        {
            transform.localPosition = m_leftPosition;
            // Flip the parent object's scale to mirror all children
            transform.localScale = new Vector3(-m_originalScale.x, m_originalScale.y, m_originalScale.z);
        }
    }

    public void Fire()
    {
        if (m_isFiring) return;
        StartCoroutine(FireRoutine());
    }

    private IEnumerator FireRoutine()
    {
        m_isFiring = true;

        // Recoil Animation
        transform.DOKill();
        float angle = m_player.IsFacingRight() ? m_recoilAngle : -m_recoilAngle;
        transform.DOLocalRotate(new Vector3(0, 0, angle), m_recoilDuration / 2)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                transform.DOLocalRotate(Vector3.zero, m_recoilDuration / 2).SetEase(Ease.InQuad);
            });

        // Eject Casing
        if (m_shellCasingPrefab != null && m_casingEjectPoint != null)
        {
            GameObject casingGO = Instantiate(m_shellCasingPrefab, m_casingEjectPoint.position, m_casingEjectPoint.rotation);
            ShellCasing casing = casingGO.GetComponent<ShellCasing>();
            Collider2D casingCollider = casingGO.GetComponent<Collider2D>();
            
            // Ignore collision with ALL colliders on the player
            Collider2D[] playerColliders = m_player.GetComponents<Collider2D>();
            if (casingCollider != null)
            {
                foreach (var col in playerColliders)
                {
                    Physics2D.IgnoreCollision(casingCollider, col);
                }
                
                Collider2D[] childColliders = m_player.GetComponentsInChildren<Collider2D>();
                foreach (var col in childColliders)
                {
                    Physics2D.IgnoreCollision(casingCollider, col);
                }

                // Ignore collision with all enemies
                Enemy[] enemies = FindObjectsOfType<Enemy>();
                foreach (var enemy in enemies)
                {
                    Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
                    if (enemyCollider != null)
                    {
                        Physics2D.IgnoreCollision(casingCollider, enemyCollider);
                    }
                }
            }

            if (casing != null)
            {
                float direction = m_player.IsFacingRight() ? -1 : 1;
                casing.Initialize(new Vector2(m_casingForce.x * direction, m_casingForce.y), m_casingTorque * direction);
            }
        }

        yield return new WaitForSeconds(m_recoilDuration);
        m_isFiring = false;
    }

    public Vector3 GetFirePosition()
    {
        if (m_firePoint != null)
        {
            return m_firePoint.position;
        }
        return transform.position;
    }
}
