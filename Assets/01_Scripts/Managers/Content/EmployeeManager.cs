using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>직원 후보 데이터, 선택 고용과 현재 직원 목록을 한 곳에서 관리합니다.</summary>
public class EmployeeManager
{
    #region 변수

    private const string EmployeeLabel = "EmployeeData";
    private GameObject employeePrefab;
    private readonly List<EmployeeController> employees = new List<EmployeeController>();
    private readonly List<EmployeeData> candidates = new();
    private readonly HashSet<string> hiredIds = new();
    private bool candidatesLoaded;

    public IReadOnlyList<EmployeeController> Employees => employees;
    public IReadOnlyList<EmployeeData> Candidates => candidates;
    public bool CandidatesLoaded => candidatesLoaded;

    /// <summary>기존 직접 고용 API의 가격 필드입니다. 후보별 실제 고용비는 EmployeeData를 사용합니다.</summary>
    public int HireCost { get; set; } = 1000;

    /// <summary>직원 후보 로드나 고용 상태가 바뀌면 화면에 알립니다.</summary>
    public event Action OnChanged;

    #endregion

    #region 초기화와 데이터 로드

    /// <summary>공용 프리팹과 10_Data의 직원 후보 에셋을 읽기 시작합니다.</summary>
    public void Init()
    {
        candidates.Clear();
        hiredIds.Clear();
        candidatesLoaded = false;
        LoadPrefab();
        Managers.Resource.LoadAllAsync<EmployeeData>(EmployeeLabel, null, OnEmployeeDataLoaded);
    }

    /// <summary>기존 직접 고용 호출을 위해 공용 프리팹 주소를 읽습니다.</summary>
    private void LoadPrefab()
    {
        if (Managers.Resource.IsLoaded)
        {
            employeePrefab = Managers.Resource.Load<GameObject>("Prefab_Employee");
        }
        else
        {
            Managers.Resource.assetLoaded += () =>
            {
                employeePrefab = Managers.Resource.Load<GameObject>("Prefab_Employee");
            };
        }

#if UNITY_EDITOR
        if (employeePrefab == null)
        {
            employeePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03_Prefabs/New Folder/Employee.prefab");
        }
#endif
    }

    /// <summary>로드된 직원 데이터를 중복 ID 없이 등록하고 화면 표시 순서로 정렬합니다.</summary>
    private void OnEmployeeDataLoaded(Dictionary<string, UnityEngine.Object> loaded)
    {
        candidates.Clear();
        candidatesLoaded = true;
        if (loaded == null)
        {
            Debug.LogError("[EmployeeManager] EmployeeData 라벨의 직원 후보가 없습니다.");
            OnChanged?.Invoke();
            return;
        }

        HashSet<string> usedIds = new();
        foreach (UnityEngine.Object asset in loaded.Values)
        {
            EmployeeData data = asset as EmployeeData;
            if (data == null)
                continue;

            if (string.IsNullOrWhiteSpace(data.Id) || string.IsNullOrWhiteSpace(data.Name) ||
                data.Prefab == null || data.HirePrice < 0 || data.Tasks == null || data.Tasks.Count == 0)
            {
                Debug.LogWarning($"[EmployeeManager] 직원 후보 설정이 부족합니다: {data.name}");
                continue;
            }

            if (!usedIds.Add(data.Id))
            {
                Debug.LogWarning($"[EmployeeManager] 중복 직원 ID를 건너뜁니다: {data.Id}");
                continue;
            }

            candidates.Add(data);
        }

        candidates.Sort((left, right) =>
        {
            int order = left.Order.CompareTo(right.Order);
            if (order != 0)
                return order;

            return string.CompareOrdinal(left.Id, right.Id);
        });

        OnChanged?.Invoke();
    }

    #endregion

    #region 후보 조회와 선택 고용

    /// <summary>화면의 고정된 후보 카드 순서에 해당하는 직원 데이터를 반환합니다.</summary>
    public EmployeeData GetCandidateAt(int index)
    {
        if (index < 0 || index >= candidates.Count)
            return null;

        return candidates[index];
    }

    /// <summary>이 후보가 현재 실행 중 이미 고용되었는지 확인합니다.</summary>
    public bool IsHired(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        return hiredIds.Contains(id);
    }

    /// <summary>후보 설정, 중복 고용과 플레이어 잔액을 확인합니다.</summary>
    public bool CanHire(EmployeeData data, PlayerController player)
    {
        if (data == null || player == null || data.Prefab == null || data.HirePrice < 0)
            return false;

        if (IsHired(data.Id) || player.money < data.HirePrice)
            return false;

        return candidates.Contains(data);
    }

