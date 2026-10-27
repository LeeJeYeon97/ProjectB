using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>매장 재고와 손님 주문 예약을 한 곳에서 관리합니다.</summary>
public class IngredientManager
{
    /// <summary>재고가 보충되면 재료를 기다리는 손님에게 알립니다.</summary>
    public event Action OnIngredientsChanged;

    #region 변수
    private const string IngredientLabel = "IngredientData";

    // 재료별 실제 보유 수량을 저장합니다.
    private readonly Dictionary<Define.FoodIngredientType, int> amounts = new();

    // UI와 구매 처리에서 재료의 이름과 가격을 확인할 수 있도록 데이터 참조를 보관합니다.
    private readonly Dictionary<Define.FoodIngredientType, IngredientCatalog> ingredientData = new();

    // 주문별 예약 수량을 저장해 여러 손님이 같은 재고를 동시에 주문하지 않게 합니다.
    private readonly Dictionary<CustomerController, Dictionary<Define.FoodIngredientType, int>> reservedAmounts = new();

    #endregion

    #region 초기화

    /// <summary>데이터 폴더의 재료 에셋을 비동기로 읽어 매장 시작 재고를 준비합니다.</summary>
    public void Init()
    {
        amounts.Clear();
        ingredientData.Clear();
        reservedAmounts.Clear();

        // Addressables의 재료 라벨을 사용하므로 Resources 폴더에 복사본을 둘 필요가 없습니다.
        Managers.Resource.LoadAllAsync<IngredientCatalog>(IngredientLabel, null, OnIngredientsLoaded);
    }

    /// <summary>로드된 재료 정의를 종류별 시작 수량으로 등록합니다.</summary>
    private void OnIngredientsLoaded(Dictionary<string, UnityEngine.Object> loaded)
    {
        if (loaded == null)
        {
            Debug.LogError("[IngredientManager] 재료 데이터를 찾지 못해 모든 재고를 0으로 시작합니다.");
            return;
        }

        int registeredCount = 0;
        foreach (UnityEngine.Object asset in loaded.Values)
        {
            IngredientCatalog ingredient = asset as IngredientCatalog;
            if (ingredient == null)
                continue;

            if (!IsValidType(ingredient.Type) || ingredient.StartingAmount < 0)
            {
                Debug.LogWarning($"[IngredientManager] 잘못된 재료 데이터를 건너뜁니다: {ingredient.name}");
                continue;
            }

            if (amounts.ContainsKey(ingredient.Type))
            {
                Debug.LogWarning($"[IngredientManager] 중복 재료 종류를 건너뜁니다: {ingredient.Type}");
                continue;
            }

            amounts.Add(ingredient.Type, ingredient.StartingAmount);
            ingredientData.Add(ingredient.Type, ingredient);
            registeredCount++;
        }

        if (registeredCount == 0)
            Debug.LogError("[IngredientManager] 사용할 수 있는 재료 데이터가 없습니다.");

        // 로드 전에 착석한 손님이 있다면 확보된 재고로 주문을 다시 시도하게 합니다.
        OnIngredientsChanged?.Invoke();
    }

    #endregion

    #region 재고 조회와 보충

    /// <summary>예약되지 않은 재고만으로 레시피 재료를 모두 만들 수 있는지 확인합니다.</summary>
    public bool HasIngredients(IReadOnlyList<FoodIngredient> ingredients)
    {
        if (!TryBuildAmountList(ingredients, out Dictionary<Define.FoodIngredientType, int> needed))
            return false;

        foreach (KeyValuePair<Define.FoodIngredientType, int> item in needed)
        {
            int available = GetAmount(item.Key) - GetReservedAmount(item.Key);
            if (available < item.Value)
                return false;
        }

        return true;
    }

    /// <summary>현재 재고 수량을 반환하며, 등록되지 않은 재료는 0으로 취급합니다.</summary>
    public int GetAmount(Define.FoodIngredientType type)
    {
        return amounts.TryGetValue(type, out int amount) ? amount : 0;
    }

