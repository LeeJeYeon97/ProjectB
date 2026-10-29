using UnityEngine;

/// <summary>한 개의 퀘스트가 보여줄 문구, 완료 조건, 완료 보상을 보관합니다.</summary>
[CreateAssetMenu(fileName = "Quest_", menuName = "Core/Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("화면 표시")]
    [Tooltip("퀘스트를 구분할 고유한 이름입니다.")]
    public string id;

    [Tooltip("퀘스트 화면에 표시할 제목입니다.")]
    public string title;

    [TextArea]
    [Tooltip("플레이어가 해야 할 일을 설명하는 문장입니다.")]
    public string description;

    [Header("현재 완료 조건")]
    [Tooltip("퀘스트가 기다리는 게임 행동입니다.")]
    public Define.QuestObjectiveType objectiveType;

    [Tooltip("행동의 대상을 선택합니다. Any를 고르면 해당 행동의 모든 대상을 셉니다.")]
    public Define.QuestTargetType targetType;

    [Tooltip("해금 퀘스트에서 모든 해금 지점을 목표로 삼습니다.")]
    public bool anyUnlockTarget;

    [Tooltip("해금 퀘스트에서 요구하는 해금 데이터입니다. 모든 해금 지점을 선택하면 사용하지 않습니다.")]
    public UnlockNodeData unlockTarget;

    [Min(1)]
    [Tooltip("행동이 몇 번 발생해야 퀘스트가 완료되는지 설정합니다.")]
    public int targetAmount = 1;

    [Header("완료 보상")]
    [Min(0)]
    [Tooltip("퀘스트를 완료하면 지급할 게임 머니입니다.")]
    public int rewardMoney = 100;

}
