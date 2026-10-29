using UnityEngine;

[CreateAssetMenu(fileName = "UnlockNode_", menuName = "Content/UnlockNodeData")]
public class UnlockNodeData : ScriptableObject
{
    [Header("선행 해금 데이터")]
    [SerializeField] private UnlockNodeData prerequisiteData;

    [Header("가격")]
    [SerializeField] private int price = 100;

    [Header("생성할 프리팹")]
    [SerializeField] private GameObject targetPrefab;

    // 선행 조건은 다른 해금 데이터 자체를 참조해 비교합니다.
    public UnlockNodeData PrerequisiteData => prerequisiteData;

    // 발판은 가격과 생성 대상을 데이터에서 읽고 씬별 설정은 직접 참조합니다.
    public int Price => price;
    public GameObject TargetPrefab => targetPrefab;
}
