using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EnemyDieFall : MonoBehaviour
{
    [SerializeField] private float m_jumpForce = 10f;
    [SerializeField] private float m_gravity = 30f;
    [SerializeField] private float m_rotationSpeed = 360f;

    private Vector3 m_velocity;

    public void Initialize(Sprite sprite, Vector3 startPosition)
    {
        transform.position = startPosition;
        
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
        }
        sr.sprite = sprite;
        
        // Flatten the sprite slightly to look squashed
        transform.localScale = new Vector3(1.2f, 0.5f, 1f);

        m_velocity = new Vector3(0, m_jumpForce, 0);
        
        // Add rotation for effect
        transform.DORotate(new Vector3(0, 0, 180), 0.5f).SetLoops(-1, LoopType.Incremental);
    }

    void Update()
    {
        // Apply gravity
        m_velocity.y -= m_gravity * Time.deltaTime;
        transform.position += m_velocity * Time.deltaTime;

        // Destroy if it falls below the screen (assuming -10 is safe)
        if (transform.position.y < -10f)
        {
            Destroy(gameObject);
        }
    }
}
