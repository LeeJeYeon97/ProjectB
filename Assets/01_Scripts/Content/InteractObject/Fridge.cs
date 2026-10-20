using UnityEngine;

/// <summary>냉장고가 제공하는 레시피와 최소 제조 시간을 설정합니다.</summary>
public class Fridge : MenuMachine
{
    /// <summary>냉장고 메뉴 작업에 적용할 최소 시간을 지정합니다.</summary>
    public override float MinimumProductionDuration => 0.1f;
}
