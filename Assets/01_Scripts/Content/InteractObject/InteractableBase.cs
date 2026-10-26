using UnityEngine;

/// <summary>
/// 상호작용 오브젝트의 공통 동작입니다. 오브젝트의 Trigger Collider가 플레이어를
/// 감지하면 하위 클래스가 제공하는 규칙에 따라 자동으로 상호작용합니다.
/// </summary>
public abstract class InteractableBase : MonoBehaviour
{
    private int playerLayer;

    [Header("상호작용 종류")]
    [SerializeField] private Define.InteractionType interactionType;

    private GameObject currentActor;
    private GameObject candidateActor;
    // 플레이어나 직원이 이 대상을 먼저 사용 중인지 기억합니다.
    private GameObject reservedActor;
    private float timer;
    private bool actorInRange;
    private bool progressBubbleActive;
    private bool taskProgressBubbleActive;

    [Header("공용 상호작용 UI")]
    protected UI_Bubble uiBubble;
    [Tooltip("말풍선이 따라갈 위치입니다. 프리팹 안에 BubbleAnchor 자식 오브젝트를 만들어 연결합니다.")]
    [SerializeField] private Transform bubbleAnchor;



    /// <summary>리소스가 준비된 뒤 각 상호작용 객체가 필요한 상태 표시를 갱신합니다.</summary>
    public virtual void RefreshStatusBubble() { }

    /// <summary>진행 말풍선이 필요 없는 상호작용은 하위 클래스에서 끌 수 있습니다.</summary>
    protected virtual bool UsesProgressBubble => true;

    public abstract float Duration { get; }

    /// <summary>플레이어나 직원이 이 대상을 맡아 작업 중인지 알려줍니다.</summary>
    public bool IsWorking => reservedActor != null;

    /// <summary>현재 대상을 맡은 작업 주체를 확인합니다.</summary>
    public GameObject WorkingActor => reservedActor;



    /// <summary>상호작용 가능 여부는 각 하위 객체가 자신의 규칙으로 판단합니다.</summary>
    public virtual bool CanInteract(GameObject actor) => false;

    /// <summary>상호작용 시작 시 하위 객체가 필요한 처리를 수행합니다.</summary>
    public virtual void OnStartInteract(GameObject actor) { }

    public virtual void OnUpdateInteract(GameObject actor, float progress) { }

    /// <summary>상호작용 완료 시 하위 객체가 필요한 결과 처리를 수행합니다.</summary>
    public virtual void OnCompleteInteract(GameObject actor) { }

    public virtual void OnCancelInteract(GameObject actor) { }

    /// <summary>상호작용이 종료되거나 취소된 뒤 하위 객체가 상태를 정리합니다.</summary>
    protected virtual void OnInteractionEnded() { }

    /// <summary>캐릭터가 상호작용 범위를 완전히 벗어났을 때 하위 동작을 정리합니다.</summary>
    protected virtual void OnActorLeftRange(GameObject actor) { }

    /// <summary>플레이어가 작업을 시작할 때 하위 객체에 선점 정리를 알립니다.</summary>
    protected virtual void OnPlayerStartingInteraction(GameObject actor) { }


    private void Awake()
    {
        playerLayer = LayerMask.NameToLayer("Player");
    }
    protected static bool TryGetPlayer(GameObject actor, out PlayerController player)
    {
        player = actor != null ? actor.GetComponent<PlayerController>() : null;
        return player != null;
    }

    /// <summary>상호작용 범위에 들어온 콜라이더가 속한 플레이어나 직원을 찾습니다.</summary>
    protected GameObject GetActor(Collider other)
    {
        if (other == null)
            return null;

        // 플레이어는 Player Layer의 콜라이더만 인정해 스택용 자식 콜라이더를 제외합니다.
        if (other.gameObject.layer == playerLayer)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null)
                return player.gameObject;
        }

        // 직원 프리팹은 현재 Default Layer를 사용하므로 컴포넌트로 별도 확인합니다.
        EmployeeController employee = other.GetComponentInParent<EmployeeController>();
        return employee != null ? employee.gameObject : null;
    }

    /// <summary>리소스가 준비된 뒤 현재 상태 말풍선을 갱신합니다.</summary>
    protected virtual void OnEnable()
    {
        RefreshStatusBubble();
    }

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
        return Duration * scale;
    }

    private void OnTriggerEnter(Collider other)
    {
        GameObject actor = GetActor(other);
        if (actor == null)
            return;

        actorInRange = true;
        candidateActor = actor;
        TryBeginInteraction(actor);
    }

    /// <summary>상호작용 범위 안에 머무는 작업자의 기계별 반복 처리를 수행합니다.</summary>
    protected virtual void OnTriggerStay(Collider other)
    {
        GameObject actor = GetActor(other);
        if (actor == null || !isActiveAndEnabled)
            return;

        // Layer가 허용된 작업자가 계속 범위 안에 있음을 갱신합니다.
        actorInRange = true;
        candidateActor = actor;
    }

    private void OnTriggerExit(Collider other)
    {
        GameObject actor = GetActor(other);
        if (actor == null)
            return;

        // 현재 범위 후보나 작업자와 다른 캐릭터의 퇴장 이벤트는 무시합니다.
        if (actor != candidateActor && actor != currentActor)
            return;

        actorInRange = false;
        candidateActor = null;
        OnActorLeftRange(actor);
        if (currentActor == actor)
            CancelInteraction();
    }

    private void Update()
    {
        // 캐릭터가 파괴되거나 비활성화되면 물리 퇴장 이벤트가 오지 않을 수 있어 범위를 정리합니다.
        if (actorInRange && (candidateActor == null || !candidateActor.activeInHierarchy))
        {
            GameObject actor = candidateActor;
            actorInRange = false;
            candidateActor = null;
            if (actor != null)
                OnActorLeftRange(actor);
        }

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
        CancelInteraction();
        reservedActor = null;
        HideTaskProgress();
        actorInRange = false;
        candidateActor = null;

        // 객체가 꺼지면 말풍선을 초기화하고 공용 풀에 돌려 다른 객체가 재사용합니다.
        if (uiBubble != null && Managers.Instance != null)
        {
            uiBubble.ResetForPool();
            Managers.Pool.Release(uiBubble.gameObject);
        }
        uiBubble = null;
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
}