    /// <summary>선택한 후보의 비용을 지불하고 공용 프리팹에 해당 데이터만 적용합니다.</summary>
    public EmployeeController TryHire(string id, PlayerController player, Vector3? customSpawnPosition = null)
    {
        EmployeeData data = candidates.Find(candidate => candidate.Id == id);
        if (!CanHire(data, player))
            return null;

        // 무료 테스트 후보는 금액을 차감하지 않고 유료 후보만 기존 지불 API를 사용합니다.
        if (data.HirePrice > 0 && !player.TrySpendMoney(data.HirePrice))
            return null;

        EmployeeController controller = SpawnEmployee(data.Prefab, customSpawnPosition, data);
        if (controller == null)
        {
            // 생성이나 초기화가 실패했으면 지불한 돈을 돌려줍니다.
            if (data.HirePrice > 0)
                player.UpdateMoney(data.HirePrice);

            return null;
        }

        hiredIds.Add(data.Id);
        OnChanged?.Invoke();
        return controller;
    }

    #endregion

    #region 기존 고용과 직원 생성

    /// <summary>기존 호출부용 공용 직원 생성 API입니다. 후보 구매에는 TryHire를 사용합니다.</summary>
    public EmployeeController HireEmployee(Vector3? customSpawnPosition = null)
    {
        if (employeePrefab == null)
            LoadPrefab();

        if (employeePrefab == null)
        {
            Debug.LogError("[EmployeeManager] Employee 프리팹을 로드할 수 없습니다!");
            return null;
        }

        return SpawnEmployee(employeePrefab, customSpawnPosition, null);
    }

    /// <summary>한 직원 인스턴스를 만들고 능력과 업무를 적용한 뒤 대기 작업을 재배정합니다.</summary>
    private EmployeeController SpawnEmployee(GameObject prefab, Vector3? customSpawnPosition, EmployeeData data)
    {
        Vector3 spawnPos = GetSpawnPosition(customSpawnPosition);
        GameObject go = null;
        EmployeeController controller = null;
        try
        {
            go = Managers.Resource.Instantiate(prefab, spawnPos, Quaternion.identity);
            if (go == null)
                return null;

            controller = go.GetComponent<EmployeeController>();
            if (controller == null)
                controller = go.AddComponent<EmployeeController>();

            controller.Init();
            if (data != null)
                controller.ApplyHireData(data);

            // 과거 공통 성장 값이 있다면 직원별 기본 능력치 뒤에 추가로 반영합니다.
            if (Managers.Growth != null)
                Managers.Growth.ApplyStartingEmployeeStats(controller);

            if (data != null)
                go.name = $"Employee_{data.Id}";
            else
                go.name = $"Employee_{employees.Count + 1}";

            employees.Add(controller);
            Managers.Task.AssignWaitingTasks();
            Debug.Log($"[EmployeeManager] 직원 고용: {go.name} (현재 {employees.Count}명)");
            return controller;
        }
        catch (Exception error)
        {
            // 중간에 실패한 직원이 씬에 남지 않도록 생성 오브젝트를 정리합니다.
            if (controller != null)
                employees.Remove(controller);

            if (go != null)
                Managers.Resource.Destroy(go);

            Debug.LogError($"[EmployeeManager] 직원 생성 실패: {error}");
            return null;
        }
    }

    /// <summary>고용 위치를 지정하지 않았다면 계산대, 플레이어, 시작 구역 순서로 찾습니다.</summary>
    private Vector3 GetSpawnPosition(Vector3? customSpawnPosition)
    {
        if (customSpawnPosition.HasValue)
            return customSpawnPosition.Value;

        if (Managers.Game.Counter != null && Managers.Game.Counter.PayPoint != null)
            return Managers.Game.Counter.PayPoint.position + Vector3.back * 1.5f;

        if (PlayerController.Instance != null)
            return PlayerController.Instance.transform.position + Vector3.right * 1.5f;

        GameObject counter = GameObject.Find("StationZone/Zone_Counter");
        if (counter != null)
            return counter.transform.position;

        return Vector3.zero;
    }


    /// <summary>씬을 정리할 때 직원 목록과 이번 실행의 중복 고용 기록을 비웁니다.</summary>
    public void Clear()
    {
        employees.Clear();
        hiredIds.Clear();
        OnChanged?.Invoke();
    }

    #endregion
}

