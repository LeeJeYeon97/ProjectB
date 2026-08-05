using System.Collections;
using UnityEngine;
using static Define;

public class SeatStation : BaseStation
{
    [Header("Seat Configuration")]
    public SeatState CurrentState = SeatState.Empty;
    public Transform customerSitPoint; // 손님이 앉을 실제 3D 좌표/회전 기준점
    public Transform foodServePoint;   // 음식을 올려놓을 테이블 위 좌표

    [Header("Interaction Settings")]
    public float interactionDuration = 1.5f; // 청소 또는 주문받기에 걸리는 시간
    private float currentProgress = 0f;

    public CustomerController currentCustomer;

    
    private void Start()
    {
        //stationID = $"Seat_{GetInstanceID()}";
        Managers.Customer.RegisterSeat(this);
    }
    public void SetState(SeatState state)
    {
        CurrentState = state;
    }
    // 플레이어가 자리(발판) 위에 올라와 상호작용할 때 호출됨 (0.1초 간격)
    public override void OnCharacterStay(GameObject character)
    {
        if (!isOperational) return;

        // 플레이어 정보를 가져옴
        PlayerController player = character.GetComponent<PlayerController>();
        if (player == null) return;

        switch (CurrentState)
        {
            case SeatState.Dirty:
                // 1. 자리 치우기 (청소 중)
                currentProgress += Time.deltaTime;
                // TODO: UI에 청소 ProgressBar 표시 (예: currentProgress / interactionDuration)
                if (currentProgress >= interactionDuration)
                {
                    CleanSeat();
                    currentProgress = 0f;
                }
                break;

            case SeatState.Occupied:
                if (currentCustomer == null) return;

                // 2. 주문받기
                if (currentCustomer.CurrentState == CustomerState.WaitingToOrder)
                {
                    currentProgress += Time.deltaTime;
                    if (currentProgress >= interactionDuration)
                    {
                        TakeOrder();
                        currentProgress = 0f;
                    }
                }
                // 3. 음식 서빙하기
                else if (currentCustomer.CurrentState == CustomerState.WaitingForFood)
                {
                    // 플레이어의 가방(StackingSystem)에 해당 음식이 있는지 검사
                    StackingSystem stacking = player.GetComponent<StackingSystem>();
                    if (stacking != null && HasMatchingFood(stacking))
                    {
                        ServeFood(stacking);
                    }
                }
                break;
        }
    }

    private void CleanSeat()
    {
        CurrentState = SeatState.Empty;
        Debug.Log("자리가 깨끗하게 치워졌습니다! 이제 새로운 손님을 받을 수 있습니다.");
        // TODO: 청소 이펙트 생성, 접시 오브젝트 파괴 등
    }

    private void TakeOrder()
    {
        currentCustomer.OnOrderTaken();
        Debug.Log("손님의 주문을 받았습니다!");
        // TODO: 머리 위 말풍선 변경 (주문완료 대기 중 상태로)
    }

    private bool HasMatchingFood(StackingSystem stacking)
    {
        // TODO: 플레이어가 머리 위에 쌓아둔 마지막 아이템(혹은 매칭되는 타입)이 손님이 원하는 음식 유형인지 체크
        // 현재 StackingSystem의 리스트를 검사하는 함수 작성 필요
        return true;
    }

    private void ServeFood(StackingSystem stacking)
    {
        // TODO: StackingSystem에서 음식을 제거(Pop)하고, foodServePoint에 배치하는 연출 실행
        currentCustomer.OnFoodServed();
        Debug.Log("음식을 서빙했습니다. 식사를 시작합니다!");
    }
}