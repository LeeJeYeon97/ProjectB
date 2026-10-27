using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using static Define;

[RequireComponent(typeof(NavMeshAgent))]
public class CustomerController : MonoBehaviour
{
    [Header("State")]
    public CustomerState CurrentState = CustomerState.None;
    /// <summary>손님이 줄에 도착했고 대기줄 방해 행동이 없을 때만 좌석으로 이동할 수 있습니다.</summary>
    public bool CanLeaveQueue => CurrentState == CustomerState.WaitingAtEntrance &&
                                 customerType != CustomerType.QueueNoiser;

    [SerializeField]
    private ItemType orderedType;
    public ItemType OrderedType => orderedType;

    // 이 손님의 주문을 레시피 에셋 참조로 기억해 말풍선에 해당 이미지를 전달합니다.
    private FoodRecipeData orderedRecipe;
    /// <summary>손님이 주문한 실제 레시피를 제조 직원에게 전달합니다.</summary>
    public FoodRecipeData OrderedRecipe => orderedRecipe;

    [Header("Timers")]
    public float eatingDuration = 5.0f; // 밥 먹는 시간

    [Header("Payment Settings")]
    public int payAmount = 15;          // 식사 가격

    private NavMeshAgent agent;
    private SeatStation assignedSeat;
    private Transform exitPoint;
    private Transform counterPayPoint;
    private Counter targetCounter;

    private Animator animator; // 1. 애니메이터 캐싱용 변수 추가
    private float arrivedAtTime;
    private float foodWaitSeconds;
    /// <summary>리뷰 계산에 사용할 손님의 음식 대기 시간을 돌려줍니다.</summary>
    public float FoodWaitSeconds => foodWaitSeconds;

    private int mealInterruptionCount;
    /// <summary>리뷰 계산에 사용할 식사 중 방해 횟수를 돌려줍니다.</summary>
    public int MealInterruptionCount => mealInterruptionCount;

    private CustomerType customerType = CustomerType.Normal;
    private bool servedMenuCorrectly = true;
    /// <summary>받은 메뉴가 주문과 일치했는지 리뷰 계산에 전달합니다.</summary>
    public bool ServedMenuCorrectly => servedMenuCorrectly;

    private CustomerResolve resolveWork;
    private Coroutine noiseRoutine;
    private Coroutine queueArrivalRoutine;
    private bool waitingForIngredients;
    private bool subscribedToIngredientChanges;

    [Header("방해 손님 설정")]
    [SerializeField, Min(0.5f)] private float noiseRange = 4f;
    [SerializeField, Min(0.5f)] private float noiseInterval = 3f;


    [Header("UI")]
    [Tooltip("손님 말풍선이 따라갈 위치입니다. 프리팹 안의 BubbleAnchor를 연결합니다.")]
    [SerializeField] private Transform bubbleAnchor;
    private UI_Bubble uiBubble;
    private UI_Bubble hidingBubble;

    [Header("회전 설정")]
    [SerializeField] private float turnDuration = 0.35f;

    /// <summary>손님의 실제 이동 속도를 애니메이터에 전달합니다.</summary>
    private void Update()
    {
        // 이동 컴포넌트가 아직 준비되지 않았다면 애니메이션 값을 갱신하지 않습니다.
        if (animator == null || agent == null)
            return;

        animator.SetFloat("Speed", agent.velocity.magnitude);
    }

    /// <summary>손님이 활성화될 때 재고 보충 알림을 구독합니다.</summary>
    private void OnEnable()
    {
        SubscribeToIngredientChanges();
    }

    /// <summary>손님이 풀로 돌아가거나 제거될 때 소음과 말풍선 상태를 정리합니다.</summary>
    private void OnDisable()
    {
        StopNoise();
        UnsubscribeFromIngredientChanges();
        Managers.Ingredient?.ReleaseReservation(this);
        ReleaseBubble(uiBubble);
        ReleaseBubble(hidingBubble);
        uiBubble = null;
        hidingBubble = null;
    }

