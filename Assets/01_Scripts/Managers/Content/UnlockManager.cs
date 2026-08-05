using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class UnlockManager
{
    // 모든 UnlockNodeData 에셋을 ID로 빠르게 찾기 위한 데이터베이스용 딕셔너리
    private Dictionary<int, UnlockNodeData> allNodeDatas = new Dictionary<int, UnlockNodeData>();

    // 씬 앵커들
    private Dictionary<string, UnlockAnchor> sceneAnchors = new Dictionary<string, UnlockAnchor>();

    // 열린 앵커들
    private HashSet<string> unlockedIDs = new HashSet<string>(); // unique ID 기반 해금 상태 관리

    private GameObject stationPrefab;

    public void Init()
    {
        stationPrefab = Managers.Resource.Load<GameObject>("Prefab_UnlockStation");
        
        //// Resources 폴더나 에셋 로드를 통해 전체 UnlockNodeData 캐싱
        UnlockNodeData[] allNodes = Managers.Resource.LoadAll<UnlockNodeData>("Data_UnlockNode");
        foreach (var node in allNodes)
        {
            if (node.Id > 0)
            {
                allNodeDatas[node.Id] = node;
            }
        }

        // 1. 세이브 데이터 로드 (unlockedIDs 채우기)
        LoadSaveData();

       // 2.씬 내의 모든 UnlockAnchor 자동 수집
       UnlockAnchor[] anchors = UnityEngine.Object.FindObjectsByType<UnlockAnchor>(FindObjectsSortMode.None);
        foreach (var anchor in anchors)
        {
            if (anchor.NodeData == null) continue;

            string id = anchor.UniqueID;
            sceneAnchors[id] = anchor;

            // [핵심] 아직 해금되지 않은 구역(A방식) 오브젝트는 시작 시 자동으로 꺼줍니다.
            if (anchor.UnlockType == UnlockType.ToggleActive && !unlockedIDs.Contains(id))
            {
                if (anchor.TargetSceneObject != null)
                {
                    anchor.TargetSceneObject.SetActive(false);
                }
            }
        }

        // 3. 현재 해금 상태에 맞게 맵 상태 빌드
        RefreshMapState();
    }

    public void RefreshMapState()
    {
        foreach (var pair in sceneAnchors)
        {
            string id = pair.Key;
            UnlockAnchor anchor = pair.Value;

            // 이미 해금 완료된 경우 -> 대상 생성 혹은 활성화
            if (unlockedIDs.Contains(id))
            {
                HandleObjectState(anchor, immediate: true);
                continue;
            }

            // 선행 조건이 충족되었는가?
            string prereqID = anchor.preRequisiteID;
            bool isPrereqMet = string.IsNullOrEmpty(prereqID) || unlockedIDs.Contains(prereqID);
            if (isPrereqMet || prereqID == "None_0")
            {
                SpawnUnlockStation(anchor);
            }
        }
    }

    private void SpawnUnlockStation(UnlockAnchor anchor)
    {
        string stationName = $"Station_{anchor.name}";
        if (GameObject.Find(stationName) != null) return;

        // 발판 스폰 (가구 앵커든 구역 앵커든 발판은 완전히 동일하게 스폰됨)
        GameObject stationGo = Managers.Resource.Instantiate(stationPrefab, anchor.StationPosition, Quaternion.identity);
        stationGo.name = stationName;

        stationGo.GetComponent<UnlockStation>().Setup(anchor);
    }

    private void HandleObjectState(UnlockAnchor anchor, bool immediate)
    {
        string id = anchor.name;

        // C방식: 프리팹 동적 스폰
        if (anchor.UnlockType == UnlockType.SpawnPrefab)
        {
            string objName = $"Target_{id}";
            if (GameObject.Find(objName) != null) return;

            GameObject targetGo = Managers.Resource.Instantiate(anchor.NodeData.targetPrefab, anchor.SpawnPosition, anchor.SpawnRotation);
            targetGo.name = objName;

            if (!immediate)
            {
                targetGo.transform.localScale = Vector3.zero;
                targetGo.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
            }
        }
        // A방식: 씬 오브젝트 활성화 (구역 확장)
        else if (anchor.UnlockType == UnlockType.ToggleActive)
        {
            if (anchor.TargetSceneObject == null) return;

            if (!anchor.TargetSceneObject.activeSelf)
            {
                anchor.TargetSceneObject.SetActive(true);

                if (!immediate)
                {
                    // 구역이 해금될 때 "뿅" 하고 커지는 가벼운 연출 적용
                    anchor.TargetSceneObject.transform.localScale = Vector3.zero;
                    anchor.TargetSceneObject.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutCubic);
                }
            }
        }
    }

    public void UnlockNode(string uniqueID)
    {
        if (unlockedIDs.Contains(uniqueID)) return;

        unlockedIDs.Add(uniqueID);
        SaveData();

        if (sceneAnchors.TryGetValue(uniqueID, out UnlockAnchor anchor))
        {
            HandleObjectState(anchor, immediate: false);
        }

        // 새로운 해금으로 인해 다음 단계의 발판이 열리도록 상태 갱신
        RefreshMapState();
    }

    private void LoadSaveData()
    {
        //// 예시: 세이브 시스템에서 해금된 ID 목록을 가져옴 (["Counter_0", "Stool_0", "Stool_1"])
        //List<string> savedIDs = SaveSystem.GetUnlockedNodeIDs();
        //
        //unlockedIDs.Clear();
        //foreach (string id in savedIDs)
        //{
        //    unlockedIDs.Add(id);
        //}
    }

    private void SaveData()
    {
        //// 저장할 때는 unlockedIDs 목록만 모아서 문자열 리스트로 저장
        //List<string> idsToSave = new List<string>(unlockedIDs);
        //SaveSystem.SaveUnlockedNodeIDs(idsToSave);
    }
}