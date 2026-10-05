using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static Define;

[RequireComponent(typeof(NavMeshAgent))]
public class EmployeeController : MonoBehaviour
{
    private enum ServingStep
    {
        None,
        MakeBeer,
        DeliverBeer,
    }

    [Header("State")]
    [SerializeField]
    private EmployeeState currentState = EmployeeState.Idle;

    public EmployeeState CurrentState => currentState;


    [Header("Available Tasks")]
    [Tooltip("이 직원이 수행할 수 있는 업무")]
    [SerializeField]
    private List<EmployeeTaskType> availableTasks = new();

    public IReadOnlyList<EmployeeTaskType> AvailableTasks => availableTasks;


    [Header("Current Task")]
    [SerializeField]
    private EmployeeTask currentEmployeeTask;

    [SerializeField]
    private EmployeeTaskType currentTask = EmployeeTaskType.None;

    public EmployeeTaskType CurrentTask => currentTask;

    
    [Header("Movement Settings")]
    [Tooltip("직원의 이동 속도")]
    [SerializeField]
    private float moveSpeed = 3.8f;

    [Tooltip("목표 지점 도착 인정 거리")]
    [SerializeField]
    private float stoppingDistance = 0.8f;

    [Header("Target")]
    [SerializeField]
    private InteractableBase target;

    public InteractableBase Target => target;


    private NavMeshAgent agent;
    private Animator animator;
    private StackingSystem stacking;
    private EmployeeWander wander;
    private UI_BaseStation progressUI;
    private BeerMachine sourceMachine;
    private ServingStep servingStep;

    private float workProgressTimer = 0f;

    [Header("직원별 능력치")]
    [Tooltip("이 직원만 사용하는 이동 속도 업그레이드 단계입니다.")]
    [SerializeField, Min(0)]
    private int moveUpgradeLevel;

    [Tooltip("이 직원의 기본 이동 속도에 더하는 값입니다.")]
    [SerializeField, Min(0f)]
    private float moveSpeedBonus;

    [Tooltip("이 직원만 사용하는 작업 속도 업그레이드 단계입니다.")]
    [SerializeField, Min(0)]
    private int interactionUpgradeLevel;

    [Tooltip("이 직원의 작업 시간에 곱하는 배율입니다. 1보다 낮을수록 작업이 빨라집니다.")]
    [SerializeField, Range(0.25f, 1f)]
    private float interactionTimeScale = 1f;

    /// <summary>이 직원만 가진 작업 속도 업그레이드 단계입니다.</summary>
    public int InteractionUpgradeLevel => interactionUpgradeLevel;

    /// <summary>이 직원만 가진 이동 속도 업그레이드 단계입니다.</summary>
    public int MoveUpgradeLevel => moveUpgradeLevel;

    /// <summary>이 직원만 사용하는 작업 시간 배율입니다.</summary>
    public float InteractionTimeScale => interactionTimeScale;

    /// <summary>대기 중에 직원이 서성일지 실시간으로 켜거나 끕니다.</summary>
    public bool WanderingEnabled
    {
        get => wander != null && wander.WanderingEnabled;
        set
        {
            if (wander == null)
                wander = GetComponent<EmployeeWander>();

            if (wander != null)
                wander.WanderingEnabled = value;
        }
    }

    /// <summary>직원이 생산한 물건을 담는 공용 스택입니다.</summary>
    public StackingSystem Stacking => stacking;


    #region Init

    public void Init()
    {
        InitializeComponents();
        InitializeAgent();
        InitializeUI();

        // 고용 위치를 기준으로 서성임 컴포넌트의 대기와 이동 상태를 초기화합니다.
        //wander.Initialize();

        SetState(EmployeeState.Idle);

        // 프리팹이나 외부 설정에 이미 등록된 업무를 보존하면서 필요한 기본 업무를 추가합니다.
        AddTaskIfMissing(EmployeeTaskType.Cleaning);
        AddTaskIfMissing(EmployeeTaskType.Serving);
        AddTaskIfMissing(EmployeeTaskType.Casher);
        AddTaskIfMissing(EmployeeTaskType.DishWash);
    }

    /// <summary>기본 업무를 중복 없이 직원의 수행 가능 목록에 등록합니다.</summary>
    private void AddTaskIfMissing(EmployeeTaskType taskType)
    {
        if (!availableTasks.Contains(taskType))
            availableTasks.Add(taskType);
    }

