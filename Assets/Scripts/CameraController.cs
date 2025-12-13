using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoSingleton<CameraController>
{
    private Transform m_cameraTransform;
    private Vector3 m_originalCameraPos;
    private Coroutine m_shakeCoroutine;

    void Start()
    {
        if (Camera.main != null)
        {
            m_cameraTransform = Camera.main.transform;
            m_originalCameraPos = m_cameraTransform.localPosition;
        }
    }

    public void Shake(float duration, float magnitude)
    {
        if (m_cameraTransform == null) return;

        if (m_shakeCoroutine != null)
        {
            StopCoroutine(m_shakeCoroutine);
        }
        m_shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
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
}
