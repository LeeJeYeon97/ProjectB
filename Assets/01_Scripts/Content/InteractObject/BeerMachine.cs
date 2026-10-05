using UnityEngine;
using DG.Tweening;

public class BeerMachine : InteractableBase
{
    private static int sharedCapacityBonus;
    [Header("맥주 제작 설정")]
    [SerializeField] private GameObject beerPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform cupDisplayPoint;
    [SerializeField] private float duration = 2.0f;

    [Header("잔 재고 설정")]
    [SerializeField, Min(0)] private int dishCount = 1;
    [SerializeField, Min(1)] private int maxDishCount = 10;
    private int baseMaxDishCount;
    // 한 기계에서 동시에 한 주체만 작업하도록 현재 작업자를 저장합니다.
    private GameObject workingActor;

    // 잔을 넣은 직후 빈손 상태로 자동 맥주 생산이 이어지지 않게 기억합니다.
    private GameObject actorJustLoadedDish;

    // 표시 지점에 놓인 빈 잔을 생산 출구로 옮기는 연출을 보관합니다.
    private Tween cupMoveTween;

    /// <summary>기계가 제공하는 기본 제조 시간입니다. 작업자 능력치는 이 값에 별도로 반영합니다.</summary>
    public float BaseProductionDuration => Mathf.Max(0.25f, duration);

    /// <summary>현재 플레이어나 직원이 이 기계를 사용 중인지 알려줍니다.</summary>
    public bool IsWorking => workingActor != null;

    /// <summary>직원이 새 작업을 시작할 수 있는 잔이 남아 있는지 알려줍니다.</summary>
    private bool CanEmployeeWork => !IsWorking && dishCount > 0;

    /// <summary>직원이 바로 사용할 수 있는 맥주 기계가 있는지 알려줍니다.</summary>
    public static bool HasEmployeeBeer
    {
        get
        {
            BeerMachine[] machines = FindObjectsByType<BeerMachine>(FindObjectsSortMode.None);
            foreach (BeerMachine machine in machines)
            {
                if (machine != null && machine.isActiveAndEnabled && machine.CanEmployeeWork)
                    return true;
            }

            return false;
        }
    }

    /// <summary>현재 비어 있는 기계에 남아 있는 잔 수량을 모두 더합니다.</summary>
    public static int EmployeeDishCount
    {
        get
        {
            int count = 0;
            BeerMachine[] machines = FindObjectsByType<BeerMachine>(FindObjectsSortMode.None);
            foreach (BeerMachine machine in machines)
            {
                if (machine != null && machine.isActiveAndEnabled && machine.CanEmployeeWork)
                    count += machine.dishCount;
            }

            return count;
        }
    }

    /// <summary>작업 중이 아닌 기계 하나를 골라 해당 직원이 독점 사용하게 합니다.</summary>
    public static bool TryStartWorkForEmployee(EmployeeController employee, out BeerMachine machineForWork)
    {
        machineForWork = null;
        if (employee == null)
            return false;

        BeerMachine[] machines = FindObjectsByType<BeerMachine>(FindObjectsSortMode.None);
        foreach (BeerMachine machine in machines)
        {
            if (machine == null || !machine.isActiveAndEnabled || !machine.CanEmployeeWork)
                continue;

            machine.workingActor = employee.gameObject;
            machineForWork = machine;
            return true;
        }

        return false;
    }

    /// <summary>기계를 점유한 직원이 맥주를 만들고, 생산이 끝나면 기계 점유를 해제합니다.</summary>
    public bool ProduceBeerForEmployee(EmployeeController employee, StackingSystem stack)
    {
        if (employee == null || stack == null || workingActor != employee.gameObject)
            return false;

        if (!isActiveAndEnabled || dishCount <= 0 || !stack.CheckListItem(Define.ItemType.Beer))
            return false;

        Vector3 beerPosition = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion beerRotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;
        GameObject beer = beerPrefab != null
            ? Managers.Resource.Instantiate(beerPrefab, beerPosition, beerRotation)
            : Managers.Resource.Instantiate("Item_Beer", beerPosition, beerRotation);

        if (beer == null)
            return false;

        // 새 맥주 생성에 성공한 뒤 기존 빈 잔을 없애고 그 자리에 맥주를 쌓습니다.
        RemoveDisplayedEmptyCup();

        dishCount--;
        stack.PickUpItem(beer);
        workingActor = null;
        Managers.Task.AssignWaitingTasks();
        Debug.Log($"직원 {employee.name}이 맥주를 생산했습니다. 남은 잔 재고: {dishCount}");
        return true;
    }

