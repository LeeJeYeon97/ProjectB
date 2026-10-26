using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static Define;

public class UnlockManager
{
    // 씬에 배치된 해금 설정 발판들
    private Dictionary<string, UnlockStation> sceneStations = new Dictionary<string, UnlockStation>();

    // 해금이 끝난 노드의 ID를 보관합니다.
    private HashSet<string> unlockedIDs = new HashSet<string>(); // unique ID 기반 해금 상태 관리

    /// <summary>
    /// 맵에 미리 배치된 발판을 모으고 현재 해금 상태에 맞게 표시합니다.
    /// </summary>
    public void Init()
    {
        // 저장된 해금 상태를 먼저 읽은 뒤 씬 발판의 표시 상태를 결정합니다.
        LoadSaveData();

        // 조건 때문에 비활성화된 발판도 다시 찾을 수 있도록 비활성 오브젝트를 포함합니다.
        sceneStations.Clear();
        UnlockStation[] stations = UnityEngine.Object.FindObjectsByType<UnlockStation>(FindObjectsInactive.Include);
        foreach (UnlockStation station in stations)
        {
            if (!station.HasSettings)
            {
                // 설정되지 않은 발판은 씬 편집 중에만 보이고 게임에서는 숨깁니다.
                station.gameObject.SetActive(false);
                continue;
            }

            if (!station.NodeData.HasValidPrerequisite)
            {
                Debug.LogError($"선행 해금 데이터에 ID가 없습니다: {station.name}", station);
                station.gameObject.SetActive(false);
                continue;
            }

            string id = station.UniqueID;
            if (sceneStations.ContainsKey(id))
            {
                Debug.LogError($"중복된 해금 ID입니다: {id}", station);
                station.gameObject.SetActive(false);
                continue;
            }

            sceneStations[id] = station;
            station.Setup();

            // 아직 해금되지 않은 구역은 시작 시 대상 오브젝트를 숨깁니다.
            if (station.UnlockType == UnlockType.ToggleActive && !unlockedIDs.Contains(id))
            {
                if (station.TargetSceneObject != null)
                {
                    station.TargetSceneObject.SetActive(false);
                }
            }
        }

        // 현재 해금 상태에 맞게 대상과 발판을 표시합니다.
        RefreshMapState();
    }

    /// <summary>
    /// 열린 대상은 표시하고 발판은 선행 조건에 맞게 켜거나 끕니다.
    /// </summary>
    public void RefreshMapState()
    {
      foreach (var pair in sceneStations)
        {
            string id = pair.Key;
            UnlockStation station = pair.Value;

            // 이미 해금 완료된 경우 -> 대상 생성 혹은 활성화
            if (unlockedIDs.Contains(id))
            {
                station.gameObject.SetActive(false);
                HandleObjectState(station, immediate: true);
                continue;
            }

            // 선행 조건이 충족되었는가?
            string prereqID = station.PrerequisiteID;
            bool isPrereqMet = string.IsNullOrEmpty(prereqID) || unlockedIDs.Contains(prereqID);
            if (isPrereqMet)
            {
                station.gameObject.SetActive(true);
            }
            else
            {
                station.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 해금 방식에 따라 프리팹을 만들거나 씬에 준비된 구역을 활성화합니다.
    /// </summary>
    private void HandleObjectState(UnlockStation sceneStation, bool immediate)
    {
        string id = sceneStation.UniqueID;

        // C방식: 프리팹 동적 스폰
        if (sceneStation.UnlockType == UnlockType.SpawnPrefab)
        {
            string objName = $"{id}";
            if (GameObject.Find(objName) != null) return;

            GameObject targetGo = Managers.Resource.Instantiate(sceneStation.NodeData.TargetPrefab, sceneStation.SpawnPosition, sceneStation.SpawnRotation);
            targetGo.name = objName;

            if (!immediate)
            {
                targetGo.transform.localScale = Vector3.zero;
                targetGo.transform.DOScale(Vector3.one, 0.3f)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() => RefreshObstacle(targetGo));
            }
        }
        // A방식: 씬 오브젝트 활성화 (구역 확장)
        else if (sceneStation.UnlockType == UnlockType.ToggleActive)
        {
            if (sceneStation.TargetSceneObject == null) return;

            if (!sceneStation.TargetSceneObject.activeSelf)
            {
                sceneStation.TargetSceneObject.SetActive(true);

                if (!immediate)
                {
                    // 구역이 해금될 때 "뿅" 하고 커지는 가벼운 연출 적용
                    sceneStation.TargetSceneObject.transform.localScale = Vector3.zero;
                    sceneStation.TargetSceneObject.transform.DOScale(Vector3.one, 0.3f)
                        .SetEase(Ease.OutCubic)
                        .OnComplete(() => RefreshObstacle(sceneStation.TargetSceneObject));
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

        if (sceneStations.TryGetValue(uniqueID, out UnlockStation station))
        {
            HandleObjectState(station, immediate: false);
        }

        // 새로운 해금으로 인해 다음 단계의 발판이 열리도록 상태 갱신
        RefreshMapState();

        // 새로 생성되거나 켜진 메뉴 기계가 있다면 다음 손님부터 주문 후보에 포함합니다.
        Managers.Customer.RefreshAvailableRecipes();
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
