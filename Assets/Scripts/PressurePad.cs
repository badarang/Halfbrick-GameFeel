using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PressurePad : MonoBehaviour {

    private SpriteRenderer m_sprite = null;
    private Color m_defaultColor = new Color();
    private Vector3 m_originalScale;

    [Header("Bounce Settings")]
    [SerializeField] private float m_bounceForce = 20f;
    [SerializeField] private float m_animationDuration = 0.2f;

	void Start () {
        m_sprite = transform.GetComponent<SpriteRenderer>();
        m_defaultColor = m_sprite.color;
        m_originalScale = transform.localScale;
    }
	
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();
            if (player != null)
            {
                StartCoroutine(BouncePlayer(player));
            }
        }
    }

    private IEnumerator BouncePlayer(Player player)
    {
        GetComponent<Collider2D>().enabled = false;

        QuestManager.Instance.AddQuestProgress("use_pressure_pad");

        transform.DOScaleY(m_originalScale.y * 0.5f, m_animationDuration / 2).SetEase(Ease.OutQuad);

        Transform playerTransform = player.m_rendererTransform;
        Vector3 playerOriginalScale = playerTransform.localScale;
        playerTransform.DOScale(new Vector3(1.5f, 0.5f, 1f), m_animationDuration / 2).SetEase(Ease.OutQuad);

        UnitRenderer playerRender = player.GetComponentInChildren<UnitRenderer>();
        if (playerRender != null)
        {
            StartCoroutine(playerRender.ApplyBounceEffectRoutine(m_animationDuration));
        }

        yield return new WaitForSeconds(m_animationDuration / 2);

        player.Bounce(new Vector2(0, m_bounceForce));

        transform.DOScaleY(m_originalScale.y, m_animationDuration / 2).SetEase(Ease.InQuad);

        playerTransform.DOScale(new Vector3(0.7f, 1.3f, 1f), m_animationDuration).SetEase(Ease.OutQuad).OnComplete(() => {
            playerTransform.DOScale(playerOriginalScale, 0.1f);
        });

        yield return new WaitForSeconds(0.5f);
        GetComponent<Collider2D>().enabled = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // This is handled by the coroutine now
    }
}
