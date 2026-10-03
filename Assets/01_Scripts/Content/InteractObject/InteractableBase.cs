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
    private readonly HashSet<Collider> playerCollidersInside = new HashSet<Collider>();
    private float timer;
    private bool actorInRange;
    private bool progressBubbleActive;

    [Header("Interaction Progress UI")]
    [SerializeField] protected UI_Bubble uiBubble;

    /// <summary>진행 말풍선이 필요 없는 상호작용은 하위 클래스에서 끌 수 있습니다.</summary>
    protected virtual bool UsesProgressBubble => true;

    public abstract float Duration { get; }

    /// <summary>캐릭터와 현재 상태에 따라 상호작용 시간을 다르게 정합니다.</summary>
    protected virtual float GetDuration(GameObject actor) => Duration;

    public abstract bool CanInteract(GameObject actor);

    public virtual void OnStartInteract(GameObject actor) { }

    public virtual void OnUpdateInteract(GameObject actor, float progress) { }

    public abstract void OnCompleteInteract(GameObject actor);

    public virtual void OnCancelInteract(GameObject actor) { }

    protected virtual void OnInteractionEnded() { }

    /// <summary>캐릭터가 상호작용 범위를 완전히 벗어났을 때 하위 동작을 정리합니다.</summary>
    protected virtual void OnActorLeftRange(GameObject actor) { }

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
        if (actor == null || currentActor != null || !CanInteract(actor))
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
                GameObject bubbleObject = Instantiate(bubblePrefab, transform);
                bubbleObject.name = "UI_Bubble";
                uiBubble = bubbleObject.GetComponent<UI_Bubble>();
                bubbleObject.transform.localScale = Vector3.one * 0.01f;
            }
        }

        if (uiBubble == null)
            return;

        if (!progressBubbleActive)
        {
            uiBubble.gameObject.SetActive(true);
            uiBubble.SetBubble(Define.BubbleType.Progress);
            progressBubbleActive = true;
        }
        uiBubble.SetProgress(progress);
    }

    protected virtual void OnDisable()
    {
        CancelInteraction();
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
