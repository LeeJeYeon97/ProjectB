using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using static Define;

[RequireComponent(typeof(NavMeshAgent))]
public class CustomerController : MonoBehaviour
{
    // 결제 이벤트 (손님이 지불한 금액 전달)
    public event Action<int> OnPayment;

    [Header("State")]
    public CustomerState CurrentState = CustomerState.None;

    [SerializeField]
    private ItemType orderedType;
    public ItemType OrderedType => orderedType;

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


    [Header("UI")]
    [SerializeField] private UI_InteractionProgress progressUI;
    [SerializeField] private Vector3 uiOffset = new Vector3(0f, 1.8f, 0f);
    [SerializeField] private UI_Bubble uiBubble;

    #region 초기화

    public void Init(Transform entrancePoint, Transform exit)
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>(); // agent 할당 확인
        if (animator == null) animator = GetComponent<Animator>(); // animator 할당 확인

        if (uiBubble != null)
        {
            uiBubble.gameObject.SetActive(false); // 초기에는 말풍선 비활성화
        }

        if (progressUI == null)
        {
            progressUI = GetComponentInChildren<UI_InteractionProgress>(true);
        }

        if (progressUI != null)
        {
            progressUI.transform.localPosition = uiOffset;
            progressUI.gameObject.SetActive(false); // 초기에는 프로그레스바 비활성화
        }

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

        // 3. 정지 해제 및 경로 지정 성공 여부 확인
        agent.isStopped = false;
        bool hasPath = agent.SetDestination(entrancePoint.position);

        if (!hasPath)
        {
            Debug.LogError($"[NavMesh] {entrancePoint.name} 위치까지의 경로를 계산할 수 없습니다. 목적지가 NavMesh 구역 내에 있는지 확인하세요.");
        }

