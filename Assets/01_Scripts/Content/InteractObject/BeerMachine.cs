using UnityEngine;

/// <summary>맥주 제조에 필요한 빈 잔 재고와 보충 처리를 관리합니다.</summary>
public class BeerMachine : MenuMachine
{
    [Header("빈 잔 투입")]
    [SerializeField, Min(0f)] private float transferInterval = 0.5f;

    [Header("빈 잔 재고")]
    [SerializeField, Min(0)] private int dishCount = 1;
    [SerializeField, Min(1)] private int maxDishCount = 10;

    private float nextDishTransferTime;

    /// <summary>맥주머신 한 대가 메뉴를 만드는 데 필요한 최소 시간을 지정합니다.</summary>
    public override float MinimumProductionDuration => 0.5f;

    /// <summary>잔이 하나 이상 있을 때만 맥주 제조 작업을 시작할 수 있습니다.</summary>
    protected override bool HasProductionStock => dishCount > 0;

    /// <summary>맥주 한 잔을 만들 때 빈 잔 재고 하나를 사용합니다.</summary>
    protected override void OnMenuProduced()
    {
        dishCount--;
        RefreshDishAlert();
    }

    /// <summary>활성화될 때 잔 재고 상태를 말풍선에 표시합니다.</summary>
    public override void RefreshStatusBubble()
    {
        base.RefreshStatusBubble();
        RefreshDishAlert();
    }

    /// <summary>빈 잔 재고가 없으면 부족 상태를 표시하고, 있으면 상태 말풍선을 닫습니다.</summary>
    public void RefreshDishAlert()
    {
        if (!isActiveAndEnabled)
            return;

        if (dishCount <= 0)
        {
            ShowStatusBubble(Define.BubbleType.BeerMissing);
            return;
        }

        HideStatusBubble();
    }

    /// <summary>잔 투입 구역에 있는 플레이어에게 간격을 두고 깨끗한 잔을 받습니다.</summary>
    protected override void OnTriggerStay(Collider other)
    {
        // 공통 범위 상태는 부모가 갱신하고, 여기서는 빈 잔 보충만 처리합니다.
        base.OnTriggerStay(other);

        if (Time.time < nextDishTransferTime)
            return;

        GameObject actor = GetActor(other);
        if (actor != null && TryLoadCleanDish(actor))
            nextDishTransferTime = Time.time + transferInterval;
    }

    /// <summary>빈 잔을 든 플레이어에게서 잔 하나를 받아 재고에 넣습니다.</summary>
    private bool TryLoadCleanDish(GameObject actor)
    {
        if (!TryGetPlayer(actor, out PlayerController player))
            return false;

        if (player.Stacking == null)
            return false;

        if (dishCount >= maxDishCount)
            return false;

        if (player.Stacking.GetFirstListItemType() != Define.ItemType.Beer_Empty)
            return false;

        // 재고로 흡수한 잔 오브젝트는 플레이어 스택에서 꺼내며 바로 제거합니다.
        player.Stacking.PopItem();
        dishCount++;
        RefreshDishAlert();
        Managers.Task?.AssignWaitingTasks();
        return true;
    }

    /// <summary>직원 진행 표시가 닫힌 뒤 빈 잔 재고 상태를 다시 표시합니다.</summary>
    public override void HideTaskProgress()
    {
        base.HideTaskProgress();
        RefreshDishAlert();
    }

    /// <summary>제조가 끝난 뒤 플레이어 점유 정리를 이어받고 잔 부족 표시를 갱신합니다.</summary>
    protected override void OnInteractionEnded()
    {
        base.OnInteractionEnded();
        RefreshDishAlert();
    }

    /// <summary>꺼진 기계가 다음 활성화 때 즉시 잔을 받을 수 있도록 대기 시간을 초기화합니다.</summary>
    protected override void OnDisable()
    {
        nextDishTransferTime = 0f;
        base.OnDisable();
    }
}
