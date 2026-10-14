using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>현재 퀘스트의 진행 순서와 설정된 보상 지급을 관리합니다.</summary>
[Serializable]
public class QuestManager
{
    /// <summary>카탈로그 순서대로 만든 퀘스트 실행 객체를 보관합니다.</summary>
    private readonly List<Quest> _quests = new List<Quest>();
    private int _currentIndex;

    /// <summary>현재 진행 중인 퀘스트 객체를 반환합니다.</summary>
    public Quest CurrentQuest => _currentIndex < _quests.Count ? _quests[_currentIndex] : null;

    /// <summary>퀘스트 표시 정보가 바뀌면 화면에 알립니다.</summary>
    public event Action<string, string, int, int, bool> OnQuestChanged;

    /// <summary>Inspector에서 받은 카탈로그를 연결하고 첫 퀘스트부터 진행하게 합니다.</summary>
    public void Init(QuestCatalog catalog)
    {
        // 다시 초기화될 때 이전 실행 상태가 남지 않도록 현재 목록부터 비웁니다.
        _quests.Clear();
        _currentIndex = 0;

        // 데이터가 누락된 경우 조용히 진행이 멈추지 않도록 원인을 콘솔에 남깁니다.
        if (catalog == null)
        {
            Debug.LogError("[QuestManager] QuestCatalog 에셋이 Managers에 연결되지 않았습니다.");
            NotifyCurrentQuest();
            return;
        }

        // 카탈로그 순서를 유지하면서 각 설정 에셋에 대응하는 실행 객체를 만듭니다.
        if (catalog.quests == null)
        {
            Debug.LogError("[QuestManager] QuestCatalog의 퀘스트 목록이 비어 있거나 초기화되지 않았습니다.");
            NotifyCurrentQuest();
            return;
        }

        foreach (QuestData data in catalog.quests)
        {
            if (data == null)
            {
                Debug.LogWarning("[QuestManager] QuestCatalog에 비어 있는 퀘스트 항목이 있습니다.");
                continue;
            }

            _quests.Add(new Quest(data));
        }

        StartCurrentQuest();
    }

    /// <summary>현재 순서의 퀘스트를 시작하고 화면에 목표를 알립니다.</summary>
    public void StartCurrentQuest()
    {
        Quest quest = CurrentQuest;
        if (quest == null)
        {
            NotifyCurrentQuest();
            return;
        }

        // 시작 여부와 진행도 초기화는 해당 퀘스트 객체가 책임집니다.
        quest.Start();
        NotifyCurrentQuest();
    }

    /// <summary>게임 행동을 받아 현재 퀘스트 목표와 맞으면 진행 수량을 더합니다.</summary>
    public void ReportProgress(Define.QuestObjectiveType objectiveType, Define.QuestTargetType targetType, int amount = 1)
    {
        Quest quest = CurrentQuest;
        if (quest == null || !quest.IsStarted || quest.Data == null || amount <= 0 || targetType == Define.QuestTargetType.Unknown)
            return;

        // 행동 종류는 같아야 하며 Any 대상은 해당 행동의 모든 대상을 받습니다.
        if (quest.Data.objectiveType != objectiveType ||
            (quest.Data.targetType != Define.QuestTargetType.Any && quest.Data.targetType != targetType))
        {
            return;
        }

        // 행동 발생 횟수를 더하고 현재 퀘스트 UI에 새 진행도를 알립니다.
        quest.AddProgress(amount);
        NotifyCurrentQuest();
        if (quest.Progress < Mathf.Max(1, quest.Data.targetAmount))
            return;

        // 필요한 행동 횟수에 도달했으므로 완료 처리와 보상 지급 후 다음 퀘스트로 이동합니다.
        quest.Complete();
        GiveReward(quest.Data);
        _currentIndex++;
        StartCurrentQuest();
    }

    /// <summary>아이템 종류를 퀘스트 대상 enum으로 바꿔 제조나 서빙 진행을 보고합니다.</summary>
    public void ReportItemProgress(Define.QuestObjectiveType objectiveType, Define.ItemType itemType, int amount = 1)
    {
        // 아이템 종류와 목표 대상의 연결 규칙을 매니저 한 곳에서 관리합니다.
        ReportProgress(objectiveType, GetItemTargetType(itemType), amount);
    }

