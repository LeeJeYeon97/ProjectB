using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;

/// <summary>직원이 수행해야 할 작업과 작업 배정을 관리합니다.</summary>
[Serializable]
public class EmployeeTask
{
    /// <summary>이 작업이 요청하는 업무 종류입니다.</summary>
    public EmployeeTaskType Type { get; }

    /// <summary>이 작업을 수행할 상호작용 대상입니다.</summary>
    public InteractableBase Target { get; }

    /// <summary>작업을 맡은 직원이며, 아직 배정되지 않았으면 비어 있습니다.</summary>
    public EmployeeController AssignedEmployee { get; set; }

    /// <summary>업무 종류와 대상을 받아 새 작업을 준비합니다.</summary>
    public EmployeeTask(EmployeeTaskType type, InteractableBase target)
    {
        Type = type;
        Target = target;
    }
}

/// <summary>작업 대기열을 유지하고 가능한 직원에게 작업을 배정합니다.</summary>
public class TaskManager
{
    // 작업이 만들어진 순서를 유지해 직원이 한가해질 때 오래된 일부터 맡깁니다.
    private readonly List<EmployeeTask> tasks = new();

    /// <summary>현재 대기 중이거나 진행 중인 직원 작업 목록입니다.</summary>
    public IReadOnlyList<EmployeeTask> Tasks => tasks;

    /// <summary>새 작업이 등록되면 중복을 막고 대기열에 넣은 뒤 배정을 시도합니다.</summary>
    public EmployeeTask CreateTask(EmployeeTaskType type, InteractableBase target)
    {
        if (target == null)
            return null;

        // 같은 대상의 같은 업무가 이미 대기 중이거나 진행 중이면 새로 만들지 않습니다.
        foreach (EmployeeTask task in tasks)
        {
            if (task.Type == type && task.Target == target)
                return task;
        }

        EmployeeTask newTask = new EmployeeTask(type, target);
        tasks.Add(newTask);
        Debug.Log($"[TaskManager] 작업 등록: {type}, 대상: {target.name}");

        AssignWaitingTasks();
        return newTask;
    }

    /// <summary>대기 작업을 가능한 직원에게 차례로 배정합니다.</summary>
    public void AssignWaitingTasks()
    {
        // 목록 앞쪽에 있는 오래된 작업부터 확인하고, 제거한 항목은 같은 칸을 다시 검사합니다.
        for (int i = 0; i < tasks.Count;)
        {
            EmployeeTask task = tasks[i];

            // 대상의 상태가 바뀌어 더 이상 할 일이 없어진 작업은 대기열에서 정리합니다.
            if (!CanWork(task))
            {
                tasks.RemoveAt(i);
                continue;
            }

            // 이미 직원이 맡은 작업은 다른 직원에게 중복 배정하지 않습니다.
            if (task.AssignedEmployee != null)
            {
                i++;
                continue;
            }

            EmployeeController worker = FindAvailableEmployee(task);
            if (worker == null)
            {
                i++;
                continue;
            }

            // 플레이어나 다른 직원이 이미 사용 중인 대상은 맡지 않고 대기열에 남깁니다.
            if (!task.Target.TryReserve(worker.gameObject))
            {
                i++;
                continue;
            }

            // 직원이 작업을 받아들였을 때만 작업 기록을 배정 상태로 바꿉니다.
            if (worker.AssignTask(task) && tasks.Contains(task))
            {
                task.AssignedEmployee = worker;
                if (task.Target is SeatStation seat && task.Type == EmployeeTaskType.Cleaning)
                    seat.AssignedCleaner = worker;
            }
            else
            {
                // 작업을 거절한 직원이 대상을 계속 잠그지 않도록 즉시 선점권을 돌려줍니다.
                task.Target.ReleaseReservation(worker.gameObject);
            }

            i++;
        }
    }

