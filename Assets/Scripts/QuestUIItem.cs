using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Added for TextMeshPro

public class QuestUIItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_descriptionText;
    [SerializeField] private TextMeshProUGUI m_progressText;
    [SerializeField] private GameObject m_checkMark; // Replaced completedLine with checkMark

    public QuestItem Quest { get; private set; }

    public void Setup(QuestItem quest)
    {
        Quest = quest;
        m_descriptionText.text = quest.description;
        
        // Indentation for sub-quests
        if (quest.isSubQuest)
        {
            m_descriptionText.margin = new Vector4(20, 0, 0, 0);
        }

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (Quest == null) return;

        if (Quest.isCompleted)
        {
            m_checkMark.SetActive(true);
            m_descriptionText.fontStyle = FontStyles.Strikethrough;
        }
        else
        {
            m_checkMark.SetActive(false);
            m_descriptionText.fontStyle = FontStyles.Normal;
        }

        if (Quest.targetAmount > 0)
        {
            m_progressText.text = $"({Quest.currentAmount}/{Quest.targetAmount})";
        }
        else
        {
            m_progressText.text = "";
        }
        
        if (m_progressText.transform.parent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_progressText.transform.parent.GetComponent<RectTransform>());
        }
    }
}
