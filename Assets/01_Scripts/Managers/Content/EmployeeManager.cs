using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static Define;
using static UnityEngine.Rendering.DebugUI;

[Serializable]
public class EmployeeTask
{
    public EmployeeTaskType Type { get; }

    public SeatStation Target { get; }
    
    public EmployeeController AssignedEmployee { get; set; }

    public EmployeeTask(EmployeeTaskType type, SeatStation target = null)
    {
        Type = type;
        Target = target;
    }
}
public class EmployeeManager
{
    private GameObject employeePrefab;
    private readonly List<EmployeeController> employees = new List<EmployeeController>();

    public IReadOnlyList<EmployeeController> Employees => employees;
    public int HireCost { get; set; } = 1000;

    private readonly List<EmployeeTask> tasks = new();

    public IReadOnlyList<EmployeeTask> Tasks => tasks;

    public event Action<EmployeeTask> OnTaskCreated;


    public void Init()
    {
        LoadPrefab();
        RegisterEvent();
    }
    private void RegisterEvent()
    {
        //Managers.Station.OnRegisterDirtyTable -= CreateTask();
        //Managers.Station.OnRegisterDirtyTable += CreateTask();
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
        employees.Add(controller);

        Debug.Log($"[EmployeeManager] 신규 직원이 고용되었습니다! (현재 직원 수: {employees.Count})");
        return controller;
    }

    public void CreateTask(EmployeeTaskType type, SeatStation target = null)
    {
        EmployeeTask task = new EmployeeTask(type, target);

        tasks.Add(task);

        Debug.Log($"[EmployeeManager] Task 생성 : {type}" );
        OnTaskCreated?.Invoke(task);

        AssignTask(task);
    }

    // 작업 할당
    private void AssignTask(EmployeeTask task)
    {
        EmployeeController employee = FindAvailableEmployee(task);

        if (employee == null)
            return;

        // 작업에 직원 할당
        task.AssignedEmployee = employee;
        employee.AssignTask(task);
    }

    private EmployeeController FindAvailableEmployee(EmployeeTask task)
    {
        if (task == null) return null;
        if (employees.Count <= 0) return null;
        if (task.AssignedEmployee != null) return null;

        foreach(EmployeeController employee in employees)
        {
            if(employee == null) continue;

            // 일 할 수 없는 타입이면 패스
            if(employee.HasTask(task.Type) == false) continue;

            // 직원이 일을 안하고있으면 리턴
            if (employee.CurrentState != EmployeeState.Idle) continue;

            return employee;
        }
        return null;
    }
    public void Clear()
    {
        employees.Clear();
    }
}

