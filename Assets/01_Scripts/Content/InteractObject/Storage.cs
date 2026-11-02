using UnityEngine;

/// <summary>운반한 재료를 공용 창고 재고에 입고하는 지점입니다.</summary>
public class Storage : InteractableBase
{
    #region 창고 설정

    [Tooltip("공용 기본 용량 외에 이 창고가 추가하는 용량입니다. 증가 수치 확정 전에는 0으로 둡니다.")]
    [SerializeField, Min(0)] private int capacity;
    /// <summary>기본 추가 용량과 이 창고의 강화 용량을 합산합니다.</summary>
    public int Capacity
    {
        get
        {
            int extra = 0;
            if (upgradeData != null) extra = upgradeData.GetExtraCapacity(UpgradeLevel);
            return capacity + extra;
        }
    }

    [Header("개별 창고 강화")]
    [SerializeField] private StorageUpgradeData upgradeData;
    [Tooltip("각 창고마다 중복되지 않는 저장용 ID를 설정합니다.")]
    [SerializeField] private string upgradeId;
    public StorageUpgradeData UpgradeData => upgradeData;
    public string UpgradeId => upgradeId;
    public int UpgradeLevel => Managers.Growth.GetStorageLevel(upgradeId);

    [Tooltip("한 번에 재료 한 개를 입고하는 시간입니다.")]
    [SerializeField, Min(0.01f)] private float depositInterval = 0.2f;
    public override float Duration => depositInterval;

    #endregion

    #region 유니티 함수

    /// <summary>활성 창고의 용량을 재고 매니저에 연결합니다.</summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        if (!Application.isPlaying) return;
        Managers.Ingredient.RegisterStorage(this);
    }

    /// <summary>창고가 사라지면 용량과 진행 중인 상호작용을 정리합니다.</summary>
    protected override void OnDisable()
    {
        base.OnDisable();
        if (!Application.isPlaying) return;
        if (Managers.Instance != null) Managers.Ingredient.UnregisterStorage(this);
    }

    #endregion

    #region 입고 기능

    /// <summary>재료를 든 플레이어만 남은 창고 공간에 입고할 수 있습니다.</summary>
    public override bool CanInteract(GameObject actor)
    {
        if (!TryGetPlayer(actor, out PlayerController player)) return false;
        if (player.Stacking == null) return false;
        if (player.Stacking.GetFirstListItemType() != Define.ItemType.Ingredient) return false;
        return Managers.Ingredient.TotalAmount < Managers.Ingredient.Capacity;
    }

    /// <summary>한 주기가 끝날 때 스택 맨 위 재료 한 개를 입고합니다.</summary>
    public override void OnCompleteInteract(GameObject actor)
    {
        if (!CanInteract(actor)) return;
        if (!TryGetPlayer(actor, out PlayerController player)) return;
        DepositOne(player.Stacking);
    }

    /// <summary>플레이어와 직원이 같은 용량 검사와 입고 처리를 사용합니다.</summary>
    public bool DepositOne(StackingSystem stack)
    {
        if (stack == null || stack.StackCount == 0) return false;
        GameObject item = stack.StackedItems[stack.StackCount - 1];
        if (item == null) return false;
        IngredientItem ingredient = item.GetComponent<IngredientItem>();
        if (ingredient == null) return false;
        if (!Managers.Ingredient.AddIngredients(ingredient.IngredientType, 1)) return false;

        // 재고 등록에 성공한 뒤에만 운반 물건을 스택에서 분리하고 제거합니다.
        GameObject taken = stack.TakeTopItem();
        if (taken != null) Managers.Resource.Destroy(taken);
        return true;
    }

    #endregion
}
