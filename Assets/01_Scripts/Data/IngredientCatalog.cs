using UnityEngine;

/// <summary>재료 한 종류의 정보와 매장 시작 수량을 보관합니다.</summary>
[CreateAssetMenu(fileName = "IngredientData", menuName = "Content/Ingredient Data")]
public class IngredientCatalog : ScriptableObject
{
    #region 변수

    [Tooltip("레시피와 재고에서 사용하는 재료 종류입니다.")]
    public Define.FoodIngredientType Type;

    [Tooltip("화면에 표시할 재료 이름입니다.")]
    public string Name;

    [Min(0)]
    [Tooltip("재료 한 개의 구매 가격입니다.")]
    public int Price;

    public GameObject Prefab;

    [Min(0)]
    [Tooltip("매장을 처음 시작할 때 보유할 재료 수량입니다.")]
    public int StartingAmount = 10;

    #endregion
}

