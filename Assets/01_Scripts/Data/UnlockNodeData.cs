using UnityEngine;

[CreateAssetMenu(fileName = "UnlockNode_", menuName = "Content/UnlockNodeData")]
public class UnlockNodeData : ScriptableObject
{
    [Header("선행 해금 데이터")]
    [SerializeField] private UnlockNodeData prerequisiteData;

    [Tooltip("생성하는 기계의 저장용 고유 ID입니다. 기계 해금 데이터에서만 설정합니다.")]
    [SerializeField] private string machineUpgradeId;
    public string MachineUpgradeId => machineUpgradeId;

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
