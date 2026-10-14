using UnityEngine;

/// <summary>방해 손님 곁에 머무는 플레이어의 해결 작업과 진행 표시를 관리합니다.</summary>
[RequireComponent(typeof(SphereCollider))]
public class CustomerResolve : InteractableBase
{
    protected override Define.InteractionType DefaultInteractionType => Define.InteractionType.ResolveCustomer;
    [SerializeField] private CustomerController customer;
    [SerializeField, Min(0.1f)] private float workDuration = 2.5f;

    /// <summary>방해를 해결하는 데 필요한 기본 작업 시간입니다.</summary>
    public override float Duration => workDuration;

    /// <summary>빈손으로 접근한 플레이어가 현재 방해 중인 손님만 도울 수 있게 합니다.</summary>
    public override bool CanInteract(GameObject actor)
    {
        if (!TryGetPlayer(actor, out PlayerController player) ||
            player.Stacking == null || player.Stacking.StackCount > 0)
        {
            return false;
        }

        return customer != null && customer.CanResolveDisturbance();
    }

    /// <summary>진행 시간이 채워지면 손님의 방해 행동을 끝냅니다.</summary>
    public override void OnCompleteInteract(GameObject actor)
    {
        if (CanInteract(actor))
            customer.ResolveDisturbance();
    }

    /// <summary>작업 가능한 방해 손님이면 느낌표를, 해결했거나 이동 중이면 숨깁니다.</summary>
    public void RefreshMark()
    {
        if (IsWorking)
            return;

        if (customer != null && customer.CanResolveDisturbance())
            ShowStatusBubble(Define.BubbleType.Disturbance);
        else
            HideStatusBubble();
    }

    /// <summary>공용 말풍선 프리팹이 로드된 뒤 방해 손님의 표시를 다시 계산합니다.</summary>
    public override void RefreshStatusBubble() => RefreshMark();

    /// <summary>작업이 취소되면 남은 방해 행동을 다시 표시합니다.</summary>
    protected override void OnInteractionEnded()
    {
        RefreshMark();
    }

    /// <summary>프리팹에서 참조와 트리거 설정이 빠지지 않게 보완합니다.</summary>
    private void OnValidate()
    {
        if (customer == null)
            customer = GetComponentInParent<CustomerController>();

        SphereCollider area = GetComponent<SphereCollider>();
        if (area != null)
            area.isTrigger = true;
    }
}
