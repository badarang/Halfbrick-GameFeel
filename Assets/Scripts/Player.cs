using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public class Player : MonoSingleton<Player>
{
    public Transform m_rendererTransform;

    public float m_moveAccel = (0.03f * 60.0f);
    public float m_groundFriction = 0.85f;
    public float m_gravity = (-0.15f * 60.0f); 
    public float m_jumpVel = 0.95f; 
    public float m_jumpMinTime = 0.06f;
    public float m_jumpMaxTime = 0.20f;
    public float m_airFallFriction = 0.975f;
    public float m_airMoveFriction = 0.85f;
    
    private Rigidbody2D m_rigidBody = null;
    private BoxCollider2D m_collider = null; 
    private PlayerRender m_playerRender = null; 

    private bool m_jumpPressed = false;
    private bool m_jumpHeld = false;
    private bool m_wantsRight = false;
    private bool m_wantsLeft = false;
    private bool m_shootPressed = false;
    private bool m_fireRight = true;
    private bool m_hasWeapon = false;
    private float m_stateTimer = 0.0f;
    private Vector2 m_vel = new Vector2(0, 0);
    private List<GameObject> m_groundObjects = new List<GameObject>();

    private bool m_isMoving = false;
    private float m_moveTimer = 0.0f;
    private const float MOVE_TIME = 0.2f; 
    private Vector3 m_walkStartPosition;
    
    private bool m_movingRight = true;
    private Quaternion m_startRotation;
    
    private float m_currentMoveDistance = 0.5f;
    private float m_targetRotationAngle = 90.0f;
    private float m_playerSize = 0.5f;

    private enum State
    {
        Idle = 0,
        Falling,
        Jumping,
        Walking
    };

    private State m_state = State.Idle;

    void Start ()
    {
        m_rigidBody = transform.GetComponent<Rigidbody2D>();
        m_collider = transform.GetComponent<BoxCollider2D>(); 

        if (m_rendererTransform == null)
        {
            Debug.LogError("Renderer Transform is not assigned in the Player script!", this);
            m_rendererTransform = transform;
        }
        else
        {
            m_playerRender = m_rendererTransform.GetComponent<PlayerRender>();
        }
    }

    private void Update()
    {
        UpdateInput();

        if (m_shootPressed && m_hasWeapon)
        {
            GameObject projectileGO = ObjectPooler.Instance.GetObject("Bullet");
            if (projectileGO)
            {
                projectileGO.GetComponent<Bullet>().Fire(transform.position, m_fireRight);
            }
        }
    }

    void FixedUpdate()
    {
        switch (m_state)
        {
            case State.Idle:
                Idle();
                break;
            case State.Falling:
                Falling();
                break;
            case State.Jumping:
                Jumping();
                break;
            case State.Walking:
                Walking();
                break;
            default:
                break;
        }

        if(m_wantsRight == true)
        {
            m_fireRight = true;
        }
        else if(m_wantsLeft == true)
        {
            m_fireRight = false;
        }
    }

    public void GiveWeapon()
    {
        m_hasWeapon = true;
    }

    public Vector2 GetVelocity()
    {
        return m_vel;
    }

    public void TakeDamage(Vector2 knockbackForce)
    {
        m_vel = knockbackForce;
        m_state = State.Falling;
        m_isMoving = false;
        m_rigidBody.isKinematic = false;
        
        if (m_playerRender != null)
        {
            StartCoroutine(m_playerRender.ApplyHitFlashEffectRoutine(0.5f));
        }
    }

    public void Bounce(Vector2 bounceForce)
    {
        m_vel = bounceForce;
        m_state = State.Falling; 
        m_isMoving = false;
        m_rigidBody.isKinematic = false;
    }

    void Idle()
    {
        if (m_rigidBody.isKinematic) m_rigidBody.isKinematic = false;

        m_vel = Vector2.zero;
        m_isMoving = false;
        if (m_groundObjects.Count == 0)
        {
            m_state = State.Falling;
            return;
        }

        if (m_jumpPressed || m_jumpHeld)
        {
            m_stateTimer = 0;
            m_state = State.Jumping;
            return;
        }

        if (m_wantsLeft || m_wantsRight)
        {
            m_state = State.Walking;
            return;
        }
    }

    void Falling()
    {
        if (m_rigidBody.isKinematic) m_rigidBody.isKinematic = false;

        m_vel.y += m_gravity * Time.fixedDeltaTime;
        m_vel.y *= m_airFallFriction;
        
        float rotateSpeed = 360.0f;

        if (m_wantsLeft)
        {
            m_vel.x -= m_moveAccel * Time.fixedDeltaTime;
            m_rendererTransform.Rotate(0, 0, rotateSpeed * Time.fixedDeltaTime);
        }
        else if (m_wantsRight)
        {
            m_vel.x += m_moveAccel * Time.fixedDeltaTime;
            m_rendererTransform.Rotate(0, 0, -rotateSpeed * Time.fixedDeltaTime);
        }

        m_vel.x *= m_airMoveFriction;

        ApplyVelocity();
    }

    void Jumping()
    {
        if (m_rigidBody.isKinematic) m_rigidBody.isKinematic = false;

        m_stateTimer += Time.fixedDeltaTime;

        if (m_stateTimer < m_jumpMinTime || (m_jumpHeld && m_stateTimer < m_jumpMaxTime))
        {
            m_vel.y = m_jumpVel;
        }

        m_vel.y += m_gravity * Time.fixedDeltaTime;

        if (m_vel.y <= 0)
        {
            m_state = State.Falling;
        }

        float rotateSpeed = 360.0f;

        if (m_wantsLeft)
        {
            m_vel.x -= m_moveAccel * Time.fixedDeltaTime;
            m_rendererTransform.Rotate(0, 0, rotateSpeed * Time.fixedDeltaTime);
        }
        else if (m_wantsRight)
        {
            m_vel.x += m_moveAccel * Time.fixedDeltaTime;
            m_rendererTransform.Rotate(0, 0, -rotateSpeed * Time.fixedDeltaTime);
        }

        m_vel.x *= m_airMoveFriction;

        ApplyVelocity();
    }

    void Walking()
    {
        if (m_isMoving)
        {
            m_moveTimer += Time.fixedDeltaTime;
            float t = m_moveTimer / MOVE_TIME;

            if (t >= 1.0f)
            {
                t = 1.0f;
                m_isMoving = false;
            }

            float easedT = t * t;

            float targetAngle = m_movingRight ? -m_targetRotationAngle : m_targetRotationAngle;
            float angle = Mathf.Lerp(0, targetAngle, easedT);
            
            float halfSize = m_playerSize * 0.5f;
            Vector3 pivotOffset = new Vector3(m_movingRight ? halfSize : -halfSize, -halfSize, 0);
            Vector3 pivotPoint = m_walkStartPosition + pivotOffset;

            Quaternion rotation = Quaternion.Euler(0, 0, angle);
            Vector3 startOffset = m_walkStartPosition - pivotPoint;
            Vector3 newPos = pivotPoint + (rotation * startOffset);

            m_rigidBody.transform.position = newPos;
            m_rendererTransform.rotation = m_startRotation * rotation;

            if (!m_isMoving)
            {
                float targetX = m_walkStartPosition.x + (m_movingRight ? m_currentMoveDistance : -m_currentMoveDistance);
                Vector3 finalPos = m_rigidBody.transform.position;
                finalPos.x = targetX;
                finalPos.y = m_walkStartPosition.y;
                m_rigidBody.transform.position = finalPos;
                
                Vector3 euler = m_rendererTransform.rotation.eulerAngles;
                euler.z = Mathf.Round(euler.z / 90.0f) * 90.0f;
                m_rendererTransform.rotation = Quaternion.Euler(euler);

                m_rigidBody.isKinematic = false;
                m_rigidBody.velocity = Vector2.zero;

                CameraController.Instance.Shake(0.1f, 0.15f);

                Vector2 origin = transform.position;
                Vector2 size = new Vector2(m_playerSize * 0.9f, 0.05f); 
                float distance = m_playerSize * 0.6f; 
                
                RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0, Vector2.down, distance);
                
                bool hasGround = false;
                if (hit.collider != null && !hit.collider.isTrigger && hit.collider.gameObject != gameObject)
                {
                    hasGround = true;
                }

                if (!hasGround)
                {
                    m_state = State.Falling;
                    return;
                }
            }
            else
            {
                return;
            }
        }

        if (m_jumpPressed || m_jumpHeld)
        {
            m_stateTimer = 0;
            m_state = State.Jumping;
            m_isMoving = false;
            m_rigidBody.isKinematic = false; 
            m_vel.x = 0;
            return;
        }

        if (!m_isMoving && m_groundObjects.Count == 0)
        {
            m_state = State.Falling;
            m_isMoving = false;
            m_rigidBody.isKinematic = false; 
            return;
        }

        if (m_wantsLeft || m_wantsRight)
        {
            m_playerSize = m_collider != null ? m_collider.size.x * transform.localScale.x : 0.5f;
            
            bool checkRight = !m_wantsLeft; 
            Vector2 direction = checkRight ? Vector2.right : Vector2.left;
            
            Vector2 originCast = transform.position;
            Vector2 sizeCast = m_collider != null ? m_collider.size * (Vector2)transform.localScale : new Vector2(0.5f, 0.5f);
            sizeCast *= 0.95f; 
            
            float maxDistance = m_playerSize; 
            
            RaycastHit2D[] hits = Physics2D.BoxCastAll(originCast, sizeCast, 0, direction, maxDistance);
            
            float actualDistance = maxDistance;
            
            foreach (var h in hits)
            {
                if (h.collider.gameObject != gameObject && !h.collider.isTrigger)
                {
                    if (h.distance < actualDistance)
                    {
                        actualDistance = h.distance;
                    }
                }
            }
            
            if (actualDistance < 0.05f)
            {
                return;
            }
            
            m_currentMoveDistance = actualDistance;
            m_targetRotationAngle = 90.0f * (m_currentMoveDistance / m_playerSize);

            m_isMoving = true;
            m_moveTimer = 0.0f;
            m_walkStartPosition = m_rigidBody.transform.position;
            m_startRotation = m_rendererTransform.rotation;
            m_movingRight = checkRight; 

            m_rigidBody.isKinematic = true;
            m_rigidBody.velocity = Vector2.zero;
        }
        else
        {
            m_state = State.Idle;
            m_vel.x = 0;
        }
    }

    void ApplyVelocity()
    {
        Vector3 pos = m_rigidBody.transform.position;
        pos.x += m_vel.x;
        pos.y += m_vel.y;
        m_rigidBody.transform.position = pos;
    }

    void UpdateInput()
    {
        m_wantsLeft = Input.GetKey(KeyCode.LeftArrow);
        m_wantsRight = Input.GetKey(KeyCode.RightArrow);
        m_jumpPressed = Input.GetKeyDown(KeyCode.UpArrow);
        m_jumpHeld = Input.GetKey(KeyCode.UpArrow);
        m_shootPressed = Input.GetKeyDown(KeyCode.Space);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        ProcessCollision(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        ProcessCollision(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (m_isMoving) return;

        m_groundObjects.Remove(collision.gameObject);
    }

    private void ProcessCollision(Collision2D collision)
    {
        if (m_isMoving) return;

        m_groundObjects.Remove(collision.gameObject);
        
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (Mathf.Abs(contact.normal.y) > Mathf.Abs(contact.normal.x))
            {
                if (contact.normal.y > 0)
                {
                    if (m_groundObjects.Contains(contact.collider.gameObject) == false)
                    {
                        m_groundObjects.Add(contact.collider.gameObject);
                    }
                    if (m_state == State.Falling)
                    {
                        m_rendererTransform.rotation = Quaternion.identity;

                        CameraController.Instance.Shake(0.2f, 0.5f);

                        if (m_wantsRight || m_wantsLeft)
                        {
                            m_state = State.Walking;
                        }
                        else
                        {
                            m_state = State.Idle;
                        }
                    }
                }
                else
                {
                    m_vel.y = 0;
                    m_state = State.Falling;
                }
            }
            else
            {
                if ((contact.normal.x > 0 && m_vel.x < 0) || (contact.normal.x < 0 && m_vel.x > 0))
                {
                    m_vel.x = 0;
                }
            }
        }
    }
}
