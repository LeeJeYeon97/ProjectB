using DG.Tweening;
using UnityEngine;

/// <summary>플레이어가 들고 있는 음식만 휴지통으로 옮겨 폐기합니다.</summary>
public class TrashCan : InteractableBase
{
    #region 폐기 설정

    [Header("음식 폐기 설정")]
    [Tooltip("음식을 한 개씩 버리는 시간 간격입니다.")]
    [SerializeField, Min(0.01f)] private float throwInterval = 0.2f;
    public override float Duration => throwInterval;

    [Tooltip("음식이 날아가서 사라지는 휴지통 안쪽 위치입니다.")]
    [SerializeField] private Transform trashPoint;

    [Tooltip("음식이 휴지통까지 날아가는 시간입니다.")]
    [SerializeField, Min(0.01f)] private float flyDuration = 0.45f;

    [Tooltip("음식이 날아갈 때 위로 솟는 높이입니다.")]
    [SerializeField, Min(0f)] private float jumpHeight = 0.8f;

    // 빠른 반복 폐기에서는 매번 진행 말풍선을 만들지 않습니다.
    protected override bool UsesProgressBubble => false;

    #endregion

    #region 폐기 상호작용

    /// <summary>플레이어가 음식 아이템을 맨 위에 들고 있을 때만 폐기를 시작합니다.</summary>
    public override bool CanInteract(GameObject actor)
    {
        if (!TryGetPlayer(actor, out PlayerController player)) return false;
        if (player.Stacking == null) return false;
        if (player.Stacking.StackCount == 0) return false;
        if (trashPoint == null) return false;

        // 음식 이외의 물건은 제거하지 않고 그대로 둡니다.
        return player.Stacking.GetFirstListItemType() == Define.ItemType.Menu;
    }

    /// <summary>폐기 간격은 플레이어의 작업 속도 성장과 관계없이 설정한 값으로 유지합니다.</summary>
    protected override float GetDuration(GameObject actor)
    {
        return Mathf.Max(0.01f, throwInterval);
    }

    /// <summary>맨 위 음식을 한 개 꺼내 휴지통으로 날리고 도착하면 제거합니다.</summary>
    public override void OnCompleteInteract(GameObject actor)
    {
        // 작업 중 스택이 바뀌었을 수 있으므로 꺼내기 직전에 음식인지 다시 확인합니다.
        if (!CanInteract(actor)) return;
        if (!TryGetPlayer(actor, out PlayerController player)) return;

        GameObject item = player.Stacking.TakeTopItem();
        if (item == null) return;

        // 도착점의 현재 위치를 보관해 휴지통이 비활성화되어도 이미 던진 음식은 마무리합니다.
        Vector3 destination = trashPoint.position;
        bool removed = false;
        item.transform.DOJump(destination, jumpHeight, 1, Mathf.Max(0.01f, flyDuration))
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // 폐기한 음식의 재료는 현재 기획에 따라 돌려주지 않습니다.
                removed = true;
                RemoveItem(item);
            })
            .OnKill(() =>
            {
                // 외부에서 이동 연출을 중단해도 스택에서 분리된 음식이 씬에 남지 않게 정리합니다.
                if (!removed) RemoveItem(item);
            });
    }

    /// <summary>풀링 아이템은 기존 리소스 관리자의 반환 경로로 처리합니다.</summary>
    private void RemoveItem(GameObject item)
    {
        if (item == null) return;

        // 이미 다른 곳에서 풀에 반환한 오브젝트를 중복 반환하지 않습니다.
        Poolable poolable = item.GetComponent<Poolable>();
        if (poolable != null && poolable.IsPooled && !poolable.IsUsing) return;
        Managers.Resource.Destroy(item);
    }

    #endregion
}
