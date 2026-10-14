using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 상호작용 오브젝트의 공통 동작입니다. 오브젝트의 Trigger Collider가 플레이어를
/// 감지하면 하위 클래스가 제공하는 규칙에 따라 자동으로 상호작용합니다.
/// </summary>
public abstract class InteractableBase : MonoBehaviour
{
    [Header("상호작용 종류")]
    [SerializeField] private Define.InteractionType interactionType;
    [SerializeField] private Define.MenuType availableMenus;

    [Header("제조와 기계 업그레이드")]
    [SerializeField] private string machineId;
    [SerializeField] private string[] upgradeIds = Array.Empty<string>();
    [SerializeField] private GameObject beerPrefab;
    [SerializeField] private GameObject testMealPrefab;
    [SerializeField, Range(0f, 1f)] private float testMealChance = 0.35f;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform cupDisplayPoint;
    [SerializeField, Min(0.25f)] private float productionDuration = 2f;
    [SerializeField, Min(0)] private int dishCount = 1;
    [SerializeField, Min(1)] private int maxDishCount = 10;

    private GameObject currentActor;
    private GameObject candidateActor;
    // 플레이어나 직원이 이 대상을 먼저 사용 중인지 기억합니다.
    private GameObject reservedActor;
    private readonly HashSet<Collider> playerCollidersInside = new HashSet<Collider>();
    private float timer;
    private bool actorInRange;
    private bool progressBubbleActive;
    private bool taskProgressBubbleActive;
    private int baseMaxDishCount;
    private GameObject machineWorkingActor;
    private Tween cupMoveTween;

    [Header("공용 상호작용 UI")]
    protected UI_Bubble uiBubble;
    [Tooltip("말풍선이 따라갈 위치입니다. 프리팹 안에 BubbleAnchor 자식 오브젝트를 만들어 연결합니다.")]
    [SerializeField] private Transform bubbleAnchor;

    /// <summary>프리팹에서 종류를 지정하지 않았을 때 하위 객체가 제공할 기본 종류입니다.</summary>
    protected virtual Define.InteractionType DefaultInteractionType => Define.InteractionType.None;

    /// <summary>프리팹에서 메뉴를 지정하지 않았을 때 하위 제조 기계가 제공할 기본 메뉴입니다.</summary>
    protected virtual Define.MenuType DefaultMenus => Define.MenuType.None;

    /// <summary>현재 객체가 수행하는 상호작용의 종류입니다.</summary>
    public Define.InteractionType InteractionType => interactionType != Define.InteractionType.None
        ? interactionType : DefaultInteractionType;

    /// <summary>제조 기계가 만들 수 있는 메뉴 종류입니다.</summary>
    public Define.MenuType AvailableMenus => availableMenus != Define.MenuType.None ? availableMenus : DefaultMenus;

    /// <summary>업그레이드 저장에 사용하는 이 오브젝트의 고유 ID입니다.</summary>
    public string MachineId => machineId;

    /// <summary>이 오브젝트가 제공하는 업그레이드 ID 목록입니다.</summary>
    public IReadOnlyList<string> UpgradeIds => upgradeIds;

    /// <summary>직원 작업과 플레이어 작업에서 공통으로 사용하는 제조 최소 시간입니다.</summary>
    public virtual float MinimumProductionDuration => 0.5f;

    /// <summary>현재 이 제조 기계를 점유한 직원 또는 플레이어입니다.</summary>
    public GameObject MachineWorkingActor => machineWorkingActor;

    /// <summary>아이템 종류가 이 기계의 제조 메뉴 목록에 포함되는지 확인합니다.</summary>
    public bool CanProduceMenu(Define.ItemType item)
    {
        Define.MenuType menu = item == Define.ItemType.Beer ? Define.MenuType.Beer
            : item == Define.ItemType.TestMeal ? Define.MenuType.TestMeal : Define.MenuType.None;
        return InteractionType == Define.InteractionType.ProduceMenu && menu != Define.MenuType.None &&
            (AvailableMenus & menu) == menu;
    }

    /// <summary>빈손인 플레이어의 스택 검사에 사용할 첫 제조 메뉴를 고릅니다.</summary>
    public Define.ItemType GetFirstMenuItem()
    {
        if (CanProduceMenu(Define.ItemType.Beer))
            return Define.ItemType.Beer;
        if (CanProduceMenu(Define.ItemType.TestMeal))
            return Define.ItemType.TestMeal;
        return Define.ItemType.None;
    }

