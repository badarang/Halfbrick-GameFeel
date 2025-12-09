using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spikes : MonoBehaviour {

    private SpriteRenderer m_sprite = null;
    private Color m_defaultColor = new Color();
    // Use this for initialization
    void Start()
    {
        m_sprite = transform.GetComponent<SpriteRenderer>();
        m_defaultColor = m_sprite.color;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        m_sprite.color = new Color(m_defaultColor.r / 2, m_defaultColor.g / 2, m_defaultColor.b / 2, 1);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        m_sprite.color = m_defaultColor;
    }
}
