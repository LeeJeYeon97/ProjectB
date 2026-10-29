using UnityEngine;

/// <summary>배달되어 실제로 운반할 수 있는 재료 한 개의 종류를 보관합니다.</summary>
[RequireComponent(typeof(ItemController))]
public class IngredientItem : MonoBehaviour
{
    #region 재료 정보

    [Tooltip("이 물건이 나타내는 재료의 종류입니다.")]
    [SerializeField] private Define.FoodIngredientType ingredientType;
    public Define.FoodIngredientType IngredientType => ingredientType;

    // 한 물건을 직원과 플레이어가 동시에 가져가지 않도록 운반자를 기억합니다.
    private GameObject reservedActor;

    #endregion

    #region 재료 정보 설정

    /// <summary>배송할 때 재료 데이터에 맞는 종류를 연결합니다.</summary>
    public void SetIngredientType(Define.FoodIngredientType type)
    {
        ingredientType = type;
    }

    /// <summary>물건이 비어 있거나 같은 운반자가 예약한 경우에만 인계합니다.</summary>
    public bool TryReserve(GameObject actor)
    {
        if (actor == null) return false;
        if (reservedActor != null && reservedActor != actor) return false;
        reservedActor = actor;
        return true;
    }

    /// <summary>예약한 운반자만 자신의 예약을 취소합니다.</summary>
    public void Release(GameObject actor)
    {
        if (reservedActor == actor) reservedActor = null;
    }

    /// <summary>다른 운반자의 예약을 침범하지 않고 가져갈 수 있는지 확인합니다.</summary>
    public bool CanTake(GameObject actor)
    {
        if (actor == null) return false;
        if (reservedActor == null) return true;
        return reservedActor == actor;
    }

    #endregion
}
