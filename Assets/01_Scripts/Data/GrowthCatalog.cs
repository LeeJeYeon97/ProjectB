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
    [Tooltip("기계별 업그레이드 화면에서만 구매하는 항목인지 설정합니다.")]
    public bool isMachineUpgrade;
    [Tooltip("다음 단계 효과를 표시하는 문장입니다. 값은 {0} 위치에 들어갑니다.")]
    public string effectFormat = "효과 +{0}";
    [Tooltip("효과 문장에 넣기 전에 단계 값을 곱할 수치입니다.")]
    public float effectValueScale = 1f;
}