    /// <summary>작업을 마치거나 취소한 직원이 점유한 기계가 있으면 점유를 해제합니다.</summary>
    public static void StopWorkForEmployee(EmployeeController employee)
    {
        if (employee == null)
            return;

        BeerMachine[] machines = FindObjectsByType<BeerMachine>(FindObjectsSortMode.None);
        foreach (BeerMachine machine in machines)
        {
            if (machine != null)
            {
                if (machine.workingActor == employee.gameObject)
                    machine.workingActor = null;
            }
        }
    }

    /// <summary>빈손이나 맥주를 들고 맥주를 만들 때 적용할 기계의 기본 시간입니다.</summary>
    public override float Duration => BaseProductionDuration;

    /// <summary>모든 맥주 기계와 이후 생성될 기계에 저장된 업그레이드를 반영합니다.</summary>
    public static void ApplyGrowthToAll(float capacityBonus)
    {
        sharedCapacityBonus = Mathf.Max(0, Mathf.RoundToInt(capacityBonus));
        BeerMachine[] machines = FindObjectsByType<BeerMachine>(FindObjectsSortMode.None);
        foreach (BeerMachine machine in machines)
            machine.ApplyGrowth();
    }

    /// <summary>공용 보관량 증가를 현재 기계의 최대 잔 수에 더합니다.</summary>
    private void ApplyGrowth()
    {
        if (baseMaxDishCount <= 0)
            baseMaxDishCount = maxDishCount;
        maxDishCount = Mathf.Max(1, baseMaxDishCount + sharedCapacityBonus);
    }

    private void OnEnable()
    {
        if (Managers.Growth != null)
            ApplyGrowthToAll(Managers.Growth.GetTotalValue("machine_capacity"));

        // 씬 시작 시에도 잔 재고가 0이라면 곧바로 보충 알림을 띄웁니다.
        RefreshDishAlert();
    }

    /// <summary>깨끗한 잔을 기계에 넣는 동작은 즉시 끝냅니다.</summary>
    protected override float GetDuration(GameObject actor)
    {
        if (GetHeldItemType(actor) == Define.ItemType.Beer_Empty)
            return 0f;

        PlayerController player = actor != null ? actor.GetComponent<PlayerController>() : null;
        float playerScale = player != null ? player.InteractionTimeScale : 1f;
        return Duration * playerScale;
    }

    /// <summary>플레이어가 기계를 먼저 사용하면 현재 기계를 맡은 직원 작업을 대기 상태로 돌립니다.</summary>
    protected override void OnPlayerStartingInteraction(GameObject actor)
    {
        // 플레이어가 먼저 시작하면 이 기계를 사용 중이던 직원 작업만 취소합니다.
        EmployeeController employee = workingActor != null
            ? workingActor.GetComponent<EmployeeController>()
            : null;
        if (employee != null)
        {
            Managers.Task?.TakeOverTask(employee);
            StopWorkForEmployee(employee);
        }
    }

    /// <summary>플레이어가 상호작용을 시작하면 기계를 점유해 직원이 새 작업을 받지 못하게 합니다.</summary>
    public override void OnStartInteract(GameObject actor)
    {
        workingActor = actor;

        // 깨끗한 잔을 기계에 넣는 동작은 생산이 아니므로, 맥주를 만들 때만 잔을 출구로 옮깁니다.
        if (GetHeldItemType(actor) != Define.ItemType.Beer_Empty && dishCount > 0)
            MoveDisplayedEmptyCupToSpout();
    }

    /// <summary>직원이 실제 생산 작업을 시작할 때 빈 잔을 출구로 옮깁니다.</summary>
    public void StartBeerProductionForEmployee()
    {
        MoveDisplayedEmptyCupToSpout();
    }

    /// <summary>플레이어가 생산을 마치거나 취소하면 점유를 풀고 대기 직원을 다시 배정합니다.</summary>
    protected override void OnInteractionEnded()
    {
        if (workingActor != null && workingActor.GetComponent<PlayerController>() != null)
        {
            workingActor = null;
            Managers.Task?.AssignWaitingTasks();
        }

        // 생산 완료로 마지막 잔을 썼다면 진행 말풍선 대신 보충 알림을 보여줍니다.
        RefreshDishAlert();
    }

