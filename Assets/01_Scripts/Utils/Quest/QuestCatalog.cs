using System.Collections.Generic;
using UnityEngine;

/// <summary>게임에서 순서대로 진행할 퀘스트 에셋 목록을 보관합니다.</summary>
[CreateAssetMenu(fileName = "QuestCatalog", menuName = "Core/Quest Catalog")]
public class QuestCatalog : ScriptableObject
{
    [Tooltip("위에서 아래 순서로 플레이어에게 보여줄 퀘스트입니다.")]
    public List<QuestData> quests = new List<QuestData>();
}
