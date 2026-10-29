using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>업그레이드 단계와 구매, 효과 적용, 저장을 한 곳에서 처리합니다.</summary>
[Serializable]
public class GrowthManager
{
    #region 변수

    private const string SaveKey = "ProjectB.Growth.Save";
    private const int CurrentSaveVersion = 3;
    private const string CatalogAddress = "GrowthCatalog";

    [SerializeField] private GrowthData catalog;
    private readonly Dictionary<string, int> levels = new Dictionary<string, int>();

    public event Action OnChanged;

    #endregion

    #region 초기화

    /// <summary>성장 카탈로그를 Addressables에서 읽기 시작합니다.</summary>
    public void Init()
    {
        // 카탈로그가 준비되기 전에는 저장 단계를 해석하거나 효과를 적용하지 않습니다.
        catalog = null;
        levels.Clear();
        Managers.Resource.LoadAsync<GrowthData>(CatalogAddress, OnCatalogLoaded);
    }

    /// <summary>카탈로그 로드가 끝나면 저장 단계를 복원하고 현재 플레이어에게 효과를 적용합니다.</summary>
    private void OnCatalogLoaded(GrowthData loadedCatalog)
    {
        if (loadedCatalog == null)
        {
            Debug.LogError("[GrowthManager] 성장 카탈로그를 불러오지 못했습니다. 성장 구매를 사용할 수 없습니다.");
            OnChanged?.Invoke();
            return;
        }

        catalog = loadedCatalog;
        Load();
        ApplyAll();

        // 먼저 열린 성장 화면도 실제 가격과 단계를 다시 표시하도록 알립니다.
        OnChanged?.Invoke();
    }

    #endregion

    /// <summary>항목 ID에 해당하는 현재 업그레이드 단계를 돌려줍니다.</summary>
    public int GetLevel(string id)
    {
        return levels.TryGetValue(id, out int level) ? level : 0;
    }

    /// <summary>다음 단계 가격을 가져오며, 최대 단계에 도달했으면 0을 돌려줍니다.</summary>
    public int GetNextPrice(string id)
    {
        GrowthItem item = FindItem(id);
        int level = GetLevel(id);
        return item == null || item.prices == null || level >= item.prices.Length ? 0 : item.prices[level];
    }

    /// <summary>구매가 가능하면 재화를 차감하고 단계와 효과를 함께 갱신합니다.</summary>
    public bool TryUpgrade(string id)
    {
        // 직원 능력치는 개별 직원 버튼에서만 구매합니다.
        if (id == "employee_speed" || id == "employee_interact")
            return false;

        GrowthItem item = FindItem(id);
        int level = GetLevel(id);
        int price = GetNextPrice(id);
        PlayerController player = PlayerController.Instance;

        if (item == null || player == null || price <= 0 || item.values == null || level >= item.values.Length)
            return false;
        if (!player.TrySpendMoney(price))
            return false;

        levels[id] = level + 1;
        Save();
        ApplyAll();
        OnChanged?.Invoke();
        return true;
    }

    /// <summary>선택한 직원 한 명의 다음 능력치 업그레이드를 구매합니다.</summary>
    public bool TryUpgradeEmployeeStat(EmployeeController employee, string id)
    {
        if (id != "employee_speed" && id != "employee_interact")
            return false;

        GrowthItem item = FindItem(id);
        PlayerController player = PlayerController.Instance;
        if (employee == null || item == null || player == null ||
            item.prices == null || item.values == null)
            return false;

        int level = id == "employee_speed"
            ? employee.MoveUpgradeLevel
            : employee.InteractionUpgradeLevel;
        if (level >= item.prices.Length || level >= item.values.Length)
            return false;

        int price = item.prices[level];
        if (price <= 0 || !player.TrySpendMoney(price))
            return false;

        int nextLevel = level + 1;
        float totalValue = GetValueAtLevel(item, nextLevel);
        if (id == "employee_speed")
            employee.SetMoveUpgrade(nextLevel, totalValue);
        else
            employee.SetInteractionUpgrade(nextLevel, totalValue);

        // 직원 고용 데이터는 아직 저장되지 않으므로 이 능력치는 현재 직원 인스턴스에만 유지합니다.
        OnChanged?.Invoke();
        return true;
    }