    #region 초기화

    /// <summary>손님의 주문과 이동 상태를 초기화한 뒤 입구 대기점으로 보냅니다.</summary>
    public void Init(Transform entrancePoint, Transform exit, CustomerType type = CustomerType.Normal)
    {
        // 풀에서 재사용되기 전에 남은 소음 코루틴이 새 손님의 상태에 영향을 주지 않게 합니다.
        StopNoise();
        Managers.Ingredient?.ReleaseReservation(this);

        // 리뷰 대기 시간은 손님이 가게에 도착한 시점부터 음식이 제공될 때까지 측정합니다.
        arrivedAtTime = Time.time;
        foodWaitSeconds = 0f;
        mealInterruptionCount = 0;
        customerType = type;
        orderedType = ItemType.None;
        orderedRecipe = null;
        waitingForIngredients = false;
        servedMenuCorrectly = true;

        // 오브젝트 풀에서 다시 사용할 때 재고 보충 알림을 다시 받도록 연결합니다.
        SubscribeToIngredientChanges();
        gameObject.name = $"Customer_{customerType}";

        if (agent == null) agent = GetComponent<NavMeshAgent>(); // agent 할당 확인
        if (animator == null) animator = GetComponent<Animator>(); // animator 할당 확인

        // 1. Agent 컴포넌트 체크
        if (agent == null)
        {
            Debug.LogError("[NavMesh] NavMeshAgent 컴포넌트를 찾을 수 없습니다!");
            return;
        }
        // 2. NavMesh 위에 올려져 있는지 체크 및 Warp 처리
        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning("[NavMesh] NPC가 NavMesh 위에 있지 않아 가장 가까운 NavMesh 위치로 Warp합니다.");
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 3.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            else
            {
                Debug.LogError("[NavMesh] 주변에서 NavMesh 구역을 찾을 수 없습니다. Bake 상태를 확인하세요.");
                return;
            }
        }

        exitPoint = exit;
        CurrentState = CustomerState.WalkEntrance;

        BindCounter();

        // 실제 대기 위치에 도착한 뒤에만 줄 손님의 소음과 해결 작업을 시작합니다.
        MoveToQueuePoint(entrancePoint);

