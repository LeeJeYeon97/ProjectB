using UnityEngine;

/// <summary>
/// 상호작용 오브젝트의 공통 동작입니다. 오브젝트의 Trigger Collider가 플레이어를
/// 감지하면 하위 클래스가 제공하는 규칙에 따라 자동으로 상호작용합니다.
/// </summary>
public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    private GameObject currentActor;
    private GameObject candidateActor;
    private float timer;
    private bool actorInRange;

    public abstract float Duration { get; }

    public abstract bool CanInteract(GameObject actor);

    public virtual void OnStartInteract(GameObject actor) { }

    public virtual void OnUpdateInteract(GameObject actor, float progress) { }

    public abstract void OnCompleteInteract(GameObject actor);

    public virtual void OnCancelInteract(GameObject actor) { }

    protected virtual void Awake()
    {
        Collider interactionCollider = GetComponent<Collider>();
        if (interactionCollider == null || !interactionCollider.isTrigger)
        {
            Debug.LogError($"{name}: 상호작용 감지를 위해 Trigger Collider가 필요합니다.", this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
            return;

        actorInRange = true;
        candidateActor = player.gameObject;
        TryBeginInteraction(player.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
            return;

        // Player의 자식 Collider가 여러 개일 수 있어, 실제 퇴장은 위치로 확인합니다.
        if (!IsActorInside(player))
        {
            actorInRange = false;
            candidateActor = null;
            if (currentActor == player.gameObject)
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

        if (!CanInteract(currentActor))
        {
            CancelInteraction();
            return;
        }

        if (Duration <= 0f)
        {
            OnCompleteInteract(currentActor);
            CancelInteraction();
            return;
        }

        timer += Time.deltaTime;
        OnUpdateInteract(currentActor, Mathf.Clamp01(timer / Duration));
        if (timer >= Duration)
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

        if (Duration <= 0f)
        {
            OnCompleteInteract(currentActor);
            CancelInteraction();
        }
    }

    private bool IsActorInside(PlayerController player)
    {
        Collider interactionCollider = GetComponent<Collider>();
        if (interactionCollider == null)
            return false;

        Collider[] colliders = player.GetComponentsInChildren<Collider>();
        foreach (Collider playerCollider in colliders)
        {
            if (playerCollider != null && playerCollider.enabled &&
                interactionCollider.bounds.Intersects(playerCollider.bounds))
                return true;
        }
        return false;
    }

    private void CancelInteraction()
    {
        if (currentActor != null)
            OnCancelInteract(currentActor);

        currentActor = null;
        timer = 0f;
    }

    protected virtual void OnDisable()
    {
        CancelInteraction();
        actorInRange = false;
        candidateActor = null;
    }

    protected static bool TryGetPlayer(GameObject actor, out PlayerController player)
    {
        player = actor != null ? actor.GetComponent<PlayerController>() : null;
        return player != null;
    }
}
