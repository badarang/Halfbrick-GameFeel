using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;
using DG.Tweening; // Added for DoTween

public class Player : MonoSingleton<Player>
{
    public Transform m_rendererTransform;

    [Header("Movement")]
    public float m_moveAccel = (0.03f * 60.0f);
    public float m_groundFriction = 0.85f;
    public float m_gravity = (-0.15f * 60.0f); 
    public float m_jumpVel = 0.95f; 
    public float m_jumpMinTime = 0.06f;
    public float m_jumpMaxTime = 0.20f;
    public float m_airFallFriction = 0.975f;
    public float m_airMoveFriction = 0.85f;
    public float m_groundPoundSpeed = 20.0f;
    public float m_groundPoundPrepareTime = 0.5f; // Duration of the spin
    
    [Header("Combat")]
    public float m_invincibilityDuration = 2.0f;
    public float m_weaponRecoilForce = 2.0f;
    public float m_recoilDuration = 0.1f; // Duration to disable input after shooting

    [Header("Collision")]
    public LayerMask m_obstacleLayerMask; // Layer mask for obstacle detection

    private Rigidbody2D m_rigidBody = null;
    private BoxCollider2D m_collider = null; 
    private UnitRenderer m_unitRenderer = null; 
    private Weapon m_weapon = null; // Reference to the Weapon component

    private bool m_jumpPressed = false;
    private bool m_jumpHeld = false;
    private bool m_wantsRight = false;
    private bool m_wantsLeft = false;
    private bool m_shootPressed = false;
    private bool m_groundPoundPressed = false;
    private bool m_fireRight = true;
    private bool m_hasWeapon = false;
    private bool m_isInvincible = false;
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
    private bool isFacingRight = true;
    
    private bool m_wasGroundPounding = false;
    private float m_recoilTimer = 0.0f;

    private enum State
    {
        Idle = 0,
        Falling,
        Jumping,
        Walking,
        GroundPoundPrepare, // New state for spin
        GroundPoundFall     // Renamed from GroundPounding
    };

    private State m_state = State.Idle;

    void Start ()
    {
        m_rigidBody = transform.GetComponent<Rigidbody2D>();
        m_collider = transform.GetComponent<BoxCollider2D>(); 
        m_weapon = GetComponentInChildren<Weapon>(true); // Get weapon reference

        if (m_rendererTransform == null)
        {
            Debug.LogError("Renderer Transform is not assigned in the Player script!", this);
            m_rendererTransform = transform;
        }
        else
        {
            m_unitRenderer = m_rendererTransform.GetComponent<UnitRenderer>();
        }
    }

    private void Update()
    {
        UpdateInput();

        if (m_shootPressed && m_hasWeapon)
        {
            // Fire Bullet
            GameObject projectileGO = ObjectPooler.Instance.GetObject("Bullet");
            if (projectileGO)
            {
                Vector3 firePos = (m_weapon != null) ? m_weapon.GetFirePosition() : transform.position;
                projectileGO.GetComponent<Bullet>().Fire(firePos, m_fireRight);
                QuestManager.Instance.CompleteQuest("shoot_enemy");
            }

            // Apply Recoil & Effects
            if (m_weapon != null)
            {
                m_weapon.Fire();
            }
            
            // Player knockback
            m_vel.x += isFacingRight ? -m_weaponRecoilForce : m_weaponRecoilForce;
            m_recoilTimer = m_recoilDuration; // Set recoil timer

            // If on ground and moving, interrupt movement to apply recoil
            if (m_state == State.Walking && m_isMoving)
            {
                m_isMoving = false;
                m_rigidBody.isKinematic = false;
            }

            // Camera effects
            Vector2 shakeDirection = isFacingRight ? Vector2.left : Vector2.right;
            CameraController.Instance.Shake(shakeDirection, 0.1f, 0.3f);
            CameraController.Instance.Punch(2f, 0.2f);
        }
    }

