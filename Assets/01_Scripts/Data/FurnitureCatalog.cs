using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>배치 화면에서 사용할 가구 이름, 가격, 프리팹을 보관합니다.</summary>
[CreateAssetMenu(fileName = "FurnitureCatalog", menuName = "Content/Furniture Catalog")]
public class FurnitureCatalog : ScriptableObject
{
    public List<FurnitureItem> items = new List<FurnitureItem>();
}

/// <summary>가구 한 종류의 저장 ID와 배치 설정입니다.</summary>
[Serializable]
public class FurnitureItem
{
    public string id;
    public string title;
    public int price;
    public GameObject prefab;

    [Header("배치 검사")]
    [Tooltip("가구가 바닥에서 차지하는 앞뒤, 좌우 크기입니다.")]
    public Vector2 placementSize = new Vector2(0.8f, 0.8f);
    [Tooltip("가구가 바닥부터 차지한다고 검사할 높이입니다.")]
    public float placementHeight = 1.2f;
    [Tooltip("프리팹 원점이 바닥에서 떠야 할 때 더하는 높이입니다.")]
    public float heightOffset;
}
