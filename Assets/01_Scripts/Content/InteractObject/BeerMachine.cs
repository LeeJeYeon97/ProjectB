using UnityEngine;

public class BeerMachine : InteractableBase
{
    [Header("맥주 제작 설정")]
    [SerializeField] private GameObject beerPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float duration = 2.0f;

    [Header("잔 재고 설정")]
    [SerializeField, Min(0)] private int dishCount = 1;
    [SerializeField, Min(1)] private int maxDishCount = 10;

    // 잔을 넣은 직후 빈손 상태로 자동 맥주 생산이 이어지지 않게 기억합니다.
    private GameObject actorJustLoadedDish;

    /// <summary>빈손이나 맥주를 들고 맥주를 만들 때 걸리는 시간입니다.</summary>
    public override float Duration => duration;

    /// <summary>깨끗한 잔을 기계에 넣는 동작은 즉시 끝냅니다.</summary>
    protected override float GetDuration(GameObject actor)
    {
        return GetHeldItemType(actor) == Define.ItemType.Beer_Empty ? 0f : duration;
    }

    /// <summary>기계에 잔을 넣거나, 빈손과 맥주 스택으로 맥주를 만들 수 있는지 확인합니다.</summary>
    public override bool CanInteract(GameObject actor)
    {
        if (actor == null)
            return false;

        PlayerController player = actor.GetComponent<PlayerController>();
        if (player == null || player.Stacking == null)
            return false;

        // 잔을 넣은 뒤에는 플레이어가 기계 범위를 나갔다가 다시 들어와야 다음 동작을 고릅니다.
        if (actor == actorJustLoadedDish)
            return false;

        Define.ItemType heldType = player.Stacking.GetFirstListItemType();

        // 깨끗한 잔을 들고 있으면 기계가 가득 차지 않은 경우에만 넣을 수 있습니다.
        if (heldType == Define.ItemType.Beer_Empty)
        {
            return dishCount < maxDishCount;
        }

        // 빈손이거나 이미 만든 맥주를 들고 있으면 기존처럼 차례로 맥주를 받을 수 있습니다.
        // CheckListItem은 빈 스택과 같은 종류의 맥주 스택을 모두 허용하고 가득 찬 손은 막습니다.
        if (heldType == Define.ItemType.None || heldType == Define.ItemType.Beer)
        {
            return dishCount > 0 &&
                   player.Stacking.CheckListItem(Define.ItemType.Beer);
        }

        return false;
    }

    /// <summary>잔은 기계 재고로 옮기고, 빈손이나 맥주를 든 경우에는 맥주 하나를 생산합니다.</summary>
    public override void OnCompleteInteract(GameObject actor)
    {
        if (actor == null)
            return;

        PlayerController player = actor.GetComponent<PlayerController>();
        if (player == null || player.Stacking == null)
            return;

        Define.ItemType heldType = player.Stacking.GetFirstListItemType();
        if (heldType == Define.ItemType.Beer_Empty)
        {
            // 손에 든 잔을 제거하고 기계 안의 사용 가능한 잔 수를 늘립니다.
            // CheckListItem은 새 아이템을 추가할 공간을 검사하므로, 빼는 동작에서는 사용하지 않습니다.
            if (dishCount >= maxDishCount ||
                player.Stacking.StackCount <= 0 ||
                player.Stacking.GetFirstListItemType() != Define.ItemType.Beer_Empty)
            {
                return;
            }

            player.Stacking.PopItem();
            dishCount++;
            actorJustLoadedDish = actor;
            Debug.Log($"깨끗한 맥주잔을 비어머신에 넣었습니다. 잔 재고: {dishCount}");
            return;
        }

        // 완료 직전에 조건을 다시 확인해 재고나 스택이 바뀌었으면 생산하지 않습니다.
        if (dishCount <= 0 || !player.Stacking.CheckListItem(Define.ItemType.Beer))
            return;

        Vector3 beerPosition = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion beerRotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;
        GameObject beer = beerPrefab != null
            ? Managers.Resource.Instantiate(beerPrefab, beerPosition, beerRotation)
            : Managers.Resource.Instantiate("Item_Beer", beerPosition, beerRotation);

        if (beer == null)
            return;

        player.Stacking.PickUpItem(beer);
        dishCount--;
        Debug.Log($"맥주 생산 완료! 남은 잔 재고: {dishCount}");
    }

    /// <summary>잔을 넣은 플레이어가 범위를 나가면 다음 방문부터 맥주를 만들 수 있게 합니다.</summary>
    protected override void OnActorLeftRange(GameObject actor)
    {
        if (actorJustLoadedDish == actor)
            actorJustLoadedDish = null;
    }

    /// <summary>현재 손에 든 스택의 종류를 가져오고, 빈손이면 None을 돌려줍니다.</summary>
    private static Define.ItemType GetHeldItemType(GameObject actor)
    {
        PlayerController player = actor != null ? actor.GetComponent<PlayerController>() : null;
        if (player == null || player.Stacking == null)
            return Define.ItemType.None;

        return player.Stacking.GetFirstListItemType();
    }
}
