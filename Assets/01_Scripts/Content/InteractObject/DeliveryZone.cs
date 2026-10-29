using UnityEngine;

/// <summary>배송 위치와 재료 반환 범위를 씬에서 제공하는 상호작용 지점입니다.</summary>
public class DeliveryZone : InteractableBase
{
    #region 배송 지점 설정

    [SerializeField, Min(0.01f)] private float returnInterval = 0.2f;
    public override float Duration => returnInterval;

    [Tooltip("배송한 재료를 놓을 기준 위치입니다. 비어 있으면 배송 지점 중심을 사용합니다.")]
    [SerializeField] private Transform deliveryPoint;
    private PlayerController returningPlayer;
    private bool waitingForPlayerExit;

    #endregion

    #region 유니티 함수

    /// <summary>배송 지점이 활성화되면 대기 중인 운반 업무를 확인합니다.</summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        if (Application.isPlaying) RequestWork();
    }

    /// <summary>직원이 같은 범위에 들어와도 플레이어의 반환 종료를 별도로 확인합니다.</summary>
    private void LateUpdate()
    {
        if (!waitingForPlayerExit) return;
        Collider zone = GetComponent<Collider>();
        Collider player = null;
        if (returningPlayer != null) player = returningPlayer.GetComponent<Collider>();
        if (zone != null && player != null && player.enabled)
        {
            if (zone.bounds.Intersects(player.bounds)) return;
        }
        returningPlayer = null;
        waitingForPlayerExit = false;
        Managers.Ingredient.EnableReturnedItems();
        RequestWork();
    }

    /// <summary>배송 지점이 꺼져도 반환한 물건을 다시 가져갈 수 있게 합니다.</summary>
    protected override void OnDisable()
    {
        base.OnDisable();
        if (!Application.isPlaying || !waitingForPlayerExit) return;
        returningPlayer = null;
        waitingForPlayerExit = false;
        Managers.Ingredient.EnableReturnedItems();
    }

    #endregion

    #region 배송 및 반환

    /// <summary>창고가 가득 찬 경우 플레이어가 든 재료를 배송 지점으로 돌려놓습니다.</summary>
    public override bool CanInteract(GameObject actor)
    {
        if (!TryGetPlayer(actor, out PlayerController player)) return false;
        // 직원이 배송 업무를 수행 중일 때 플레이어의 반환으로 업무를 강제 중단하지 않습니다.
        if (IsWorking && WorkingActor != actor) return false;
        if (player.Stacking == null) return false;
        if (player.Stacking.GetFirstListItemType() != Define.ItemType.Ingredient) return false;
        return Managers.Ingredient.TotalAmount >= Managers.Ingredient.Capacity;
    }

    /// <summary>반환 중 다시 줍지 않도록 플레이어가 떠날 때까지 물건의 충돌체를 잠시 끕니다.</summary>
    public override void OnCompleteInteract(GameObject actor)
    {
        if (!CanInteract(actor)) return;
        if (!TryGetPlayer(actor, out PlayerController player)) return;
        GameObject item = player.Stacking.TakeTopItem();
        if (item == null) return;
        returningPlayer = player;
        waitingForPlayerExit = true;
        Managers.Ingredient.ReturnDelivery(item, actor, true);
    }

    /// <summary>플레이어가 배송 지점에서 떠나면 반환 물품을 다시 주울 수 있습니다.</summary>
    protected override void OnActorLeftRange(GameObject actor)
    {
        if (!TryGetPlayer(actor, out PlayerController player)) return;
        returningPlayer = null;
        waitingForPlayerExit = false;
        Managers.Ingredient.EnableReturnedItems();
        RequestWork();
    }

    /// <summary>배송 지점 안의 표시 자리에서 지정한 순서의 위치를 반환합니다.</summary>
    public Vector3 GetDeliveryPosition(int count)
    {
        Transform point = deliveryPoint;
        if (point == null) point = transform;
        int column = count % 4;
        int row = count / 4 % 3;
        return point.position + point.rotation * new Vector3((column - 1.5f) * 0.4f, 0.25f, (row - 1) * 0.4f);
    }

    /// <summary>입고할 공간과 배송 물품이 있으면 직원 운반 업무를 등록합니다.</summary>
    public void RequestWork()
    {
        if (!Application.isPlaying) return;
        if (Managers.Instance == null) return;
        if (!Managers.Ingredient.HasDelivery()) return;
        Managers.Task.CreateTask(Define.EmployeeTaskType.CarryIngredient, this);
    }

    #endregion
}
