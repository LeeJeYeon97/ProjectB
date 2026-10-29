using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using static Define;

public class UnlockManager
{
    #region 해금 상태

    // 데이터 에셋 하나를 하나의 해금 지점으로 취급합니다.
    private readonly Dictionary<UnlockNodeData, UnlockStation> sceneStations = new Dictionary<UnlockNodeData, UnlockStation>();
    private readonly HashSet<UnlockNodeData> unlockedData = new HashSet<UnlockNodeData>();
    private readonly Dictionary<UnlockNodeData, GameObject> spawnedObjects = new Dictionary<UnlockNodeData, GameObject>();
    private ulong sceneHandle;
    private bool hasScene;

    #endregion

    #region 해금 관리

    /// <summary>현재 씬의 발판을 데이터별로 등록하고 선행 조건에 맞게 표시합니다.</summary>
    public void Init()
    {
        ulong currentScene = SceneManager.GetActiveScene().handle.GetRawData();
        if (!hasScene || sceneHandle != currentScene)
        {
            // 저장 기능이 없으므로 다른 씬으로 이동하면 이전 씬의 실행 상태를 버립니다.
            sceneStations.Clear();
            spawnedObjects.Clear();
            unlockedData.Clear();
            sceneHandle = currentScene;
            hasScene = true;
        }
        sceneStations.Clear();

        UnlockStation[] stations = Object.FindObjectsByType<UnlockStation>(FindObjectsInactive.Include);
        foreach (UnlockStation station in stations)
        {
            if (!station.HasSettings)
            {
                station.gameObject.SetActive(false);
                continue;
            }

            UnlockNodeData data = station.NodeData;
            if (sceneStations.ContainsKey(data))
            {
                Debug.LogError($"[UnlockManager] 같은 해금 데이터가 여러 발판에 연결되었습니다: {data.name}", station);
                station.gameObject.SetActive(false);
                continue;
            }

            sceneStations.Add(data, station);
            if (!unlockedData.Contains(data))
            {
                station.Setup();
                if (station.UnlockType == UnlockType.ToggleActive)
                {
                    if (station.TargetSceneObject != null) station.TargetSceneObject.SetActive(false);
                }
            }
        }

        // 등록을 모두 마친 뒤 선행 발판 누락과 순환 참조를 검사합니다.
        foreach (KeyValuePair<UnlockNodeData, UnlockStation> pair in sceneStations)
        {
            CheckPrerequisites(pair.Key, pair.Value);
        }
        RefreshMapState();
    }

    /// <summary>해금 여부와 선행 데이터에 따라 발판과 대상을 갱신합니다.</summary>
    public void RefreshMapState()
    {
        foreach (KeyValuePair<UnlockNodeData, UnlockStation> pair in sceneStations)
        {
            UnlockNodeData data = pair.Key;
            UnlockStation station = pair.Value;
            if (unlockedData.Contains(data))
            {
                station.gameObject.SetActive(false);
                HandleObjectState(station, true);
                continue;
            }

            UnlockNodeData previous = data.PrerequisiteData;
            bool canOpen = previous == null;
            if (previous != null) canOpen = unlockedData.Contains(previous);
            station.gameObject.SetActive(canOpen);
        }
    }

    /// <summary>등록된 데이터를 한 번 해금하고 대상과 다음 발판을 갱신합니다.</summary>
    public void UnlockNode(UnlockNodeData data)
    {
        if (data == null) return;
        if (unlockedData.Contains(data)) return;
        if (!sceneStations.TryGetValue(data, out UnlockStation station))
        {
            Debug.LogWarning($"[UnlockManager] 씬에 등록되지 않은 해금 데이터입니다: {data.name}", data);
            return;
        }

        unlockedData.Add(data);
        HandleObjectState(station, false);
        RefreshMapState();
        Managers.Customer.RefreshAvailableRecipes();
    }

    /// <summary>선행 데이터가 씬에 존재하는지와 서로 순환하는지 확인합니다.</summary>
    private void CheckPrerequisites(UnlockNodeData data, UnlockStation station)
    {
        HashSet<UnlockNodeData> visited = new HashSet<UnlockNodeData>();
        UnlockNodeData current = data;
        while (current != null)
        {
            if (!visited.Add(current))
            {
                Debug.LogError($"[UnlockManager] 선행 해금 데이터가 순환합니다: {data.name}", station);
                return;
            }
            if (!sceneStations.ContainsKey(current))
            {
                Debug.LogWarning($"[UnlockManager] 선행 데이터의 발판이 씬에 없습니다: {data.name} → {current.name}", station);
                return;
            }
            current = current.PrerequisiteData;
        }
    }

    /// <summary>해금 방식에 따라 대상을 한 번 생성하거나 씬 오브젝트를 켭니다.</summary>
    private void HandleObjectState(UnlockStation station, bool immediate)
    {
        UnlockNodeData data = station.NodeData;
        if (station.UnlockType == UnlockType.SpawnPrefab)
        {
            // 이름 검색 대신 데이터에 연결된 생성 결과를 확인해 다른 오브젝트와 충돌하지 않도록 합니다.
            if (spawnedObjects.TryGetValue(data, out GameObject existing))
            {
                if (existing != null) return;
            }
            GameObject target = Managers.Resource.Instantiate(data.TargetPrefab, station.SpawnPosition, station.SpawnRotation);
            if (target == null)
            {
                Debug.LogError($"[UnlockManager] 해금할 프리팹을 생성하지 못했습니다: {data.name}", station);
                return;
            }
            target.name = data.name;
            spawnedObjects[data] = target;
            if (!immediate)
            {
                target.transform.localScale = Vector3.zero;
                target.transform.DOScale(Vector3.one, 0.3f)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() => RefreshObstacle(target));
            }
        }
        else if (station.UnlockType == UnlockType.ToggleActive)
        {
            GameObject target = station.TargetSceneObject;
            if (target == null) return;
            if (target.activeSelf) return;
            target.SetActive(true);
            if (!immediate)
            {
                target.transform.localScale = Vector3.zero;
                target.transform.DOScale(Vector3.one, 0.3f)
                    .SetEase(Ease.OutCubic)
                    .OnComplete(() => RefreshObstacle(target));
            }
        }
    }

    /// <summary>크기 연출이 끝난 뒤 활성 장애물의 NavMesh 영역을 다시 계산합니다.</summary>
    private void RefreshObstacle(GameObject target)
    {
        if (target == null) return;
        NavMeshObstacle[] obstacles = target.GetComponentsInChildren<NavMeshObstacle>(true);
        foreach (NavMeshObstacle obstacle in obstacles)
        {
            if (obstacle == null) continue;
            if (!obstacle.enabled) continue;
            if (!obstacle.gameObject.activeInHierarchy) continue;
            obstacle.enabled = false;
            obstacle.enabled = true;
        }
    }

    #endregion
}
