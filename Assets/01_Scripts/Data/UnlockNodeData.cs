using UnityEngine;

[CreateAssetMenu(fileName = "UnlockNode_", menuName = "Content/UnlockNodeData")]
public class UnlockNodeData : ScriptableObject
{
    [Header("해금 ID")]
    [SerializeField] private Define.UnlockGroupType groupType = Define.UnlockGroupType.None;
    [SerializeField] private int index;

    [Header("선행 해금 데이터")]
    [SerializeField] private UnlockNodeData prerequisiteData;

    [Header("가격")]
    [SerializeField] private int price = 100;

    [Header("생성할 프리팹")]
    [SerializeField] private GameObject targetPrefab;

    // 그룹을 지정하지 않은 데이터는 아직 해금 지점으로 사용할 준비가 되지 않았습니다.
    public bool HasID => groupType != Define.UnlockGroupType.None;

    // 선행 데이터가 있다면 그 데이터에도 유효한 ID가 있어야 조건을 비교할 수 있습니다.
    public bool HasValidPrerequisite => prerequisiteData == null || prerequisiteData.HasID;

    // 하나의 데이터 에셋이 하나의 해금 지점을 나타내도록 ID를 데이터에서 만듭니다.
    public string UniqueID => $"{groupType}_{index}";

    // 선행 데이터가 없다면 시작부터 구매할 수 있도록 빈 조건을 전달합니다.
    public string PrerequisiteID => prerequisiteData != null ? prerequisiteData.UniqueID : null;

    // 발판은 가격과 생성 대상을 데이터에서 읽고 씬별 설정은 직접 참조합니다.
    public int Price => price;
    public GameObject TargetPrefab => targetPrefab;
}