    /// <summary>직원의 진행 표시가 닫힌 뒤에도 잔이 없으면 보충 알림을 다시 보여줍니다.</summary>
    public override void HideTaskProgress()
    {
        base.HideTaskProgress();
        RefreshDishAlert();
    }

    /// <summary>현재 잔 재고를 확인해 맥주 기계의 보충 말풍선을 갱신합니다.</summary>
    private void RefreshDishAlert()
    {
        if (!isActiveAndEnabled || uiBubble == null)
            return;

        // 재고가 없을 때만 알림을 켜고, 잔을 넣으면 이전 알림을 닫습니다.
        if (dishCount <= 0)
            uiBubble.SetBubble(Define.BubbleType.BeerMissing);
        else
            uiBubble.Hide();
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

            // 잔은 맥주 생성 위치와 분리된 표시 지점으로 옮깁니다.
            Transform displayPoint = cupDisplayPoint != null ? cupDisplayPoint : transform;
            player.Stacking.PopItem(displayPoint);
            dishCount++;
            actorJustLoadedDish = actor;
            RefreshDishAlert();
            // 직원이 잔을 기다리는 서빙 작업을 갖고 있으면 재고가 늘어난 즉시 다시 배정합니다.
            Managers.Task.AssignWaitingTasks();
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

        // 새 맥주 생성에 성공한 뒤 생산 작업이 끝난 빈 잔을 제거합니다.
        RemoveDisplayedEmptyCup();

        player.Stacking.PickUpItem(beer);
        dishCount--;
        Managers.Task.AssignWaitingTasks();
        Debug.Log($"맥주 생산 완료! 남은 잔 재고: {dishCount}");
    }

    /// <summary>잔을 넣은 플레이어가 범위를 나가면 다음 방문부터 맥주를 만들 수 있게 합니다.</summary>
    protected override void OnActorLeftRange(GameObject actor)
    {
        if (actorJustLoadedDish == actor)
            actorJustLoadedDish = null;
    }

    /// <summary>표시 지점에 있는 빈 잔을 찾아 생산 출구까지 옮깁니다.</summary>
    private void MoveDisplayedEmptyCupToSpout()
    {
        if (cupDisplayPoint == null || spawnPoint == null || cupDisplayPoint.childCount == 0)
            return;

        // 이전 이동 연출이 남아 있으면 중복 이동하지 않도록 먼저 정리합니다.
        cupMoveTween?.Kill();

        Transform cup = cupDisplayPoint.GetChild(0);
        if (cup == null)
            return;

        // 잔을 기계의 자식으로 유지해 기계 위치가 바뀌어도 출구를 따라가게 합니다.
        cup.SetParent(transform, true);
        cupMoveTween = cup.DOMove(spawnPoint.position, 0.35f)
            .SetEase(Ease.OutQuad);
    }

    /// <summary>표시 지점이나 출구에 놓인 빈 잔을 제거합니다.</summary>
    private void RemoveDisplayedEmptyCup()
    {
        cupMoveTween?.Kill();
        cupMoveTween = null;

        Transform cup = null;
        if (spawnPoint != null)
        {
            // 출구 위치로 옮긴 잔은 기계의 자식이므로 가까운 빈 잔을 찾습니다.
            foreach (Transform child in transform)
            {
                ItemController item = child.GetComponent<ItemController>();
                if (item != null && item.type == Define.ItemType.Beer_Empty)
                {
                    cup = child;
                    break;
                }
            }
        }

        // 아직 출구로 옮기지 않은 상태라면 기존 표시 지점에서 찾아 제거합니다.
        if (cup == null && cupDisplayPoint != null && cupDisplayPoint.childCount > 0)
            cup = cupDisplayPoint.GetChild(0);

        if (cup != null)
            Managers.Resource.Destroy(cup.gameObject);
    }

    /// <summary>기계가 꺼질 때 진행 중인 작업자를 놓아 다음 활성화 때 다시 쓸 수 있게 합니다.</summary>
    protected override void OnDisable()
    {
        cupMoveTween?.Kill();
        cupMoveTween = null;
        base.OnDisable();
        workingActor = null;
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
