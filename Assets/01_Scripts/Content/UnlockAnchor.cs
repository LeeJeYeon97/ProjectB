using UnityEngine;
using static Define;

public class UnlockAnchor : MonoBehaviour
{
    public string UniqueID => $"{groupType}_{index}";
    public string preRequisiteID => $"{prerequisiteGroupType}_{prerequisiteIndex}";

    [Header("Data Connection")]
    [SerializeField] private UnlockNodeData nodeData;

    [Header("그룹 타입 (대안 B)")]
    [SerializeField] private UnlockGroupType groupType = UnlockGroupType.None;
    [SerializeField] private int index = 0;
    [SerializeField] private int priceOverride = -1;

    [Header("선행 조건")]
    [SerializeField] private UnlockGroupType prerequisiteGroupType = UnlockGroupType.None;
    [SerializeField] private int prerequisiteIndex = 0;

    [Header("해금 방식")]
    [SerializeField] private UnlockType unlockType = UnlockType.SpawnPrefab;

    [Header("For ToggleActive (A방식 구역 확장용)")]
    [Tooltip("A방식일 때 활성화할 씬 안의 구역 오브젝트를 연결합니다.")]
    [SerializeField] private GameObject targetSceneObject;

    [Header("Visual Editor Settings")]
    [Tooltip("해금 발판(UnlockStation)이 소환될 위치")]
    [SerializeField] private Transform stationSpawnPoint;

    // Getters
    public UnlockNodeData NodeData => nodeData;
    public UnlockType UnlockType => unlockType;
    public GameObject TargetSceneObject => targetSceneObject;
    public Vector3 SpawnPosition => transform.position;
    public Quaternion SpawnRotation => transform.rotation;

    public Vector3 StationPosition
    {
        get
        {
            if (stationSpawnPoint != null) return stationSpawnPoint.position;
            return transform.position + transform.forward * -1f; // 지정 안 하면 정면 1m 앞에 발판 생성
        }
    }
    public int Price => (priceOverride > 0) ? priceOverride : (nodeData != null ? nodeData.price : 0);

    private void OnDrawGizmos()
    {
        // 에디터에서 디자이너가 발판 소환 위치를 직관적으로 볼 수 있게 녹색 구로 표시
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(StationPosition, 0.4f);

        // 정면 방향 표시
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * -1.0f);
    }
}