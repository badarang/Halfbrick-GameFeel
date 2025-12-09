using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public class Player : MonoSingleton<Player>
{
    public float m_moveAccel = (0.12f * 60.0f);
    public float m_groundFriction = 0.85f;
    public float m_gravity = (-0.05f * 60.0f);
    public float m_jumpVel = 0.75f;
    public float m_jumpMinTime = 0.06f;
    public float m_jumpMaxTime = 0.20f;
    public float m_airFallFriction = 0.975f;
    public float m_airMoveFriction = 0.85f;

    private Rigidbody2D m_rigidBody = null;
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

    private enum State
    {
        Idle = 0,
        Falling,
        Jumping,
        Walking
    };

    private State m_state = State.Idle;

    // Use this for initialization
    void Start ()
    {
        m_rigidBody = transform.GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        UpdateInput();

        if (m_shootPressed && m_hasWeapon)
        {
            //Fire
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

    void Idle()
    {
        m_vel = Vector2.zero;
        //Check to see whether to go into movement of some sort
        if (m_groundObjects.Count == 0)
        {
            //No longer on the ground, fall.
            m_state = State.Falling;
            return;
        }

        //Check input for other state transitions
        if (m_jumpPressed || m_jumpHeld)
        {
            m_stateTimer = 0;
            m_state = State.Jumping;
            return;
        }

        //Test for input to move
        if (m_wantsLeft || m_wantsRight)
        {
            m_state = State.Walking;
            return;
        }
    }

    void Falling()
    {
        m_vel.y += m_gravity * Time.fixedDeltaTime;
        m_vel.y *= m_airFallFriction;
        if (m_wantsLeft)
        {
            m_vel.x -= m_moveAccel * Time.fixedDeltaTime;
        }
        else if (m_wantsRight)
        {
            m_vel.x += m_moveAccel * Time.fixedDeltaTime;
        }

        m_vel.x *= m_airMoveFriction;

        ApplyVelocity();
    }

    void Jumping()
    {
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

        if (m_wantsLeft)
        {
            m_vel.x -= m_moveAccel * Time.fixedDeltaTime;
        }
        else if (m_wantsRight)
        {
            m_vel.x += m_moveAccel * Time.fixedDeltaTime;
        }

        m_vel.x *= m_airMoveFriction;

        ApplyVelocity();
    }

    void Walking()
    {
        if (m_wantsLeft)
        {
            m_vel.x -= m_moveAccel * Time.fixedDeltaTime;
        }
        else if (m_wantsRight)
        {
            m_vel.x += m_moveAccel * Time.fixedDeltaTime;
        }
        else if (m_vel.x >= -0.05f && m_vel.x <= 0.05)
        {
            m_state = State.Idle;
            m_vel.x = 0;
        }

        m_vel.y = 0;
        m_vel.x *= m_groundFriction;

        ApplyVelocity();

        if (m_groundObjects.Count == 0)
        {
            //No longer on the ground, fall.
            m_state = State.Falling;
            return;
        }

        if (m_jumpPressed || m_jumpHeld)
        {
            m_stateTimer = 0;
            m_state = State.Jumping;
            return;
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
        m_groundObjects.Remove(collision.gameObject);
    }

    private void ProcessCollision(Collision2D collision)
    {
        m_groundObjects.Remove(collision.gameObject);
        Vector3 pos = m_rigidBody.transform.position;

        foreach (ContactPoint2D contact in collision.contacts)
        {
            //Push back out
            Vector2 impulse = contact.normal * (contact.normalImpulse / Time.fixedDeltaTime);
            pos.x += impulse.x;
            pos.y += impulse.y;

            if (Mathf.Abs(contact.normal.y) > Mathf.Abs(contact.normal.x))
            {
                //Hit ground
                if (contact.normal.y > 0)
                {
                    if (m_groundObjects.Contains(contact.collider.gameObject) == false)
                    {
                        m_groundObjects.Add(contact.collider.gameObject);
                    }
                    if (m_state == State.Falling)
                    {
                        //If we've been pushed up, we've hit the ground.  Go to a ground-based state.
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
                //Hit Roof
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
        m_rigidBody.transform.position = pos;
    }
}
