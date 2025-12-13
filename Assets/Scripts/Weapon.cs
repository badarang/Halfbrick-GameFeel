using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private SpriteRenderer m_spriteRenderer;
    [SerializeField] private Vector3 m_rightPosition = new Vector3(0.5f, 0, 0);
    [SerializeField] private Vector3 m_leftPosition = new Vector3(-0.5f, 0, 0);
    [SerializeField] private Transform m_firePoint; // Transform for the muzzle position

    private Player m_player;

    void Start()
    {
        m_player = GetComponentInParent<Player>();
        if (m_spriteRenderer == null)
        {
            m_spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    void Update()
    {
        if (m_player != null)
        {
            UpdateOrientation();
        }
    }

    private void UpdateOrientation()
    {
        if (m_player.IsFacingRight())
        {
            transform.localPosition = m_rightPosition;
            m_spriteRenderer.flipX = false;
            // Adjust fire point local position if needed, or rely on child transform rotation
        }
        else
        {
            transform.localPosition = m_leftPosition;
            m_spriteRenderer.flipX = true;
        }
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
