using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "UnlockNode_", menuName = "Content/UnlockNodeData")]
public class UnlockNodeData : ScriptableObject
{
    public int Id;
    public UnlockNodeData prerequisiteData;       // 선행 해금 데이터
    public int price = 100;             // 해금 가격
    public GameObject targetPrefab;     // 실제 스폰될 프리팹

//    #if UNITY_EDITOR
//    // Reset()은 유니티에서 이 에셋이 '처음 생성될 때' 1번 자동으로 호출되는 함수입니다.
//    private void Reset()
//    {
//        AutoSetId();
//    }

//    // 인스펙터 창에서 스크립트 이름(UnlockNodeData)을 우클릭했을 때 수동으로도 실행할 수 있게 해줍니다.
//    [ContextMenu("ID 자동 재할당 (Auto Set ID)")]
//    public void AutoSetId()
//    {
//        // 프로젝트 내에 있는 모든 UnlockNodeData 에셋을 찾습니다.
//        string[] guids = AssetDatabase.FindAssets("t:UnlockNodeData");
//        int maxId = 0; // 시작 ID를 0으로 잡습니다. (첫 생성 시 ID는 1이 됨)

//        foreach (string guid in guids)
//        {
//            string path = AssetDatabase.GUIDToAssetPath(guid);
//            UnlockNodeData node = AssetDatabase.LoadAssetAtPath<UnlockNodeData>(path);

//            // 자기 자신은 비교 대상에서 제외하고, 가장 높은 ID 값을 찾습니다.
//            if (node != null && node != this)
//            {
//                if (node.Id > maxId)
//                {
//                    maxId = node.Id;
//                }
//            }
//        }

//        // 찾은 가장 높은 ID에 1을 더해서 내 ID로 설정합니다.
//        this.Id = maxId + 1;

//        // 에셋이 수정되었음을 유니티 엔진에 알려서 저장되게 합니다.
//        EditorUtility.SetDirty(this);
//    }
//#endif
}