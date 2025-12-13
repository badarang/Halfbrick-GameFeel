using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CameraController : MonoSingleton<CameraController>
{
    private Camera m_camera;
    private Transform m_cameraTransform;
    private Vector3 m_originalCameraPos;
    private float m_originalFOV;
    private Coroutine m_shakeCoroutine;

    void Start()
    {
        if (Camera.main != null)
        {
            m_camera = Camera.main;
            m_cameraTransform = m_camera.transform;
            m_originalCameraPos = m_cameraTransform.localPosition;
            m_originalFOV = m_camera.fieldOfView;
        }
    }

    public void Shake(float duration, float magnitude)
    {
        if (m_cameraTransform == null) return;

        if (m_shakeCoroutine != null)
        {
            StopCoroutine(m_shakeCoroutine);
            m_cameraTransform.localPosition = m_originalCameraPos; 
        }
        m_shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    // New directional shake method
    public void Shake(Vector2 direction, float duration, float magnitude)
    {
        if (m_cameraTransform == null) return;

        if (m_shakeCoroutine != null)
        {
            StopCoroutine(m_shakeCoroutine);
            m_cameraTransform.localPosition = m_originalCameraPos;
        }
        m_shakeCoroutine = StartCoroutine(DirectionalShakeRoutine(direction, duration, magnitude));
    }

    public void Punch(float punchAmount, float duration)
    {
        if (m_camera == null) return;

        m_camera.DOKill(); 
        m_camera.DOFieldOfView(m_originalFOV - punchAmount, duration / 2).SetEase(Ease.OutQuad).OnComplete(() => {
            m_camera.DOFieldOfView(m_originalFOV, duration / 2).SetEase(Ease.InQuad);
        });
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float yOffset = Random.Range(-1f, 1f) * magnitude;
            m_cameraTransform.localPosition = m_originalCameraPos + new Vector3(0, yOffset, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        m_cameraTransform.localPosition = m_originalCameraPos;
    }

    private IEnumerator DirectionalShakeRoutine(Vector2 direction, float duration, float magnitude)
    {
        float elapsed = 0.0f;
        
        // Initial kick in the direction
        m_cameraTransform.localPosition = m_originalCameraPos + (Vector3)(direction.normalized * magnitude);

        while (elapsed < duration)
        {
            // Shake around the offset position, gradually returning to original
            float damping = 1.0f - (elapsed / duration);
            float xOffset = Random.Range(-0.5f, 0.5f) * magnitude * damping;
            float yOffset = Random.Range(-0.5f, 0.5f) * magnitude * damping;
            
            // Main shake component is along the direction vector
            Vector3 shakeOffset = (Vector3)(direction.normalized * magnitude * damping * (Mathf.Sin(elapsed * 50f))); 

            m_cameraTransform.localPosition = m_originalCameraPos + shakeOffset + new Vector3(xOffset, yOffset, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        m_cameraTransform.localPosition = m_originalCameraPos;
    }
}
