using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>업그레이드 단계와 구매, 효과 적용, 저장을 한 곳에서 처리합니다.</summary>
[Serializable]
public class GrowthManager
{
    private const string SaveKey = "ProjectB.Growth.Save";
    private const int CurrentSaveVersion = 1;

    [SerializeField] private GrowthCatalog catalog;
    private readonly Dictionary<string, int> levels = new Dictionary<string, int>();

    public event Action OnChanged;

    /// <summary>저장한 단계를 불러오고 현재 플레이어와 직원에 효과를 적용합니다.</summary>
    public void Init()
    {
        catalog = Managers.Resource.LoadResource<GrowthCatalog>("GrowthCatalog");
        if (catalog == null)
            catalog = CreateDefaultCatalog();
        Load();
        ApplyAll();
    }

    /// <summary>에셋 카탈로그가 아직 없을 때도 첫 업그레이드 흐름을 쓸 수 있게 기본값을 만듭니다.</summary>
    private static GrowthCatalog CreateDefaultCatalog()
    {
        GrowthCatalog data = ScriptableObject.CreateInstance<GrowthCatalog>();
        data.items = new List<GrowthItem>
        {
            MakeItem("player_speed", "플레이어 이동 속도", new[] { 100, 250, 500 }, new[] { 0.5f, 0.5f, 0.5f }),
            MakeItem("player_stack", "플레이어 적재량", new[] { 150, 300, 600 }, new[] { 2f, 2f, 2f }),
            MakeItem("player_interact", "상호작용 속도", new[] { 150, 300, 600 }, new[] { 0.08f, 0.08f, 0.08f }),
            MakeItem("employee_speed", "직원 이동 속도", new[] { 200, 400, 800 }, new[] { 0.4f, 0.4f, 0.4f }),
            MakeItem("employee_interact", "직원 작업 속도", new[] { 200, 400, 800 }, new[] { 0.08f, 0.08f, 0.08f }),
            MakeItem("machine_capacity", "기계 잔 보관량", new[] { 250, 500, 1000 }, new[] { 2f, 2f, 2f })
        };
        return data;
    }

    /// <summary>기본 카탈로그의 한 항목을 보기 쉬운 형태로 만듭니다.</summary>
    private static GrowthItem MakeItem(string id, string title, int[] prices, float[] values)
    {
        return new GrowthItem { id = id, title = title, prices = prices, values = values };
    }

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
        // 직원 능력치는 개별 직원 버튼에서만 구매하도록 공통 업그레이드 요청을 막습니다.
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

        BeerMachine.ApplyGrowthToAll(GetValue("machine_capacity"));
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
        if (!PlayerPrefs.HasKey(SaveKey))
            return;

        GrowthSaveData data = JsonUtility.FromJson<GrowthSaveData>(PlayerPrefs.GetString(SaveKey));
        if (data == null || data.version != CurrentSaveVersion || data.items == null)
        {
            Debug.LogWarning("성장 데이터 버전이 맞지 않아 기본 단계로 시작합니다.");
            return;
        }

        foreach (GrowthSaveItem item in data.items)
        {
            if (item == null || string.IsNullOrEmpty(item.id) || item.level <= 0)
                continue;

            // 이전 저장의 청소·제조·기계 속도 단계를 새 직원 작업 속도 단계로 옮깁니다.
            string id = item.id == "employee_clean" ||
                        item.id == "employee_make" ||
                        item.id == "machine_time"
                ? "employee_interact"
                : item.id;

            // 여러 예전 업그레이드가 함께 저장되어도 가장 높은 단계를 유지합니다.
            levels[id] = Mathf.Max(GetLevel(id), item.level);
        }
    }

    /// <summary>현재 업그레이드 단계를 JSON으로 저장합니다.</summary>
    private void Save()
    {
        GrowthSaveData data = new GrowthSaveData { version = CurrentSaveVersion };
        foreach (KeyValuePair<string, int> pair in levels)
            data.items.Add(new GrowthSaveItem { id = pair.Key, level = pair.Value });

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
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
