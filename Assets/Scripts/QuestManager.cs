using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; // Added for TextMeshPro

[System.Serializable]
public class QuestItem
{
    public string id;
    public string description;
    public int targetAmount = 1;
    public int currentAmount = 0;
    public bool isCompleted = false;
    public bool isSubQuest = false; 
}

public class QuestManager : MonoSingleton<QuestManager>
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI m_questTitleText;
    [SerializeField] private Transform m_questListContent; // Parent for UI items
    [SerializeField] private GameObject m_questItemPrefab; // Prefab for a single quest UI item
    [SerializeField] private GameObject m_completionPanel;
    [SerializeField] private TextMeshProUGUI m_completionText;
    [SerializeField] private ParticleSystem m_confettiEffect;

    [Header("Quests")]
    [SerializeField] private List<QuestItem> m_quests = new List<QuestItem>();

    private List<QuestUIItem> m_questUIItems = new List<QuestUIItem>();
    private bool m_allQuestsCompleted = false;

    void Start()
    {
        if (m_quests.Count == 0)
        {
            InitializeDefaultQuests();
        }
        
        CreateQuestUI();
        UpdateQuestUI();
        m_completionPanel.SetActive(false);
    }

    void Update()
    {
        if (m_allQuestsCompleted && Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void InitializeDefaultQuests()
    {
        m_quests.Add(new QuestItem { id = "touch_spikes", description = "1. 가시에 닿기" });
        m_quests.Add(new QuestItem { id = "hit_by_enemy", description = "2. 적에게 맞기" });
        m_quests.Add(new QuestItem { id = "use_pressure_pad", description = "3. 압력 패드 익숙해지기", targetAmount = 3 });
        m_quests.Add(new QuestItem { id = "shoot_enemy", description = "4-1. 총 쏘기", isSubQuest = true });
        m_quests.Add(new QuestItem { id = "stomp_enemy", description = "4-2. 적 밟기", isSubQuest = true });
        m_quests.Add(new QuestItem { id = "ground_pound_enemy", description = "4-3. z 키를 눌러서 적 밟기", isSubQuest = true });
    }

    private void CreateQuestUI()
    {
        // Header for section 4
        if (m_questItemPrefab != null)
        {
            GameObject headerGO = new GameObject("QuestHeader");
            headerGO.transform.SetParent(m_questListContent, false);
            TextMeshProUGUI headerText = headerGO.AddComponent<TextMeshProUGUI>();
            headerText.text = "4. 적을 죽이는 3가지 방법";
            headerText.fontSize = 14; // Example size
            headerText.color = Color.black;
            headerText.fontStyle = FontStyles.Bold;
        }

        foreach (var quest in m_quests)
        {
            GameObject itemGO = Instantiate(m_questItemPrefab, m_questListContent);
            QuestUIItem uiItem = itemGO.GetComponent<QuestUIItem>();
            uiItem.Setup(quest);
            m_questUIItems.Add(uiItem);
        }
    }

    public void CompleteQuest(string questId)
    {
        AddQuestProgress(questId, 1);
    }

    public void AddQuestProgress(string questId, int amount = 1)
    {
        if (m_allQuestsCompleted) return;

        QuestItem quest = m_quests.Find(q => q.id == questId);
        if (quest != null && !quest.isCompleted)
        {
            quest.currentAmount += amount;
            if (quest.currentAmount >= quest.targetAmount)
            {
                quest.currentAmount = quest.targetAmount;
                quest.isCompleted = true;
                CheckForAllQuestsCompleted();
            }
            UpdateQuestUI();
        }
    }

    private void UpdateQuestUI()
    {
        for (int i = 0; i < m_quests.Count; i++)
        {
            if (i < m_questUIItems.Count)
            {
                m_questUIItems[i].UpdateUI(m_quests[i]);
            }
        }
    }

    private void CheckForAllQuestsCompleted()
    {
        foreach (var quest in m_quests)
        {
            if (!quest.isCompleted) return;
        }

        m_allQuestsCompleted = true;
        StartCoroutine(AllQuestsCompletedRoutine());
    }

    private IEnumerator AllQuestsCompletedRoutine()
    {
        m_completionPanel.SetActive(true);
        m_completionText.text = "Congrats!";
        if (m_confettiEffect != null)
        {
            m_confettiEffect.Play();
        }

        yield return new WaitForSeconds(3.0f);

        m_completionText.text = "Still Enough?\nPress R to Retry!";
    }
}
