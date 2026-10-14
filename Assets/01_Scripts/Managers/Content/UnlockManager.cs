using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
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

            // 해금 앵커의 고정 ID를 기계 저장 ID로 사용해 다시 생성해도 업그레이드가 이어지게 합니다.
            // 프리팹 종류와 관계없이 공통 업그레이드 연결부가 있으면 해금 ID를 저장 ID로 씁니다.
            MonoBehaviour[] components = targetGo.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour component in components)
            {
                if (component is InteractableBase machine && machine.UpgradeIds.Count > 0)
                {
                    machine.SetMachineId(anchor.UniqueID);
                    break;
                }
            }

            if (!immediate)
            {
                targetGo.transform.localScale = Vector3.zero;
                targetGo.transform.DOScale(Vector3.one, 0.3f)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() => RefreshObstacle(targetGo));
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
                    anchor.TargetSceneObject.transform.DOScale(Vector3.one, 0.3f)
                        .SetEase(Ease.OutCubic)
                        .OnComplete(() => RefreshObstacle(anchor.TargetSceneObject));
                }
            }
        }
    }

    /// <summary>크기 연출이 끝난 뒤 장애물을 다시 켜서 최신 크기로 NavMesh를 깎도록 합니다.</summary>
    private void RefreshObstacle(GameObject target)
    {
        if (target == null)
            return;

        // 프리팹의 루트나 자식에 붙은 장애물을 모두 갱신해 가구 구조가 달라도 재사용할 수 있게 합니다.
        NavMeshObstacle[] obstacles = target.GetComponentsInChildren<NavMeshObstacle>(true);
        foreach (NavMeshObstacle obstacle in obstacles)
        {
            // 원래 꺼져 있던 장애물의 활성 상태는 유지합니다.
            if (obstacle == null || !obstacle.enabled || !obstacle.gameObject.activeInHierarchy)
                continue;

            // 컴포넌트를 다시 켜면 Unity가 현재 위치와 크기를 기준으로 carve 영역을 다시 계산합니다.
            obstacle.enabled = false;
            obstacle.enabled = true;
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
