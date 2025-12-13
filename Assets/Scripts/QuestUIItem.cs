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

    public void Setup(QuestItem quest)
    {
        m_descriptionText.text = quest.description;
        
        // Indentation for sub-quests
        if (quest.isSubQuest)
        {
            m_descriptionText.margin = new Vector4(20, 0, 0, 0);
        }

        UpdateUI(quest);
    }

    public void UpdateUI(QuestItem quest)
    {
        if (quest.isCompleted)
        {
            m_checkMark.SetActive(true);
            m_progressText.text = "(완료!)";
            m_descriptionText.color = Color.gray;
            m_descriptionText.fontStyle = FontStyles.Strikethrough; // Apply strikethrough
        }
        else
        {
            m_checkMark.SetActive(false);
            m_descriptionText.color = Color.black;
            m_descriptionText.fontStyle = FontStyles.Normal; // Remove strikethrough
            
            if (quest.targetAmount > 1)
            {
                m_progressText.text = $"({quest.currentAmount}/{quest.targetAmount})";
            }
            else
            {
                m_progressText.text = "";
            }
        }
    }
}