    /// <summary>허용된 메뉴에서 임시 메뉴 확률을 반영해 실제 제조 메뉴를 고릅니다.</summary>
    public Define.ItemType ChooseMenu(float testMealChance)
    {
        bool canMakeBeer = CanProduceMenu(Define.ItemType.Beer);
        bool canMakeTestMeal = CanProduceMenu(Define.ItemType.TestMeal);
        if (canMakeBeer && canMakeTestMeal)
            return Random.value < testMealChance ? Define.ItemType.TestMeal : Define.ItemType.Beer;
        return GetFirstMenuItem();
    }

    /// <summary>현재 객체에 연결된 공용 말풍선에 상태를 표시합니다.</summary>
    public void ShowStatusBubble(Define.BubbleType type)
    {
        if (EnsureProgressBubble())
            uiBubble.SetBubble(type);
    }

    /// <summary>현재 객체에 표시 중인 상태 말풍선을 닫습니다.</summary>
    public void HideStatusBubble()
    {
        if (uiBubble != null)
            uiBubble.Hide();
    }

    /// <summary>리소스 로드가 끝난 뒤 각 객체의 현재 상태 표시를 다시 계산합니다.</summary>
    public virtual void RefreshStatusBubble()
    {
        if (InteractionType == Define.InteractionType.ProduceMenu)
            RefreshDishAlert();
    }

    /// <summary>진행 말풍선이 필요 없는 상호작용은 하위 클래스에서 끌 수 있습니다.</summary>
    protected virtual bool UsesProgressBubble => true;

    public abstract float Duration { get; }

    /// <summary>플레이어나 직원이 이 대상을 맡아 작업 중인지 알려줍니다.</summary>
    public bool IsWorking => reservedActor != null;

    /// <summary>현재 대상을 맡은 작업 주체를 확인합니다.</summary>
    public GameObject WorkingActor => reservedActor;

    /// <summary>대상이 비어 있거나 이미 같은 주체가 맡았을 때만 선점권을 줍니다.</summary>
    public bool TryReserve(GameObject actor)
    {
        if (actor == null || (reservedActor != null && reservedActor != actor))
            return false;

        reservedActor = actor;
        return true;
    }

    /// <summary>선점한 주체만 대상의 잠금을 해제할 수 있게 합니다.</summary>
    public void ReleaseReservation(GameObject actor)
    {
        if (actor != null && reservedActor == actor)
            reservedActor = null;
    }

    /// <summary>캐릭터와 현재 상태에 따라 상호작용 시간을 다르게 정합니다.</summary>
    protected virtual float GetDuration(GameObject actor)
    {
        // 플레이어 업그레이드만 적용하고 직원의 자동 작업 시간은 직원 설정을 따릅니다.
        PlayerController player = actor != null ? actor.GetComponent<PlayerController>() : null;
        float scale = player != null ? player.InteractionTimeScale : 1f;
        float scaledDuration = Duration * scale;
        return InteractionType == Define.InteractionType.ProduceMenu
            ? Mathf.Max(MinimumProductionDuration, scaledDuration)
            : scaledDuration;
    }

    /// <summary>기본 구현은 메뉴 제조 객체의 재고와 플레이어 손을 확인합니다.</summary>
    public virtual bool CanInteract(GameObject actor)
    {
        if (InteractionType != Define.InteractionType.ProduceMenu ||
            !TryGetPlayer(actor, out PlayerController player) || player.Stacking == null || dishCount <= 0)
            return false;

        Define.ItemType held = player.Stacking.GetFirstListItemType();
        if (held == Define.ItemType.None)
            return player.Stacking.CheckListItem(GetFirstMenuItem());

        return CanProduceMenu(held) && player.Stacking.CheckListItem(held);
    }

    /// <summary>제조 상호작용이 시작되면 기계를 점유하고 진열된 잔 연출을 시작합니다.</summary>
    public virtual void OnStartInteract(GameObject actor)
    {
        if (InteractionType != Define.InteractionType.ProduceMenu)
            return;

        machineWorkingActor = actor;
        MoveDisplayedEmptyCupToSpout();
    }

    public virtual void OnUpdateInteract(GameObject actor, float progress) { }

    /// <summary>제조 상호작용이 완료되면 선택된 메뉴를 플레이어 손에 제공합니다.</summary>
    public virtual void OnCompleteInteract(GameObject actor)
    {
        if (InteractionType != Define.InteractionType.ProduceMenu ||
            !TryGetPlayer(actor, out PlayerController player) || player.Stacking == null)
            return;

        TryProduceMenu(actor, player.Stacking, Define.ItemType.None);
    }