    /// <summary>새 직원에게 예전 공통 이동·작업 업그레이드를 시작 능력치로 넘깁니다.</summary>
    public void ApplyStartingEmployeeStats(EmployeeController employee)
    {
        if (employee == null)
            return;

        GrowthItem moveItem = FindItem("employee_speed");
        int moveLevel = Mathf.Clamp(GetLevel("employee_speed"), 0, moveItem != null && moveItem.values != null ? moveItem.values.Length : 0);
        employee.SetMoveUpgrade(moveLevel, moveItem != null ? GetValueAtLevel(moveItem, moveLevel) : 0f);

        GrowthItem workItem = FindItem("employee_interact");
        int workLevel = Mathf.Clamp(GetLevel("employee_interact"), 0, workItem != null && workItem.values != null ? workItem.values.Length : 0);
        employee.SetInteractionUpgrade(workLevel, workItem != null ? GetValueAtLevel(workItem, workLevel) : 0f);
    }

    /// <summary>단계와 현재 가진 가격 정보를 UI에서 사용할 수 있게 제공합니다.</summary>
    public GrowthItem GetItem(string id) => FindItem(id);

    /// <summary>직원과 기계가 생성될 때에도 저장된 전체 효과를 읽도록 제공합니다.</summary>
    public float GetTotalValue(string id) => GetValue(id);

    /// <summary>저장된 모든 효과를 현재 씬의 대상에 다시 적용합니다.</summary>
    private void ApplyAll()
    {
        PlayerController player = PlayerController.Instance;
        if (player != null)
        {
            player.ApplyGrowth(
                GetValue("player_speed"),
                GetValue("player_stack"),
                GetValue("player_interact"));
        }

    }

    /// <summary>구매한 마지막 단계의 효과값을 돌려줍니다.</summary>
    private float GetValue(string id)
    {
        GrowthItem item = FindItem(id);
        int level = GetLevel(id);
        return GetValueAtLevel(item, level);
    }

    /// <summary>업그레이드 자료와 단계에서 지금까지 쌓인 시간 감소량을 계산합니다.</summary>
    private static float GetValueAtLevel(GrowthItem item, int level)
    {
        if (item == null || item.values == null || level <= 0 || level > item.values.Length)
            return 0f;

        float total = 0f;
        for (int i = 0; i < level; i++)
            total += item.values[i];
        return total;
    }

    /// <summary>카탈로그에서 ID가 일치하는 항목을 찾습니다.</summary>
    private GrowthItem FindItem(string id)
    {
        if (catalog == null || catalog.items == null)
            return null;
        return catalog.items.Find(item => item != null && item.id == id);
    }

    /// <summary>저장 파일의 버전을 확인하고 업그레이드 단계를 복원합니다.</summary>
    private void Load()
    {
        levels.Clear();
        GrowthSaveData data = Managers.PlayerPrefs.GetJson<GrowthSaveData>(SaveKey);
        if (data == null || (data.version < 1 || data.version > CurrentSaveVersion))
        {
            if (!Managers.PlayerPrefs.HasKey(SaveKey))
                return;
            Debug.LogWarning("성장 데이터 버전이 맞지 않아 기본 단계로 시작합니다.");
            return;
        }

        if (data.items != null)
        {
            foreach (GrowthSaveItem item in data.items)
            {
                if (item == null || string.IsNullOrEmpty(item.id) || item.level <= 0)
                    continue;

                // 이전 저장의 청소·제조 업그레이드 단계를 직원 작업 속도로 옮깁니다.
                string id = item.id == "employee_clean" ||
                            item.id == "employee_make"
                    ? "employee_interact"
                    : item.id;

                if (FindItem(id) == null)
                    continue;

                // 여러 예전 업그레이드가 함께 저장되어도 가장 높은 단계를 유지합니다.
                levels[id] = Mathf.Max(GetLevel(id), item.level);
            }
        }

    }

    /// <summary>현재 업그레이드 단계를 JSON으로 저장합니다.</summary>
    private void Save()
    {
        GrowthSaveData data = new GrowthSaveData { version = CurrentSaveVersion };
        foreach (KeyValuePair<string, int> pair in levels)
            data.items.Add(new GrowthSaveItem { id = pair.Key, level = pair.Value });
        Managers.PlayerPrefs.SetJson(SaveKey, data);
    }
}

/// <summary>업그레이드 저장 형식의 최상위 자료입니다.</summary>
[Serializable]
public class GrowthSaveData
{
    public int version;
    public List<GrowthSaveItem> items = new List<GrowthSaveItem>();
}

/// <summary>업그레이드 ID와 보유 단계를 저장합니다.</summary>
[Serializable]
public class GrowthSaveItem
{
    public string id;
    public int level;
}
