using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Rigidbody2D))]
public class ShellCasing : MonoBehaviour
{
    [SerializeField] private float m_lifeTime = 2.0f;
    [SerializeField] private float m_fadeTime = 0.5f;

    private Rigidbody2D m_rb;
    private SpriteRenderer m_spriteRenderer;

    void Awake()
    {
        m_rb = GetComponent<Rigidbody2D>();
        m_spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(Vector2 force, float torque)
    {
        m_rb.AddForce(force, ForceMode2D.Impulse);
        m_rb.AddTorque(torque, ForceMode2D.Impulse);
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(m_lifeTime - m_fadeTime);
        m_spriteRenderer.DOFade(0, m_fadeTime).OnComplete(() => Destroy(gameObject));
    }
}
