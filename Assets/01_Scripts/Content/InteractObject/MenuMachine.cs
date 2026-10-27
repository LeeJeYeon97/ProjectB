using UnityEngine;

/// <summary>연결한 레시피를 읽고 메뉴 완성품을 만드는 기계입니다.</summary>
public class MenuMachine : InteractableBase
{
    [Header("메뉴 머신 타입")]
    [SerializeField] private Define.FoodCookingMachineType type;

    [Header("제조 레시피")]
    [SerializeField] private FoodRecipeData recipe;
    /// <summary>이 기계가 사용하는 레시피를 외부에서 확인합니다.</summary>
    public FoodRecipeData Recipe => recipe;
    /// <summary>기계가 실제로 켜져 있고 레시피와 완성품 설정이 올바른지 확인합니다.</summary>
    public bool CanOfferRecipe => isActiveAndEnabled && IsRecipeReady(recipe);
    /// <summary>현재 레시피에서 설정한 제조 시간을 반환합니다.</summary>
    public override float Duration => GetProductionDuration();

    [Header("메뉴 생성 위치")]
    [SerializeField] private Transform spawnPoint;

    [Header("제조 시간")]
    [Tooltip("이 기계에서 메뉴 하나를 만드는 데 필요한 최소 시간입니다.")]
    [SerializeField, Min(0.1f)] private float minimumProductionDuration = 0.1f;
    /// <summary>기계별로 설정한 최소 제조 시간을 반환합니다.</summary>
    public float MinimumProductionDuration => minimumProductionDuration;

    private GameObject machineWorkingActor;
    /// <summary>현재 메뉴 제조 작업을 맡은 플레이어나 직원을 반환합니다.</summary>
    public GameObject MachineWorkingActor => machineWorkingActor;

    /// <summary>기계가 비활성화되면 현재 작업자를 정리합니다.</summary>
    protected override void OnDisable()
    {
        ClearMachineWork();
        base.OnDisable();
    }

    /// <summary>레시피 설정과 플레이어가 같은 메뉴를 더 들 수 있는지 확인합니다.</summary>
    public override bool CanInteract(GameObject actor)
    {
        if (!TryGetPlayer(actor, out PlayerController player))
            return false;

        if (player.Stacking == null)
            return false;

        if (!IsRecipeReady(recipe))
            return false;

        // 손님 주문이 예약한 수량을 제외하고 쓸 수 있는 재고가 있는지 확인합니다.
        bool hasUnreservedIngredients = Managers.Ingredient.HasIngredients(recipe.Ingredients);
        bool hasReservedOrder = Managers.Ingredient.HasReservedOrder(recipe);
        if (!hasUnreservedIngredients && !hasReservedOrder)
            return false;

        return player.Stacking.CanHoldMenu(recipe);
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

        Managers.Ingredient.TryGetReservedCustomer(recipe, out CustomerController reservedCustomer);
        TryProduceMenu(actor, player.Stacking, Define.ItemType.None, reservedCustomer);
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

    /// <summary>완성품이 메뉴 분류이고 이 기계의 레시피 연결이 올바른지 확인합니다.</summary>
    public bool CanProduceMenu(Define.ItemType itemType)
    {
        if (itemType != Define.ItemType.Menu)
            return false;

        return IsRecipeReady(recipe);
    }

    /// <summary>기계 종류, 레시피 재료, 완성품 프리팹의 연결이 올바른지 확인합니다.</summary>
    private bool IsRecipeReady(FoodRecipeData recipe)
    {
        if (recipe == null)
            return false;

        if (type == Define.FoodCookingMachineType.None)
            return false;

        if (recipe.CookingMachineType != type)
            return false;

        if (recipe.MenuPrefab == null)
            return false;

        ItemController menuItem = recipe.MenuPrefab.GetComponent<ItemController>();
        if (menuItem == null)
            return false;

        if (menuItem.type != Define.ItemType.Menu)
            return false;

        if (menuItem.MenuRecipe != recipe)
            return false;

        // 요구 재료 목록은 레시피가 소유하며, 실제 수량은 제조 시작과 완료 시 재고 매니저가 확인합니다.
        if (recipe.Ingredients == null)
            return false;

        foreach (FoodIngredient ingredient in recipe.Ingredients)
        {
            if (ingredient == null)
                return false;

            if (ingredient.Type == Define.FoodIngredientType.None)
                return false;

            if (ingredient.Amount <= 0)
                return false;
        }

        return true;
    }

    /// <summary>직원이 사용할 수 있는 메뉴 기계인지 확인하고 작업자를 기록합니다.</summary>
    public bool TryStartMachineWorkForEmployee(EmployeeController employee, CustomerController customer)
    {
        if (employee == null)
            return false;

        if (machineWorkingActor != null)
            return false;

        if (!isActiveAndEnabled)
            return false;

        if (!IsRecipeReady(recipe))
            return false;

        if (customer == null || customer.OrderedRecipe != recipe)
            return false;

        if (!Managers.Ingredient.HasReservation(customer, recipe.Ingredients))
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
        CustomerController customer,
        out MenuMachine machine)
    {
        machine = null;
        if (employee == null)
            return false;

        MenuMachine[] machines = FindObjectsByType<MenuMachine>(FindObjectsSortMode.None);
        for (int i = 0; i < machines.Length; i++)
        {
            if (!machines[i].TryStartMachineWorkForEmployee(employee, customer))
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
    public bool TryProduceMenu(
        GameObject actor,
        StackingSystem stack,
        Define.ItemType requestedType,
        CustomerController reservedCustomer = null)
    {
        if (actor == null)
            return false;

        if (stack == null)
            return false;

        if (machineWorkingActor != actor)
            return false;

        if (!isActiveAndEnabled)
            return false;

        if (!IsRecipeReady(recipe))
            return false;

        if (reservedCustomer == null)
        {
            if (!Managers.Ingredient.HasIngredients(recipe.Ingredients))
                return false;
        }
        else
        {
            if (reservedCustomer.OrderedRecipe != recipe)
                return false;

            if (!Managers.Ingredient.HasReservation(reservedCustomer, recipe.Ingredients))
                return false;
        }

        if (requestedType != Define.ItemType.None && requestedType != Define.ItemType.Menu)
            return false;

        if (!stack.CanHoldMenu(recipe))
            return false;

        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;
        GameObject menu = Managers.Resource.Instantiate(recipe.MenuPrefab, spawnPosition, spawnRotation);
        if (menu == null)
            return false;

        // 완성품을 스택에 넣기 전에 재료 전부를 확인하고 차감해 재고 부족 시 생성물을 정리합니다.
        bool ingredientsUsed = false;
        if (reservedCustomer == null)
            ingredientsUsed = Managers.Ingredient.TryUseIngredients(recipe.Ingredients);
        else
            ingredientsUsed = Managers.Ingredient.TryUseReservedIngredients(reservedCustomer, recipe.Ingredients);

        if (!ingredientsUsed)
        {
            Managers.Resource.Destroy(menu);
            return false;
        }

        // 재료 차감에 성공한 뒤에만 완성품을 작업자의 적재 목록에 전달합니다.
        stack.PickUpItem(menu);

        // 플레이어가 손님 주문용 메뉴를 만들면 해당 직원 작업은 중복되지 않도록 제거합니다.
        if (reservedCustomer != null && actor.GetComponent<PlayerController>() != null)
            Managers.Task?.RemoveServingTaskFor(reservedCustomer);

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

    /// <summary>기계가 꺼질 때 작업 예약 정보를 비웁니다.</summary>
    private void ClearMachineWork()
    {
        machineWorkingActor = null;
    }
}
