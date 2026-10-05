using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상호작용 오브젝트의 공통 동작입니다. 오브젝트의 Trigger Collider가 플레이어를
/// 감지하면 하위 클래스가 제공하는 규칙에 따라 자동으로 상호작용합니다.
/// </summary>
public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    private GameObject currentActor;
    private GameObject candidateActor;
    // 플레이어나 직원이 이 대상을 먼저 사용 중인지 기억합니다.
    private GameObject reservedActor;
    private readonly HashSet<Collider> playerCollidersInside = new HashSet<Collider>();
    private float timer;
    private bool actorInRange;
    private bool progressBubbleActive;
    private bool taskProgressBubbleActive;

    [Header("Interaction Progress UI")]
    [SerializeField] protected UI_Bubble uiBubble;

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
        return Duration * scale;
    }

    public abstract bool CanInteract(GameObject actor);

    public virtual void OnStartInteract(GameObject actor) { }

    public virtual void OnUpdateInteract(GameObject actor, float progress) { }

    public abstract void OnCompleteInteract(GameObject actor);

    public virtual void OnCancelInteract(GameObject actor) { }

    protected virtual void OnInteractionEnded() { }

    /// <summary>캐릭터가 상호작용 범위를 완전히 벗어났을 때 하위 동작을 정리합니다.</summary>
    protected virtual void OnActorLeftRange(GameObject actor) { }

    /// <summary>플레이어가 직접 작업을 시작할 때 하위 대상이 직원 예약을 정리하도록 알립니다.</summary>
    protected virtual void OnPlayerStartingInteraction(GameObject actor) { }

    protected virtual void Awake()
    {
        Collider interactionCollider = GetComponent<Collider>();
        if (interactionCollider == null || !interactionCollider.isTrigger)
        {
            Debug.LogError($"{name}: 상호작용 감지를 위해 Trigger Collider가 필요합니다.", this);
        }

        if (uiBubble == null)
            uiBubble = GetComponentInChildren<UI_Bubble>(true);

        if (uiBubble != null)
        {
            uiBubble.Hide();
            progressBubbleActive = false;
        }
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
    }

    /// <summary>이미 연결된 말풍선을 찾거나 진행 말풍선 프리팹을 만들어 준비합니다.</summary>
    private bool EnsureProgressBubble()
    {
        if (uiBubble == null)
            uiBubble = GetComponentInChildren<UI_Bubble>(true);

        if (uiBubble == null)
        {
            GameObject bubblePrefab = Managers.Resource.Load<GameObject>("UI_Bubble");
#if UNITY_EDITOR
            if (bubblePrefab == null)
                bubblePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03_Prefabs/UI/SubItem/UI_Bubble.prefab");
#endif
            if (bubblePrefab != null)
            {
                GameObject bubbleObject = Managers.Resource.Instantiate(bubblePrefab, transform);
                bubbleObject.name = "UI_Bubble";
                uiBubble = bubbleObject.GetComponent<UI_Bubble>();
                bubbleObject.transform.localScale = Vector3.one * 0.01f;
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
        playerCollidersInside.Clear();
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
