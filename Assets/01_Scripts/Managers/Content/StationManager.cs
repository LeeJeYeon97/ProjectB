using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;
using static UnityEngine.Rendering.DebugUI;

[Serializable]
public class StationManager
{
    [SerializeField]
    private List<SeatStation> allSeats = new();
    public List<SeatStation> AllSeats => allSeats;

    public event Action<EmployeeTaskType, BaseStation> OnRegisterDirtyTable;

    // 더러운 테이블 등록
    public void RegisterDirtyTable(BaseStation seat)
    {
        Managers.Employee.CreateTask(EmployeeTaskType.Cleaning, seat);
    }

    public void RegisterSeat(SeatStation seat)
    {
        if (!allSeats.Contains(seat))
            allSeats.Add(seat);
    }

    public SeatStation FindAvailableSeat()
    {
        foreach (var seat in allSeats)
        {
            if (seat.CurrentState == SeatState.Empty &&
                seat.isOperational)
            {
                return seat;
            }
        }
        return null;
    }

}