        // 초기 생성 시 입구 대기 위치로 이동
        agent.destination = entrancePoint.position;
        StartCoroutine(CoCheckArrivalAtEntrance());
    }


    private void OnDestroy()
    {
        if (targetCounter != null)
        {
            OnPayment -= targetCounter.ReceivePayment;
        }
    }

    private void BindCounter()
    {
        targetCounter = Managers.Game.Counter;
        counterPayPoint = Managers.Game.Counter.PayPoint;
        
        if (targetCounter != null)
        {
            OnPayment -= targetCounter.ReceivePayment; // 중복 구독 방지
            OnPayment += targetCounter.ReceivePayment;
        }
    }

    #endregion

    private void Update()
    {
        // 3. 매 프레임 에이전트의 실제 속도(velocity)를 애니메이터의 Speed 파라미터에 전달
        if (animator != null && agent != null)
        {
            // 캐릭터가 움직이는 속도를 파라미터로 넘겨주어 IDLE -> WALK 전환을 트리거합니다.
            animator.SetFloat("Speed", agent.velocity.magnitude);
        }
    }
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
    private IEnumerator CoCheckArrivalAtEntrance()
    {
        yield return CoWaitUntilArrival(0.2f);

        if (CurrentState == CustomerState.WalkEntrance)
        {
            CurrentState = CustomerState.WaitingAtEntrance;
        }
    }
    #endregion


    #region 손님이 입구에서 좌석으로 들어가는 코드
    // 매니저가 손님에게 자리를 지정해 줄 때 호출
    public void AssignSeat(SeatStation seat)
    {
        assignedSeat = seat;
        assignedSeat.CurrentState = SeatState.Reserved;
        assignedSeat.currentCustomer = this;

        if (CurrentState != CustomerState.WalkEntrance)
        {
            CurrentState = CustomerState.MovingToSeat;
            if (agent != null)
            {
                agent.enabled = true;
                agent.isStopped = false;
                agent.SetDestination(seat.customerSitPoint.position);
            }
        }

        StartCoroutine(CoCheckArrivalAtSeat());
    }
    // 빈 자리로 가는 코루틴
    private IEnumerator CoCheckArrivalAtSeat()
    {
        // WalkEntrance 상태라면 WaitingAtEntrance 상태가 될 때까지 대기
        while (CurrentState == CustomerState.WalkEntrance)
        {
            yield return null;
        }

        // 입구 도착 후 좌석으로 이동 시작
        CurrentState = CustomerState.MovingToSeat;
        if (agent != null && assignedSeat != null && assignedSeat.customerSitPoint != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
            agent.SetDestination(assignedSeat.customerSitPoint.position);
        }

        // 목적지 도착 대기
        yield return CoWaitUntilArrival(0.2f);

        // 4. 자리에 착석 및 자동 주문 (음식 대기 상태로 바로 진입)
        // CurrentState = CustomerState.WaitingToOrder;
        CurrentState = CustomerState.WaitingForFood;
        orderedType = ItemType.Beer;

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

        if (uiBubble != null)
        {
            uiBubble.gameObject.SetActive(true); // 말풍선 활성화
            uiBubble.SetImage();
        }

        // TODO: 앉는 애니메이션 재생 (Animator.SetTrigger("Sit"))
        Debug.Log("손님이 자리에 앉아 자동으로 주문을 넣었습니다. (음식 대기 중)");
    }
    #endregion

    
    #region 음식받고 음식 먹는 코드
    public void OnFoodServed()
    {
        CurrentState = CustomerState.Eating;
        // TODO: 말풍선 비활성화, 식사 애니메이션 작동
        
        if (uiBubble != null)
        {
            uiBubble.gameObject.SetActive(false); // 말풍선 비활성화
        }

        Debug.Log("밥 먹습니다..");
        StartCoroutine(CoEating());
    }

    private IEnumerator CoEating()
    {
        if (progressUI != null)
        {
            progressUI.gameObject.SetActive(true);
            progressUI.SetVisible(true);
            progressUI.SetProgress(0f);
        }

        float elapsed = 0f;
        while (elapsed < eatingDuration)
        {
            elapsed += Time.deltaTime;
            float ratio = eatingDuration > 0f ? Mathf.Clamp01(elapsed / eatingDuration) : 1f;

            if (progressUI != null)
            {
                progressUI.SetProgress(ratio);
            }

            yield return null;
        }

        if (progressUI != null)
        {
            progressUI.SetProgress(1f);
            progressUI.gameObject.SetActive(false);
        }

        // 식사 끝, 계산하러 이동 준비
        CurrentState = CustomerState.MovingToCounter;
        if (agent != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
        }

        // 자리를 "치워야 하는 상태"로 전환
        if (assignedSeat != null)
        {
            assignedSeat.CurrentState = SeatState.Dirty;
            assignedSeat.currentCustomer = null;
            assignedSeat = null;
        }

        if (counterPayPoint != null && agent != null)
        {
            agent.SetDestination(counterPayPoint.position);
        }

        StartCoroutine(CoCheckArrivalAtCounter());
    }

    #endregion


    #region 값 지불

    // 카운터로 가는 코루틴
    private IEnumerator CoCheckArrivalAtCounter()
    {
        yield return CoWaitUntilArrival(0.2f);

        CurrentState = CustomerState.WaitingToPay;
        Debug.Log("카운터에 도착하여 결제를 요청합니다.");

        // 일정 시간 후 결제 처리 (또는 카운터 발판에 플레이어가 상호작용하면 결제하게 할 수도 있음)
        yield return new WaitForSeconds(0.5f);
        Pay();
    }


    private void Pay()
    {
        // 카운터 바인딩 재확인 (런타임 생성 대비)
        BindCounter();

        // 카운터로 결제 이벤트 전송 (카운터가 지정된 위치에 돈을 생성)
        OnPayment?.Invoke(payAmount);

        // 계산 완료 후 퇴장 상태로 전환
        CurrentState = CustomerState.Leaving;
        if (agent != null && exitPoint != null)
        {
            agent.isStopped = false;
            agent.SetDestination(exitPoint.position);
        }

        StartCoroutine(CoCheckExit());
    }
    // 나가는 코루틴
    private IEnumerator CoCheckExit()
    {
        yield return CoWaitUntilArrival(0.5f);

        Debug.Log("손님이 퇴장하여 오브젝트가 정리됩니다.");
        Managers.Resource.Destroy(gameObject);
    }

    #endregion
}