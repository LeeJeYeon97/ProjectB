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


    [Header("position")]
    public Transform customerSitPoint; // 손님이 앉을 실제 3D 좌표/회전 기준점
    public Transform foodServePoint;   // 음식을 올려놓을 테이블 위 좌표


    private void Start()
    {
        UpdateState();
        Managers.Station.RegisterSeat(this);
    }

    public void SetState(SeatState state)
    {
        CurrentState = state;
    }
    public void UpdateState()
    {
        if (uiBubble == null)
            uiBubble = GetComponentInChildren<UI_Bubble>(true);
        
        if (currentState == SeatState.Dirty)
        {
            if (uiBubble != null)
            {
                uiBubble.gameObject.SetActive(true);
                uiBubble.SetBubble(Define.BubbleType.Cleaning);
            }
        }
        else
        {
            if (uiBubble != null)
            {
                uiBubble.Hide();
            }
        }
    }

    /// <summary>
    /// Dirty 상태의 좌석을 비운 상태로 되돌립니다.
    /// 플레이어가 청소한 경우에만 빈 잔을 플레이어 스택에 넣습니다.
    /// </summary>
    public bool CleanSeat(PlayerController cleaner = null)
    {
        if (currentState != SeatState.Dirty)
            return false;

        if (cleaner != null &&
            (cleaner.Stacking == null || !cleaner.Stacking.CheckListItem(ItemType.Beer_Dirty)))
        {
            return false;
        }

        if (cleaner != null)
        {
            Vector3 spawnPosition = foodServePoint != null ? foodServePoint.position : transform.position;
            GameObject dirtyBeer = Managers.Resource.Instantiate("Item_BeerDirty", spawnPosition, Quaternion.identity);

            if (dirtyBeer == null)
                return false;

            cleaner.Stacking.PickUpItem(dirtyBeer);
        }

        // 플레이어가 직접 청소하든 직원이 청소하든 더러운 접시를 재고에 남깁니다.
        if (Managers.Dish != null)
            Managers.Dish.AddDirtyDish();

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

        // 서빙은 손님이 실제로 음식 대기 중이고, 손에 주문과 맞는 아이템이 있을 때만 가능하다.
        if (currentState != SeatState.Occupied ||
            currentCustomer == null ||
            currentCustomer.CurrentState != CustomerState.WaitingForFood ||
            player.Stacking.StackCount <= 0)
        {
            return false;
        }

        GameObject topItem = player.Stacking.StackedItems[player.Stacking.StackCount - 1];
        ItemController item = topItem != null ? topItem.GetComponent<ItemController>() : null;
        return item != null && item.type == currentCustomer.OrderedType;
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
            player.Stacking.PopItem();
            currentCustomer.OnFoodServed();
            Debug.Log("음식을 서빙했습니다. 식사를 시작합니다!");
        }
    }

    protected override void OnInteractionEnded()
    {
        UpdateState();
    }

}
