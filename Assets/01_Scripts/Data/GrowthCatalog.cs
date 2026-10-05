using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>업그레이드 항목의 이름, 가격, 단계별 효과를 보관합니다.</summary>
[CreateAssetMenu(fileName = "GrowthCatalog", menuName = "Content/Growth Catalog")]
public class GrowthCatalog : ScriptableObject
{
    public List<GrowthItem> items = new List<GrowthItem>();
}

/// <summary>한 종류의 업그레이드가 각 단계에서 요구하는 값입니다.</summary>
[Serializable]
public class GrowthItem
{
    public string id;
    public string title;
    public int[] prices;
    public float[] values;
}