    /// <summary>재료의 표시 이름과 구매 가격을 담은 데이터 에셋을 반환합니다.</summary>
    public IngredientCatalog GetIngredientData(Define.FoodIngredientType type)
    {
        ingredientData.TryGetValue(type, out IngredientCatalog data);
        return data;
    }

    /// <summary>상점이나 보급 시스템이 호출할 수 있도록 재료 수량을 늘립니다.</summary>
    public bool AddIngredients(Define.FoodIngredientType type, int amount)
    {
        if (!IsValidType(type) || amount <= 0)
            return false;

        int currentAmount = GetAmount(type);
        if (amount > int.MaxValue - currentAmount)
            return false;

        amounts[type] = currentAmount + amount;
        OnIngredientsChanged?.Invoke();
        return true;
    }

    #endregion

    #region 주문 예약과 사용

    /// <summary>손님 주문에 필요한 재료를 예약해 다른 주문이 같은 수량을 사용하지 못하게 합니다.</summary>
    public bool TryReserveForOrder(CustomerController customer, IReadOnlyList<FoodIngredient> ingredients)
    {
        if (customer == null || reservedAmounts.ContainsKey(customer))
            return false;

        if (!TryBuildAmountList(ingredients, out Dictionary<Define.FoodIngredientType, int> needed))
            return false;

        foreach (KeyValuePair<Define.FoodIngredientType, int> item in needed)
        {
            int available = GetAmount(item.Key) - GetReservedAmount(item.Key);
            if (available < item.Value)
                return false;
        }

        reservedAmounts.Add(customer, needed);
        return true;
    }

    /// <summary>손님 주문에 현재 레시피 전체 수량이 예약되어 있는지 확인합니다.</summary>
    public bool HasReservation(CustomerController customer, IReadOnlyList<FoodIngredient> ingredients)
    {
        if (customer == null || !reservedAmounts.TryGetValue(customer, out Dictionary<Define.FoodIngredientType, int> reserved))
            return false;

        if (!TryBuildAmountList(ingredients, out Dictionary<Define.FoodIngredientType, int> needed))
            return false;

        return AreAmountsEqual(reserved, needed);
    }

    /// <summary>해당 레시피를 기다리는 손님 중 예약된 재료가 있는지 확인합니다.</summary>
    public bool HasReservedOrder(FoodRecipeData recipe)
    {
        if (recipe == null)
            return false;

        foreach (KeyValuePair<CustomerController, Dictionary<Define.FoodIngredientType, int>> item in reservedAmounts)
        {
            CustomerController customer = item.Key;
            if (customer == null || customer.CurrentState != Define.CustomerState.WaitingForFood)
                continue;

            if (customer.OrderedRecipe != recipe)
                continue;

            if (AreAmountsEqual(item.Value, BuildAmountList(recipe.Ingredients)))
                return true;
        }

        return false;
    }

    /// <summary>플레이어가 만들 레시피를 기다리는 손님 예약 하나를 가져옵니다.</summary>
    public bool TryGetReservedCustomer(FoodRecipeData recipe, out CustomerController customer)
    {
        customer = null;
        if (recipe == null)
            return false;

        foreach (KeyValuePair<CustomerController, Dictionary<Define.FoodIngredientType, int>> item in reservedAmounts)
        {
            CustomerController candidate = item.Key;
            if (candidate == null || candidate.CurrentState != Define.CustomerState.WaitingForFood)
                continue;

            if (candidate.OrderedRecipe != recipe)
                continue;

            if (!AreAmountsEqual(item.Value, BuildAmountList(recipe.Ingredients)))
                continue;

            customer = candidate;
            return true;
        }

        return false;
    }

    /// <summary>예약된 주문 재료를 실제 재고에서 차감하고 예약을 해제합니다.</summary>
    public bool TryUseReservedIngredients(CustomerController customer, IReadOnlyList<FoodIngredient> ingredients)
    {
        if (!HasReservation(customer, ingredients))
            return false;

        Dictionary<Define.FoodIngredientType, int> reserved = reservedAmounts[customer];
        foreach (KeyValuePair<Define.FoodIngredientType, int> item in reserved)
        {
            if (GetAmount(item.Key) < item.Value)
                return false;
        }

        foreach (KeyValuePair<Define.FoodIngredientType, int> item in reserved)
            amounts[item.Key] = GetAmount(item.Key) - item.Value;

        reservedAmounts.Remove(customer);
        return true;
    }

