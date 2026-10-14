using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>업그레이드 단계와 구매, 효과 적용, 저장을 한 곳에서 처리합니다.</summary>
[Serializable]
public class GrowthManager
{
    private const string SaveKey = "ProjectB.Growth.Save";
    private const int CurrentSaveVersion = 3;

    [SerializeField] private GrowthCatalog catalog;
    private readonly Dictionary<string, int> levels = new Dictionary<string, int>();
    private readonly Dictionary<string, MachineGrowthSaveItem> machineLevels = new Dictionary<string, MachineGrowthSaveItem>();
    private int oldMachineCapacityLevel;

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
            MakeItem("machine_capacity", "기계 잔 보관량", new[] { 250, 500, 1000 }, new[] { 2f, 2f, 2f }, true),
            MakeItem("machine_speed", "기계 제조 속도", new[] { 400, 800, 1600 }, new[] { 0.1f, 0.1f, 0.1f }, true),
            MakeItem("sink_work_speed", "싱크대 작업 속도", new[] { 400, 800, 1600 }, new[] { 0.1f, 0.1f, 0.1f }, true)
        };
        return data;
    }

    /// <summary>기본 카탈로그의 한 항목을 보기 쉬운 형태로 만듭니다.</summary>
    private static GrowthItem MakeItem(string id, string title, int[] prices, float[] values, bool isMachineUpgrade = false)
    {
        return new GrowthItem
        {
            id = id,
            title = title,
            prices = prices,
            values = values,
            isMachineUpgrade = isMachineUpgrade
        };
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

    /// <summary>특정 기계가 보유한 업그레이드 단계를 돌려줍니다.</summary>
    public int GetMachineLevel(string machineId, string growthId)
    {
        if (string.IsNullOrEmpty(machineId))
            return 0;

        MachineGrowthSaveItem data = GetMachineData(machineId);
        if (data == null)
            return growthId == "machine_capacity" ? oldMachineCapacityLevel : 0;

        // 업그레이드 종류가 늘어나도 저장 자료 구조를 바꾸지 않고 ID로 단계를 찾습니다.
        if (data.upgrades != null)
        {
            GrowthLevelSaveItem savedLevel = data.upgrades.Find(item => item != null && item.id == growthId);
            if (savedLevel != null)
                return savedLevel.level;
        }

        // 예전 저장 파일의 두 기계 업그레이드는 새 목록 형식으로 옮기기 전까지 읽을 수 있게 둡니다.
        return growthId == "machine_capacity" ? data.capacityLevel :
            growthId == "machine_speed" ? data.speedLevel : 0;
    }

    /// <summary>특정 기계의 다음 단계 가격을 가져오며, 더 살 단계가 없으면 0을 돌려줍니다.</summary>
    public int GetMachineNextPrice(string machineId, string growthId)
    {
        GrowthItem item = FindItem(growthId);
        int level = GetMachineLevel(machineId, growthId);
        return item == null || item.prices == null || level >= item.prices.Length ? 0 : item.prices[level];
    }

    /// <summary>기계 업그레이드를 구매하고 해당 기계에만 단계와 효과를 반영합니다.</summary>
    public bool TryUpgradeMachine(InteractableBase machine, string growthId)
    {
        if (machine == null || string.IsNullOrEmpty(machine.MachineId) ||
            machine.UpgradeIds == null || !machine.UpgradeIds.Contains(growthId))
            return false;

        GrowthItem item = FindItem(growthId);
        PlayerController player = PlayerController.Instance;
        int level = GetMachineLevel(machine.MachineId, growthId);
        int price = GetMachineNextPrice(machine.MachineId, growthId);
        if (item == null || !item.isMachineUpgrade || player == null || item.values == null || level >= item.values.Length || price <= 0)
            return false;
        if (!player.TrySpendMoney(price))
            return false;

        MachineGrowthSaveItem data = GetOrCreateMachineData(machine.MachineId);
        SetMachineLevel(data, growthId, level + 1);

        Save();
        machine.ApplyGrowth();
        OnChanged?.Invoke();
        return true;
    }

    /// <summary>기계의 잔 보관량 증가 단계를 실제 추가 잔 수로 바꿔 제공합니다.</summary>
    public int GetMachineCapacityBonus(string machineId)
    {
        return Mathf.RoundToInt(GetMachineValue(machineId, "machine_capacity"));
    }

    /// <summary>기계의 속도 단계에 해당하는 총 시간 감소 비율을 제공합니다.</summary>
    public float GetMachineTimeReduction(string machineId)
    {
        return Mathf.Clamp01(GetMachineValue(machineId, "machine_speed"));
    }

    /// <summary>기계 업그레이드 ID에 해당하는 누적 효과값을 기계가 직접 사용할 수 있게 제공합니다.</summary>
    public float GetMachineUpgradeValue(string machineId, string growthId)
    {
        return GetMachineValue(machineId, growthId);
    }

    /// <summary>기계가 처음 등록될 때 예전 공용 보관량 단계를 해당 기계에 한 번 옮깁니다.</summary>
    public void RegisterMachine(InteractableBase machine)
    {
        if (machine == null || string.IsNullOrEmpty(machine.MachineId) || GetMachineData(machine.MachineId) != null)
            return;

        MachineGrowthSaveItem data = GetOrCreateMachineData(machine.MachineId);
        // 이전 버전의 공용 잔 보관량 단계는 첫 등록 기계에만 넘겨 기존 진행을 보존합니다.
        if (oldMachineCapacityLevel > 0)
            SetMachineLevel(data, "machine_capacity", oldMachineCapacityLevel);
        Save();
    }

    /// <summary>기계 업그레이드 단계를 ID 기준으로 저장 자료에 추가하거나 갱신합니다.</summary>
    private static void SetMachineLevel(MachineGrowthSaveItem data, string growthId, int level)
    {
        if (data.upgrades == null)
            data.upgrades = new List<GrowthLevelSaveItem>();

        GrowthLevelSaveItem savedLevel = data.upgrades.Find(item => item != null && item.id == growthId);
        if (savedLevel == null)
        {
            savedLevel = new GrowthLevelSaveItem { id = growthId };
            data.upgrades.Add(savedLevel);
        }

        savedLevel.level = level;

        // 구버전 데이터와 이어 붙이는 동안 기존 필드도 최신 값으로 유지합니다.
        if (growthId == "machine_capacity")
            data.capacityLevel = level;
        else if (growthId == "machine_speed")
            data.speedLevel = level;
    }

    /// <summary>기계 ID에 맞는 저장 자료를 찾아 돌려줍니다.</summary>
    private MachineGrowthSaveItem GetMachineData(string machineId)
    {
        return machineLevels.TryGetValue(machineId, out MachineGrowthSaveItem data) ? data : null;
    }

    /// <summary>기계 저장 자료가 없으면 새로 만들어 관리 목록에 넣습니다.</summary>
    private MachineGrowthSaveItem GetOrCreateMachineData(string machineId)
    {
        if (!machineLevels.TryGetValue(machineId, out MachineGrowthSaveItem data))
        {
            data = new MachineGrowthSaveItem { id = machineId };
            machineLevels.Add(machineId, data);
        }
        return data;
    }

    /// <summary>기계별 단계와 카탈로그 수치로 현재까지 누적된 효과를 계산합니다.</summary>
    private float GetMachineValue(string machineId, string growthId)
    {
        GrowthItem item = FindItem(growthId);
        int level = GetMachineLevel(machineId, growthId);
        return GetValueAtLevel(item, level);
    }

    /// <summary>구매가 가능하면 재화를 차감하고 단계와 효과를 함께 갱신합니다.</summary>
    public bool TryUpgrade(string id)
    {
        // 직원 능력치는 개별 직원 버튼에서만 구매하고, 기계 항목은 기계 화면에서만 구매합니다.
        if (id == "employee_speed" || id == "employee_interact")
            return false;

        GrowthItem item = FindItem(id);
        if (item != null && item.isMachineUpgrade)
            return false;
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

        MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is InteractableBase machine && machine.UpgradeIds.Count > 0)
                machine.ApplyGrowth();
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
        machineLevels.Clear();
        oldMachineCapacityLevel = 0;
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

                // 이전 저장의 청소·제조·기계 속도 단계를 새 직원 작업 속도 단계로 옮깁니다.
                string id = item.id == "employee_clean" ||
                            item.id == "employee_make" ||
                            item.id == "machine_time"
                    ? "employee_interact"
                    : item.id;

                if (id == "machine_capacity")
                {
                    oldMachineCapacityLevel = Mathf.Max(oldMachineCapacityLevel, item.level);
                    continue;
                }

                // 여러 예전 업그레이드가 함께 저장되어도 가장 높은 단계를 유지합니다.
                levels[id] = Mathf.Max(GetLevel(id), item.level);
            }
        }

        if (data.machines != null)
        {
            foreach (MachineGrowthSaveItem item in data.machines)
            {
                if (item != null && !string.IsNullOrEmpty(item.id))
                    machineLevels[item.id] = item;
            }
        }
    }

    /// <summary>현재 업그레이드 단계를 JSON으로 저장합니다.</summary>
    private void Save()
    {
        GrowthSaveData data = new GrowthSaveData { version = CurrentSaveVersion };
        foreach (KeyValuePair<string, int> pair in levels)
            data.items.Add(new GrowthSaveItem { id = pair.Key, level = pair.Value });
        foreach (MachineGrowthSaveItem item in machineLevels.Values)
            data.machines.Add(item);

        Managers.PlayerPrefs.SetJson(SaveKey, data);
    }
}

/// <summary>업그레이드 저장 형식의 최상위 자료입니다.</summary>
[Serializable]
public class GrowthSaveData
{
    public int version;
    public List<GrowthSaveItem> items = new List<GrowthSaveItem>();
    public List<MachineGrowthSaveItem> machines = new List<MachineGrowthSaveItem>();
}

/// <summary>기계 고유 ID별 업그레이드 단계를 저장합니다.</summary>
[Serializable]
public class MachineGrowthSaveItem
{
    public string id;
    public List<GrowthLevelSaveItem> upgrades = new List<GrowthLevelSaveItem>();
    // 버전 1, 2 저장 파일을 읽고 옮기기 위한 이전 필드입니다.
    public int capacityLevel;
    public int speedLevel;
}

/// <summary>기계 한 대가 가진 업그레이드 하나의 ID와 단계를 저장합니다.</summary>
[Serializable]
public class GrowthLevelSaveItem
{
    public string id;
    public int level;
}

/// <summary>업그레이드 ID와 보유 단계를 저장합니다.</summary>
[Serializable]
public class GrowthSaveItem
{
    public string id;
    public int level;
}