    public virtual void OnCancelInteract(GameObject actor) { }

    /// <summary>제조 작업이 끝나면 점유를 풀고 잔 부족 안내와 대기 작업을 갱신합니다.</summary>
    protected virtual void OnInteractionEnded()
    {
        if (InteractionType != Define.InteractionType.ProduceMenu)
            return;

        if (machineWorkingActor != null && machineWorkingActor.GetComponent<PlayerController>() != null)
        {
            machineWorkingActor = null;
            Managers.Task?.AssignWaitingTasks();
        }
        RefreshDishAlert();
    }

    /// <summary>캐릭터가 상호작용 범위를 완전히 벗어났을 때 하위 동작을 정리합니다.</summary>
    protected virtual void OnActorLeftRange(GameObject actor) { }

    /// <summary>플레이어가 직접 작업을 시작할 때 하위 대상이 직원 예약을 정리하도록 알립니다.</summary>
    /// <summary>플레이어가 제조를 시작하면 이 기계에서 작업하던 직원을 먼저 양도시킵니다.</summary>
    protected virtual void OnPlayerStartingInteraction(GameObject actor)
    {
        if (InteractionType != Define.InteractionType.ProduceMenu || machineWorkingActor == null)
            return;

        EmployeeController employee = machineWorkingActor.GetComponent<EmployeeController>();
        if (employee == null)
            return;

        Managers.Task?.TakeOverTask(employee);
        StopMachineWorkForEmployee(employee);
    }

    protected virtual void Awake()
    {
        Collider interactionCollider = GetComponent<Collider>();
        if (interactionCollider == null || !interactionCollider.isTrigger)
        {
            Debug.LogError($"{name}: 상호작용 감지를 위해 Trigger Collider가 필요합니다.", this);
        }

        // 기존 프리팹의 직접 연결은 사용하지 않고 필요할 때 공용 풀에서 받습니다.
        uiBubble = null;
        progressBubbleActive = false;
    }

    /// <summary>업그레이드가 설정된 객체의 저장 정보를 등록하고 현재 효과를 적용합니다.</summary>
    protected virtual void OnEnable()
    {
        if (UpgradeIds.Count > 0 && !string.IsNullOrEmpty(machineId) && Managers.Growth != null)
        {
            Managers.Growth.RegisterMachine(this);
            ApplyGrowth();
        }

        RefreshStatusBubble();
    }

    /// <summary>잔 부족과 제조 진행도를 표시하는 공용 말풍선을 통해 재고 상태를 알립니다.</summary>
    public void RefreshDishAlert()
    {
        if (InteractionType != Define.InteractionType.ProduceMenu || !isActiveAndEnabled)
            return;

        if (dishCount <= 0)
            ShowStatusBubble(Define.BubbleType.BeerMissing);
        else
            HideStatusBubble();
    }

    /// <summary>현재 기계의 업그레이드 단계에 맞춰 잔 보관 한도를 계산합니다.</summary>
    public virtual void ApplyGrowth()
    {
        if (InteractionType != Define.InteractionType.ProduceMenu)
            return;

        if (baseMaxDishCount <= 0)
            baseMaxDishCount = maxDishCount;

        int bonus = Managers.Growth != null ? Managers.Growth.GetMachineCapacityBonus(machineId) : 0;
        maxDishCount = Mathf.Max(1, baseMaxDishCount + bonus);
        dishCount = Mathf.Clamp(dishCount, 0, maxDishCount);
    }

    /// <summary>현재 기계의 기본 시간에서 업그레이드 감소량을 적용합니다.</summary>
    protected float GetProductionDuration()
    {
        float reduction = Managers.Growth != null ? Managers.Growth.GetMachineTimeReduction(machineId) : 0f;
        return Mathf.Max(MinimumProductionDuration, productionDuration * (1f - reduction));
    }

    /// <summary>해금 앵커가 전달한 ID를 저장하고 해당 ID의 성장 단계를 적용합니다.</summary>
    public virtual void SetMachineId(string id)
    {
        if (string.IsNullOrEmpty(id) || machineId == id)
            return;

        machineId = id;
        if (UpgradeIds.Count > 0 && Managers.Growth != null)
        {
            Managers.Growth.RegisterMachine(this);
            ApplyGrowth();
        }
    }

