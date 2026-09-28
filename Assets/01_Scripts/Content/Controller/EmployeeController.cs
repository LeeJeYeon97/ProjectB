using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static Define;

[RequireComponent(typeof(NavMeshAgent))]
public class EmployeeController : MonoBehaviour
{
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


    [Header("Cleaning Settings")]
    [Tooltip("테이블 1개를 청소하는 데 걸리는 시간")]
    [SerializeField]
    private float cleanDuration = 2.0f;


    [Header("Target")]
    [SerializeField]
    private BaseStation target;

    public BaseStation Target => target;


    private NavMeshAgent agent;
    private Animator animator;
    private UI_InteractionProgress progressUI;

    private float workProgressTimer = 0f;


    #region Init

    public void Init()
    {
        InitializeComponents();
        InitializeAgent();
        InitializeUI();

        SetState(EmployeeState.Idle);

        availableTasks.Add(EmployeeTaskType.Cleaning);
    }

    private void InitializeComponents()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (progressUI == null)
            progressUI = GetComponentInChildren<UI_InteractionProgress>(true);
    }

    private void InitializeAgent()
    {
        if (agent == null)
            return;

        agent.speed = moveSpeed;
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
        // 나중에 대기 행동 추가
        // 예:
        // - 서성이기
        // - 카운터 대기
        // - 손님 기다리기
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

        agent.isStopped = false;
        agent.SetDestination(target.transform.position);

        SetState(EmployeeState.MovingToWork);

        Debug.Log(
            $"[EmployeeController] {name} → {currentTask} 업무로 이동합니다.");
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

        workProgressTimer += Time.deltaTime;

        float ratio =
            cleanDuration > 0f
                ? Mathf.Clamp01(
                    workProgressTimer / cleanDuration)
                : 1f;

        // 테이블 Progress
        seat.SetCleanProgress(ratio);

        // 직원 Progress UI
        UpdateProgressUI(ratio);

        // 완료
        if (workProgressTimer >= cleanDuration)
        {
            FinishCleaning(seat);
        }
    }

    private void FinishCleaning(SeatStation seat)
    {
        Debug.Log(
            $"[EmployeeController] {name}이 " +
            $"테이블({seat.name}) 청소를 완료했습니다.");

        seat.CleanSeat();

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
        // TODO:
        // DishStation 구현 후 작성
    }

    private void UpdateServing()
    {
        // TODO:
        // Serving 대상 구현 후 작성
    }

    private void UpdateCashier()
    {
        // TODO:
        // Cashier 구현 후 작성
    }

    #endregion


    #region Task Finish / Cancel

    private void FinishTask()
    {
        if (currentEmployeeTask != null)
        {
            currentEmployeeTask.AssignedEmployee = null;
        }

        currentEmployeeTask = null;
        currentTask = EmployeeTaskType.None;
        target = null;

        workProgressTimer = 0f;

        HideProgressUI();

        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        SetState(EmployeeState.Idle);
    }

    public void CancelTask()
    {
        if (currentEmployeeTask != null)
        {
            if (currentEmployeeTask.AssignedEmployee == this)
            {
                currentEmployeeTask.AssignedEmployee = null;
            }
        }

        // Cleaning 전용 할당 해제
        SeatStation seat = target as SeatStation;

        if (seat != null &&
            seat.AssignedCleaner == this)
        {
            seat.AssignedCleaner = null;
        }

        currentEmployeeTask = null;
        currentTask = EmployeeTaskType.None;
        target = null;

        workProgressTimer = 0f;

        HideProgressUI();

        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        SetState(EmployeeState.Idle);
    }

    #endregion


    #region UI

    private void UpdateProgressUI(float ratio)
    {
        if (progressUI == null)
            return;

        if (!progressUI.gameObject.activeSelf)
        {
            progressUI.gameObject.SetActive(true);
            progressUI.SetVisible(true);
        }

        progressUI.SetProgress(ratio);
    }

    private void HideProgressUI()
    {
        if (progressUI == null)
            return;

        progressUI.SetProgress(0f);
        progressUI.gameObject.SetActive(false);
    }

    #endregion


    #region Movement

    private void FaceTarget()
    {
        if (target == null)
            return;

        Vector3 lookTarget = target.transform.position;
        lookTarget.y = transform.position.y;

        transform.LookAt(lookTarget);
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