        // 풀에서 재사용된 손님도 이번 방해 유형과 상태에 맞게 표시를 새로 고칩니다.
        if (resolveWork == null)
            resolveWork = GetComponentInChildren<CustomerResolve>(true);
        resolveWork?.RefreshMark();
    }


    /// <summary>현재 활성화된 계산대와 손님 결제 위치를 다시 가져옵니다.</summary>
    private void BindCounter()
    {
        targetCounter = Managers.Game.Counter;
        counterPayPoint = targetCounter != null ? targetCounter.PayPoint : null;
    }

    #endregion

    #region NavMesh 이동 보조 루틴

    /// <summary>
    /// 에이전트가 경로 계산을 완료하고 목적지에 도달할 때까지 대기하는 공통 코루틴
    /// </summary>
    private IEnumerator CoWaitUntilArrival(float stoppingDistance = 0.2f)
    {
        yield return null;

        while (agent != null && agent.enabled && agent.isOnNavMesh &&
               (agent.pathPending || agent.remainingDistance > stoppingDistance))
        {
            yield return null;
        }
    }

    #endregion

    #region 생성 -> 입구 루틴

    // 입구로 가는 코루틴
    private IEnumerator CoCheckArrivalAtEntrance(Transform point)
    {
        yield return CoWaitUntilArrival(0.2f);

        // 경로가 중간에 끊기거나 에이전트가 비활성화되면 줄에 선 것으로 처리하지 않습니다.
        Vector3 gap = point != null ? point.position - transform.position : Vector3.zero;
        gap.y = 0f;
        if (point != null && agent != null && agent.enabled && agent.isOnNavMesh &&
            gap.sqrMagnitude <= 0.25f && CurrentState == CustomerState.WalkEntrance)
        {
            CurrentState = CustomerState.WaitingAtEntrance;
            resolveWork?.RefreshMark();

            // 줄에 도착하기 전에는 소음 피해를 주지 않습니다.
            if (customerType == CustomerType.QueueNoiser)
                StartNoise();
        }

        queueArrivalRoutine = null;
    }

    /// <summary>대기열 재정렬을 포함해 줄 위치로 이동하고, 도착 전 상태를 다시 설정합니다.</summary>
    public void MoveToQueuePoint(Transform point)
    {
        if (point == null || agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        // 이전 목적지의 도착 코루틴이 새 위치를 도착한 것으로 처리하지 않게 합니다.
        if (queueArrivalRoutine != null)
            StopCoroutine(queueArrivalRoutine);

        StopNoise();
        CurrentState = CustomerState.WalkEntrance;
        resolveWork?.RefreshMark();
        agent.isStopped = false;
        if (!agent.SetDestination(point.position))
        {
            Debug.LogError($"[NavMesh] {point.name} 위치까지의 경로를 계산할 수 없습니다.", this);
            return;
        }
        queueArrivalRoutine = StartCoroutine(CoCheckArrivalAtEntrance(point));
    }

    #endregion


    #region 손님이 입구에서 좌석으로 들어가는 코드
    // 매니저가 손님에게 자리를 지정해 줄 때 호출
    public void AssignSeat(SeatStation seat)
    {
        if (seat == null || !CanLeaveQueue)
            return;

        assignedSeat = seat;
        assignedSeat.SetState(SeatState.Reserved);
        assignedSeat.currentCustomer = this;

        StartCoroutine(CoCheckArrivalAtSeat());
    }
    // 빈 자리로 가는 코루틴
    private IEnumerator CoCheckArrivalAtSeat()
    {
        // 입구 도착 후 좌석으로 이동 시작
        CurrentState = CustomerState.MovingToSeat;
        resolveWork?.RefreshMark();
        if (agent != null && assignedSeat != null && assignedSeat.customerSitPoint != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
            agent.SetDestination(assignedSeat.customerSitPoint.position);
        }

        // 목적지 도착 대기
        yield return CoWaitUntilArrival(0.2f);

        // 착석 소음 손님은 소음을 해결하기 전까지 주문을 만들지 않습니다.
        CurrentState = customerType == CustomerType.SeatNoiser
            ? CustomerState.WaitingToOrder
            : CustomerState.WaitingForFood;

        if (assignedSeat != null && assignedSeat.customerSitPoint != null)
        {
            transform.position = assignedSeat.customerSitPoint.position;
            transform.rotation = assignedSeat.customerSitPoint.rotation;
        }

        if (agent != null && agent.enabled)
        {
            agent.enabled = false; // 앉아 있는 동안 네비게이션 정지
        }

        if (assignedSeat != null)
        {
            Debug.Log("좌석 지정완료");
            assignedSeat.SetState(SeatState.Occupied);
        }

        // 착석 소음형은 자리에 앉은 뒤 주변 손님에게 주기적으로 소란을 피웁니다.
        if (customerType == CustomerType.SeatNoiser)
            StartNoise();

        resolveWork?.RefreshMark();

        // TODO: 앉는 애니메이션 재생 (Animator.SetTrigger("Sit"))

        if (CurrentState == CustomerState.WaitingForFood)
            PlaceOrder();
        else
            Debug.Log("[Customer] 착석 소음 손님이 주문 전에 해결을 기다립니다.", this);
    }

    /// <summary>착석 후 또는 소음 해결 후 주문을 한 번 만들고 서빙 작업을 등록합니다.</summary>
    private void PlaceOrder()
    {
        if (assignedSeat == null || CurrentState != CustomerState.WaitingForFood)
            return;

        // 같은 손님에게 주문 요청이 다시 와도 이미 정한 레시피와 이미지를 유지합니다.
        if (orderedRecipe != null)
            return;

        // 활성 기계의 레시피에서 한 가지를 고르고, 이번 손님의 주문으로 보관합니다.
        if (!Managers.Customer.TryGetRandomRecipe(out FoodRecipeData recipe))
        {
            Debug.Log("[Customer] 주문 가능한 메뉴가 없어 재고 보충을 기다립니다.", this);
            waitingForIngredients = true;
            return;
        }

        // 주문 시 재료를 예약해 다른 주문이 같은 재고를 먼저 소비하지 못하게 합니다.
        if (!Managers.Ingredient.TryReserveForOrder(this, recipe.Ingredients))
        {
            Debug.Log($"[Customer] {recipe.FoodName} 재료가 부족해 좌석에서 보충을 기다립니다.", this);
            waitingForIngredients = true;
            return;
        }

        waitingForIngredients = false;
        orderedRecipe = recipe;
        // 기존 직원 작업과 서빙 흐름은 메뉴 분류 값을 계속 사용합니다.
        orderedType = ItemType.Menu;
        GetBubble()?.SetOrder(orderedRecipe);
        Managers.Task.CreateTask(EmployeeTaskType.Serving, assignedSeat);
        Debug.Log($"[Customer] {orderedRecipe.FoodName} 주문 완료", this);
    }

    /// <summary>재고가 보충되었을 때 아직 주문하지 못한 착석 손님의 주문을 다시 시도합니다.</summary>
    private void OnIngredientsChanged()
    {
        if (!waitingForIngredients)
            return;

        if (CurrentState != CustomerState.WaitingForFood)
            return;

        PlaceOrder();
    }

    /// <summary>매니저가 준비된 경우 재고 보충 알림을 한 번만 구독합니다.</summary>
    private void SubscribeToIngredientChanges()
    {
        if (subscribedToIngredientChanges || Managers.Instance == null)
            return;

        IngredientManager ingredientManager = Managers.Ingredient;
        if (ingredientManager == null)
            return;

        ingredientManager.OnIngredientsChanged += OnIngredientsChanged;
        subscribedToIngredientChanges = true;
    }

    /// <summary>손님이 비활성화될 때 재고 매니저의 이벤트 참조를 해제합니다.</summary>
    private void UnsubscribeFromIngredientChanges()
    {
        if (!subscribedToIngredientChanges || Managers.Instance == null)
            return;

        IngredientManager ingredientManager = Managers.Ingredient;
        if (ingredientManager != null)
            ingredientManager.OnIngredientsChanged -= OnIngredientsChanged;

        subscribedToIngredientChanges = false;
    }
    #endregion

    
    #region 음식받고 음식 먹는 코드
    public void OnFoodServed(ItemType servedType)
    {
        // 주문이 끝났으므로 남아 있는 예약을 해제합니다. 제조 때 이미 사용된 예약은 무시됩니다.
        Managers.Ingredient.ReleaseReservation(this);

        // 음식이 처음 제공될 때까지의 대기 시간을 기록해 리뷰 평가에 전달합니다.
        foodWaitSeconds = Mathf.Max(0f, Time.time - arrivedAtTime);
        servedMenuCorrectly = servedType == orderedType;
        CurrentState = CustomerState.Eating;

        if (!servedMenuCorrectly)
            Debug.LogWarning($"[Customer] 주문과 다른 메뉴를 받았습니다. 주문: {orderedType}, 받은 메뉴: {servedType}");

        // 음식을 받으면 음식 그림을 진행 이미지로 바꾸고, 새 식사의 진행도를 0부터 표시합니다.
        if (GetBubble() != null)
        {
            uiBubble.SetBubble(Define.BubbleType.Progress);
            uiBubble.SetProgress(0f);
        }

        Debug.Log("밥 먹습니다..");
        StartCoroutine(CoEating());
    }

    /// <summary>식사 중 방해가 발생했음을 기록해 손님 리뷰에 반영합니다.</summary>
    public void RegisterMealInterruption()
    {
        // 대기줄이나 좌석에 있는 동안만 방해를 기록하고, 일반 손님에게만 적용합니다.
        if (customerType != CustomerType.Normal || !CanBeAffectedByNoise())
            return;

        mealInterruptionCount++;
    }

    /// <summary>정해진 간격으로 소음을 내고 근처 일반 손님에게 방해를 전달합니다.</summary>
    private IEnumerator CoMakeNoise()
    {
        while (CanMakeNoise())
        {
            yield return new WaitForSeconds(noiseInterval);

            // 대기줄이나 좌석 상태가 바뀌어 소음 조건을 벗어나면 반복을 끝냅니다.
            if (!CanMakeNoise())
                yield break;

            CustomerController[] customers = FindObjectsByType<CustomerController>();
            float rangeSquared = noiseRange * noiseRange;
            int disturbedCount = 0;

            // 범위 안에서 대기하거나 식사 중인 일반 손님을 찾아 방해 횟수를 남깁니다.
            foreach (CustomerController customer in customers)
            {
                if (customer == null || customer == this ||
                    customer.customerType != CustomerType.Normal ||
                    !customer.CanBeAffectedByNoise())
                    continue;

                Vector3 offset = customer.transform.position - transform.position;
                offset.y = 0f;
                if (offset.sqrMagnitude > rangeSquared)
                    continue;

                customer.RegisterMealInterruption();
                disturbedCount++;
            }

            Debug.Log($"[Customer] {customerType} 손님이 큰 소리를 냈습니다. 영향받은 손님: {disturbedCount}명");
        }
    }

    /// <summary>같은 손님에게 소음 반복 작업이 두 번 등록되지 않도록 시작합니다.</summary>
    private void StartNoise()
    {
        StopNoise();
        noiseRoutine = StartCoroutine(CoMakeNoise());
    }

    /// <summary>방해 해결이나 손님 재사용 시 남은 소음 반복 작업을 즉시 멈춥니다.</summary>
    private void StopNoise()
    {
        if (noiseRoutine == null)
            return;

        StopCoroutine(noiseRoutine);
        noiseRoutine = null;
    }

    /// <summary>현재 유형과 손님 상태가 소음을 내도 되는 조합인지 확인합니다.</summary>
    private bool CanMakeNoise()
    {
        if (customerType == CustomerType.QueueNoiser)
            return CurrentState == CustomerState.WaitingAtEntrance;

        if (customerType == CustomerType.SeatNoiser)
            return assignedSeat != null &&
                   CurrentState == CustomerState.WaitingToOrder;

        return false;
    }

    /// <summary>플레이어가 지금 이 손님의 방해 행동을 해결할 수 있는지 확인합니다.</summary>
    public bool CanResolveDisturbance()
    {
        switch (customerType)
        {
            case CustomerType.QueueNoiser:
                return CurrentState == CustomerState.WaitingAtEntrance;

            case CustomerType.SeatNoiser:
                return assignedSeat != null && CurrentState == CustomerState.WaitingToOrder;

            case CustomerType.SeatHog:
                // 자리 독점은 식사를 마친 뒤 시작되므로 그 전에는 해결할 수 없습니다.
                return CurrentState == CustomerState.OccupyingSeat;

            default:
                return false;
        }
    }

    /// <summary>방해 행동을 끝내고 일반 손님으로 돌리며, 자리 독점 중이었다면 계산대로 보냅니다.</summary>
    public bool ResolveDisturbance()
    {
        if (!CanResolveDisturbance())
            return false;

        CustomerType resolvedType = customerType;
        bool wasOccupyingSeat = CurrentState == CustomerState.OccupyingSeat;

        // 손님 유형을 바꾸고 소음 반복 작업을 멈춰 주변 손님에게 추가 피해가 생기지 않게 합니다.
        customerType = CustomerType.Normal;
        gameObject.name = "Customer_Normal";
        StopNoise();

        // 착석 소음은 해결이 끝나야 주문과 직원 서빙 작업이 생깁니다.
        if (resolvedType == CustomerType.SeatNoiser && CurrentState == CustomerState.WaitingToOrder)
        {
            CurrentState = CustomerState.WaitingForFood;
            PlaceOrder();
        }

        // 식사를 마친 자리 독점 손님은 일반 손님과 같은 계산 및 퇴장 흐름으로 되돌립니다.
        if (wasOccupyingSeat)
            BeginPaymentTrip();

        resolveWork?.RefreshMark();

        Debug.Log($"[Customer] 방해 해결 완료: {resolvedType} 손님이 일반 손님으로 바뀌었습니다.", this);
        return true;
    }

    /// <summary>소음이 리뷰에 영향을 줄 수 있는 대기 또는 좌석 상태인지 확인합니다.</summary>
    private bool CanBeAffectedByNoise()
    {
        return CurrentState == CustomerState.WalkEntrance ||
               CurrentState == CustomerState.WaitingAtEntrance ||
               CurrentState == CustomerState.MovingToSeat ||
               CurrentState == CustomerState.WaitingForFood ||
               CurrentState == CustomerState.Eating;
    }

    private IEnumerator CoEating()
    {
        if (uiBubble != null)
        {
            uiBubble.SetProgress(0f);
        }

        float elapsed = 0f;
        while (elapsed < eatingDuration)
        {
            elapsed += Time.deltaTime;
            float ratio = eatingDuration > 0f ? Mathf.Clamp01(elapsed / eatingDuration) : 1f;

            if (uiBubble != null)
            {
                uiBubble.SetProgress(ratio);
            }

            yield return null;
        }

        if (uiBubble != null)
        {
            uiBubble.SetProgress(1f);
        }

        // 자리 독점형만 식사를 마친 뒤에도 퇴장하지 않고 좌석을 계속 차지합니다.
        if (customerType == CustomerType.SeatHog)
        {
            CurrentState = CustomerState.OccupyingSeat;
            resolveWork?.RefreshMark();
            HideBubble();

            Debug.Log("[Customer] 방해 손님이 식사를 마쳤지만 자리를 계속 독점합니다.");
            yield break;
        }

        // 식사를 마친 일반 손님은 좌석을 비우고 계산대로 이동합니다.
        BeginPaymentTrip();
    }

    /// <summary>식사가 끝난 손님의 좌석을 치우기 대상으로 바꾸고 계산대 이동을 시작합니다.</summary>
    private void BeginPaymentTrip()
    {
        if (CurrentState == CustomerState.MovingToCounter ||
            CurrentState == CustomerState.WaitingToPay ||
            CurrentState == CustomerState.Leaving)
        {
            return;
        }

        CurrentState = CustomerState.MovingToCounter;
        resolveWork?.RefreshMark();

        // 식사가 끝나면 그림을 유지한 채 말풍선 축소 애니메이션을 재생합니다.
        HideBubble();

        // 앉아 있는 동안 꺼 둔 이동 기능을 복구하고 실제 경로가 생긴 뒤 좌석을 비웁니다.
        StartCoroutine(CoMoveToCounter());
    }

    #endregion


    #region 값 지불

    /// <summary>좌석에서 계산대까지 유효한 경로를 찾고 실제 도착했을 때 결제를 요청합니다.</summary>
    private IEnumerator CoMoveToCounter()
    {
        bool seatReleased = false;

        while (CurrentState == CustomerState.MovingToCounter)
        {
            // 카운터가 아직 생성되지 않았거나 이동 경로가 막혔으면 잠시 뒤 다시 시도합니다.
            if (!TryStartCounterMove(out Vector3 destination))
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            if (!seatReleased && assignedSeat != null)
            {
                // 실제 이동을 시작한 시점에만 좌석을 비우고 청소 작업을 등록합니다.
                assignedSeat.SetState(SeatState.Dirty);
                Managers.Station.RegisterDirtyTable(assignedSeat);
                assignedSeat.currentCustomer = null;
                assignedSeat = null;
                seatReleased = true;
            }

            // 이동이 멈추거나 경로가 끊기면 무한 대기하지 않고 다시 길을 찾습니다.
            float moveStartedAt = Time.time;
            while (agent != null && agent.enabled && agent.isOnNavMesh &&
                   (agent.pathPending || agent.remainingDistance > Mathf.Max(0.2f, agent.stoppingDistance + 0.1f)))
            {
                if ((!agent.pathPending && agent.pathStatus != NavMeshPathStatus.PathComplete) ||
                    Time.time - moveStartedAt > 30f)
                    break;

                yield return null;
            }

            // 경로가 중간에 사라져도 계산대 도착으로 간주하지 않습니다.
            if (agent == null || !agent.enabled || !agent.isOnNavMesh || agent.pathPending ||
                agent.pathStatus != NavMeshPathStatus.PathComplete ||
                agent.remainingDistance > Mathf.Max(0.2f, agent.stoppingDistance + 0.1f) ||
                Vector3.Distance(agent.nextPosition, destination) > 0.5f)
            {
                yield return new WaitForSeconds(1f);
                continue;
            }

            break;
        }

        if (CurrentState != CustomerState.MovingToCounter)
            yield break;

        CurrentState = CustomerState.WaitingToPay;
        Debug.Log("카운터에 도착하여 결제를 요청합니다.");

        // 도착 시점의 카운터 참조를 다시 확인해 회전과 대기 등록에 같은 대상을 사용합니다.
        BindCounter();

        // 결제 대기 중임을 결제 이미지로 알리고, 말풍선이 닫혀 있었다면 다시 팝업으로 표시합니다.
        GetBubble()?.SetBubble(Define.BubbleType.Payment);

        // 도착 후 이동 회전을 끄고, 카운터를 향해 제자리에서 부드럽게 회전합니다.
        if (agent != null)
        {
            agent.isStopped = true;
            agent.updateRotation = false;
        }
        yield return CoTurnTo(targetCounter != null ? targetCounter.transform : null);

        // 손님은 돈을 내지 않고, 카운터에서 직원이나 플레이어가 수금할 때까지 기다립니다.
        if (targetCounter != null)
            targetCounter.AddWaitingCustomer(this);
    }

    /// <summary>앉은 위치와 결제 위치를 NavMesh 위로 맞춘 뒤 완전한 이동 경로만 시작합니다.</summary>
    private bool TryStartCounterMove(out Vector3 destination)
    {
        destination = Vector3.zero;
        BindCounter();

        // 계산대가 아직 열리지 않았다면 현재 자리에 머물고 다음 시도에서 다시 찾습니다.
        if (agent == null)
            return false;

        if (targetCounter == null)
            return false;

        if (counterPayPoint == null)
            return false;

        // 손님 위치와 계산대 위치를 각각 NavMesh 위의 실제 이동 지점으로 맞춥니다.
        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit start, 2f, agent.areaMask))
            return false;

        if (!NavMesh.SamplePosition(counterPayPoint.position, out NavMeshHit end, 2f, agent.areaMask))
            return false;

        agent.enabled = true;
        if (!agent.isOnNavMesh)
        {
            if (!agent.Warp(start.position))
                return false;
        }

        if (!agent.isOnNavMesh)
            return false;

        // 목표 근처에 도달 가능한 완전한 경로가 없으면 결제 대기 상태로 넘어가지 않습니다.
        NavMeshPath path = new NavMeshPath();
        if (!agent.CalculatePath(end.position, path) || path.status != NavMeshPathStatus.PathComplete)
            return false;

        agent.updateRotation = true;
        agent.isStopped = false;
        destination = end.position;
        return agent.SetDestination(destination);
    }


    public void CompletePayment()
    {
        // 계산 완료 후 퇴장 상태로 전환
        if (CurrentState != CustomerState.WaitingToPay)
            return;

        CurrentState = CustomerState.Leaving;
        HideBubble();

        StartCoroutine(CoCheckExit());
    }

    /// <summary>
    /// 현재 높이를 유지한 채 목표 위치의 수평 방향을 향하도록 천천히 회전합니다.
    /// </summary>
    private IEnumerator CoTurnTo(Transform target)
    {
        // 목표가 없거나 방향 차이가 거의 없으면 회전 트윈을 만들지 않습니다.
        if (target == null)
            yield break;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            yield break;

        // 캐릭터의 상하 기울기는 바꾸지 않고 수평 회전만 부드럽게 적용합니다.
        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        Tween turnTween = transform
            .DORotateQuaternion(targetRotation, turnDuration)
            .SetEase(Ease.OutSine);

        yield return turnTween.WaitForCompletion();
    }

    // 나가는 코루틴
    private IEnumerator CoCheckExit()
    {
        // 퇴장하기 전에 제자리에서 출구를 바라보고, 회전이 끝난 뒤 이동을 시작합니다.
        if (agent != null && agent.enabled && agent.isOnNavMesh && exitPoint != null)
        {
            agent.isStopped = true;
            agent.updateRotation = false;
            yield return CoTurnTo(exitPoint);

            agent.SetDestination(exitPoint.position);
            agent.updateRotation = true;
            agent.isStopped = false;
        }

        yield return CoWaitUntilArrival(0.5f);

        Debug.Log("손님이 퇴장하여 오브젝트가 정리됩니다.");
        Managers.Resource.Destroy(gameObject);
    }

    /// <summary>표시가 필요한 순간에만 공용 말풍선을 빌려 손님의 기준점에 연결합니다.</summary>
    private UI_Bubble GetBubble()
    {
        if (uiBubble != null)
            return uiBubble;

        // 닫힘 애니메이션 중 다시 표시해야 하면 새 말풍선을 만들지 않고 같은 것을 사용합니다.
        if (hidingBubble != null)
        {
            uiBubble = hidingBubble;
            hidingBubble = null;
            return uiBubble;
        }

        if (Managers.Instance == null)
            return null;

        uiBubble = Managers.UI.MakeSubItem<UI_Bubble>();
        if (uiBubble != null)
            uiBubble.SetAnchor(bubbleAnchor != null ? bubbleAnchor : transform);

        return uiBubble;
    }

    /// <summary>말풍선을 닫고 애니메이션이 끝나면 소유 관계를 풀에 돌려줍니다.</summary>
    private void HideBubble()
    {
        if (uiBubble == null)
            return;

        UI_Bubble bubble = uiBubble;
        uiBubble = null;
        hidingBubble = bubble;

        // 손님이 표시 도중 다시 말풍선을 쓰면 이전 닫힘 완료 알림은 반환하지 않습니다.
        bubble.Hide(() =>
        {
            if (hidingBubble != bubble)
                return;

            hidingBubble = null;
            ReleaseBubble(bubble);
        });
    }

    /// <summary>말풍선의 그림과 애니메이션을 초기화하고 UI 풀에 반납합니다.</summary>
    private static void ReleaseBubble(UI_Bubble bubble)
    {
        if (bubble == null || Managers.Instance == null)
            return;

        bubble.ResetForPool();
        Managers.Pool.Release(bubble.gameObject);
    }

    #endregion
}
