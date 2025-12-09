using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float m_moveSpeed = (2.0f * 60.0f);
    public float m_hitDamage = 1.0f;

    private Rigidbody2D m_rigidBody = null;
    private Vector2 m_vel = new Vector2(0, 0);

    // Start is called before the first frame update
    void Start()
    {
        m_rigidBody = transform.GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 pos = m_rigidBody.transform.position;
        pos.x += m_vel.x * Time.fixedDeltaTime;
        pos.y += m_vel.y * Time.fixedDeltaTime;
        m_rigidBody.transform.position = pos;
    }

    public void Fire(Vector3 startPos, bool directionRight)
    {
        gameObject.SetActive(true);
        transform.position = startPos;
        m_vel.x = directionRight ? m_moveSpeed : -m_moveSpeed;
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        ProcessCollision(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        ProcessCollision(collision);
    }

    private void ProcessCollision(Collision2D collision)
    {
        if (m_rigidBody)
        {
            Vector3 pos = m_rigidBody.transform.position;

            Enemy enemyObject = collision.gameObject.GetComponent<Enemy>();
            if(enemyObject)
            {
                //Do damage
                enemyObject.InflictDamage(m_hitDamage);
            }

            foreach (ContactPoint2D contact in collision.contacts)
            {
                //Push back out
                Vector2 impulse = contact.normal * (contact.normalImpulse / Time.fixedDeltaTime);
                pos.x += impulse.x;
                pos.y += impulse.y;

                //Is this a wall, or an enemy?
                ObjectPooler.Instance.ReturnObject(gameObject);
            }
            m_rigidBody.transform.position = pos;
        }
    }
}
