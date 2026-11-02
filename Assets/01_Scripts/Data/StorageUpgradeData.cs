using System;
using UnityEngine;

/// <summary>창고별 강화 가격과 강화 후 추가되는 공용 재고 용량을 보관합니다.</summary>
[CreateAssetMenu(fileName = "StorageUpgradeData", menuName = "Content/Storage Upgrade Data")]
public class StorageUpgradeData : ScriptableObject
{
    #region 단계 설정

    [Tooltip("각 단계의 가격과 해당 단계에서 추가되는 전체 용량입니다. 초기 수치는 임시값입니다.")]
    [SerializeField] private StorageUpgradeStep[] steps;
    public int MaxLevel => steps == null ? 0 : steps.Length;

    #endregion

    #region 단계 조회

    /// <summary>현재 단계에서 다음 단계의 가격과 추가 용량을 확인합니다.</summary>
    public bool TryGetStep(int level, out StorageUpgradeStep step)
    {
        step = null;
        if (steps == null) return false;
        if (level < 0 || level >= steps.Length) return false;
        step = steps[level];
        if (step == null) return false;
        if (step.Price <= 0) return false;
        if (step.ExtraCapacity <= 0) return false;
        // 이전 단계보다 용량이 감소하는 잘못된 설정은 구매하지 않습니다.
        if (level > 0)
        {
            StorageUpgradeStep previous = steps[level - 1];
            if (previous == null) return false;
            if (step.ExtraCapacity <= previous.ExtraCapacity) return false;
        }
        return true;
    }

    /// <summary>보유 단계에 해당하는 전체 추가 용량을 반환합니다.</summary>
    public int GetExtraCapacity(int level)
    {
        if (level <= 0) return 0;
        level = Mathf.Min(level, MaxLevel);
        if (TryGetStep(level - 1, out StorageUpgradeStep step)) return step.ExtraCapacity;
        return 0;
    }

    #endregion
}

/// <summary>한 단계의 가격과 해당 단계에서 제공하는 전체 추가 용량입니다.</summary>
[Serializable]
public class StorageUpgradeStep
{
    [SerializeField, Min(1)] private int price = 100;
    public int Price => price;

    [SerializeField, Min(1)] private int extraCapacity = 5;
    public int ExtraCapacity => extraCapacity;
}
