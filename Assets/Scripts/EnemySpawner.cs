using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject m_enemyPrefab;
    [SerializeField] private float m_respawnTime = 3.0f;

    private GameObject m_currentEnemy;

    void Start()
    {
        SpawnEnemy();
    }

    public void SpawnEnemy()
    {
        if (m_enemyPrefab != null)
        {
            m_currentEnemy = Instantiate(m_enemyPrefab, transform.position, Quaternion.identity);
            Enemy enemyScript = m_currentEnemy.GetComponent<Enemy>();
            if (enemyScript != null)
            {
                enemyScript.SetSpawner(this);
            }
        }
    }

    public void OnEnemyDied()
    {
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(m_respawnTime);
        SpawnEnemy();
    }
}
