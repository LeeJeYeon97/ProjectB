using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_GameScene : UI_Scene
{
    private enum Texts
    {
        Text_Money,
    }

    private enum Buttons
    {
        Button_HireEmployee,
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
    private TMP_Text _employeeGrowthTitle;
    private readonly Dictionary<string, Button> _employeeGrowthButtons = new Dictionary<string, Button>();
    private readonly Dictionary<string, TMP_Text> _employeeGrowthLabels = new Dictionary<string, TMP_Text>();
    private TMP_Text _seatCountText;

    private static readonly string[] GrowthIds =
    {
        "player_speed", "player_stack", "player_interact",
        "machine_capacity"
    };

    public override void Init()
    {
        base.Init();
        FixCanvasSize();

        Bind<TMP_Text>(typeof(Texts));
        Bind<Button>(typeof(Buttons));

        _moneyText = GetTMP((int)Texts.Text_Money);

        Button hireBtn = GetButton((int)Buttons.Button_HireEmployee);
        if (hireBtn != null)
        {
            hireBtn.onClick.AddListener(OnClickHireEmployee);
        }

        BindPlayer();
        CreateDishCountUI();
        BindDishManager();
        CreateGrowthUI();
        BindSeatCount();
    }

    /// <summary>화면 전체를 덮는 캔버스가 0 크기로 저장된 경우 정상 크기로 복구합니다.</summary>
    private void FixCanvasSize()
    {
        RectTransform canvasRect = transform as RectTransform;
        if (canvasRect == null)
            return;

        // 화면 전체를 기준으로 늘어나도록 앵커와 여백을 통일합니다.
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;
        canvasRect.pivot = new Vector2(0.5f, 0.5f);
        canvasRect.localScale = Vector3.one;
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

    /// <summary>
    /// 화면 왼쪽 위에 접시 수량을 보여주는 간단한 배경과 글자를 만듭니다.
    /// 프리팹에 별도 오브젝트를 추가하지 않아도 UI_GameScene과 함께 표시됩니다.
    /// </summary>
    private void CreateDishCountUI()
    {
        // 다시 초기화되는 경우 기존 표시가 있으면 중복 생성하지 않습니다.
        if (_dishCountText != null)
            return;

        GameObject panelObject = new GameObject("Panel_DishCount", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(transform, false);

        // 기준 해상도와 화면 비율이 달라도 왼쪽 위에 고정되도록 앵커를 설정합니다.
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(32f, -105f);
        panelRect.sizeDelta = new Vector2(340f, 112f);

        // 어두운 반투명 배경으로 게임 화면 위에서도 글자가 잘 보이게 합니다.
        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.06f, 0.08f, 0.1f, 0.78f);
        panelImage.raycastTarget = false;

        GameObject textObject = new GameObject("Text_DishCount", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panelObject.transform, false);

        // 글자를 패널 안쪽에 배치하고 두 줄의 재고를 표시할 공간을 확보합니다.
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 12f);
        textRect.offsetMax = new Vector2(-12f, -12f);

        _dishCountText = textObject.GetComponent<TextMeshProUGUI>();
        _dishCountText.font = TMP_Settings.defaultFontAsset;
        _dishCountText.fontSize = 27f;
        _dishCountText.color = Color.white;
        _dishCountText.alignment = TextAlignmentOptions.Left;
        _dishCountText.enableWordWrapping = false;
        _dishCountText.raycastTarget = false;
    }

    /// <summary>업그레이드 버튼과 가구 배치 진입 버튼을 화면에 생성합니다.</summary>
    private void CreateGrowthUI()
    {
        if (_furniturePlacement == null)
            _furniturePlacement = gameObject.AddComponent<FurniturePlacementController>();

        Button openButton = CreateButton("Button_Growth", transform, "성장", new Vector2(1f, 1f), new Vector2(-115f, -65f), new Vector2(190f, 66f));
        openButton.onClick.AddListener(() =>
        {
            if (_growthPanel != null)
                _growthPanel.SetActive(!_growthPanel.activeSelf);
        });

        GameObject panel = new GameObject("Panel_Growth", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        panel.transform.SetParent(transform, false);
        _growthPanel = panel;
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-18f, -140f);
        panelRect.sizeDelta = new Vector2(Mathf.Min(420f, Screen.width - 32f), Mathf.Min(560f, Screen.height - 180f));
        panel.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.09f, 0.94f);

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(panel.transform, false);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(8f, 8f);
        viewportRect.offsetMax = new Vector2(-8f, -8f);

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 7f;
        layout.padding = new RectOffset(5, 5, 5, 5);
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = panel.GetComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        foreach (string id in GrowthIds)
        {
            Button button = CreateButton("Button_" + id, content.transform, "", Vector2.zero, Vector2.zero, new Vector2(0f, 64f));
            _growthTexts[id] = button.GetComponentInChildren<TMP_Text>();
            button.onClick.AddListener(() => OnClickGrowth(id));
        }

        // 테스트 중 직원마다 다른 작업 속도를 구매할 수 있도록 직원별 버튼 영역을 만듭니다.
        GameObject employeeRoot = new GameObject(
            "Panel_EmployeeWorkGrowth",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));
        employeeRoot.transform.SetParent(content.transform, false);
        _employeeGrowthRoot = employeeRoot.transform;

        VerticalLayoutGroup employeeLayout = employeeRoot.GetComponent<VerticalLayoutGroup>();
        employeeLayout.spacing = 6f;
        employeeLayout.childControlHeight = true;
        employeeLayout.childControlWidth = true;
        employeeLayout.childForceExpandHeight = false;
        employeeLayout.childForceExpandWidth = true;

        ContentSizeFitter employeeFitter = employeeRoot.GetComponent<ContentSizeFitter>();
        employeeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        employeeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        _employeeGrowthTitle = CreateLabel(
            "Text_EmployeeWorkGrowth",
            _employeeGrowthRoot,
            "직원별 능력치 (테스트)",
            22f);
        LayoutElement employeeTitleLayout = _employeeGrowthTitle.gameObject.AddComponent<LayoutElement>();
        employeeTitleLayout.preferredHeight = 42f;

        Button chairButton = CreateButton("Button_PlaceChair", content.transform, "의자 배치", Vector2.zero, Vector2.zero, new Vector2(0f, 64f));
        chairButton.onClick.AddListener(OnClickPlaceChair);
        _seatCountText = CreateLabel("Text_SeatCount", content.transform, "좌석 0개", 25f);
        LayoutElement seatLayout = _seatCountText.gameObject.AddComponent<LayoutElement>();
        seatLayout.preferredHeight = 50f;

        _growthPanel.SetActive(false);
        RefreshGrowthUI();
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

        // 직원 목록의 순번과 능력치 이름으로 버튼 키를 만들어 폐기 예정인 Instance ID를 쓰지 않습니다.
        string key = employeeIndex + "_" + growthId;
        currentKeys.Add(key);

        if (!_employeeGrowthButtons.TryGetValue(key, out Button button))
        {
            button = CreateButton(
                "Button_" + key,
                _employeeGrowthRoot,
                "",
                Vector2.zero,
                Vector2.zero,
                new Vector2(0f, 58f));

            _employeeGrowthButtons[key] = button;
            _employeeGrowthLabels[key] = button.GetComponentInChildren<TMP_Text>();
            button.onClick.AddListener(() => OnClickEmployeeGrowth(employee, growthId));
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

    /// <summary>의자 선택을 배치 모드로 넘기고 구매는 배치를 확정할 때 처리합니다.</summary>
    private void OnClickPlaceChair()
    {
        if (_furniturePlacement != null && !_furniturePlacement.BeginPlacement("chair"))
            Debug.LogWarning("의자 배치 실패: FurnitureCatalog의 chair 항목과 프리팹을 확인하세요.");
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

    /// <summary>공통 버튼 프레임과 글자를 만들어 화면 해상도에 맞게 배치합니다.</summary>
    private Button CreateButton(string objectName, Transform parent, string text, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        bool arrangedByLayout = parent.GetComponent<LayoutGroup>() != null;
        if (arrangedByLayout)
        {
            // 세로 목록 안에서는 부모 너비를 따라가도록 가로 앵커를 사용합니다.
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, size.y);
        }
        else
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.15f, 0.3f, 0.38f, 0.96f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
        layout.preferredHeight = size.y;

        TMP_Text label = CreateLabel("Text", buttonObject.transform, text, 23f);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10f, 4f);
        labelRect.offsetMax = new Vector2(-10f, -4f);
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;
        return button;
    }

    /// <summary>기본 TMP 글자를 만들어 런타임 UI에 붙입니다.</summary>
    private TMP_Text CreateLabel(string objectName, Transform parent, string text, float fontSize)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TMP_Text label = textObject.GetComponent<TMP_Text>();
        label.font = TMP_Settings.defaultFontAsset;
        label.text = text;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Left;
        label.raycastTarget = false;
        return label;
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
    }
}