    private void InitializeComponents()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();

        //if (wander == null)
        //    wander = GetComponent<EmployeeWander>();

        // 오래된 직원 프리팹에도 컴포넌트를 보완해 고용 시 누락으로 멈추지 않게 합니다.
        //if (wander == null)
        //    wander = gameObject.AddComponent<EmployeeWander>();

        if (stacking == null)
            stacking = GetComponent<StackingSystem>();

        // 프리팹 구성이 바뀌어도 직원을 고용할 때 스택 기능이 빠지지 않게 보완합니다.
        if (stacking == null)
            stacking = gameObject.AddComponent<StackingSystem>();

        if (stacking.stackPoint == null)
        {
            Transform[] childPoints = GetComponentsInChildren<Transform>(true);
            foreach (Transform childPoint in childPoints)
            {
                if (childPoint.name == "Hand_R")
                {
                    stacking.stackPoint = childPoint;
                    break;
                }
            }
        }

        if (progressUI == null)
            progressUI = GetComponentInChildren<UI_BaseStation>(true);
    }

    private void InitializeAgent()
    {
        if (agent == null)
            return;

        agent.speed = moveSpeed + moveSpeedBonus;
        agent.stoppingDistance = stoppingDistance;
        agent.acceleration = 12f;
        agent.angularSpeed = 480f;

        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(
                transform.position,
                out NavMeshHit hit,
                3.0f,
                NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }
    }

    /// <summary>이 직원 한 명의 이동 속도 업그레이드 단계와 추가 속도를 설정합니다.</summary>
    public void SetMoveUpgrade(int level, float totalSpeedBonus)
    {
        moveUpgradeLevel = Mathf.Max(0, level);
        moveSpeedBonus = Mathf.Max(0f, totalSpeedBonus);
        ApplyMoveSpeed();
    }

    /// <summary>이 직원 한 명의 작업 속도 단계와 실제 작업 시간 배율을 설정합니다.</summary>
    public void SetInteractionUpgrade(int level, float totalTimeReduction)
    {
        interactionUpgradeLevel = Mathf.Max(0, level);
        interactionTimeScale = Mathf.Clamp(1f - totalTimeReduction, 0.25f, 1f);
    }

    /// <summary>현재 직원의 NavMeshAgent에 개인 이동 속도 능력치를 적용합니다.</summary>
    private void ApplyMoveSpeed()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        if (agent != null)
            agent.speed = moveSpeed + moveSpeedBonus;
    }

    private void InitializeUI()
    {
        if (progressUI != null)
        {
            progressUI.SetProgress(0f);
            progressUI.gameObject.SetActive(false);
        }
    }

    #endregion


    #region Task Capability

    /// <summary>
    /// 해당 업무를 수행할 수 있는 직원인지 확인합니다.
    /// </summary>
    public bool HasTask(EmployeeTaskType taskType)
    {
        return availableTasks.Contains(taskType);
    }

    #endregion


    #region Update

    private void Update()
    {
        UpdateAnimation();

        switch (currentState)
        {
            case EmployeeState.Idle:
                UpdateIdle();
                break;

            case EmployeeState.MovingToWork:
                UpdateMovingToTask();
                break;

            case EmployeeState.Working:
                UpdateTask();
                break;
        }
    }

    private void UpdateAnimation()
    {
        if (animator != null &&
            agent != null &&
            agent.enabled)
        {
            animator.SetFloat(
                "Speed",
                agent.velocity.magnitude);
        }
    }

    private void UpdateIdle()
    {
        // 직원이 한가할 때의 NavMesh 이동은 별도 컴포넌트에 맡깁니다.
        wander?.Tick();
    }

    #endregion


    #region Task Assignment

    /// <summary>
    /// EmployeeManager가 직원에게 Task를 할당합니다.
    /// </summary>
    public bool AssignTask(EmployeeTask task)
    {
        if (task == null)
            return false;

        // 이미 다른 업무를 수행 중이면 거부
        if (currentState != EmployeeState.Idle)
        {
            Debug.Log(
                $"[EmployeeController] {name}은 현재 작업 중입니다.");

            return false;
        }

        // 해당 업무를 수행할 능력이 없는 경우
        if (!HasTask(task.Type))
        {
            Debug.Log(
                $"[EmployeeController] {name}은 {task.Type} 업무를 수행할 수 없습니다.");

            return false;
        }

        // Target이 필요한데 없는 경우
        if (task.Target == null)
        {
            Debug.LogWarning(
                $"[EmployeeController] {name}에게 할당된 Task의 Target이 없습니다.");

            return false;
        }

        // 이미 맥주를 들고 있으면 배달 단계부터 시작하고, 빈손이면 기계 잔을 먼저 예약합니다.
        if (task.Type == EmployeeTaskType.Serving)
        {
            if (stacking == null)
                return false;

            if (stacking.GetFirstListItemType() == Define.ItemType.Beer)
            {
                servingStep = ServingStep.DeliverBeer;
            }
            else if (!stacking.CheckListItem(Define.ItemType.Beer))
            {
                return false;
            }
            else if (BeerMachine.TryStartWorkForEmployee(this, out sourceMachine))
            {
                servingStep = ServingStep.MakeBeer;
            }
            else
            {
                return false;
            }
        }

        currentEmployeeTask = task;
        currentTask = task.Type;
        target = task.Target;

        task.AssignedEmployee = this;

        workProgressTimer = 0f;

        // 업무를 수행하기 위해 이동
        MoveToTask();

        return true;
    }

    private void MoveToTask()
    {
        if (target == null)
        {
            CancelTask();
            return;
        }

        if (agent == null ||
            !agent.enabled ||
            !agent.isOnNavMesh)
        {
            CancelTask();
            return;
        }

        Transform moveTarget = GetMoveTarget();
        if (moveTarget == null)
        {
            CancelTask();
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(moveTarget.position);

        SetState(EmployeeState.MovingToWork);

        Debug.Log(
            $"[EmployeeController] {name} → {currentTask} 업무로 이동합니다.");
    }

    /// <summary>플레이어가 작업을 가져가면 직원의 이동, 진행 표시, 재고 예약을 안전하게 해제합니다.</summary>
    public void StopTaskForPlayer()
    {
        // 현재 목적지로 계속 이동하지 않도록 경로를 지우고 NavMesh 이동을 멈춥니다.
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.isStopped = false;
        }

        // 직원이 맡은 기계 점유와 작업 진행 표시를 정리합니다.
        HideProgressUI();
        BeerMachine.StopWorkForEmployee(this);
        sourceMachine = null;
        servingStep = ServingStep.None;
        currentEmployeeTask = null;
        currentTask = EmployeeTaskType.None;
        target = null;
        workProgressTimer = 0f;
        SetState(EmployeeState.Idle);
    }

    #endregion


    #region Moving

    private void UpdateMovingToTask()
    {
        if (!IsTaskTargetValid())
        {
            CancelTask();
            return;
        }

        if (agent == null ||
            !agent.enabled ||
            !agent.isOnNavMesh)
        {
            return;
        }

        if (agent.pathPending)
            return;

        if (agent.remainingDistance <=
            agent.stoppingDistance + 0.15f)
        {
            StartWorking();
        }
    }

    private bool IsTaskTargetValid()
    {
        if (currentEmployeeTask == null)
            return false;

        if (target == null)
            return false;

        if (!target.gameObject.activeInHierarchy)
            return false;

        // 이동 도중 다른 캐릭터가 먼저 처리한 작업은 목적지에 도착하기 전에 취소합니다.
        if (currentTask == EmployeeTaskType.Serving &&
            target is SeatStation servingSeat &&
            !servingSeat.IsWaitingToServe)
        {
            return false;
        }

        if (currentTask == EmployeeTaskType.Serving &&
            servingStep == ServingStep.MakeBeer &&
            (sourceMachine == null || !sourceMachine.isActiveAndEnabled))
        {
            return false;
        }

        return true;
    }

    #endregion


    #region Working

    private void StartWorking()
    {
        if (target == null)
        {
            CancelTask();
            return;
        }

        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        FaceTarget();

        workProgressTimer = 0f;

        // 맥주 생산 업무라면 작업 진행도를 세기 시작하기 전에 기계 출구로 빈 잔을 옮깁니다.
        if (currentTask == EmployeeTaskType.Serving && servingStep == ServingStep.MakeBeer)
            sourceMachine?.StartBeerProductionForEmployee();

        SetState(EmployeeState.Working);

        Debug.Log(
            $"[EmployeeController] {name}이 {currentTask} 업무를 시작합니다.");
    }

    private void UpdateTask()
    {
        if (currentEmployeeTask == null)
        {
            CancelTask();
            return;
        }

        switch (currentTask)
        {
            case EmployeeTaskType.Cleaning:
                UpdateCleaning();
                break;

            case EmployeeTaskType.DishWash:
                UpdateDishWash();
                break;

            case EmployeeTaskType.Serving:
                UpdateServing();
                break;

            case EmployeeTaskType.Casher:
                UpdateCashier();
                break;

            default:
                CancelTask();
                break;
        }
    }

    #endregion


    #region Cleaning

    private void UpdateCleaning()
    {
        SeatStation seat = target as SeatStation;
        if (seat == null)
        {
            CancelTask();
            return;
        }

        // 이미 다른 이유로 청소가 끝난 경우
        if (seat.CurrentState != SeatState.Dirty)
        {
            CancelTask();
            return;
        }

        // 청소도 다른 업무와 같은 방식으로 좌석 기본 시간에 직원 속도 배율을 적용합니다.
        if (UpdateWorkProgress(GetWorkDuration(seat)))
        {
            FinishCleaning(seat);
        }
    }

    private void FinishCleaning(SeatStation seat)
    {
        Debug.Log(
            $"[EmployeeController] {name}이 " +
            $"테이블({seat.name}) 청소를 완료했습니다.");

        if (!seat.CleanSeatForEmployee(stacking))
        {
            CancelTask();
            return;
        }

        if (seat.AssignedCleaner == this)
        {
            seat.AssignedCleaner = null;
        }

        FinishTask();
    }

    #endregion


    #region Other Tasks

    private void UpdateDishWash()
    {
        Sink sink = target as Sink;
        if (sink == null || !sink.isActiveAndEnabled || Managers.Dish == null ||
            stacking == null || stacking.GetFirstListItemType() != Define.ItemType.Beer_Dirty)
        {
            CancelTask();
            return;
        }

        if (UpdateWorkProgress(GetWorkDuration(sink)))
        {
            if (sink.WashForEmployee(stacking))
                FinishTask();
            else
                CancelTask();
        }
    }

    private void UpdateServing()
    {
        SeatStation seat = target as SeatStation;
        if (seat == null || !seat.isActiveAndEnabled || !seat.IsWaitingToServe)
        {
            CancelTask();
            return;
        }

        if (servingStep == ServingStep.MakeBeer)
        {
            if (sourceMachine == null || !sourceMachine.isActiveAndEnabled)
            {
                CancelTask();
                return;
            }

            // 직원은 기계의 기본 제조 시간에 공통 작업 속도 배율을 적용합니다.
            if (!UpdateWorkProgress(GetWorkDuration(sourceMachine)))
                return;

            if (!sourceMachine.ProduceBeerForEmployee(this, stacking))
            {
                CancelTask();
                return;
            }

            // 맥주를 스택에 올린 뒤 같은 작업을 유지한 채 손님 자리로 이동합니다.
            sourceMachine.HideTaskProgress();
            sourceMachine = null;
            servingStep = ServingStep.DeliverBeer;
            workProgressTimer = 0f;
            MoveToTask();
            return;
        }

        if (servingStep != ServingStep.DeliverBeer ||
            stacking == null ||
            stacking.GetFirstListItemType() != Define.ItemType.Beer)
        {
            CancelTask();
            return;
        }

        if (UpdateWorkProgress(GetWorkDuration(seat)))
        {
            if (seat.ServeForEmployee(stacking))
                FinishTask();
            else
                CancelTask();
        }
    }

    private void UpdateCashier()
    {
        Counter counter = target as Counter;
        if (counter == null || !counter.isActiveAndEnabled || !counter.HasWaitingCustomers)
        {
            CancelTask();
            return;
        }

        if (UpdateWorkProgress(GetWorkDuration(counter)))
        {
            if (counter.CollectForEmployee())
                FinishTask();
            else
                CancelTask();
        }
    }

    #endregion

    /// <summary>대상이 제공하는 기본 작업 시간에 이 직원만의 작업 속도 배율을 적용합니다.</summary>
    private float GetWorkDuration(InteractableBase workTarget)
    {
        if (workTarget == null)
            return 0f;

        return Mathf.Max(0f, workTarget.Duration * interactionTimeScale);
    }

    /// <summary>
    /// 직원의 현재 작업 시간을 갱신하고 말풍선 진행도를 표시합니다.
    /// 작업 시간이 0초이면 진행도를 완료로 표시하고 바로 다음 처리로 넘어갑니다.
    /// </summary>
    /// <param name="duration">대상 기본 시간과 직원 속도에서 계산한 실제 작업 시간입니다.</param>
    /// <returns>계산한 시간이 지났거나 0초 작업이면 true를 반환합니다.</returns>
    private bool UpdateWorkProgress(float duration)
    {
        // 0초 작업은 타이머를 나누지 않고 즉시 완료 처리합니다.
        if (duration <= 0f)
        {
            UpdateProgressUI(1f);
            return true;
        }

        // 작업이 진행되는 동안 누적 시간을 진행도로 바꾸어 말풍선에 표시합니다.
        workProgressTimer += Time.deltaTime;
        UpdateProgressUI(workProgressTimer / duration);
        return workProgressTimer >= duration;
    }


    #region Task Finish / Cancel

    private void FinishTask()
    {
        EmployeeTask finishedTask = currentEmployeeTask;

        // 대상 참조를 비우기 전에 말풍선을 닫아 작업 중 표시가 남지 않게 합니다.
        HideProgressUI();
        BeerMachine.StopWorkForEmployee(this);
        sourceMachine = null;
        servingStep = ServingStep.None;

        currentEmployeeTask = null;
        currentTask = EmployeeTaskType.None;
        target = null;

        workProgressTimer = 0f;

        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        SetState(EmployeeState.Idle);

        // 직원이 한가해진 뒤 완료 작업을 정리하고 다음 대기 작업을 배정합니다.
        if (finishedTask != null && Managers.Instance != null)
            Managers.Task.FinishTask(finishedTask);
    }

    public void CancelTask()
    {
        EmployeeTask cancelledTask = currentEmployeeTask;

        // 취소된 작업도 대상 말풍선과 진행 표시를 바로 정리합니다.
        HideProgressUI();
        BeerMachine.StopWorkForEmployee(this);
        sourceMachine = null;
        servingStep = ServingStep.None;

        currentEmployeeTask = null;
        currentTask = EmployeeTaskType.None;
        target = null;

        workProgressTimer = 0f;

        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        SetState(EmployeeState.Idle);

        // 취소된 작업은 관리 목록에서 제거해 잘못된 대상에 다시 배정되지 않게 합니다.
        if (cancelledTask != null && Managers.Instance != null)
            Managers.Task.FinishTask(cancelledTask);
    }

    #endregion


    #region UI

    private void UpdateProgressUI(float ratio)
    {
        // 직원 진행도는 작업 대상의 말풍선에 표시해 서빙·결제·설거지도 같은 방식으로 보입니다.
        InteractableBase progressTarget =
            currentTask == EmployeeTaskType.Serving && servingStep == ServingStep.MakeBeer
                ? sourceMachine
                : target;
        progressTarget?.ShowTaskProgress(ratio);

        // 프리팹에 기존 진행 UI가 연결되어 있으면 함께 갱신해 이전 배치도 호환합니다.
        if (progressUI != null)
        {
            if (!progressUI.gameObject.activeSelf)
                progressUI.gameObject.SetActive(true);

            progressUI.SetProgress(ratio);
        }
    }

    private void HideProgressUI()
    {
        sourceMachine?.HideTaskProgress();
        target?.HideTaskProgress();

        if (progressUI != null)
        {
            progressUI.SetProgress(0f);
            progressUI.gameObject.SetActive(false);
        }
    }

    #endregion


    #region Movement

    private void FaceTarget()
    {
        Transform moveTarget = GetMoveTarget();
        if (moveTarget == null)
            return;

        Vector3 lookTarget = moveTarget.position;
        lookTarget.y = transform.position.y;

        transform.LookAt(lookTarget);
    }

    /// <summary>서빙 단계에 따라 맥주 기계나 손님 좌석 중 이동 목적지를 고릅니다.</summary>
    private Transform GetMoveTarget()
    {
        if (currentTask == EmployeeTaskType.Serving && servingStep == ServingStep.MakeBeer)
            return sourceMachine != null ? sourceMachine.transform : null;

        return target != null ? target.transform : null;
    }

    #endregion


    #region State

    private void SetState(EmployeeState nextState)
    {
        currentState = nextState;

        if (currentState == EmployeeState.Idle)
        {
            currentTask = EmployeeTaskType.None;
        }
    }

    #endregion


    private void OnDisable()
    {
        CancelTask();
    }
}
