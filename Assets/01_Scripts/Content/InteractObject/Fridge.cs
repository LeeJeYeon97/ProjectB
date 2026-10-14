using UnityEngine;

/// <summary>냉장고는 상호작용 베이스의 메뉴 제조 기능을 사용합니다.</summary>
public class Fridge : InteractableBase
{
    protected override Define.InteractionType DefaultInteractionType => Define.InteractionType.ProduceMenu;
    protected override Define.MenuType DefaultMenus => Define.MenuType.Beer | Define.MenuType.TestMeal;

    /// <summary>냉장고의 제조 시간을 기계 속도 업그레이드 계산에 전달합니다.</summary>
    public override float MinimumProductionDuration => 0.1f;

    /// <summary>기계 속도 업그레이드를 반영한 제조 시간을 돌려줍니다.</summary>
    public override float Duration => GetProductionDuration();
}