    /// <summary>제조 재고와 작업자 상태를 확인해 직원에게 이 기계를 배정합니다.</summary>
    public bool TryStartMachineWorkForEmployee(EmployeeController employee)
    {
        if (InteractionType != Define.InteractionType.ProduceMenu || employee == null ||
            machineWorkingActor != null || dishCount <= 0 || !isActiveAndEnabled)
            return false;

        machineWorkingActor = employee.gameObject;
        return true;
    }

    /// <summary>기계에 배정된 직원의 작업 점유를 해제합니다.</summary>
    public void StopMachineWorkForEmployee(EmployeeController employee)
    {
        if (employee != null && machineWorkingActor == employee.gameObject)
            machineWorkingActor = null;
    }

    /// <summary>씬의 제조 객체 중 첫 번째로 사용 가능한 기계를 직원에게 배정합니다.</summary>
    public static bool TryStartMachineWorkForEmployee(EmployeeController employee, out InteractableBase machine)
    {
        machine = null;
        if (employee == null)
            return false;

        foreach (InteractableBase interaction in FindObjectsByType<InteractableBase>(FindObjectsSortMode.None))
        {
            if (interaction.TryStartMachineWorkForEmployee(employee))
            {
                machine = interaction;
                return true;
            }
        }
        return false;
    }

    /// <summary>직원이 업무를 끝내거나 취소될 때 현재 씬의 기계 점유를 모두 정리합니다.</summary>
    public static void StopEmployeeMachineWork(EmployeeController employee)
    {
        if (employee == null)
            return;

        foreach (InteractableBase interaction in FindObjectsByType<InteractableBase>(FindObjectsSortMode.None))
            interaction.StopMachineWorkForEmployee(employee);
    }

    /// <summary>직원이 시작한 제조의 잔 이동 연출을 플레이어 작업과 같이 보여줍니다.</summary>
    public void StartProductionForEmployee()
    {
        if (InteractionType == Define.InteractionType.ProduceMenu)
            MoveDisplayedEmptyCupToSpout();
    }

    /// <summary>제조를 완료할 수 있는 작업자가 요청한 메뉴를 만들어 손에 전달합니다.</summary>
    public bool TryProduceMenu(GameObject actor, StackingSystem stack, Define.ItemType requestedType)
    {
        if (InteractionType != Define.InteractionType.ProduceMenu || actor == null || stack == null ||
            machineWorkingActor != actor || !isActiveAndEnabled || dishCount <= 0)
            return false;

        Define.ItemType held = stack.GetFirstListItemType();
        Define.ItemType produced = held != Define.ItemType.None ? held : requestedType;
        if (produced == Define.ItemType.None)
            produced = ChooseMenu(testMealChance);
        if (!CanProduceMenu(produced) || !stack.CheckListItem(produced))
            return false;

        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;
        GameObject menu = produced == Define.ItemType.TestMeal
            ? (testMealPrefab != null ? Managers.Resource.Instantiate(testMealPrefab, position, rotation)
                : Managers.Resource.Instantiate("Item_TestMeal", position, rotation))
            : (beerPrefab != null ? Managers.Resource.Instantiate(beerPrefab, position, rotation)
                : Managers.Resource.Instantiate("Item_Beer", position, rotation));
        if (menu == null)
            return false;

        // 메뉴가 실제로 생성된 뒤에만 진열 잔과 재고를 줄여 양쪽 수량을 맞춥니다.
        RemoveDisplayedEmptyCup();
        dishCount--;
        stack.PickUpItem(menu);
        if (actor.GetComponent<PlayerController>() == null)
            machineWorkingActor = null;
        Managers.Task?.AssignWaitingTasks();

        ItemController item = menu.GetComponent<ItemController>();
        if (item != null && Managers.Quest != null)
            Managers.Quest.ReportItemProgress(Define.QuestObjectiveType.Produce, item.type);
        RefreshDishAlert();
        return true;
    }

    /// <summary>작업자가 가져온 깨끗한 빈 잔 하나를 기계의 잔 재고로 옮깁니다.</summary>
    public bool TryLoadCleanDish(GameObject actor)
    {
        if (InteractionType != Define.InteractionType.ProduceMenu ||
            !TryGetPlayer(actor, out PlayerController player) || player.Stacking == null ||
            dishCount >= maxDishCount || player.Stacking.GetFirstListItemType() != Define.ItemType.Beer_Empty)
            return false;

        Transform display = cupDisplayPoint != null ? cupDisplayPoint : transform;
        player.Stacking.PopItem(display);
        dishCount++;
        RefreshDishAlert();
        Managers.Task?.AssignWaitingTasks();
        return true;
    }