    void FixedUpdate()
    {
        m_wasGroundPounding = (m_state == State.GroundPoundFall);

        if (m_recoilTimer > 0)
        {
            m_recoilTimer -= Time.fixedDeltaTime;
        }

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
            case State.GroundPoundPrepare:
                GroundPoundPrepare();
                break;
            case State.GroundPoundFall:
                GroundPoundFall();
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

    public bool IsGroundPounding()
    {
        return m_state == State.GroundPoundFall || m_wasGroundPounding;
    }

    public bool IsFacingRight()
    {
        return isFacingRight;
    }

    public void GiveWeapon()
    {
        m_hasWeapon = true;
        if (m_weapon != null)
        {
            m_weapon.gameObject.SetActive(true);
        }
    }

    public Vector2 GetVelocity()
    {
        return m_vel;
    }

    public bool TakeDamage(Vector2 knockbackForce)
    {
        if (m_isInvincible) return false;

        m_vel = knockbackForce;
        m_state = State.Falling;
        m_isMoving = false;
        m_rigidBody.isKinematic = false;
        
        m_rendererTransform.DOKill();
        
        if (m_unitRenderer != null)
        {
            StartCoroutine(m_unitRenderer.ApplyHitFlashEffectRoutine(0.5f));
        }
        
        StartCoroutine(InvincibilityRoutine());
        return true;
    }

    private IEnumerator InvincibilityRoutine()
    {
        m_isInvincible = true;
        if (m_unitRenderer != null)
        {
            m_unitRenderer.StartInvincibilityFlash();
        }
        
        yield return new WaitForSeconds(m_invincibilityDuration);
        
        if (m_unitRenderer != null)
        {
            m_unitRenderer.StopInvincibilityFlash();
        }
        m_isInvincible = false;
    }

    public void Bounce(Vector2 bounceForce)
    {
        m_vel = bounceForce;
        m_state = State.Jumping; 
        m_isMoving = false;
        m_rigidBody.isKinematic = false;
        
        m_rendererTransform.DOKill();
    }

    void Idle()
    {
        if (m_rigidBody.isKinematic) m_rigidBody.isKinematic = false;

        // If recoiling, apply friction and slide
        if (m_recoilTimer > 0)
        {
            if (!IsGrounded())
            {
                m_state = State.Falling;
                return;
            }
            m_vel.x *= m_groundFriction;
            ApplyVelocity();
            return;
        }

        m_vel = Vector2.zero;
        m_isMoving = false;
        if (!IsGrounded())
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
    
    private bool IsGrounded()
    {
        for (int i = m_groundObjects.Count - 1; i >= 0; i--)
        {
            if (m_groundObjects[i] == null || !m_groundObjects[i].activeInHierarchy)
            {
                m_groundObjects.RemoveAt(i);
                continue;
            }
            return true;
        }
        return false;
    }

    void Falling()
    {
        if (m_rigidBody.isKinematic) m_rigidBody.isKinematic = false;

        if (m_groundPoundPressed)
        {
            StartGroundPound();
            m_groundPoundPressed = false; 
            return;
        }

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

        if (m_groundPoundPressed)
        {
            StartGroundPound();
            m_groundPoundPressed = false; 
            return;
        }

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

    void StartGroundPound()
    {
        m_state = State.GroundPoundPrepare;
        m_stateTimer = 0.0f;
        m_vel = Vector2.zero; // Stop movement
        m_rigidBody.velocity = Vector2.zero;
        m_rigidBody.isKinematic = true; // Disable physics/gravity

        // Spin animation
        Vector3 rotationAxis = new Vector3(0, 0, isFacingRight ? -360 : 360);
        m_rendererTransform.DORotate(rotationAxis, m_groundPoundPrepareTime, RotateMode.LocalAxisAdd)
            .SetEase(Ease.OutQuad);
    }

    void GroundPoundPrepare()
    {
        m_stateTimer += Time.fixedDeltaTime;
        if (m_stateTimer >= m_groundPoundPrepareTime)
        {
            m_state = State.GroundPoundFall;
            m_rigidBody.isKinematic = false; // Re-enable physics for collision
        }
    }

    void GroundPoundFall()
    {
        m_vel.x = 0;
        m_vel.y = -m_groundPoundSpeed;
        ApplyVelocity();
    }

    void Walking()
    {
        if (m_recoilTimer > 0)
        {
            if (!IsGrounded())
            {
                m_state = State.Falling;
                return;
            }
            m_isMoving = false;
            m_rigidBody.isKinematic = false;
            m_vel.x *= m_groundFriction;
            ApplyVelocity();
            return;
        }

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

                if (!IsGrounded())
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

        if (!m_isMoving && !IsGrounded())
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
            
            RaycastHit2D[] hits = Physics2D.BoxCastAll(originCast, sizeCast, 0, direction, maxDistance, m_obstacleLayerMask);
            
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

        if (m_wantsLeft)
        {
            isFacingRight = false;
        }
        else if (m_wantsRight)
        {
            isFacingRight = true;
        }

        m_jumpPressed = Input.GetKeyDown(KeyCode.UpArrow);
        m_jumpHeld = Input.GetKey(KeyCode.UpArrow);
        m_shootPressed = Input.GetKeyDown(KeyCode.Space);
        
        if (Input.GetKeyDown(KeyCode.Z))
        {
            // Only set the flag if the player is in the air
            if (m_state == State.Falling || m_state == State.Jumping)
            {
                m_groundPoundPressed = true;
            }
        }
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
                    if (m_state == State.Falling || m_state == State.GroundPoundFall)
                    {
                        if (m_state == State.GroundPoundFall)
                        {
                             CameraController.Instance.Shake(0.3f, 0.8f);
                             
                            m_rendererTransform.DOKill();
                            m_rendererTransform.localScale = Vector3.one;
                            m_rendererTransform.localPosition = Vector3.zero;

                            float squashYScale = 0.5f;
                            float stretchXScale = 1.5f;
                            float rendererHeight = m_collider.size.y;
                            float bounceHeight = (rendererHeight * (1f - squashYScale)) / 2f; 

                            Sequence squashSequence = DOTween.Sequence();
                            squashSequence.Append(m_rendererTransform.DOScale(new Vector3(stretchXScale, squashYScale, 1f), 0.1f).SetEase(Ease.OutQuad))
                                .Join(m_rendererTransform.DOLocalMoveY(-bounceHeight, 0.1f).SetEase(Ease.OutQuad)) // Changed to negative
                                .Append(m_rendererTransform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutElastic))
                                .Join(m_rendererTransform.DOLocalMoveY(0, 0.2f).SetEase(Ease.OutElastic));
                        }
                        else
                        {
                             CameraController.Instance.Shake(0.2f, 0.5f);
                        }

                        m_rendererTransform.rotation = Quaternion.identity;

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
