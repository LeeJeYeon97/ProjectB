using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "UnlockNode_", menuName = "Content/UnlockNodeData")]
public class UnlockNodeData : ScriptableObject
{
    public int Id;
    public UnlockNodeData prerequisiteData;       // 선행 해금 데이터
    public int price = 100;             // 해금 가격
    public GameObject targetPrefab;     // 실제 스폰될 프리팹
}