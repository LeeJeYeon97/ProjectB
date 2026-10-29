using UnityEngine;

/// <summary>메인 메뉴에서 재료 구매, 직원 고용, 플레이어 성장 화면을 전환합니다.</summary>
public class UI_MenuPopup : UI_Popup
{
    #region UI 요소

    /// <summary>메뉴 팝업에서 전환하는 패널입니다.</summary>
    public enum GameObjects
    {
        Panel_MainMenu,
        Panel_Market,
        Panel_HireEmployee,
        Panel_PlayerUpgrade,
    }

    /// <summary>메뉴 팝업에서 누를 수 있는 버튼입니다.</summary>
    public enum Buttons
    {
        Button_Market,
        Button_MarketBack,
        Button_BuyWater,
        Button_BuyUdonNoodle,
        Button_BuyUdonSauce,
        Button_HireEmployee,
        Button_HireBack,
        Button_HireSlot1,
        Button_HireSlot2,
        Button_HireSlot3,
        Button_Upgrade,
        Button_UpgradeBack,
        Button_BuyPlayerSpeed,
        Button_BuyPlayerStack,
        Button_BuyPlayerInteract,
    }

    /// <summary>시장 화면에 표시할 재료 정보입니다.</summary>
    public enum Texts
    {
        Text_WaterInfo,
        Text_UdonNoodleInfo,
        Text_UdonSauceInfo,
        Text_HireSlot1Info,
        Text_HireSlot2Info,
        Text_HireSlot3Info,
        Text_PlayerMoney,
        Text_PlayerSpeedInfo,
        Text_PlayerStackInfo,
        Text_PlayerInteractInfo,
    }

    #endregion

    #region 변수

    private const string PlayerSpeedId = "player_speed";
    private const string PlayerStackId = "player_stack";
    private const string PlayerInteractId = "player_interact";

    private PlayerController player;

    #endregion

    #region 초기화와 정리

    /// <summary>프리팹에 배치된 패널과 버튼을 연결하고 첫 화면을 준비합니다.</summary>
    public override void Init()
    {
        if (_init)
            return;

        base.Init();

        BindObject(typeof(GameObjects));
        BindButton(typeof(Buttons));
        BindText(typeof(Texts));

        // 메뉴 버튼과 시장 화면의 구매 버튼은 프리팹에 미리 배치해 둡니다.
        AddButtonAction(Buttons.Button_Market, ShowMarket);
        AddButtonAction(Buttons.Button_MarketBack, ShowMainMenu);
        AddButtonAction(Buttons.Button_BuyWater,
            () => BuyIngredient(Define.FoodIngredientType.Water));
        AddButtonAction(Buttons.Button_BuyUdonNoodle,
            () => BuyIngredient(Define.FoodIngredientType.UdonNoddle));
        AddButtonAction(Buttons.Button_BuyUdonSauce,
            () => BuyIngredient(Define.FoodIngredientType.UdonSauce));
        AddButtonAction(Buttons.Button_HireEmployee, ShowHireEmployee);
        AddButtonAction(Buttons.Button_HireBack, ShowMainMenu);
        AddButtonAction(Buttons.Button_HireSlot1, () => HireCandidate(0));
        AddButtonAction(Buttons.Button_HireSlot2, () => HireCandidate(1));
        AddButtonAction(Buttons.Button_HireSlot3, () => HireCandidate(2));
        AddButtonAction(Buttons.Button_Upgrade, ShowPlayerUpgrade);
        AddButtonAction(Buttons.Button_UpgradeBack, ShowMainMenu);
        AddButtonAction(Buttons.Button_BuyPlayerSpeed, () => BuyPlayerUpgrade(PlayerSpeedId));
        AddButtonAction(Buttons.Button_BuyPlayerStack, () => BuyPlayerUpgrade(PlayerStackId));
        AddButtonAction(Buttons.Button_BuyPlayerInteract, () => BuyPlayerUpgrade(PlayerInteractId));

        player = Managers.Game?.Player;

        // 재고 보충과 잔액 변경이 화면에 바로 반영되도록 두 이벤트를 구독합니다.
        if (Managers.Ingredient != null)
            Managers.Ingredient.OnIngredientsChanged += RefreshMarket;

        if (player != null)
            player.OnMoneyChanged += OnMoneyChanged;

        if (Managers.Employee != null)
            Managers.Employee.OnChanged += RefreshHireCandidates;

        // 다른 화면에서 성장이 바뀌어도 다시 열었을 때 현재 단계를 보여줍니다.
        if (Managers.Growth != null)
            Managers.Growth.OnChanged += RefreshPlayerUpgrades;

        ShowMainMenu();
    }

