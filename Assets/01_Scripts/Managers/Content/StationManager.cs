using System;
using System.Collections.Generic;
using UnityEngine;
using static Define;

[Serializable]
public class StationManager
{
    [SerializeField]
    private List<SeatStation> allSeats = new();
    public List<SeatStation> AllSeats => allSeats;
    public int AvailableSeatCount => allSeats.FindAll(seat => seat != null && seat.gameObject.activeInHierarchy).Count;
    public event Action<int> OnSeatCountChanged;

    // 더러운 테이블 등록
    public void RegisterDirtyTable(SeatStation seat)
    {
        if (seat == null || seat.CurrentState != SeatState.Dirty)
            return;

        Managers.Task.CreateTask(EmployeeTaskType.Cleaning, seat);
    }

    public void RegisterSeat(SeatStation seat)
    {
        if (!allSeats.Contains(seat))
        {
            allSeats.Add(seat);
            OnSeatCountChanged?.Invoke(AvailableSeatCount);
        }
    }

    /// <summary>비활성화되거나 철거된 좌석을 목록에서 빼고 좌석 수를 알립니다.</summary>
    public void UnregisterSeat(SeatStation seat)
    {
        if (seat != null && allSeats.Remove(seat))
            OnSeatCountChanged?.Invoke(AvailableSeatCount);
    }

    public SeatStation FindAvailableSeat()
    {
        foreach (var seat in allSeats)
        {
            if (seat != null && seat.isActiveAndEnabled && seat.gameObject.activeInHierarchy && seat.CurrentState == SeatState.Empty)
                // &&seat.isOperational)
            {
                return seat;
            }
        }
        return null;
    }

}
