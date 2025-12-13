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
    [SerializeField] private TextMeshProUGUI m_questTitleText; // Title for the quest list
    [SerializeField] private Transform m_questListContent; // Parent for UI items
    [SerializeField] private GameObject m_questItemPrefab; // Prefab for a single quest UI item
    [SerializeField] private GameObject m_completionPanel;
    [SerializeField] private ParticleSystem m_confettiEffect;

    [Header("Quests")]
    [SerializeField] private List<QuestItem> m_quests = new List<QuestItem>();

    private List<QuestUIItem> m_questUIItems = new List<QuestUIItem>();
    private bool m_allQuestsCompleted = false;
    private int m_currentQuestIndex = 0;

    void Start()
    {
        if (m_quests.Count == 0)
        {
            InitializeDefaultQuests();
        }
        
        CreateQuestUI();
        UpdateQuestTitle();
        m_completionPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    private void InitializeDefaultQuests()
    {
        m_quests.Add(new QuestItem { id = "touch_spikes", description = "1. Touch the spikes", targetAmount = 2 });
        m_quests.Add(new QuestItem { id = "hit_by_enemy", description = "2. Get hit by an enemy" });
        m_quests.Add(new QuestItem { id = "use_pressure_pad", description = "3. Get used to the pressure pad", targetAmount = 3 });
        m_quests.Add(new QuestItem { id = "shoot_enemy", description = "4. Kill an enemy: Shoot", isSubQuest = true });
        m_quests.Add(new QuestItem { id = "stomp_enemy", description = "5. Kill an enemy: Stomp", isSubQuest = true });
        m_quests.Add(new QuestItem { id = "ground_pound_enemy", description = "6. Kill an enemy: Ground Pound (Press Z)", isSubQuest = true });
    }

    private void CreateQuestUI()
    {
        // Clear existing UI
        foreach (Transform child in m_questListContent)
        {
            Destroy(child.gameObject);
        }
        m_questUIItems.Clear();

        // Show only the current quest
        if (m_currentQuestIndex < m_quests.Count)
        {
            ShowQuest(m_quests[m_currentQuestIndex]);
        }
    }

    private void ShowQuest(QuestItem quest)
    {
        GameObject itemGO = Instantiate(m_questItemPrefab, m_questListContent);
        QuestUIItem uiItem = itemGO.GetComponent<QuestUIItem>();
        uiItem.Setup(quest);
        m_questUIItems.Add(uiItem);
    }

    public void CompleteQuest(string questId)
    {
        AddQuestProgress(questId, 1);
    }

    public void AddQuestProgress(string questId, int amount = 1)
    {
        if (m_allQuestsCompleted || m_currentQuestIndex >= m_quests.Count) return;

        // Check if the progress is for the current quest
        QuestItem currentQuest = m_quests[m_currentQuestIndex];
        if (currentQuest.id != questId || currentQuest.isCompleted)
        {
            return;
        }

        currentQuest.currentAmount += amount;
        UpdateQuestUI(); 

        if (currentQuest.currentAmount >= currentQuest.targetAmount)
        {
            currentQuest.currentAmount = currentQuest.targetAmount;
            currentQuest.isCompleted = true;
            
            UpdateQuestUI(); 

            m_currentQuestIndex++;
            UpdateQuestTitle();

            if (m_currentQuestIndex < m_quests.Count)
            {
                ShowQuest(m_quests[m_currentQuestIndex]);
            }
            else
            {
                CheckForAllQuestsCompleted();
            }
        }
    }

    private void UpdateQuestUI()
    {
        foreach (var uiItem in m_questUIItems)
        {
            uiItem.UpdateUI();
        }
    }

    private void UpdateQuestTitle()
    {
        if (m_questTitleText != null)
        {
            int displayIndex = m_allQuestsCompleted ? m_quests.Count : m_currentQuestIndex;
            int totalQuests = m_quests.Count;
            m_questTitleText.text = $"How to Forge a Square Agent ({displayIndex}/{totalQuests})";
        }
    }

    private void CheckForAllQuestsCompleted()
    {
        m_allQuestsCompleted = true;
        StartCoroutine(AllQuestsCompletedRoutine());
    }

    private IEnumerator AllQuestsCompletedRoutine()
    {
        m_completionPanel.SetActive(true);
        if (m_confettiEffect != null)
        {
            m_confettiEffect.gameObject.SetActive(true);
            m_confettiEffect.Play();
        }
        yield break;
    }
}
