using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Define
{
    /// <summary>상호작용 객체가 맡은 게임 작업의 종류입니다.</summary>
    public enum InteractionType
    {
        None,
        ProduceMenu,
        WashDish,
        CollectPayment,
        ServeOrCleanSeat,
        Unlock,
        ResolveCustomer
    }

    public enum FoodIngredientType
    {
        None = 0,
        Water = 1,
        UdonNoddle = 2,
        UdonSauce = 3,
    }
    public enum FoodCookingMachineType
    {
        None = 0,
        BeerMachine = 1,
        UdonMachine = 2,

    }

    

    
    
    public enum Scene
    {
        Unknown,
        LobbyScene,
        LoginScene,
        GameScene,
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
    /// <summary>플레이어의 화면 터치와 클릭 이동 방식을 고릅니다.</summary>
    public enum MovementControlMode
    {
        DragOnly,     // 화면 드래그로만 직접 조작합니다.
        ClickToMove,  // 누른 지점까지 이동합니다.
        Both          // 직접 조작과 지점 이동을 함께 사용합니다.
    }
    public enum StackType
    {
        Stack,
        NoStack,
    }
    /// <summary>플레이어가 들 수 있는 물건의 큰 분류와 기존 잔의 상태를 구별합니다.</summary>
    public enum ItemType
    {
        None = 0,
        Money = 1,
        Menu = 2,          // 메뉴의 이름은 아이템에 연결한 레시피로 구별합니다.
        Beer_Empty = 3,
        Beer_Dirty = 4,
    }

    /// <summary>퀘스트가 감시할 게임 행동의 종류입니다.</summary>
    public enum QuestObjectiveType
    {
        Unlock = 0,          // 노드를 해금하는 행동입니다.
        Produce = 1,         // 아이템이나 메뉴를 제조하는 행동입니다.
        Serve = 2,           // 아이템이나 메뉴를 손님에게 서빙하는 행동입니다.
        PlaceFurniture = 3   // 새 가구를 설치하는 행동입니다.
    }

    /// <summary>퀘스트가 감시할 아이템, 노드, 가구 대상을 고르는 목록입니다.</summary>
    public enum QuestTargetType
    {
        Any = 0,               // 행동 종류에 맞는 모든 대상을 셉니다.
        Money = 1,             // 돈 아이템입니다.
        Beer = 2,              // 맥주 아이템입니다.
        EmptyBeer = 3,         // 깨끗한 빈 맥주잔 아이템입니다.
        DirtyBeer = 4,         // 사용한 맥주잔 아이템입니다.
        TestMeal = 5,          // 임시 테스트 메뉴 아이템입니다.
        LevelNode0 = 10,       // 첫 번째 구역 해금 노드입니다.
        CounterNode0 = 11,     // 첫 번째 카운터 해금 노드입니다.
        KitchenNode0 = 12,     // 첫 번째 주방 해금 노드입니다.
        StoolNode0 = 13,       // 첫 번째 의자 해금 노드입니다.
        StoolNode1 = 14,       // 두 번째 의자 해금 노드입니다.
        StoolNode2 = 15,       // 세 번째 의자 해금 노드입니다.
        BeerMachineNode0 = 16, // 첫 번째 맥주 기계 해금 노드입니다.
        SinkNode0 = 17,        // 첫 번째 싱크대 해금 노드입니다.
        ChairFurniture = 30,   // 의자 가구입니다.
        Unknown = 999          // 코드에서 대응하지 못한 대상을 나타냅니다.
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
        Fridge,
        UdonMachine,


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
        OccupyingSeat,      // 방해 손님이 식사를 마친 뒤에도 자리를 차지하는 중
        MovingToCounter,    // 식사 완료 후 카운터로 계산하러 가는 중
        WaitingToPay,       // 카운터 대기줄에서 대기 중
        Leaving             // 돈 내고 상점 밖 스폰 구역으로 이동 중 (퇴장)
    }

    /// <summary>일반 손님과 방해 손님의 행동 유형입니다.</summary>
    public enum CustomerType
    {
        Normal,        // 주변 손님을 방해하지 않는 일반 손님입니다.
        QueueNoiser,   // 대기줄에 있을 때만 소음을 내는 손님입니다.
        SeatNoiser,    // 좌석에 앉아 있을 때만 소음을 내는 손님입니다.
        SeatHog        // 식사를 마친 뒤에도 좌석을 비우지 않는 손님입니다.
    }

    /// <summary>손님이 식사를 마친 뒤 남기는 리뷰 등급입니다.</summary>
    public enum ReviewGrade
    {
        VeryBad = 1, // 대기와 식사 방해가 많아 매우 낮은 평가입니다.
        Bad = 2,     // 만족하지 못한 평가입니다.
        Normal = 3,  // 보통 수준의 평가입니다.
        Good = 4,    // 만족스러운 평가입니다.
        Excellent = 5 // 기다림과 식사 방해가 거의 없는 최고의 평가입니다.
    }

    public enum SeatState
    {
        Empty,      // 완전히 비어있어 손님을 받을 수 있는 상태
        Reserved,   // 손님이 이 자리로 걸어오는 중 (선점됨)
        Occupied,   // 손님이 앉아서 식사/주문 대기 중인 상태
        Dirty       // 손님이 떠난 후 접시 등이 남아 있어 청소(청소 상호작용)가 필요한 상태
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
        BeerMissing,    // 맥주 기계에 깨끗한 빈 잔이 없음
        Disturbance,    // 플레이어가 해결할 수 있는 방해 손님
    }
}

