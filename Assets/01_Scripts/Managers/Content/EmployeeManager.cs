using System.Collections.Generic;
using UnityEngine;

/// <summary>직원의 고용과 현재 고용된 직원 목록을 관리합니다.</summary>
public class EmployeeManager
{
    private GameObject employeePrefab;
    private readonly List<EmployeeController> employees = new List<EmployeeController>();

    public IReadOnlyList<EmployeeController> Employees => employees;
    public int HireCost { get; set; } = 1000;

    public void Init()
    {
        LoadPrefab();
    }

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

    public EmployeeController HireEmployee(Vector3? customSpawnPosition = null)
    {
        if (employeePrefab == null)
        {
            LoadPrefab();
        }

        if (employeePrefab == null)
        {
            Debug.LogError("[EmployeeManager] Employee 프리팹을 로드할 수 없습니다!");
            return null;
        }

        Vector3 spawnPos = Vector3.zero;
        if (customSpawnPosition.HasValue)
        {
            spawnPos = customSpawnPosition.Value;
        }
        else
        {
            // 기본 스폰 위치: 카운터 근처 또는 플레이어 위치
            if (Managers.Game.Counter != null && Managers.Game.Counter.PayPoint != null)
            {
                spawnPos = Managers.Game.Counter.PayPoint.position + Vector3.back * 1.5f;
            }
            else if (PlayerController.Instance != null)
            {
                spawnPos = PlayerController.Instance.transform.position + Vector3.right * 1.5f;
            }
            else
            {
                GameObject counterObj = GameObject.Find("StationZone/Zone_Counter");
                if (counterObj != null)
                {
                    spawnPos = counterObj.transform.position;
                }
            }
        }

        GameObject go = Managers.Resource.Instantiate(employeePrefab, spawnPos, Quaternion.identity);
        go.name = $"Employee_{employees.Count + 1}";

        EmployeeController controller = go.GetComponent<EmployeeController>();
        if (controller == null)
        {
            controller = go.AddComponent<EmployeeController>();
        }

        controller.Init();
        // 예전 공통 업그레이드가 저장되어 있으면 새 직원의 시작 능력치로만 적용합니다.
        if (Managers.Growth != null)
            Managers.Growth.ApplyStartingEmployeeStats(controller);

        employees.Add(controller);

        // 새 직원이 들어오면 이미 대기 중인 작업도 다시 배정합니다.
        Managers.Task.AssignWaitingTasks();

        Debug.Log($"[EmployeeManager] 신규 직원이 고용되었습니다! (현재 직원 수: {employees.Count})");
        return controller;
    }


    /// <summary>고용된 직원 목록을 비워 씬 정리 과정에서 오래된 참조를 남기지 않습니다.</summary>
    public void Clear()
    {
        employees.Clear();
    }
}

