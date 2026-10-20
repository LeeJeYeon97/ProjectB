using UnityEngine;

/// <summary>단일 레시피 제조와 메뉴 생성처럼 메뉴 제조 기계가 공통으로 사용하는 기능입니다.</summary>
public abstract class MenuMachine : InteractableBase
{
    [Header("제조 레시피")]
    [SerializeField] private FoodRecipeData recipe;

    [Header("메뉴 생성 위치")]
    [SerializeField] private Transform spawnPoint;

    private GameObject machineWorkingActor;

    /// <summary>기계 종류별로 설정한 최소 제조 시간을 반환합니다.</summary>
    public virtual float MinimumProductionDuration => 0.1f;

    /// <summary>현재 레시피의 제조 시간을 반환합니다.</summary>
    public override float Duration => GetProductionDuration();

    /// <summary>이 기계가 사용하는 단일 레시피를 외부에서 확인할 수 있게 합니다.</summary>
    public FoodRecipeData Recipe => recipe;

    /// <summary>현재 메뉴 제조 작업을 맡은 플레이어나 직원을 반환합니다.</summary>
    public GameObject MachineWorkingActor => machineWorkingActor;

    /// <summary>특정 제조 기계에 별도 재고 조건이 있을 때 작업 가능 여부를 반환합니다.</summary>
    protected virtual bool HasProductionStock => true;

    /// <summary>메뉴 생성이 성공한 뒤 기계별 재고를 반영할 수 있게 합니다.</summary>
    protected virtual void OnMenuProduced() { }

    /// <summary>설정된 단일 레시피와 플레이어 스택의 제조 가능 여부를 확인합니다.</summary>
    public override bool CanInteract(GameObject actor)
    {
        if (!TryGetPlayer(actor, out PlayerController player))
            return false;

        if (player.Stacking == null)
            return false;

        if (!HasProductionStock)
            return false;

        Define.ItemType heldItemType = player.Stacking.GetFirstListItemType();
        if (heldItemType != Define.ItemType.None)
        {
            if (!CanProduceMenu(heldItemType))
                return false;

            return player.Stacking.CheckListItem(heldItemType);
        }

        if (!IsRecipeReady(recipe))
            return false;

        ItemController menuItem = recipe.MenuPrefab.GetComponent<ItemController>();
        if (menuItem == null)
            return false;

        return player.Stacking.CheckListItem(menuItem.type);
    }

    /// <summary>플레이어가 기계 사용을 시작하면 이 기계의 레시피 작업을 맡습니다.</summary>
    public override void OnStartInteract(GameObject actor)
    {
        machineWorkingActor = actor;
    }

    /// <summary>제조가 끝난 플레이어에게 메뉴 프리팹을 생성해 스택으로 전달합니다.</summary>
    public override void OnCompleteInteract(GameObject actor)
    {
        if (!TryGetPlayer(actor, out PlayerController player))
            return;

        if (player.Stacking == null)
            return;

        TryProduceMenu(actor, player.Stacking, Define.ItemType.None);
    }

    /// <summary>플레이어 작업이 끝나면 기계 점유를 정리하고 직원 대기 작업을 다시 배정합니다.</summary>
    protected override void OnInteractionEnded()
    {
        if (machineWorkingActor != null)
        {
            PlayerController player = machineWorkingActor.GetComponent<PlayerController>();
            if (player != null)
            {
                machineWorkingActor = null;
                Managers.Task?.AssignWaitingTasks();
            }
        }

    }

    /// <summary>플레이어가 직원보다 먼저 기계를 사용하면 직원의 기계 작업을 해제합니다.</summary>
    protected override void OnPlayerStartingInteraction(GameObject actor)
    {
        if (machineWorkingActor == null)
            return;

        EmployeeController employee = machineWorkingActor.GetComponent<EmployeeController>();
        if (employee == null)
            return;

        Managers.Task?.TakeOverTask(employee);
        StopMachineWorkForEmployee(employee);
    }

    /// <summary>메뉴 레시피 시간을 적용하고 플레이어의 상호작용 속도와 기계 최소 시간을 반영합니다.</summary>
    protected override float GetDuration(GameObject actor)
    {
        float scaledDuration = base.GetDuration(actor);
        return Mathf.Max(MinimumProductionDuration, scaledDuration);
    }

    /// <summary>이 기계에 설정된 음식 데이터의 제조 시간을 작업 시간으로 사용합니다.</summary>
    protected float GetProductionDuration()
    {
        float baseDuration = recipe != null ? recipe.CookingDuration : 0f;
        return Mathf.Max(MinimumProductionDuration, baseDuration);
    }

