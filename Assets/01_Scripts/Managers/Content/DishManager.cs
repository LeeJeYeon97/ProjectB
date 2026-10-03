using System;

/// <summary>
/// 가게에서 사용하는 더러운 접시와 깨끗한 접시 수량을 관리합니다.
/// </summary>
[Serializable]
public class DishManager
{
    // 설거지 전 접시 수량을 보관합니다.
    private int dirtyDishCount;

    // 설거지를 마친 접시 수량을 보관합니다.
    private int cleanDishCount;

    /// <summary>현재 설거지가 필요한 접시 수량입니다.</summary>
    public int DirtyDishCount => dirtyDishCount;

    /// <summary>현재 사용할 수 있는 깨끗한 접시 수량입니다.</summary>
    public int CleanDishCount => cleanDishCount;

    /// <summary>접시 재고가 바뀔 때 UI 등이 갱신할 수 있도록 알립니다.</summary>
    public event Action<int, int> OnDishCountChanged;

    /// <summary>테이블 청소가 끝났을 때 더러운 접시를 재고에 추가합니다.</summary>
    
    public void Init()
    {
        // 깨끗한 잔은 설거지 전에는 싱크대에 없으므로 처음에는 0개로 시작합니다.
        cleanDishCount = 0;
    }
    public void AddDirtyDish(int amount = 1)
    {
        if (amount <= 0)
            return;

        dirtyDishCount += amount;
        NotifyDishCountChanged();
    }


    /// <summary>
    /// 설거지가 완료된 순간 더러운 접시를 깨끗한 접시로 바꿉니다.
    /// 수량 확인과 변경을 한 함수에서 처리해 재고가 중간 상태로 남지 않게 합니다.
    /// </summary>
    /// <param name="amount">한 번에 설거지할 접시 수량입니다.</param>
    /// <returns>요청한 수량만큼 설거지했으면 참을 반환합니다.</returns>
    public bool WashDirtyDishes(int amount = 1)
    {
        if (amount <= 0 || dirtyDishCount < amount)
            return false;

        dirtyDishCount -= amount;
        cleanDishCount += amount;
        NotifyDishCountChanged();
        return true;
    }

    /// <summary>음식 제작이나 서빙에서 깨끗한 접시를 사용합니다.</summary>
    /// <param name="amount">사용할 접시 수량입니다.</param>
    /// <returns>요청한 수량을 사용할 수 있으면 차감 후 참을 반환합니다.</returns>
    public bool UseCleanDishes(int amount = 1)
    {
        if (amount <= 0 || cleanDishCount < amount)
            return false;

        cleanDishCount -= amount;
        NotifyDishCountChanged();
        return true;
    }

    /// <summary>작업 실패 등으로 되돌려야 할 깨끗한 잔을 재고에 다시 추가합니다.</summary>
    public void AddCleanDishes(int amount = 1)
    {
        if (amount <= 0)
            return;

        cleanDishCount += amount;
        NotifyDishCountChanged();
    }

    /// <summary>재고가 바뀌었다는 사실과 현재 수량을 구독자에게 전달합니다.</summary>
    private void NotifyDishCountChanged()
    {
        OnDishCountChanged?.Invoke(dirtyDishCount, cleanDishCount);
    }
}