    /// <summary>팝업이 닫힐 때 매니저와 플레이어의 이벤트 참조를 해제합니다.</summary>
    public override void Clear()
    {
        if (Managers.Ingredient != null)
            Managers.Ingredient.OnIngredientsChanged -= RefreshMarket;

        if (player != null)
            player.OnMoneyChanged -= OnMoneyChanged;

        if (Managers.Employee != null)
            Managers.Employee.OnChanged -= RefreshHireCandidates;

        if (Managers.Growth != null)
            Managers.Growth.OnChanged -= RefreshPlayerUpgrades;

        base.Clear();
    }

    /// <summary>버튼이 프리팹에 존재할 때만 클릭 동작을 연결합니다.</summary>
    private void AddButtonAction(Buttons name, UnityEngine.Events.UnityAction action)
    {
        UnityEngine.UI.Button button = GetButton((int)name);
        if (button == null)
        {
            Debug.LogWarning($"[UI_MenuPopup] 버튼이 없습니다: {name}", this);
            return;
        }

        button.onClick.AddListener(action);
    }

    #endregion

    #region 패널 전환

    /// <summary>시장 화면을 닫고 기존 메인 메뉴 버튼을 다시 표시합니다.</summary>
    private void ShowMainMenu()
    {
        GameObject mainMenu = GetObject((int)GameObjects.Panel_MainMenu);
        GameObject market = GetObject((int)GameObjects.Panel_Market);
        GameObject hire = GetObject((int)GameObjects.Panel_HireEmployee);
        GameObject upgrade = GetObject((int)GameObjects.Panel_PlayerUpgrade);

        if (market != null)
            market.SetActive(false);

        if (hire != null)
            hire.SetActive(false);

        if (upgrade != null)
            upgrade.SetActive(false);

        if (mainMenu != null)
            mainMenu.SetActive(true);
    }

    /// <summary>메인 메뉴를 숨기고 현재 구매 가능한 재료 정보를 표시합니다.</summary>
    private void ShowMarket()
    {
        GameObject mainMenu = GetObject((int)GameObjects.Panel_MainMenu);
        GameObject market = GetObject((int)GameObjects.Panel_Market);
        GameObject hire = GetObject((int)GameObjects.Panel_HireEmployee);
        GameObject upgrade = GetObject((int)GameObjects.Panel_PlayerUpgrade);

        if (mainMenu != null)
            mainMenu.SetActive(false);

        if (hire != null)
            hire.SetActive(false);

        if (upgrade != null)
            upgrade.SetActive(false);

        if (market != null)
            market.SetActive(true);

        RefreshMarket();
    }

    /// <summary>직원 후보 패널을 열고 현재 고용 가능 상태를 표시합니다.</summary>
    private void ShowHireEmployee()
    {
        GameObject mainMenu = GetObject((int)GameObjects.Panel_MainMenu);
        GameObject market = GetObject((int)GameObjects.Panel_Market);
        GameObject hire = GetObject((int)GameObjects.Panel_HireEmployee);
        GameObject upgrade = GetObject((int)GameObjects.Panel_PlayerUpgrade);

        if (mainMenu != null)
            mainMenu.SetActive(false);

        if (market != null)
            market.SetActive(false);

        if (upgrade != null)
            upgrade.SetActive(false);

        if (hire != null)
            hire.SetActive(true);

        RefreshHireCandidates();
    }

    /// <summary>메인 메뉴를 닫고 플레이어 성장 카드와 현재 잔액을 표시합니다.</summary>
    private void ShowPlayerUpgrade()
    {
        GameObject mainMenu = GetObject((int)GameObjects.Panel_MainMenu);
        GameObject market = GetObject((int)GameObjects.Panel_Market);
        GameObject hire = GetObject((int)GameObjects.Panel_HireEmployee);
        GameObject upgrade = GetObject((int)GameObjects.Panel_PlayerUpgrade);

        if (mainMenu != null)
            mainMenu.SetActive(false);

        if (market != null)
            market.SetActive(false);

        if (hire != null)
            hire.SetActive(false);

        if (upgrade != null)
            upgrade.SetActive(true);

        RefreshPlayerUpgrades();
    }

    #endregion

    #region 직원 후보와 고용

    /// <summary>프리팹에 미리 배치된 세 카드에 직원별 능력과 고용 상태를 채웁니다.</summary>
    private void RefreshHireCandidates()
    {
        UpdateHireCard(0, Texts.Text_HireSlot1Info, Buttons.Button_HireSlot1);
        UpdateHireCard(1, Texts.Text_HireSlot2Info, Buttons.Button_HireSlot2);
        UpdateHireCard(2, Texts.Text_HireSlot3Info, Buttons.Button_HireSlot3);
    }

