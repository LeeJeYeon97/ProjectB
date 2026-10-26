using System.Collections.Generic;

/// <summary>
/// 메뉴 제조에 필요한 재료를 확인하고 사용하는 재고의 공통 규칙입니다.
/// 통합 재고와 기계별 재고가 같은 규칙을 구현하면 제조 코드를 바꾸지 않고 교체할 수 있습니다.
/// </summary>
public interface IIngredientStock
{
    /// <summary>레시피가 요구하는 모든 재료의 수량이 충분한지 확인합니다.</summary>
    bool HasIngredients(IReadOnlyList<FoodIngredient> ingredients);

    /// <summary>모든 재료가 충분할 때만 필요한 수량을 한 번에 차감합니다.</summary>
    bool TryUseIngredients(IReadOnlyList<FoodIngredient> ingredients);
}