    /// <summary>아이템 프리팹의 종류와 일치하는 제조 레시피가 있는지 확인합니다.</summary>
    public bool CanProduceMenu(Define.ItemType itemType)
    {
        if (itemType == Define.ItemType.None || !IsRecipeReady(recipe))
            return false;

        ItemController menuItem = recipe.MenuPrefab.GetComponent<ItemController>();
        return menuItem != null && menuItem.type == itemType;
    }

    /// <summary>레시피 에셋과 결과 프리팹, 프리팹의 아이템 설정이 모두 있는지 확인합니다.</summary>
    private static bool IsRecipeReady(FoodRecipeData recipe)
    {
        if (recipe == null)
            return false;

        if (recipe.MenuPrefab == null)
            return false;

        ItemController menuItem = recipe.MenuPrefab.GetComponent<ItemController>();
        if (menuItem == null)
            return false;

        return menuItem.type != Define.ItemType.None;
    }

    /// <summary>직원에게 제조 재고가 있고 작업할 수 있는 메뉴 기계를 배정합니다.</summary>
    public bool TryStartMachineWorkForEmployee(EmployeeController employee, Define.ItemType menuType)
    {
        if (employee == null)
            return false;

        if (machineWorkingActor != null)
            return false;

        if (!HasProductionStock)
            return false;

        if (!isActiveAndEnabled)
            return false;

        if (!IsRecipeReady(recipe))
            return false;

        if (!CanProduceMenu(menuType))
            return false;

        machineWorkingActor = employee.gameObject;
        return true;
    }

    /// <summary>지정한 직원이 이 기계의 점유자일 때 제조 예약을 풀어줍니다.</summary>
    public void StopMachineWorkForEmployee(EmployeeController employee)
    {
        if (employee == null)
            return;

        if (machineWorkingActor != employee.gameObject)
            return;

        machineWorkingActor = null;
    }

    /// <summary>현재 씬에서 비어 있는 메뉴 제조 기계를 찾아 직원에게 배정합니다.</summary>
    public static bool TryStartMachineWorkForEmployee(
        EmployeeController employee,
        Define.ItemType menuType,
        out MenuMachine machine)
    {
        machine = null;
        if (employee == null)
            return false;

        MenuMachine[] machines = FindObjectsByType<MenuMachine>(FindObjectsSortMode.None);
        for (int i = 0; i < machines.Length; i++)
        {
            if (!machines[i].TryStartMachineWorkForEmployee(employee, menuType))
                continue;

            machine = machines[i];
            return true;
        }

        return false;
    }

    /// <summary>직원이 업무를 끝내거나 취소할 때 씬 안의 메뉴 제조 예약을 모두 풉니다.</summary>
    public static void StopEmployeeMachineWork(EmployeeController employee)
    {
        if (employee == null)
            return;

        MenuMachine[] machines = FindObjectsByType<MenuMachine>(FindObjectsSortMode.None);
        for (int i = 0; i < machines.Length; i++)
            machines[i].StopMachineWorkForEmployee(employee);
    }

    /// <summary>레시피 메뉴를 생성해 작업자의 스택에 전달합니다.</summary>
    public bool TryProduceMenu(GameObject actor, StackingSystem stack, Define.ItemType requestedType)
    {
        if (actor == null)
            return false;

        if (stack == null)
            return false;

        if (machineWorkingActor != actor)
            return false;

        if (!isActiveAndEnabled)
            return false;

        if (!HasProductionStock)
            return false;

        if (!IsRecipeReady(recipe))
            return false;

        ItemController recipeItem = recipe.MenuPrefab.GetComponent<ItemController>();
        Define.ItemType heldItemType = stack.GetFirstListItemType();
        if (heldItemType != Define.ItemType.None && heldItemType != recipeItem.type)
            return false;

        if (requestedType != Define.ItemType.None && requestedType != recipeItem.type)
            return false;

        if (!stack.CheckListItem(recipeItem.type))
            return false;

        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;
        GameObject menu = Managers.Resource.Instantiate(recipe.MenuPrefab, spawnPosition, spawnRotation);
        if (menu == null)
            return false;

        stack.PickUpItem(menu);
        OnMenuProduced();

        if (actor.GetComponent<PlayerController>() == null)
        {
            machineWorkingActor = null;
        }

        Managers.Task?.AssignWaitingTasks();

        ItemController producedItem = menu.GetComponent<ItemController>();
        if (producedItem != null)
        {
            if (Managers.Quest != null)
                Managers.Quest.ReportItemProgress(Define.QuestObjectiveType.Produce, producedItem.type);
        }

        return true;
    }

    /// <summary>비활성화될 때 현재 기계 점유 정보와 잔 전달 대기 시간을 정리합니다.</summary>
    protected override void OnDisable()
    {
        ClearMachineWork();
        base.OnDisable();
    }

    /// <summary>기계가 꺼질 때 작업 예약 정보를 비웁니다.</summary>
    private void ClearMachineWork()
    {
        machineWorkingActor = null;
    }
}