    /// <summary>한 후보의 가격, 속도, 가능한 업무를 표시하고 고용 버튼을 제어합니다.</summary>
    private void UpdateHireCard(int index, Texts textName, Buttons buttonName)
    {
        TMPro.TMP_Text info = GetText((int)textName);
        UnityEngine.UI.Button button = GetButton((int)buttonName);
        if (info == null || button == null)
            return;

        if (Managers.Employee == null || !Managers.Employee.CandidatesLoaded)
        {
            info.text = "직원 정보를 불러오는 중";
            button.interactable = false;
            return;
        }

        EmployeeData data = Managers.Employee.GetCandidateAt(index);
        if (data == null)
        {
            info.text = "등록된 직원 후보 없음";
            button.interactable = false;
            return;
        }

        string tasks = GetTaskNames(data);
        string price = data.HirePrice.ToString();
        if (data.HirePrice == 0)
            price = "무료 테스트";

        string state = "고용 가능";
        if (Managers.Employee.IsHired(data.Id))
            state = "고용 완료";
        else if (player == null || player.money < data.HirePrice)
            state = "잔액 부족";

        info.text = $"{data.Name}\n업무: {tasks}\n이동: {data.MoveSpeed:0.0}\n작업 시간: x{data.WorkTimeScale:0.00}\n고용비: {price}\n{state}";
        button.interactable = Managers.Employee.CanHire(data, player);
    }

    /// <summary>직원이 수행할 수 있는 업무 enum을 화면용 한국어로 바꿉니다.</summary>
    private string GetTaskNames(EmployeeData data)
    {
        if (data.Tasks == null || data.Tasks.Count == 0)
            return "없음";

        System.Collections.Generic.List<string> names = new();
        foreach (Define.EmployeeTaskType task in data.Tasks)
        {
            if (task == Define.EmployeeTaskType.Serving)
                names.Add("서빙");
            else if (task == Define.EmployeeTaskType.Casher)
                names.Add("계산");
            else if (task == Define.EmployeeTaskType.Cleaning)
                names.Add("청소");
            else if (task == Define.EmployeeTaskType.DishWash)
                names.Add("설거지");
            else if (task == Define.EmployeeTaskType.CarryIngredient)
                names.Add("재료 운반");
        }

        return string.Join(", ", names);
    }

    /// <summary>선택한 카드의 직원 한 명을 고용하고 결과를 화면에 반영합니다.</summary>
    private void HireCandidate(int index)
    {
        if (Managers.Employee == null || player == null)
            return;

        EmployeeData data = Managers.Employee.GetCandidateAt(index);
        if (data == null)
            return;

        Managers.Employee.TryHire(data.Id, player);
        RefreshHireCandidates();
    }

    #endregion

    #region 플레이어 성장과 구매

    /// <summary>잔액과 세 성장 카드의 현재 단계, 다음 효과, 구매 가능 여부를 갱신합니다.</summary>
    private void RefreshPlayerUpgrades()
    {
        TMPro.TMP_Text moneyText = GetText((int)Texts.Text_PlayerMoney);
        if (moneyText != null)
        {
            if (player == null)
                moneyText.text = "보유 금액: 확인 중";
            else
                moneyText.text = $"보유 금액: {player.money:N0}";
        }

        UpdatePlayerUpgradeCard(PlayerSpeedId, Texts.Text_PlayerSpeedInfo, Buttons.Button_BuyPlayerSpeed);
        UpdatePlayerUpgradeCard(PlayerStackId, Texts.Text_PlayerStackInfo, Buttons.Button_BuyPlayerStack);
        UpdatePlayerUpgradeCard(PlayerInteractId, Texts.Text_PlayerInteractInfo, Buttons.Button_BuyPlayerInteract);
    }

    /// <summary>카탈로그의 다음 단계 정보와 저장된 단계를 한 카드에 표시합니다.</summary>
    private void UpdatePlayerUpgradeCard(string id, Texts textName, Buttons buttonName)
    {
        TMPro.TMP_Text info = GetText((int)textName);
        UnityEngine.UI.Button button = GetButton((int)buttonName);
        if (info == null || button == null)
            return;

        GrowthManager growth = Managers.Growth;
        GrowthItem item = growth?.GetItem(id);
        if (item == null || item.prices == null || item.values == null)
        {
            info.text = "성장 정보를 불러올 수 없습니다";
            button.interactable = false;
            return;
        }

        int level = growth.GetLevel(id);
        int maxLevel = Mathf.Min(item.prices.Length, item.values.Length);
        float currentValue = growth.GetTotalValue(id);
        string currentEffect = GetPlayerEffectText(id, currentValue);

        if (level >= maxLevel)
        {
            info.text = $"{item.title} ({level}/{maxLevel})\n현재: {currentEffect}\n최대 단계";
            button.interactable = false;
            return;
        }

        int price = growth.GetNextPrice(id);
        float nextValue = currentValue + item.values[level];
        string nextEffect = GetPlayerEffectText(id, nextValue);
        info.text = $"{item.title} ({level}/{maxLevel})\n현재: {currentEffect}\n다음: {nextEffect}\n가격: {price:N0}";

        // 매니저의 구매 조건과 같은 양수 가격 및 현재 잔액을 버튼에 반영합니다.
        button.interactable = player != null && price > 0 && player.money >= price;
    }