    /// <summary>기계가 꺼질 때 제조 애니메이션과 직원 점유를 정리합니다.</summary>
    protected void ClearMachineWork()
    {
        cupMoveTween?.Kill();
        cupMoveTween = null;
        machineWorkingActor = null;
    }

    /// <summary>프리팹에 업그레이드 기능이 설정된 경우 비어 있는 저장 ID를 만듭니다.</summary>
    protected virtual void OnValidate()
    {
        if (upgradeIds != null && upgradeIds.Length > 0 && string.IsNullOrEmpty(machineId))
            machineId = Guid.NewGuid().ToString("N");
    }

    /// <summary>진열된 빈 잔을 기계 출구로 옮겨 제조 중인 모습을 보여줍니다.</summary>
    private void MoveDisplayedEmptyCupToSpout()
    {
        if (cupDisplayPoint == null || spawnPoint == null || cupDisplayPoint.childCount == 0)
            return;

        cupMoveTween?.Kill();
        Transform cup = cupDisplayPoint.GetChild(0);
        cup.SetParent(transform, true);
        cupMoveTween = cup.DOMove(spawnPoint.position, 0.35f).SetEase(Ease.OutQuad);
    }

    /// <summary>제조에 사용한 빈 잔을 본체 또는 진열 위치에서 찾아 제거합니다.</summary>
    private void RemoveDisplayedEmptyCup()
    {
        cupMoveTween?.Kill();
        cupMoveTween = null;
        Transform cup = null;
        foreach (Transform child in transform)
        {
            ItemController item = child.GetComponent<ItemController>();
            if (item != null && item.type == Define.ItemType.Beer_Empty)
            {
                cup = child;
                break;
            }
        }

        if (cup == null && cupDisplayPoint != null && cupDisplayPoint.childCount > 0)
            cup = cupDisplayPoint.GetChild(0);
        if (cup != null)
            Managers.Resource.Destroy(cup.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        GameObject actor = GetActor(other);
        if (actor == null)
            return;

        // 플레이어의 자식 콜라이더가 여러 개일 수 있어, 들어온 콜라이더를 각각 기록합니다.
        playerCollidersInside.Add(other);
        actorInRange = true;
        candidateActor = actor;
        TryBeginInteraction(actor);
    }

    private void OnTriggerExit(Collider other)
    {
        GameObject actor = GetActor(other);
        if (actor == null)
            return;

        // 나간 콜라이더만 제거하고, 다른 플레이어 콜라이더가 트리거 안에 남았는지 확인합니다.
        playerCollidersInside.Remove(other);
        actorInRange = playerCollidersInside.Count > 0;
        if (!actorInRange)
        {
            candidateActor = null;
            OnActorLeftRange(actor);
            if (currentActor == actor)
                CancelInteraction();
        }
    }

    private void Update()
    {
        if (currentActor == null)
        {
            if (actorInRange)
            {
                if (candidateActor != null)
                    TryBeginInteraction(candidateActor);
            }
            return;
        }

        // 트리거 진입과 퇴장 이벤트로 관리하는 범위 상태를 확인합니다.
        if (!actorInRange)
        {
            CancelInteraction();
            return;
        }

        if (!CanInteract(currentActor))
        {
            CancelInteraction();
            return;
        }

        float interactionDuration = GetDuration(currentActor);
        if (interactionDuration <= 0f)
        {
            OnCompleteInteract(currentActor);
            CancelInteraction();
            return;
        }

        timer += Time.deltaTime;
        float progress = Mathf.Clamp01(timer / interactionDuration);
        ShowProgress(progress);
        OnUpdateInteract(currentActor, progress);
        if (timer >= interactionDuration)
        {
            OnCompleteInteract(currentActor);
            timer = 0f;
            if (!CanInteract(currentActor))
                CancelInteraction();
        }
    }

    private void TryBeginInteraction(GameObject actor)
    {
        if (actor == null || currentActor != null)
            return;

        // 먼저 현재 조건으로 플레이어가 할 수 있는 행동인지 확인해 불필요한 직원 중단을 막습니다.
        bool playerCanStart = CanInteract(actor);
        if (actor.GetComponent<PlayerController>() != null && playerCanStart)
        {
            Managers.Task?.TakeOverTask(this);
            OnPlayerStartingInteraction(actor);
        }

        if (!playerCanStart || !CanInteract(actor) || !TryReserve(actor))
            return;

        currentActor = actor;
        timer = 0f;
        OnStartInteract(currentActor);

        float interactionDuration = GetDuration(currentActor);
        if (interactionDuration <= 0f)
        {
            OnCompleteInteract(currentActor);
            CancelInteraction();
            return;
        }

        // 진행 시간이 있는 작업만 시작 시점부터 말풍선을 표시합니다.
        ShowProgress(0f);
    }

    private void CancelInteraction()
    {
        if (currentActor != null)
            OnCancelInteract(currentActor);

        // 상호작용이 끝나거나 취소되면 플레이어가 맡았던 선점권을 돌려줍니다.
        ReleaseReservation(currentActor);

        // 플레이어가 작업을 마치거나 취소한 뒤, 아직 남은 직원 작업을 다시 배정합니다.
        if (currentActor != null && currentActor.GetComponent<PlayerController>() != null)
            Managers.Task?.AssignWaitingTasks();

        if (UsesProgressBubble && uiBubble != null)
            uiBubble.Hide();
        progressBubbleActive = false;
        OnInteractionEnded();

        currentActor = null;
        timer = 0f;
    }

    private void ShowProgress(float progress)
    {
        if (!UsesProgressBubble)
            return;

        if (!EnsureProgressBubble())
            return;

        if (!progressBubbleActive)
        {
            uiBubble.gameObject.SetActive(true);
            uiBubble.SetBubble(Define.BubbleType.Progress);
            progressBubbleActive = true;
        }
        uiBubble.SetProgress(progress);
    }

    /// <summary>직원 작업의 진행도를 이 작업 대상의 Progress 말풍선에 표시합니다.</summary>
    public void ShowTaskProgress(float progress)
    {
        if (!EnsureProgressBubble())
            return;

        if (!taskProgressBubbleActive || !uiBubble.gameObject.activeSelf)
        {
            uiBubble.gameObject.SetActive(true);
            uiBubble.SetBubble(Define.BubbleType.Progress);
            taskProgressBubbleActive = true;
        }

        uiBubble.SetProgress(progress);
    }

    /// <summary>직원 작업이 끝났을 때 대상의 진행 말풍선을 닫습니다.</summary>
    public virtual void HideTaskProgress()
    {
        taskProgressBubbleActive = false;
        if (uiBubble != null)
            uiBubble.Hide();
        RefreshDishAlert();
    }

    /// <summary>이미 연결된 말풍선을 찾거나 공용 UI 생성 함수로 풀에서 준비합니다.</summary>
    private bool EnsureProgressBubble()
    {
        if (uiBubble == null && Managers.Instance != null)
        {
            // 말풍선은 씬 UI 루트에 두어 모델에 가려지지 않게 화면 위에서 표시합니다.
            uiBubble = Managers.UI.MakeSubItem<UI_Bubble>(Managers.UI.Root.transform);
            if (uiBubble != null)
            {
                // 프리팹마다 지정한 기준점을 연결하고, 이전 사용자의 표시 상태를 정리합니다.
                uiBubble.SetAnchor(bubbleAnchor != null ? bubbleAnchor : transform);
                uiBubble.ResetForPool();
            }
        }

        return uiBubble != null;
    }

    protected virtual void OnDisable()
    {
        ClearMachineWork();
        CancelInteraction();
        reservedActor = null;
        HideTaskProgress();
        actorInRange = false;
        candidateActor = null;
        playerCollidersInside.Clear();

        // 객체가 꺼지면 말풍선을 초기화하고 공용 풀에 돌려 다른 객체가 재사용합니다.
        if (uiBubble != null && Managers.Instance != null)
        {
            uiBubble.ResetForPool();
            Managers.Pool.Release(uiBubble.gameObject);
        }
        uiBubble = null;
    }

    protected static bool TryGetPlayer(GameObject actor, out PlayerController player)
    {
        player = actor != null ? actor.GetComponent<PlayerController>() : null;
        return player != null;
    }

    /// <summary>상호작용 범위에 들어온 콜라이더가 속한 플레이어나 직원을 찾습니다.</summary>
    private static GameObject GetActor(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player != null)
            return player.gameObject;

        EmployeeController employee = other.GetComponentInParent<EmployeeController>();
        return employee != null ? employee.gameObject : null;
    }
}
