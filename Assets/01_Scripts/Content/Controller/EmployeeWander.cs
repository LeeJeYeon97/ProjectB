using UnityEngine;
using UnityEngine.AI;

/// <summary>직원이 한가할 때 NavMesh 위를 서성이는 움직임만 관리합니다.</summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EmployeeWander : MonoBehaviour
{
    [Header("서성임 설정")]
    [Tooltip("직원이 한가할 때 서성일지 설정합니다. 테스트 중에는 끌 수 있습니다.")]
    [SerializeField]
    private bool wanderingEnabled = true;

    [Tooltip("서성임의 중심 지점입니다. 비워 두면 직원이 고용된 위치를 사용합니다.")]
    [SerializeField]
    private Transform wanderCenter;

    [Tooltip("중심 지점에서 무작위 목적지를 고를 때 사용하는 최대 거리입니다.")]
    [SerializeField, Min(1f)]
    private float wanderRadius = 8f;

    [Tooltip("목적지에 도착한 뒤 다음 이동을 시작하기 전 기다리는 시간 범위입니다.")]
    [SerializeField]
    private Vector2 wanderWaitRange = new Vector2(2f, 5f);

    private NavMeshAgent agent;
    private Vector3 centerPosition;
    private float waitTimer;
    private bool initialized;

    /// <summary>서성임을 테스트 중에 켜거나 끌 수 있습니다.</summary>
    public bool WanderingEnabled
    {
        get => wanderingEnabled;
        set => wanderingEnabled = value;
    }

    /// <summary>직원이 고용되거나 재사용될 때 서성임 기준 위치와 대기 시간을 설정합니다.</summary>
    public void Initialize()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        // 별도 중심 지점이 있으면 사용하고, 없으면 현재 직원 위치를 중심으로 저장합니다.
        centerPosition = wanderCenter != null ? wanderCenter.position : transform.position;
        waitTimer = Random.Range(wanderWaitRange.x, wanderWaitRange.y);
        initialized = true;
    }

    /// <summary>직원이 대기 상태일 때 한 번 호출해 이동이나 정지를 처리합니다.</summary>
    public void Tick()
    {
        if (!initialized)
            Initialize();

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        // 기능을 끄면 현재 서성임 목적지도 지워 직원이 그 자리에서 대기하게 합니다.
        if (!wanderingEnabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
            return;
        }

        // 경로 계산 중일 때는 목적지를 바꾸지 않고, 현재 목적지까지 계속 이동합니다.
        if (agent.pathPending)
            return;

        agent.isStopped = false;
        if (agent.hasPath && agent.remainingDistance > agent.stoppingDistance + 0.15f)
            return;

        // 목적지에 도착한 뒤 설정한 시간만큼 쉬었다가 다음 목적지를 고릅니다.
        waitTimer -= Time.deltaTime;
        if (waitTimer > 0f)
            return;

        if (MoveToRandomPoint())
            waitTimer = Random.Range(wanderWaitRange.x, wanderWaitRange.y);
    }

    /// <summary>중심 주변에서 도달 가능한 NavMesh 목적지를 찾아 이동합니다.</summary>
    private bool MoveToRandomPoint()
    {
        // 무작위 위치가 NavMesh 밖이거나 장애물로 막혀 있으면 다른 후보를 시도합니다.
        for (int attempt = 0; attempt < 8; attempt++)
        {
            Vector2 offset = Random.insideUnitCircle * wanderRadius;
            Vector3 wantedPoint = centerPosition + new Vector3(offset.x, 0f, offset.y);

            if (!NavMesh.SamplePosition(wantedPoint, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                continue;

            NavMeshPath path = new NavMeshPath();
            if (!agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete)
                continue;

            agent.SetDestination(hit.position);
            return true;
        }

        // 갈 수 있는 위치를 못 찾으면 잠시 기다린 뒤 다시 시도합니다.
        waitTimer = 1f;
        return false;
    }
}