    /// <summary>완료되거나 취소된 작업을 목록에서 제거하고 남은 작업을 다시 배정합니다.</summary>
    public void FinishTask(EmployeeTask task)
    {
        if (task == null)
            return;

        if (task.Target is SeatStation seat && seat.AssignedCleaner == task.AssignedEmployee)
            seat.AssignedCleaner = null;

        // 완료되거나 취소된 직원 작업의 선점권을 해제해 다음 주체가 작업할 수 있게 합니다.
        if (task.Target != null && task.AssignedEmployee != null)
            task.Target.ReleaseReservation(task.AssignedEmployee.gameObject);

        task.AssignedEmployee = null;
        tasks.Remove(task);

        // 한 번에 처리한 수량을 넘는 접시가 남아 있으면 다음 설거지 작업을 만듭니다.
        if (task.Type == EmployeeTaskType.DishWash)
            RegisterDishWashTask();

        // 같은 카운터에 아직 결제를 기다리는 손님이 있으면 수금 작업을 이어서 만듭니다.
        if (task.Type == EmployeeTaskType.Casher && task.Target is Counter counter && counter.HasWaitingCustomers)
            CreateTask(EmployeeTaskType.Casher, counter);

        AssignWaitingTasks();
    }

    /// <summary>플레이어가 직접 사용할 대상의 직원 작업을 중단하고 미배정 대기로 돌립니다.</summary>
    public void TakeOverTask(InteractableBase target)
    {
        if (target == null)
            return;

        for (int i = tasks.Count - 1; i >= 0; i--)
        {
            EmployeeTask task = tasks[i];
            if (task == null || task.Target != target || task.AssignedEmployee == null)
                continue;

            EmployeeController worker = task.AssignedEmployee;
            if (task.Type == EmployeeTaskType.Cleaning && target is SeatStation seat && seat.AssignedCleaner == worker)
                seat.AssignedCleaner = null;

            target.ReleaseReservation(worker.gameObject);
            task.AssignedEmployee = null;
            worker.StopTaskForPlayer();
        }
    }

    /// <summary>직원이 맡은 작업을 대기 상태로 돌리고 이동과 기계 점유를 함께 해제합니다.</summary>
    public void TakeOverTask(EmployeeController worker)
    {
        if (worker == null)
            return;

        EmployeeTask task = null;
        foreach (EmployeeTask item in tasks)
        {
            if (item != null && item.AssignedEmployee == worker)
            {
                task = item;
                break;
            }
        }

        if (task == null)
            return;

        if (task.Type == EmployeeTaskType.Cleaning && task.Target is SeatStation seat && seat.AssignedCleaner == worker)
            seat.AssignedCleaner = null;

        if (task.Target != null)
            task.Target.ReleaseReservation(worker.gameObject);

        task.AssignedEmployee = null;
        worker.StopTaskForPlayer();
    }

    /// <summary>비활성화된 작업 대상에 연결된 대기 작업을 모두 제거합니다.</summary>
    public void RemoveTasksFor(InteractableBase target)
    {
        if (target == null)
            return;

        for (int i = tasks.Count - 1; i >= 0; i--)
        {
            if (tasks[i] != null && tasks[i].Target == target)
            {
                if (tasks[i].Type == EmployeeTaskType.Cleaning && target is SeatStation seat)
                    seat.AssignedCleaner = null;

                // 대상이 사라질 때 그 대상을 맡았던 직원의 선점권도 정리합니다.
                if (tasks[i].AssignedEmployee != null)
                    target.ReleaseReservation(tasks[i].AssignedEmployee.gameObject);

                tasks.RemoveAt(i);
            }
        }
    }

    /// <summary>플레이어가 주문용 메뉴를 만들면 해당 손님 좌석의 직원 서빙 작업을 제거합니다.</summary>
    public void RemoveServingTaskFor(CustomerController customer)
    {
        if (customer == null || Managers.Station == null)
            return;

        foreach (SeatStation seat in Managers.Station.AllSeats)
        {
            if (seat == null || seat.currentCustomer != customer)
                continue;

            RemoveTasksFor(seat);
            return;
        }
    }