    /// <summary>노드 고유 ID를 퀘스트 대상 enum으로 바꿔 해금 행동을 보고합니다.</summary>
    public void ReportNodeUnlock(string nodeId)
    {
        // 해금 지점은 내부 문자열 대신 미리 정한 enum 대상으로 진행도를 전달합니다.
        ReportProgress(Define.QuestObjectiveType.Unlock, GetNodeTargetType(nodeId));
    }

    /// <summary>가구 카탈로그 ID를 퀘스트 대상 enum으로 바꿔 새 배치 행동을 보고합니다.</summary>
    public void ReportFurniturePlacement(string furnitureId)
    {
        // 카탈로그 ID를 인스펙터에서 선택 가능한 enum 대상으로 바꿉니다.
        ReportProgress(Define.QuestObjectiveType.PlaceFurniture, GetFurnitureTargetType(furnitureId));
    }

    /// <summary>아이템 enum을 퀘스트 대상 enum으로 바꿉니다.</summary>
    private static Define.QuestTargetType GetItemTargetType(Define.ItemType itemType)
    {
        // 새 아이템을 추가할 때 대응되는 퀘스트 대상 enum도 함께 연결합니다.
        switch (itemType)
        {
            case Define.ItemType.Money:
                return Define.QuestTargetType.Money;
            case Define.ItemType.Beer:
                return Define.QuestTargetType.Beer;
            case Define.ItemType.Beer_Empty:
                return Define.QuestTargetType.EmptyBeer;
            case Define.ItemType.Beer_Dirty:
                return Define.QuestTargetType.DirtyBeer;
            case Define.ItemType.TestMeal:
                return Define.QuestTargetType.TestMeal;
            default:
                return Define.QuestTargetType.Unknown;
        }
    }

    /// <summary>노드 고유 ID를 퀘스트 대상 enum으로 바꿉니다.</summary>
    private static Define.QuestTargetType GetNodeTargetType(string nodeId)
    {
        // 현재 씬에서 사용하는 노드 ID를 대응 enum과 연결해 문자열 오타를 막습니다.
        switch (nodeId)
        {
            case "Level_0":
                return Define.QuestTargetType.LevelNode0;
            case "Counter_0":
                return Define.QuestTargetType.CounterNode0;
            case "Kitchen_0":
                return Define.QuestTargetType.KitchenNode0;
            case "Stool_0":
                return Define.QuestTargetType.StoolNode0;
            case "Stool_1":
                return Define.QuestTargetType.StoolNode1;
            case "Stool_2":
                return Define.QuestTargetType.StoolNode2;
            case "BeerMachine_0":
                return Define.QuestTargetType.BeerMachineNode0;
            case "Sink_0":
                return Define.QuestTargetType.SinkNode0;
            default:
                return Define.QuestTargetType.Unknown;
        }
    }

    /// <summary>가구 카탈로그 ID를 퀘스트 대상 enum으로 바꿉니다.</summary>
    private static Define.QuestTargetType GetFurnitureTargetType(string furnitureId)
    {
        // 새 가구를 카탈로그에 추가할 때 대응되는 퀘스트 대상 enum도 함께 연결합니다.
        switch (furnitureId)
        {
            case "chair":
                return Define.QuestTargetType.ChairFurniture;
            default:
                return Define.QuestTargetType.Unknown;
        }
    }

    /// <summary>퀘스트 설정에 저장된 게임 머니를 한 번 지급합니다.</summary>
    private void GiveReward(QuestData data)
    {
        // 보상이 없으면 머니 갱신을 생략합니다.
        if (data == null || data.rewardMoney <= 0)
            return;

        // 플레이어가 준비되지 않았다면 지급하지 않고 원인을 콘솔에 남깁니다.
        PlayerController player = PlayerController.Instance;
        if (player == null)
        {
            Debug.LogWarning($"[QuestManager] '{data.title}' 보상을 지급할 플레이어가 없습니다.");
            return;
        }

        // 기존 머니 갱신 함수를 사용해 저장과 UI 알림도 함께 처리합니다.
        player.UpdateMoney(data.rewardMoney);
    }

    /// <summary>현재 퀘스트의 제목, 설명, 진행도, 완료 여부를 화면에 전달합니다.</summary>
    public void NotifyCurrentQuest()
    {
        Quest quest = CurrentQuest;
        if (quest == null)
        {
            OnQuestChanged?.Invoke("모든 퀘스트 완료", "새로운 목표를 준비하고 있습니다.", 0, 0, true);
            return;
        }

        OnQuestChanged?.Invoke(
            quest.Data.title,
            quest.Data.description,
            quest.Progress,
            quest.Data.targetAmount,
            quest.IsCompleted);
    }
}
