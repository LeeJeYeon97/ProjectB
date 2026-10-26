using UnityEngine;
using static Define;

public class SeatStation : InteractableBase
{
    #region 변수와 프로퍼티

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

    #endregion

    #region Unity 제공 함수

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

    #endregion

    #region 좌석 상태

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

    #endregion

    #region 좌석 청소

    /// <summary>플레이어가 더러운 좌석을 청소해 다시 사용할 수 있는 자리로 바꿉니다.</summary>
    public bool CleanSeat(PlayerController cleaner = null)
    {
        return CleanSeatInternal(cleaner, null);
    }

    /// <summary>직원이 더러운 좌석을 청소합니다. 현재는 회수 물건을 만들지 않습니다.</summary>
    public bool CleanSeatForEmployee(StackingSystem employeeStack)
    {
        return CleanSeatInternal(null, employeeStack);
    }

    /// <summary>청소가 끝난 좌석 상태를 비우고, 플레이어와 직원이 같은 결과를 사용합니다.</summary>
    private bool CleanSeatInternal(PlayerController cleaner, StackingSystem employeeStack)
    {
        if (currentState != SeatState.Dirty)
            return false;

        // 회수 물건을 다시 사용할 때는 서빙한 레시피에 맞는 식기를 생성하도록 바꿉니다.
        // 현재는 맥주잔 생성과 직원용 설거지 재고 반영을 모두 중단합니다.
        /*
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

        if (employeeStack != null && Managers.Dish != null)
        {
            Managers.Dish.AddDirtyDish();

            if (Managers.Task != null)
                Managers.Task.RegisterDishWashTask();
        }
        */

        CurrentState = SeatState.Empty;
        AssignedCleaner = null;
        Debug.Log($"좌석 {name} 청소 완료: {currentState}");
        return true;
    }

    #endregion

    #region 상호작용과 서빙

    public override bool CanInteract(GameObject actor)
    {
        if (!TryGetPlayer(actor, out PlayerController player))
            return false;

        // 청소할 때 회수 물건을 만들지 않으므로 스택 공간 없이도 자리를 비울 수 있습니다.
        if (currentState == SeatState.Dirty)
            return true;

        if (player.Stacking == null)
            return false;

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
        return true;
    }

    protected override void OnInteractionEnded()
    {
        UpdateState();
    }

    #endregion
}
