using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class WeaponPickup : MonoBehaviour
{
    private bool m_isCollected = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (m_isCollected) return;

        if (collision.CompareTag("Player"))
        {
            m_isCollected = true;
            Player player = collision.GetComponent<Player>();
            
            // Give weapon to player
            player.GiveWeapon();

            // Disable collider to prevent re-triggering
            GetComponent<Collider2D>().enabled = false;

            // Rewarding animation sequence
            Sequence sequence = DOTween.Sequence();
            
            sequence.Append(transform.DOMoveY(transform.position.y + 2f, 0.7f).SetEase(Ease.OutQuad));
            sequence.Join(transform.DOScale(1.2f, 0.7f).SetEase(Ease.OutQuad));
            sequence.Join(transform.DORotate(new Vector3(0, 720, 0), 0.7f, RotateMode.FastBeyond360).SetEase(Ease.Linear));
            
            sequence.Append(transform.DOScale(0f, 0.2f).SetEase(Ease.InBack));

            sequence.OnComplete(() => {
                Destroy(gameObject);
            });
        }
    }
}
