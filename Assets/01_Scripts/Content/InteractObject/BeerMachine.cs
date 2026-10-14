using UnityEngine;

/// <summary>맥주머신은 상호작용 베이스의 메뉴 제조 기능을 사용합니다.</summary>
public class BeerMachine : InteractableBase
{
    protected override Define.InteractionType DefaultInteractionType => Define.InteractionType.ProduceMenu;
    protected override Define.MenuType DefaultMenus => Define.MenuType.Beer | Define.MenuType.TestMeal;

    /// <summary>맥주머신 한 대가 제조에 걸리는 최소 시간을 지정합니다.</summary>
    public override float MinimumProductionDuration => 0.5f;

    /// <summary>기계 속도 업그레이드를 반영한 제조 시간을 돌려줍니다.</summary>
    public override float Duration => GetProductionDuration();
}