    /// <summary>예약되지 않은 재고가 충분할 때만 재료 전부를 한 번에 차감합니다.</summary>
    public bool TryUseIngredients(IReadOnlyList<FoodIngredient> ingredients)
    {
        if (!TryBuildAmountList(ingredients, out Dictionary<Define.FoodIngredientType, int> needed))
            return false;

        foreach (KeyValuePair<Define.FoodIngredientType, int> item in needed)
        {
            int available = GetAmount(item.Key) - GetReservedAmount(item.Key);
            if (available < item.Value)
                return false;
        }

        foreach (KeyValuePair<Define.FoodIngredientType, int> item in needed)
            amounts[item.Key] = GetAmount(item.Key) - item.Value;

        return true;
    }

    /// <summary>손님 주문이 취소되거나 손님이 풀로 돌아갈 때 예약을 돌려줍니다.</summary>
    public void ReleaseReservation(CustomerController customer)
    {
        if (customer == null)
            return;

        reservedAmounts.Remove(customer);
    }

    #endregion

    #region 내부 계산

    /// <summary>레시피의 재료를 종류별로 합치고 잘못된 항목이 있는지 검사합니다.</summary>
    private static bool TryBuildAmountList(
        IReadOnlyList<FoodIngredient> ingredients,
        out Dictionary<Define.FoodIngredientType, int> combined)
    {
        combined = new Dictionary<Define.FoodIngredientType, int>();
        if (ingredients == null)
            return false;

        foreach (FoodIngredient ingredient in ingredients)
        {
            if (ingredient == null || !IsValidType(ingredient.Type) || ingredient.Amount <= 0)
                return false;

            int currentAmount = combined.TryGetValue(ingredient.Type, out int existingAmount) ? existingAmount : 0;
            if (ingredient.Amount > int.MaxValue - currentAmount)
                return false;

            combined[ingredient.Type] = currentAmount + ingredient.Amount;
        }

        return true;
    }

    /// <summary>정상적인 레시피 재료를 합산해 비교용 목록으로 돌려줍니다.</summary>
    private static Dictionary<Define.FoodIngredientType, int> BuildAmountList(IReadOnlyList<FoodIngredient> ingredients)
    {
        if (TryBuildAmountList(ingredients, out Dictionary<Define.FoodIngredientType, int> combined))
            return combined;

        return new Dictionary<Define.FoodIngredientType, int>();
    }

    /// <summary>모든 재료 종류와 합산 수량이 같은지 확인합니다.</summary>
    private static bool AreAmountsEqual(
        Dictionary<Define.FoodIngredientType, int> first,
        Dictionary<Define.FoodIngredientType, int> second)
    {
        if (first.Count != second.Count)
            return false;

        foreach (KeyValuePair<Define.FoodIngredientType, int> item in first)
        {
            if (!second.TryGetValue(item.Key, out int amount) || amount != item.Value)
                return false;
        }

        return true;
    }

    /// <summary>특정 재료가 모든 손님에게 예약된 수량을 더합니다.</summary>
    private int GetReservedAmount(Define.FoodIngredientType type)
    {
        int total = 0;
        foreach (Dictionary<Define.FoodIngredientType, int> reservation in reservedAmounts.Values)
        {
            if (!reservation.TryGetValue(type, out int amount))
                continue;

            if (amount > int.MaxValue - total)
                return int.MaxValue;

            total += amount;
        }

        return total;
    }

    /// <summary>None 또는 정의되지 않은 재료 값을 거부합니다.</summary>
    private static bool IsValidType(Define.FoodIngredientType type)
    {
        return type != Define.FoodIngredientType.None && System.Enum.IsDefined(typeof(Define.FoodIngredientType), type);
    }

    #endregion
}
