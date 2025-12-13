using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private float m_animationDuration = 0.5f;

    private bool m_isCollected = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (m_isCollected) return;

        if (collision.CompareTag("Player"))
        {
            m_isCollected = true;
            Player player = collision.GetComponent<Player>();
            
            // Find and activate the weapon on the player
            Weapon weapon = player.GetComponentInChildren<Weapon>(true); // true to include inactive
            if (weapon != null)
            {
                weapon.gameObject.SetActive(true);
            }

            // Call original function
            player.GiveWeapon();

            // Rewarding animation
            transform.DOKill();
            Sequence sequence = DOTween.Sequence();
            sequence.Append(transform.DOMove(player.transform.position, m_animationDuration).SetEase(Ease.InBack))
                    .Join(transform.DOScale(Vector3.zero, m_animationDuration).SetEase(Ease.InBack))
                    .OnComplete(() => {
                        // Optional: Add particle effect or sound here
                        Destroy(gameObject);
                    });
        }
    }
}
