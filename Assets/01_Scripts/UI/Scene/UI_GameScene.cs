using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_GameScene : UI_Scene
{
    private enum Texts
    {
        Text_Money,
        Text_DishCount,
        Text_EmployeeWorkGrowth,
        Text_SeatCount,
    }

    private enum Buttons
    {
        Button_HireEmployee,
        Button_Growth,
        Button_player_speed,
        Button_player_stack,
        Button_player_interact,
        Button_machine_capacity,
        Button_PlaceChair,
    }

    private enum Objects
    {
        Panel_Growth,
        Button_EmployeeGrowthTemplate,
    }

    private TMP_Text _moneyText;
    private TMP_Text _dishCountText;
    private PlayerController _player;
    private DishManager _dishManager;
    private StationManager _stationManager;
    private FurniturePlacementController _furniturePlacement;
    private GameObject _growthPanel;
    private readonly Dictionary<string, TMP_Text> _growthTexts = new Dictionary<string, TMP_Text>();
    private Transform _employeeGrowthRoot;
    private Button _employeeGrowthTemplate;
    private TMP_Text _employeeGrowthTitle;
    private readonly Dictionary<string, Button> _employeeGrowthButtons = new Dictionary<string, Button>();
    private readonly Dictionary<string, TMP_Text> _employeeGrowthLabels = new Dictionary<string, TMP_Text>();
    private TMP_Text _seatCountText;
    private Button _placeFurnitureButton;

    private static readonly string[] GrowthIds =
    {
        "player_speed", "player_stack", "player_interact",
        "machine_capacity"
    };

    public override void Init()
    {
        base.Init();
        //FixCanvasSize();

        Bind<TMP_Text>(typeof(Texts));
        Bind<Button>(typeof(Buttons));
        Bind<GameObject>(typeof(Objects));

        _moneyText = GetTMP((int)Texts.Text_Money);
        _dishCountText = GetTMP((int)Texts.Text_DishCount);
        _employeeGrowthTitle = GetTMP((int)Texts.Text_EmployeeWorkGrowth);
        _seatCountText = GetTMP((int)Texts.Text_SeatCount);
        _growthPanel = GetObject((int)Objects.Panel_Growth);
        _furniturePlacement = GetComponent<FurniturePlacementController>();
        if (_furniturePlacement != null)
        {
            _furniturePlacement.OnInventoryChanged -= RefreshFurnitureButtons;
            _furniturePlacement.OnInventoryChanged += RefreshFurnitureButtons;
        }
        GameObject employeeTemplateObject = GetObject((int)Objects.Button_EmployeeGrowthTemplate);
        _employeeGrowthTemplate = employeeTemplateObject != null
            ? employeeTemplateObject.GetComponent<Button>()
            : null;
        _employeeGrowthRoot = _employeeGrowthTemplate != null ? _employeeGrowthTemplate.transform.parent : null;

        Button hireBtn = GetButton((int)Buttons.Button_HireEmployee);
        if (hireBtn != null)
        {
            hireBtn.onClick.AddListener(OnClickHireEmployee);
        }

        Button openGrowthButton = GetButton((int)Buttons.Button_Growth);
        if (openGrowthButton != null)
            openGrowthButton.onClick.AddListener(OnClickGrowthPanel);

        BindGrowthButton("player_speed", Buttons.Button_player_speed);
        BindGrowthButton("player_stack", Buttons.Button_player_stack);
        BindGrowthButton("player_interact", Buttons.Button_player_interact);
        BindGrowthButton("machine_capacity", Buttons.Button_machine_capacity);

        Button placeChairButton = GetButton((int)Buttons.Button_PlaceChair);
        if (placeChairButton != null)
        {
            placeChairButton.onClick.AddListener(OnClickBuyChair);
            TMP_Text buyLabel = placeChairButton.GetComponentInChildren<TMP_Text>(true);
            if (buyLabel != null)
                buyLabel.text = "의자 구매";
            _placeFurnitureButton = CreateFurniturePlaceButton(placeChairButton);
            RefreshFurnitureButtons();
        }

        BindPlayer();
        BindDishManager();
        BindSeatCount();
        RefreshGrowthUI();
    }

    /// <summary>프리팹에 미리 배치한 성장 버튼에 해당 업그레이드 동작을 연결합니다.</summary>
    private void BindGrowthButton(string growthId, Buttons buttonId)
    {
        Button button = GetButton((int)buttonId);
        if (button == null)
            return;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            _growthTexts[growthId] = label;

        button.onClick.AddListener(() => OnClickGrowth(growthId));
    }

    /// <summary>성장 패널을 열거나 닫고 버튼의 현재 상태를 최신 정보로 맞춥니다.</summary>
    private void OnClickGrowthPanel()
    {
        if (_growthPanel == null)
            return;

        _growthPanel.SetActive(!_growthPanel.activeSelf);
        RefreshGrowthUI();
    }

    private void OnClickHireEmployee()
    {
        if (_player == null)
        {
            _player = PlayerController.Instance;
            if (_player == null)
                _player = FindAnyObjectByType<PlayerController>();
        }

        int cost = Managers.Employee != null ? Managers.Employee.HireCost : 1000;
        if (_player != null && _player.TrySpendMoney(cost))
        {
            Managers.Employee.HireEmployee();
            RefreshEmployeeGrowthUI();
            Debug.Log($"[UI_GameScene] 직원 고용 완료! (소모 금액: {cost}원, 남은 금액: {_player.money}원)");
        }
        else
        {
            int current = _player != null ? _player.money : 0;
            Debug.LogWarning($"[UI_GameScene] 직원 고용 실패: 돈이 부족합니다! (필요: {cost}원, 현재: {current}원)");
        }
    }

    private void Start()
    {
        if (_player == null)
        {
            BindPlayer();
        }
    }

    private void Update()
    {
        if (_player == null && PlayerController.Instance != null)
        {
            BindPlayer();
        }
    }

    private void BindPlayer()
    {
        if (_player != null)
        {
            _player.OnMoneyChanged -= UpdateMoneyText;
        }

        _player = PlayerController.Instance;
        if (_player == null)
        {
            _player = FindAnyObjectByType<PlayerController>();
        }

        if (_player != null)
        {
            _player.OnMoneyChanged -= UpdateMoneyText;
            _player.OnMoneyChanged += UpdateMoneyText;
            UpdateMoneyText(_player.money);
        }
    }

    /// <summary>구매 단계와 재화 정보를 갱신하고 구매 결과를 한눈에 보이게 표시합니다.</summary>
    private void RefreshGrowthUI()
    {
        if (Managers.Growth == null)
            return;

        foreach (string id in GrowthIds)
        {
            if (!_growthTexts.TryGetValue(id, out TMP_Text label))
                continue;
            GrowthItem item = Managers.Growth.GetItem(id);
            if (item == null)
                continue;

            int level = Managers.Growth.GetLevel(id);
            int price = Managers.Growth.GetNextPrice(id);
            string nextEffect = level < item.values.Length ? $"   효과 {FormatGrowthValue(id, item.values[level])}" : "";
            label.text = price > 0
                ? $"{item.title}  Lv.{level} → {level + 1}{nextEffect}   {price:N0}원"
                : $"{item.title}  Lv.MAX";
        }

        RefreshEmployeeGrowthUI();

        if (_seatCountText != null && Managers.Station != null)
            _seatCountText.text = $"현재 좌석 {Managers.Station.AvailableSeatCount}개";
    }

    /// <summary>업그레이드별 숫자를 플레이어가 이해하기 쉬운 증가/감소 값으로 표시합니다.</summary>
    private static string FormatGrowthValue(string id, float value)
    {
        if (id == "player_stack" || id == "machine_capacity")
            return $"+{Mathf.RoundToInt(value)}개";
        if (id == "player_interact")
            return $"-{value * 100f:0}% 시간";
        return $"+{value:0.0} 속도";
    }

    /// <summary>선택한 업그레이드를 구매하고 성공 여부에 맞춰 화면을 갱신합니다.</summary>
    private void OnClickGrowth(string id)
    {
        bool bought = Managers.Growth.TryUpgrade(id);
        if (!bought)
            Debug.Log("업그레이드 구매 실패: 재화가 부족하거나 최대 단계입니다.");
        RefreshGrowthUI();
    }

    /// <summary>고용된 직원마다 작업 속도 업그레이드 버튼과 개별 단계를 표시합니다.</summary>
    private void RefreshEmployeeGrowthUI()
    {
        if (_employeeGrowthRoot == null)
            return;

        IReadOnlyList<EmployeeController> employees = Managers.Employee != null
            ? Managers.Employee.Employees
            : null;

        if (_employeeGrowthTitle != null)
            _employeeGrowthTitle.text = employees != null && employees.Count > 0
                ? "직원별 능력치 (테스트)"
                : "직원별 능력치 (직원을 고용하세요)";

        if (employees == null)
            return;

        if (Managers.Growth == null)
            return;

        HashSet<string> currentUpgradeKeys = new HashSet<string>();
        for (int employeeIndex = 0; employeeIndex < employees.Count; employeeIndex++)
        {
            EmployeeController employee = employees[employeeIndex];
            if (employee == null)
                continue;

            RefreshEmployeeGrowthButton(employee, employeeIndex, "employee_speed", currentUpgradeKeys);
            RefreshEmployeeGrowthButton(employee, employeeIndex, "employee_interact", currentUpgradeKeys);
        }

        // 해고나 제거된 직원의 버튼은 목록에서 치워 잘못된 대상을 업그레이드하지 않게 합니다.
        List<string> removedUpgradeKeys = new List<string>();
        foreach (KeyValuePair<string, Button> pair in _employeeGrowthButtons)
        {
            if (currentUpgradeKeys.Contains(pair.Key))
                continue;

            Destroy(pair.Value.gameObject);
            _employeeGrowthLabels.Remove(pair.Key);
            removedUpgradeKeys.Add(pair.Key);
        }

        foreach (string key in removedUpgradeKeys)
            _employeeGrowthButtons.Remove(key);
    }

    /// <summary>직원 한 명의 이동 또는 작업 속도를 표시하고 해당 업그레이드 버튼을 만듭니다.</summary>
    private void RefreshEmployeeGrowthButton(EmployeeController employee, int employeeIndex, string growthId, HashSet<string> currentKeys)
    {
        GrowthItem item = Managers.Growth.GetItem(growthId);
        if (item == null || item.prices == null || item.values == null)
            return;

        if (_employeeGrowthTemplate == null || _employeeGrowthRoot == null)
            return;

        //if (_employeeGrowthTitle != null)
        //    _employeeGrowthTitle.text = employees.Count > 0
        //        ? "직원별 능력치"
        //        : "직원별 능력치 (직원을 고용하세요)";

        // 직원 목록의 순번과 능력치 이름으로 버튼 키를 만들어 폐기 예정인 Instance ID를 쓰지 않습니다.
        string key = employeeIndex + "_" + growthId;
        currentKeys.Add(key);

        if (!_employeeGrowthButtons.TryGetValue(key, out Button button))
        {
            // 직원별로 수가 달라지는 항목은 프리팹의 미리 꾸민 버튼을 복제해 재사용합니다.
            button = Instantiate(_employeeGrowthTemplate, _employeeGrowthRoot, false);
            button.gameObject.name = "Button_" + key;
            button.gameObject.SetActive(true);

            _employeeGrowthButtons[key] = button;
            _employeeGrowthLabels[key] = button.GetComponentInChildren<TMP_Text>(true);

            // 버튼이 만들어질 때의 직원을 저장해 다른 직원에게 잘못 적용되지 않게 합니다.
            EmployeeController targetEmployee = employee;
            string targetGrowthId = growthId;
            button.onClick.AddListener(() => OnClickEmployeeGrowth(targetEmployee, targetGrowthId));
        }

        int level = growthId == "employee_speed"
            ? employee.MoveUpgradeLevel
            : employee.InteractionUpgradeLevel;
        TMP_Text label = _employeeGrowthLabels[key];
        string statName = growthId == "employee_speed" ? "이동 속도" : "작업 속도";

        if (level >= item.prices.Length || level >= item.values.Length)
        {
            label.text = $"{employee.name}  {statName} Lv.MAX";
            button.interactable = false;
            return;
        }

        int price = item.prices[level];
        float value = item.values[level];
        string effect = growthId == "employee_speed"
            ? $"+{value:0.0} 속도"
            : $"-{value * 100f:0}% 시간";
        label.text = $"{employee.name}  {statName} Lv.{level} → {level + 1}  ({effect})  {price:N0}원";
        button.interactable = price > 0;
    }

    /// <summary>선택한 테스트 버튼의 직원 한 명에게 지정한 능력치만 적용합니다.</summary>
    private void OnClickEmployeeGrowth(EmployeeController employee, string growthId)
    {
        bool bought = Managers.Growth != null && Managers.Growth.TryUpgradeEmployeeStat(employee, growthId);
        if (!bought)
            Debug.Log("직원 능력치 업그레이드 실패: 재화가 부족하거나 해당 능력치가 최대 단계입니다.");

        RefreshEmployeeGrowthUI();
    }

    /// <summary>의자 구매만 처리해 재고에 넣고 배치 흐름과 분리합니다.</summary>
    private void OnClickBuyChair()
    {
        if (_furniturePlacement == null)
            return;

        if (!_furniturePlacement.BuyFurniture("chair"))
        {
            Debug.LogWarning("의자 구매 실패: 재화와 FurnitureCatalog의 chair 항목을 확인하세요.");
            return;
        }

        RefreshFurnitureButtons();
    }

    /// <summary>구매해 보유 중인 의자를 꺼내 설치하는 모드로 들어갑니다.</summary>
    private void OnClickPlaceChair()
    {
        if (_furniturePlacement == null || !_furniturePlacement.BeginPlacement("chair"))
        {
            Debug.LogWarning("의자 배치 실패: 구매한 의자가 있는지 확인하세요.");
            return;
        }

        // 배치 입력이 성장 패널에 가로막히지 않도록 배치 모드가 시작되면 패널을 닫습니다.
        if (_growthPanel != null)
            _growthPanel.SetActive(false);
    }

    /// <summary>기존 구매 버튼 옆에 별도의 배치 버튼을 만들어 구매와 설치를 분리합니다.</summary>
    private Button CreateFurniturePlaceButton(Button sourceButton)
    {
        GameObject buttonObject = Instantiate(sourceButton.gameObject, sourceButton.transform.parent);
        buttonObject.name = "Button_PlaceOwnedChair";
        RectTransform sourceRect = sourceButton.GetComponent<RectTransform>();
        RectTransform newRect = buttonObject.GetComponent<RectTransform>();
        newRect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(190f, 0f);
        Button button = buttonObject.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClickPlaceChair);
        TMP_Text label = buttonObject.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = "의자 배치";
        return button;
    }

    /// <summary>구매한 의자 수를 버튼에 표시하고 재고가 있을 때만 배치 버튼을 켭니다.</summary>
    private void RefreshFurnitureButtons()
    {
        if (_furniturePlacement == null)
            return;
        int count = _furniturePlacement.GetInventoryCount("chair");
        if (_placeFurnitureButton != null)
        {
            // 새 의자 설치는 보유 수량이 필요합니다. 기존 의자는 맵에서 직접 눌러 옮깁니다.
            _placeFurnitureButton.interactable = count > 0;
            TMP_Text label = _placeFurnitureButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = $"의자 배치 ({count})";
        }
    }

    /// <summary>화면이 사라질 때 가구 재고 변경 알림 연결을 정리합니다.</summary>
    private void OnDestroy()
    {
        if (_furniturePlacement != null)
            _furniturePlacement.OnInventoryChanged -= RefreshFurnitureButtons;
    }

    /// <summary>좌석 등록 변경을 구독해 좌석 수 표시를 최신 값으로 맞춥니다.</summary>
    private void BindSeatCount()
    {
        _stationManager = Managers.Station;
        if (_stationManager == null)
            return;
        _stationManager.OnSeatCountChanged -= OnSeatCountChanged;
        _stationManager.OnSeatCountChanged += OnSeatCountChanged;
        RefreshGrowthUI();
    }

    /// <summary>좌석 수가 바뀌면 성장 패널의 좌석 정보를 다시 그립니다.</summary>
    private void OnSeatCountChanged(int count)
    {
        RefreshGrowthUI();
    }

    /// <summary>접시 재고 매니저의 변경 알림을 연결하고 현재 수량을 바로 표시합니다.</summary>
    private void BindDishManager()
    {
        _dishManager = Managers.Dish;
        if (_dishManager == null)
            return;

        _dishManager.OnDishCountChanged -= UpdateDishCountText;
        _dishManager.OnDishCountChanged += UpdateDishCountText;
        UpdateDishCountText(_dishManager.DirtyDishCount, _dishManager.CleanDishCount);
    }

    /// <summary>더러운 접시와 깨끗한 접시 수량을 화면에 갱신합니다.</summary>
    private void UpdateDishCountText(int dirtyDishCount, int cleanDishCount)
    {
        if (_dishCountText == null)
            return;

        _dishCountText.text = $"깨끗한 접시  {cleanDishCount}\n더러운 접시  {dirtyDishCount}";
    }

    public void UpdateMoneyText(int money)
    {
        if (_moneyText != null)
        {
            _moneyText.text = $"{money:N0}";
        }
    }

    public override void Clear()
    {
        base.Clear();

        if (_player != null)
        {
            _player.OnMoneyChanged -= UpdateMoneyText;
            _player = null;
        }

        if (_dishManager != null)
        {
            _dishManager.OnDishCountChanged -= UpdateDishCountText;
            _dishManager = null;
        }

        if (_stationManager != null)
        {
            _stationManager.OnSeatCountChanged -= OnSeatCountChanged;
            _stationManager = null;
        }

        if (_furniturePlacement != null)
        {
            // 이 컨트롤러는 화면 오브젝트와 함께 정리되므로 배치 상태만 취소합니다.
            _furniturePlacement.CancelPlacement();
            _furniturePlacement.enabled = false;
            _furniturePlacement = null;
        }

        foreach (Button button in _employeeGrowthButtons.Values)
        {
            if (button != null)
                Destroy(button.gameObject);
        }

        _employeeGrowthButtons.Clear();
        _employeeGrowthLabels.Clear();
    }
}
