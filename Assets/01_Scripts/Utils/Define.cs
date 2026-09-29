using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Define
{
    
    public enum Scene
    {
        Unknown,
        LobbyScene,
        LoginScene,
        GameScene,
    }
    public enum CameraType
    {
        Main,
        Boss,
        Burst,
    }
    public enum Sound
    {
        Bgm,
        Sfx,
        MaxCount,
    }
    public enum SoundID
    {
        
    }
    public enum UIEvent
    {
        Click,
        Drag,
    }

    public enum MouseEvent
    {
        Press,
        Click,
    }

    public enum CameraMode
    {
        QuarterView,
    }
    public enum StackType
    {
        Stack,
        NoStack,
    }
    public enum ItemType
    {
        None,
        Money,
        Beer,
        Beer_Empty,
        Beer_Dirty,
    }
    public enum UnlockType
    {
        SpawnPrefab,        // 상호작용 오브젝트 : 동적 생성
        ToggleActive        // 구역 확장 : 기존 씬 오브젝트 활성화
    }

    public enum UnlockGroupType
    {
        None,
        Level,
        Counter,
        Kitchen,
        Stool,
        Table,
        BeerMachine,
        Sink,

    }
    public enum CustomerState
    {
        None,
        WalkEntrance,       // 가게 앞으로 걸어가는 중
        WaitingAtEntrance,  // 가게 앞 입구에서 대기 중
        MovingToSeat,       // 예약된 자리로 이동 중
        WaitingToOrder,     // 자리에 앉아서 주문 대기 중 (말풍선 표시)
        WaitingForFood,     // 주문 완료 후 음식을 기다리는 중
        Eating,             // 음식을 먹는 중 (식사 타이머 작동)
        MovingToCounter,    // 식사 완료 후 카운터로 계산하러 가는 중
        WaitingToPay,       // 카운터 대기줄에서 대기 중
        Leaving             // 돈 내고 상점 밖 스폰 구역으로 이동 중 (퇴장)
    }

    public enum SeatState
    {
        Empty,      // 완전히 비어있어 손님을 받을 수 있는 상태
        Reserved,   // 손님이 이 자리로 걸어오는 중 (선점됨)
        Occupied,   // 손님이 앉아서 식사/주문 대기 중인 상태
        Dirty       // 손님이 떠난 후 접시 등이 남아 있어 청소(청소 상호작용)가 필요한 상태
    }
    public enum InterationType
    {
        None,
        Proximity,  // 가까이 가면 상호작용
        StandOn     // 발판을 밟으면 상호작용
    }
    public enum EmployeeTaskType
    {
        None,
        Cleaning,

        DishWash,
        Serving,
        Casher,
    }
    public enum EmployeeState
    {
        Idle,
        MovingToWork,
        Working,
    }

    public enum BubbleType
    {
        None,

        // 손님 관련
        //Order,          // 음식 주문
        Payment,        // 계산 요청

        // 좌석 관련
        FoodReady,      // 음식 서빙 필요
        Cleaning,       // 테이블 청소 필요

        // 작업대 관련
        Progress,       // 작업 진행도
    }
}