    /// <summary>씬이나 게임 상태를 초기화할 때 대기 작업 참조를 정리합니다.</summary>
    public void Clear()
    {
        // 씬이나 게임을 초기화할 때 진행 중인 작업이 대상을 계속 잠그지 않게 합니다.
        foreach (EmployeeTask task in tasks)
        {
            if (task == null || task.Target == null || task.AssignedEmployee == null)
                continue;

            task.Target.ReleaseReservation(task.AssignedEmployee.gameObject);
            if (task.Target is SeatStation seat && seat.AssignedCleaner == task.AssignedEmployee)
                seat.AssignedCleaner = null;
        }

        tasks.Clear();
    }

    /// <summary>더러운 접시가 생겼거나 싱크대가 활성화됐을 때 설거지 작업을 준비합니다.</summary>
    public void RegisterDishWashTask()
    {
        if (Managers.Dish == null || Managers.Dish.DirtyDishCount <= 0)
            return;

        Sink[] sinks = UnityEngine.Object.FindObjectsByType<Sink>(FindObjectsSortMode.None);
        foreach (Sink sink in sinks)
        {
            if (sink != null && sink.isActiveAndEnabled)
            {
                CreateTask(EmployeeTaskType.DishWash, sink);
                return;
            }
        }
    }

    /// <summary>작업 종류에 맞는 대상 상태와 필요한 재고를 확인합니다.</summary>
    private bool CanWork(EmployeeTask task)
    {
        if (task == null || task.Target == null || !task.Target.gameObject.activeInHierarchy)
            return false;

        switch (task.Type)
        {
            case EmployeeTaskType.Cleaning:
                return task.Target is SeatStation dirtySeat && dirtySeat.CurrentState == SeatState.Dirty;
            case EmployeeTaskType.Serving:
                return task.Target is SeatStation servingSeat && servingSeat.IsWaitingToServe;
            case EmployeeTaskType.Casher:
                return task.Target is Counter counter && counter.HasWaitingCustomers;
            case EmployeeTaskType.DishWash:
                return task.Target is Sink sink && sink.isActiveAndEnabled && Managers.Dish != null &&
                       Managers.Dish.DirtyDishCount > 0 &&
                       (task.AssignedEmployee != null
                           ? HasDirtyDish(task.AssignedEmployee)
                           : HasEmployeeDirtyDishes());
            default:
                return false;
        }
    }

    /// <summary>해당 작업을 수행할 수 있고 현재 한가한 직원을 찾습니다.</summary>
    private EmployeeController FindAvailableEmployee(EmployeeTask task)
    {
        if (task == null || Managers.Employee == null)
            return null;

        foreach (EmployeeController worker in Managers.Employee.Employees)
        {
            if (worker == null)
                continue;

            if (worker.CurrentState != EmployeeState.Idle)
                continue;

            if (!worker.HasTask(task.Type))
                continue;

            // 청소에서는 회수 물건을 만들지 않으므로 직원 스택 공간을 확인하지 않습니다.
            // 설거지 작업은 기존 더러운 잔 재고가 있을 때만 배정합니다.
            if (task.Type == EmployeeTaskType.DishWash && !HasDirtyDish(worker))
                continue;

            return worker;
        }

        return null;
    }

    /// <summary>직원 한 명이 실제로 더러운 잔을 들고 있는지 확인합니다.</summary>
    private static bool HasDirtyDish(EmployeeController worker)
    {
        return worker != null && worker.Stacking != null &&
               worker.Stacking.GetFirstListItemType() == Define.ItemType.Beer_Dirty;
    }

    /// <summary>더러운 잔을 들고 있는 직원이 한 명이라도 있는지 확인합니다.</summary>
    private bool HasEmployeeDirtyDishes()
    {
        if (Managers.Employee == null)
            return false;

        foreach (EmployeeController worker in Managers.Employee.Employees)
        {
            if (HasDirtyDish(worker))
                return true;
        }

        return false;
    }
}
