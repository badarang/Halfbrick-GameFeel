using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRender : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _sprite;

    public IEnumerator ApplyHitFlashEffectRoutine(float _fullDuration)
    {
        var _duration = .04f;
        var waitSec = new WaitForSeconds(_duration);
        for (var i = 0; i < 2; i++)
        {
            yield return waitSec;
            ApplyFlashColor(Color.red);
            yield return waitSec;
            ApplyFlashColor(Color.white);
            yield return waitSec;
            ApplyFlashColor(Color.yellow);
        }

        yield return new WaitForSeconds(.07f);
        _fullDuration -= _duration * 6 + .07f;
        _duration = .08f;
        var waitSec2 = new WaitForSeconds(_duration);
        var _repeatTimes = (int)(_fullDuration / (_duration * 2f));
        for (var i = 0; i < _repeatTimes; i++)
        {
            _sprite.color = new Color(1, 1, 1, 0);
            yield return waitSec2;
            _sprite.color = new Color(1, 1, 1, 1);
            yield return waitSec2;
        }

        // Reset to original state
        _sprite.color = Color.white;
        if (_sprite.material != null && _sprite.material.HasProperty("_HitEffectBlend"))
        {
            _sprite.material.SetFloat("_HitEffectBlend", 0f);
        }
    }

    // New routine for bounce effect (positive feedback)
    public IEnumerator ApplyBounceEffectRoutine(float duration)
    {
        float flashSpeed = 0.1f;
        int flashes = (int)(duration / (flashSpeed * 2));
        
        for (int i = 0; i < flashes; i++)
        {
            ApplyFlashColor(Color.yellow); // Use yellow for positive feedback
            yield return new WaitForSeconds(flashSpeed);
            
            // Reset effect
            if (_sprite.material != null && _sprite.material.HasProperty("_HitEffectBlend"))
            {
                _sprite.material.SetFloat("_HitEffectBlend", 0f);
            }
            yield return new WaitForSeconds(flashSpeed);
        }

        // Ensure reset at the end
        _sprite.color = Color.white;
        if (_sprite.material != null && _sprite.material.HasProperty("_HitEffectBlend"))
        {
            _sprite.material.SetFloat("_HitEffectBlend", 0f);
        }
    }

    private void ApplyFlashColor(Color color)
    {
        if (_sprite.material != null && _sprite.material.HasProperty("_HitEffectColor") &&
            _sprite.material.HasProperty("_HitEffectBlend"))
        {
            _sprite.material.SetColor("_HitEffectColor", color);
            _sprite.material.SetFloat("_HitEffectBlend", .5f); // Blend factor
        }
    }
}
