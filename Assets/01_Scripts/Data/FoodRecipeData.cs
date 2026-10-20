using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>레시피가 요구하는 재료 한 종류와 필요한 수량을 보관합니다.</summary>
[Serializable]
public class FoodIngredient
{
    public Define.FoodIngredientType Type;
    [Min(1)] public int Amount = 1;
}

/// <summary>메뉴 하나의 제조 정보와 결과 프리팹을 보관하는 에셋입니다.</summary>
[CreateAssetMenu(fileName = "FoodRecipeData", menuName = "Content/FoodRecipeData")]
public class FoodRecipeData : ScriptableObject
{
    [Header("메뉴 타입")]
    public Define.FoodType Type;

    [Header("메뉴 이름")]
    public string FoodName;

    [Header("요구 재료")]
    public List<FoodIngredient> Ingredients = new List<FoodIngredient>();

    [Header("만들 수 있는 기계")]
    public Define.FoodCookingMachineType CookingMachineType;

    [Header("메뉴 프리팹")]
    public GameObject MenuPrefab;

    [Header("제조 시간")]
    public float CookingDuration;

    [Header("판매가격")]
    public int Pay;


}