    /// <summary>누적 성장 수치를 카드에서 읽기 쉬운 효과 문장으로 바꿉니다.</summary>
    private string GetPlayerEffectText(string id, float value)
    {
        if (id == PlayerSpeedId)
            return $"이동 속도 +{value:0.0}";

        if (id == PlayerStackId)
            return $"최대 적재량 +{Mathf.RoundToInt(value)}";

        if (id == PlayerInteractId)
            return $"작업 시간 -{value * 100f:0}%";

        return $"효과 +{value:0.##}";
    }

    /// <summary>선택한 플레이어 능력치의 구매를 매니저에 요청하고 표시를 다시 읽습니다.</summary>
    private void BuyPlayerUpgrade(string id)
    {
        if (Managers.Growth == null || player == null)
            return;

        // 가격 차감, 단계 저장, 능력치 적용은 기존 성장 매니저가 한 번에 처리합니다.
        Managers.Growth.TryUpgrade(id);
        RefreshPlayerUpgrades();
    }

    #endregion

    #region 시장 정보와 구매

    /// <summary>세 재료의 이름·가격·보유 수량과 구매 가능 여부를 갱신합니다.</summary>
    private void RefreshMarket()
    {
        UpdateIngredientInfo(Define.FoodIngredientType.Water,
            Texts.Text_WaterInfo, Buttons.Button_BuyWater);
        UpdateIngredientInfo(Define.FoodIngredientType.UdonNoddle,
            Texts.Text_UdonNoodleInfo, Buttons.Button_BuyUdonNoodle);
        UpdateIngredientInfo(Define.FoodIngredientType.UdonSauce,
            Texts.Text_UdonSauceInfo, Buttons.Button_BuyUdonSauce);
    }

    /// <summary>재료 데이터가 준비되었는지 확인하고 한 카드의 표시를 갱신합니다.</summary>
    private void UpdateIngredientInfo(
        Define.FoodIngredientType type,
        Texts textName,
        Buttons buttonName)
    {
        TMPro.TMP_Text info = GetText((int)textName);
        UnityEngine.UI.Button buyButton = GetButton((int)buttonName);
        if (info == null || buyButton == null)
            return;

        IngredientData data = Managers.Ingredient?.GetIngredientData(type);
        if (data == null)
        {
            info.text = "재료 정보를 불러오는 중";
            buyButton.interactable = false;
            return;
        }

        int amount = Managers.Ingredient.GetAmount(type);
        info.text = $"{data.Name}\n가격: {data.Price}\n재고 {amount} (총 {Managers.Ingredient.TotalAmount}/{Managers.Ingredient.Capacity})";

        // 가격이 아직 0이면 임시 테스트 구매를 무료로 허용합니다.
        bool canPay = player != null && player.money >= data.Price;
        buyButton.interactable = data.Price >= 0 && canPay && Managers.Ingredient.CanOrder(type);
    }

    /// <summary>재료 하나를 구매해 배송 지점으로 보냅니다.</summary>
    private void BuyIngredient(Define.FoodIngredientType type)
    {
        if (Managers.Ingredient == null || player == null)
            return;

        IngredientData data = Managers.Ingredient.GetIngredientData(type);
        if (data == null || data.Price < 0)
            return;

        if (!Managers.Ingredient.CanOrder(type))
        {
            RefreshMarket();
            return;
        }

        // 유료 구매는 먼저 잔액을 확인하고 차감해 재고 변경 알림 전에 결제를 확정합니다.
        if (data.Price > 0 && !player.TrySpendMoney(data.Price))
        {
            RefreshMarket();
            return;
        }

        if (!Managers.Ingredient.Deliver(type))
        {
            // 재고 추가가 실패하면 앞에서 차감한 금액을 돌려줍니다.
            if (data.Price > 0)
                player.UpdateMoney(data.Price);

            RefreshMarket();
            return;
        }

        RefreshMarket();
    }

    /// <summary>다른 화면에서 잔액이 달라져도 구매 버튼 상태를 갱신합니다.</summary>
    private void OnMoneyChanged(int money)
    {
        RefreshMarket();
        RefreshHireCandidates();
        RefreshPlayerUpgrades();
    }

    #endregion
}
