using System;
using UnityEngine;

/// <summary>조리 기계 강화에 필요한 단계별 가격과 시간 배율을 보관합니다.</summary>
[CreateAssetMenu(fileName = "MachineUpgradeData", menuName = "Content/Machine Upgrade Data")]
public class MachineUpgradeData : ScriptableObject
{
    #region 강화 설정

    [Tooltip("각 단계의 가격과 강화 후 조리 시간 배율입니다. 수치는 기획 확정 전 임시값입니다.")]
    [SerializeField] private MachineUpgradeStep[] steps;

    /// <summary>구매할 수 있는 전체 강화 단계 수입니다.</summary>
    public int MaxLevel => steps == null ? 0 : steps.Length;

    #endregion

    #region 단계 조회

    /// <summary>현재 단계에서 다음 단계의 가격과 배율을 안전하게 가져옵니다.</summary>
    public bool TryGetStep(int level, out MachineUpgradeStep step)
    {
        step = null;
        if (steps == null) return false;
        if (level < 0 || level >= steps.Length) return false;
        step = steps[level];
        if (step == null) return false;
        if (step.Price <= 0 || step.TimeScale <= 0f || step.TimeScale > 1f) return false;
        return true;
    }

    /// <summary>강화하지 않은 기계는 1배, 강화한 기계는 해당 단계의 배율을 사용합니다.</summary>
    public float GetTimeScale(int level)
    {
        if (level <= 0) return 1f;
        level = Mathf.Min(level, MaxLevel);
        if (TryGetStep(level - 1, out MachineUpgradeStep step)) return step.TimeScale;
        return 1f;
    }

    #endregion
}

/// <summary>한 단계의 구매 가격과 구매 후 적용할 전체 시간 배율입니다.</summary>
[Serializable]
public class MachineUpgradeStep
{
    [SerializeField, Min(1)] private int price = 100;
    public int Price => price;

    [SerializeField, Range(0.01f, 1f)] private float timeScale = 0.9f;
    public float TimeScale => timeScale;
}

