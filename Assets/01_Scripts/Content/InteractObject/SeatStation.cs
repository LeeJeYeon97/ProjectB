using UnityEngine;
using static Define;

public class SeatStation : InteractableBase
{
    [Header("Seat Configuration")]
    [SerializeField]
    private SeatState currentState = SeatState.Empty;
    public SeatState CurrentState
    {
        get => currentState;
        private set
        {
            if (currentState == value)
                return;

            currentState = value;
            UpdateState();
        }
    }

    [Header("Interaction Settings")]
    [SerializeField] private float servingDuration = 0.0f;
    [SerializeField] private float cleaningDuration = 1.5f;

    // 좌석의 현재 상태에 따라 플레이어가 수행하는 행동이 달라진다.
    public override float Duration => currentState == SeatState.Dirty ? cleaningDuration : servingDuration;

    // 음식 서빙은 즉시 끝나므로 좌석 말풍선에 작업 진행도를 띄우지 않습니다.
    protected override bool UsesProgressBubble => currentState == SeatState.Dirty;
    
    public CustomerController currentCustomer;
    public EmployeeController AssignedCleaner { get; set; }
    public bool IsWaitingToServe =>
        isActiveAndEnabled &&
        currentState == SeatState.Occupied &&
        currentCustomer != null &&
        currentCustomer.CurrentState == CustomerState.WaitingForFood;

    [Header("position")]
    public Transform customerSitPoint; // 손님이 앉을 실제 3D 좌표/회전 기준점
    public Transform foodServePoint;   // 음식을 올려놓을 테이블 위 좌표


    protected override void OnEnable()
    {
        base.OnEnable();
        UpdateState();
        Managers.Station.RegisterSeat(this);
    }

    /// <summary>좌석이 비활성화되거나 제거될 때 손님 배정 목록에서 해제합니다.</summary>
    protected override void OnDisable()
    {
        base.OnDisable();

        if (Managers.Instance != null)
        {
            Managers.Task.RemoveTasksFor(this);
            Managers.Station.UnregisterSeat(this);
        }
    }

    public void SetState(SeatState state)
    {
        CurrentState = state;
    }
    public void UpdateState()
    {
        // 더러운 자리에서만 공유 말풍선을 빌리고, 청소되면 표시를 닫습니다.
        if (currentState == SeatState.Dirty)
            ShowStatusBubble(Define.BubbleType.Cleaning);
        else
            HideStatusBubble();
    }

    /// <summary>공용 말풍선 프리팹이 로드된 뒤 현재 좌석 상태를 다시 표시합니다.</summary>
    public override void RefreshStatusBubble() => UpdateState();

    /// <summary>
    /// 더러운 좌석을 비우고, 플레이어가 청소하면 더러운 잔을 플레이어 스택에 넣습니다.
    /// </summary>
    public bool CleanSeat(PlayerController cleaner = null)
    {
        return CleanSeatInternal(cleaner, null);
    }

    /// <summary>직원이 좌석을 청소하며 만든 더러운 잔을 직원 스택에 넣습니다.</summary>
    public bool CleanSeatForEmployee(StackingSystem employeeStack)
    {
        if (employeeStack == null || !employeeStack.CheckListItem(ItemType.Beer_Dirty))
            return false;

        return CleanSeatInternal(null, employeeStack);
    }

    /// <summary>좌석 청소와 잔 생성을 한 곳에서 처리해 플레이어와 직원의 상태를 맞춥니다.</summary>
    private bool CleanSeatInternal(PlayerController cleaner, StackingSystem employeeStack)
    {
        if (currentState != SeatState.Dirty)
            return false;

        if (cleaner != null &&
            (cleaner.Stacking == null || !cleaner.Stacking.CheckListItem(ItemType.Beer_Dirty)))
        {
            return false;
        }

        if (cleaner != null || employeeStack != null)
        {
            Vector3 spawnPosition = foodServePoint != null ? foodServePoint.position : transform.position;
            GameObject dirtyBeer = Managers.Resource.Instantiate("Item_BeerDirty", spawnPosition, Quaternion.identity);

            if (dirtyBeer == null)
                return false;

            if (cleaner != null)
                cleaner.Stacking.PickUpItem(dirtyBeer);
            else
                employeeStack.PickUpItem(dirtyBeer);
        }

        // 직원이 만든 잔은 실제 스택과 공용 재고에 함께 기록해 설거지 작업이 찾게 합니다.
        if (employeeStack != null && Managers.Dish != null)
        {
            Managers.Dish.AddDirtyDish();

            // 직원용 재고가 늘어난 뒤 활성 싱크대에 설거지 작업을 등록합니다.
            if (Managers.Task != null)
                Managers.Task.RegisterDishWashTask();
        }

        CurrentState = SeatState.Empty;
        AssignedCleaner = null;
        Debug.Log($"좌석 {name} 청소 완료: {currentState}");
        return true;
    }

