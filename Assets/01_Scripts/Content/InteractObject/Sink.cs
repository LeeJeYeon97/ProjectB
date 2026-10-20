using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Sink : InteractableBase
{

    [Header("설거지 설정")]
    [SerializeField] private float workDuration = 3.0f;
    [SerializeField] private int processAmount = 1;

    [Header("깨끗한 잔 진열 설정")]
    [SerializeField] private Transform cleanDishDisplayPoint;
    [SerializeField] private Vector3 cleanDishDisplayOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private float displayHeight = 0.15f;

    [Header("깨끗한 잔 회수")]
    [SerializeField, Min(0f)] private float transferInterval = 0.5f;

    // 진열 중인 잔 오브젝트를 보관해, 플레이어가 가져갈 때 실제 오브젝트를 넘깁니다.
    private readonly List<GameObject> displayedDishes = new List<GameObject>();
    private int pendingDisplayCount;
    private float nextTransferTime;

    /// <summary>싱크대에서 설거지를 하는 데 걸리는 기본 시간입니다.</summary>
    public override float Duration => workDuration;

    protected override void OnEnable()
    {
        base.OnEnable();

        // 싱크대가 나중에 열려도 이미 쌓인 더러운 접시를 작업 대기열에 올립니다.
        if (Managers.Instance != null)
            Managers.Task.RegisterDishWashTask();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        nextTransferTime = 0f;

        // 비활성화된 싱크대가 재고 변경 알림을 계속 받지 않게 합니다.
        if (Managers.Instance != null)
            Managers.Task.RemoveTasksFor(this);
    }

    /// <summary>설거지에는 플레이어의 작업 속도 업그레이드를 적용합니다.</summary>
    protected override float GetDuration(GameObject actor)
    {
        if (!TryGetPlayer(actor, out PlayerController player))
            return Duration;

        // 플레이어가 설거지 속도를 올렸다면 실제 작업 시간에도 같은 배율을 적용합니다.
        return Duration * player.InteractionTimeScale;
    }

    /// <summary>재고에 기록된 깨끗한 잔을 싱크대에 진열합니다.</summary>
    private void Start()
    {
        if (Managers.Dish == null)
            return;

        for (int i = 0; i < Managers.Dish.CleanDishCount; i++)
            CreateDisplayedDish();
    }

    /// <summary>설거지 구역에서는 더러운 잔을 들고 있는 플레이어만 작업합니다.</summary>
    public override bool CanInteract(GameObject actor)
    {
        if (!TryGetPlayer(actor, out PlayerController player) ||
            player.Stacking == null || Managers.Dish == null)
        {
            return false;
        }

        // 진열된 잔은 별도의 회수 구역에서 가져가므로 이 범위는 설거지만 담당합니다.
        return player.Stacking.GetFirstListItemType() == Define.ItemType.Beer_Dirty;
    }

    /// <summary>설거지를 마치면 더러운 잔을 소비하고 깨끗한 잔을 싱크대에 진열합니다.</summary>
    public override void OnCompleteInteract(GameObject actor)
    {
        if (!TryGetPlayer(actor, out PlayerController player) ||
            player.Stacking == null || Managers.Dish == null)
        {
            return;
        }

        // 플레이어가 들고 있는 더러운 잔은 직원용 공용 재고와 별도로 처리합니다.
        if (player.Stacking.GetFirstListItemType() == Define.ItemType.Beer_Dirty)
        {
            int playerWashAmount = Mathf.Min(processAmount, player.Stacking.StackCount);
            if (playerWashAmount <= 0)
                return;

            for (int i = 0; i < playerWashAmount; i++)
            {
                // 손에 든 더러운 잔을 꺼내 싱크대에 깨끗한 잔으로 진열합니다.
                GameObject dirtyDish = player.Stacking.StackedItems[player.Stacking.StackCount - 1];
                Vector3 startPosition = dirtyDish != null ? dirtyDish.transform.position : transform.position;
                player.Stacking.PopItem();
                Managers.Dish.AddCleanDishes();
                AnimateDishToDisplay(startPosition);
            }

            Debug.Log($"설거지 완료: 플레이어의 더러운 맥주잔 {playerWashAmount}개를 씻었습니다.");
            return;
        }

    }

    /// <summary>직원이 들고 온 더러운 잔을 사용해 깨끗한 잔을 싱크대에 진열합니다.</summary>
    public bool WashForEmployee(StackingSystem employeeStack)
    {
        if (employeeStack == null || Managers.Dish == null || Managers.Dish.DirtyDishCount <= 0 ||
            employeeStack.GetFirstListItemType() != Define.ItemType.Beer_Dirty)
            return false;

        // 직원이 실제로 들고 온 잔, 공용 재고, 처리 설정 수량을 모두 넘지 않게 합니다.
        int washAmount = Mathf.Min(processAmount, employeeStack.StackCount);
        washAmount = Mathf.Min(washAmount, Managers.Dish.DirtyDishCount);
        if (washAmount <= 0)
            return false;

        if (!Managers.Dish.WashDirtyDishes(washAmount))
            return false;

        // 직원 스택에서 더러운 잔을 제거하고 같은 수량의 깨끗한 잔을 진열합니다.
        for (int i = 0; i < washAmount; i++)
        {
            if (employeeStack.GetFirstListItemType() != Define.ItemType.Beer_Dirty)
                break;

            employeeStack.PopItem();
            CreateDisplayedDish();
        }

        Debug.Log($"직원이 들고 온 더러운 잔 {washAmount}개를 씻어 싱크대에 진열했습니다.");
        return true;
    }

    /// <summary>회수 구역의 플레이어가 들 수 있으면 진열된 깨끗한 잔 하나를 옮깁니다.</summary>
    public bool TryTakeCleanDish(GameObject actor)
    {
        if (!TryGetPlayer(actor, out PlayerController player) || player.Stacking == null ||
            Managers.Dish == null || Managers.Dish.CleanDishCount <= 0 || displayedDishes.Count <= 0 ||
            !player.Stacking.CheckListItem(Define.ItemType.Beer_Empty))
        {
            return false;
        }

        // 스택의 종류 검사는 빈손과 이미 든 깨끗한 잔을 허용하고 다른 물건은 막습니다.
        StackingSystem stack = player.Stacking;
        if (!Managers.Dish.UseCleanDishes())
            return false;

        GameObject dish = null;
        if (displayedDishes.Count > 0)
        {
            int lastIndex = displayedDishes.Count - 1;
            dish = displayedDishes[lastIndex];
            displayedDishes.RemoveAt(lastIndex);
        }

        // 저장된 수량과 진열 오브젝트가 맞지 않는 상황에서도 잔을 잃지 않도록 필요한 경우 새로 만듭니다.
        if (dish == null)
            dish = CreateDishObject(GetDisplayPosition(displayedDishes.Count));

        if (dish == null)
        {
            // 오브젝트 생성에 실패하면 차감한 재고를 되돌려 다음 상호작용에서 다시 시도하게 합니다.
            Managers.Dish.AddCleanDishes();
            return false;
        }

        dish.transform.SetParent(null);
        stack.PickUpItem(dish);
        return true;
    }

    /// <summary>회수 구역에 머무는 플레이어가 잔을 들 수 있으면 진열된 잔을 한 개씩 건넵니다.</summary>
    protected override void OnTriggerStay(Collider other)
    {
        // 공통 범위 상태는 부모가 갱신하고, 여기서는 깨끗한 잔 회수만 처리합니다.
        base.OnTriggerStay(other);

        if (Time.time < nextTransferTime)
            return;

        GameObject actor = GetActor(other);
        if (actor == null)
            return;

        // 진열 재고와 플레이어의 손 상태는 잔 회수 함수에서 확인합니다.
        // 실제 이동에 성공했을 때만 다음 잔을 위한 대기 시간을 설정합니다.
        if (TryTakeCleanDish(actor))
            nextTransferTime = Time.time + transferInterval;
    }

    /// <summary>깨끗한 잔 하나를 싱크대 위에 보이는 오브젝트로 추가합니다.</summary>
    private void CreateDisplayedDish()
    {
        GameObject dish = CreateDishObject(GetDisplayPosition(displayedDishes.Count));
        if (dish == null)
            return;

        Transform displayPoint = GetDisplayPoint();
        dish.transform.SetParent(displayPoint);
        dish.transform.localPosition = GetDisplayLocalPosition(displayedDishes.Count);
        dish.transform.localRotation = Quaternion.identity;

        DisableDishColliders(dish);

        displayedDishes.Add(dish);
    }

    /// <summary>더러운 잔 위치에서 깨끗한 잔을 출발시켜 싱크대 진열 위치로 점프시킵니다.</summary>
    private void AnimateDishToDisplay(Vector3 startPosition)
    {
        int displayIndex = displayedDishes.Count + pendingDisplayCount;
        Vector3 targetPosition = GetDisplayPosition(displayIndex);
        GameObject dish = CreateDishObject(startPosition);
        if (dish == null)
            return;

        DisableDishColliders(dish);
        pendingDisplayCount++;

        dish.transform.DOJump(targetPosition, 0.8f, 1, 0.45f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                if (dish == null)
                    return;

                dish.transform.SetParent(GetDisplayPoint());
                dish.transform.localPosition = GetDisplayLocalPosition(displayIndex);
                dish.transform.localRotation = Quaternion.identity;
                displayedDishes.Add(dish);
                pendingDisplayCount = Mathf.Max(0, pendingDisplayCount - 1);
            });
    }

    /// <summary>어드레서블에서 깨끗한 맥주잔 프리팹을 지정 위치에 만듭니다.</summary>
    private GameObject CreateDishObject(Vector3 spawnPosition)
    {
        return Managers.Resource.Instantiate("Item_BeerEmpty", spawnPosition, Quaternion.identity);
    }

    /// <summary>진열대 기준으로 몇 번째 잔인지에 맞는 월드 위치를 계산합니다.</summary>
    private Vector3 GetDisplayPosition(int displayIndex)
    {
        Transform displayPoint = GetDisplayPoint();
        return displayPoint.TransformPoint(GetDisplayLocalPosition(displayIndex));
    }

    /// <summary>진열대 기준으로 몇 번째 잔인지에 맞는 부모 기준 위치를 계산합니다.</summary>
    private Vector3 GetDisplayLocalPosition(int displayIndex)
    {
        Vector3 startPosition = cleanDishDisplayPoint != null ? Vector3.zero : cleanDishDisplayOffset;
        return startPosition + Vector3.up * (displayIndex * displayHeight);
    }

    /// <summary>진열 잔에 콜라이더가 있더라도 플레이어가 자동으로 줍지 못하게 끕니다.</summary>
    private static void DisableDishColliders(GameObject dish)
    {
        Collider[] colliders = dish.GetComponentsInChildren<Collider>();
        foreach (Collider itemCollider in colliders)
            itemCollider.enabled = false;
    }

    /// <summary>진열 위치가 지정되지 않았으면 싱크대 위쪽을 기본 위치로 사용합니다.</summary>
    private Transform GetDisplayPoint()
    {
        if (cleanDishDisplayPoint != null)
            return cleanDishDisplayPoint;

        return transform;
    }

    /// <summary>에디터에서 저장 ID가 비어 있으면 이 싱크대만의 ID를 미리 만듭니다.</summary>
}
