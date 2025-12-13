using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EnemyDieFall : MonoBehaviour
{
    [SerializeField] private float m_jumpForce = 10f;
    [SerializeField] private float m_horizontalForce = 5f; // Added horizontal force
    [SerializeField] private float m_gravity = 30f;
    [SerializeField] private float m_rotationSpeed = 360f;

    private Vector3 m_velocity;

    public void Initialize(Sprite sprite, Vector3 startPosition, bool isPlayerLeft)
    {
        transform.position = startPosition;
        
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
        }
        sr.sprite = sprite;
        
        transform.localScale = new Vector3(1.2f, 0.5f, 1f);

        // Set velocity based on player position
        float directionX = isPlayerLeft ? 1f : -1f;
        m_velocity = new Vector3(directionX * m_horizontalForce, m_jumpForce, 0);
        
        // Rotate in the direction of movement
        float rotationDirection = isPlayerLeft ? -1f : 1f;
        transform.DORotate(new Vector3(0, 0, 360 * rotationDirection), 0.5f, RotateMode.FastBeyond360)
            .SetLoops(-1, LoopType.Incremental)
            .SetEase(Ease.Linear);
    }

    void Update()
    {
        m_velocity.y -= m_gravity * Time.deltaTime;
        transform.position += m_velocity * Time.deltaTime;

        if (transform.position.y < -100f)
        {
            Destroy(gameObject);
        }
    }
}