    public override bool CanInteract(GameObject actor)
    {
        if (!TryGetPlayer(actor, out PlayerController player) || player.Stacking == null)
            return false;

        // 손님이 식사를 끝낸 뒤에만 청소할 수 있으며, 빈 잔을 담을 자리도 필요하다.
        if (currentState == SeatState.Dirty)
            return player.Stacking.CheckListItem(ItemType.Beer_Dirty);

        // 테스트를 위해 손님이 음식을 기다리는 동안 두 메뉴를 모두 서빙할 수 있게 합니다.
        if (currentState != SeatState.Occupied ||
            currentCustomer == null ||
            currentCustomer.CurrentState != CustomerState.WaitingForFood ||
            player.Stacking.StackCount <= 0)
        {
            return false;
        }

        GameObject topItem = player.Stacking.StackedItems[player.Stacking.StackCount - 1];
        ItemController item = topItem != null ? topItem.GetComponent<ItemController>() : null;
        return item != null && IsMenuItem(item.type);
    }

    public override void OnCompleteInteract(GameObject actor)
    {
        if (!TryGetPlayer(actor, out PlayerController player))
            return;

        if (currentState == SeatState.Dirty)
        {
            CleanSeat(player);
            return;
        }

        if (currentState == SeatState.Occupied && currentCustomer != null && CanInteract(actor))
        {
            // 아이템을 손님에게 주기 전에 실제 서빙 종류를 기억합니다.
            GameObject servedObject = player.Stacking.StackedItems[player.Stacking.StackCount - 1];
            ItemController servedItem = servedObject != null ? servedObject.GetComponent<ItemController>() : null;
            player.Stacking.PopItem();
            currentCustomer.OnFoodServed(servedItem != null ? servedItem.type : ItemType.None);
            // 실제로 서빙한 아이템 종류를 공통 ID로 바꿔 퀘스트에 전달합니다.
            if (servedItem != null && Managers.Quest != null)
                Managers.Quest.ReportItemProgress(Define.QuestObjectiveType.Serve, servedItem.type);
            Debug.Log("음식을 서빙했습니다. 식사를 시작합니다!");
        }
    }

    /// <summary>직원이 스택에 준비한 맥주를 꺼내 손님에게 서빙합니다.</summary>
    public bool ServeForEmployee(StackingSystem employeeStack)
    {
        if (!IsWaitingToServe || employeeStack == null || employeeStack.StackCount <= 0)
            return false;

        ItemType servedType = employeeStack.GetFirstListItemType();
        if (!IsMenuItem(servedType))
            return false;

        employeeStack.PopItem();
        currentCustomer.OnFoodServed(servedType);
        // 직원이 전달한 아이템 종류를 공통 ID로 바꿔 퀘스트에 보고합니다.
        if (Managers.Quest != null)
            Managers.Quest.ReportItemProgress(Define.QuestObjectiveType.Serve, servedType);
        Debug.Log($"직원이 좌석 {name}에 주문을 서빙했습니다.");
        return true;
    }

    /// <summary>손님에게 서빙할 수 있는 메뉴 아이템인지 확인합니다.</summary>
    private static bool IsMenuItem(ItemType itemType)
    {
        return itemType == ItemType.Beer || itemType == ItemType.TestMeal;
    }

    protected override void OnInteractionEnded()
    {
        UpdateState();
    }

}
