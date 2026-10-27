using UnityEngine;

/// <summary>메인 메뉴에서 재료 구매와 직원 선택 고용 화면을 전환합니다.</summary>
public class UI_MenuPopup : UI_Popup
{
    #region UI 요소

    /// <summary>메뉴 팝업에서 전환하는 패널입니다.</summary>
    public enum GameObjects
    {
        Panel_MainMenu,
        Panel_Market,
        Panel_HireEmployee,
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
    }

    #endregion

    #region 변수

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

        player = Managers.Game?.Player;

        // 재고 보충과 잔액 변경이 화면에 바로 반영되도록 두 이벤트를 구독합니다.
        if (Managers.Ingredient != null)
            Managers.Ingredient.OnIngredientsChanged += RefreshMarket;

        if (player != null)
            player.OnMoneyChanged += OnMoneyChanged;

        if (Managers.Employee != null)
            Managers.Employee.OnChanged += RefreshHireCandidates;

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

        if (market != null)
            market.SetActive(false);

        if (hire != null)
            hire.SetActive(false);

        if (mainMenu != null)
            mainMenu.SetActive(true);
    }

    /// <summary>메인 메뉴를 숨기고 현재 구매 가능한 재료 정보를 표시합니다.</summary>
    private void ShowMarket()
    {
        GameObject mainMenu = GetObject((int)GameObjects.Panel_MainMenu);
        GameObject market = GetObject((int)GameObjects.Panel_Market);
        GameObject hire = GetObject((int)GameObjects.Panel_HireEmployee);

        if (mainMenu != null)
            mainMenu.SetActive(false);

        if (hire != null)
            hire.SetActive(false);

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

        if (mainMenu != null)
            mainMenu.SetActive(false);

        if (market != null)
            market.SetActive(false);

        if (hire != null)
            hire.SetActive(true);

        RefreshHireCandidates();
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

        IngredientCatalog data = Managers.Ingredient?.GetIngredientData(type);
        if (data == null)
        {
            info.text = "재료 정보를 불러오는 중";
            buyButton.interactable = false;
            return;
        }

        int amount = Managers.Ingredient.GetAmount(type);
        info.text = $"{data.Name}\n가격: {data.Price}\n재고: {amount}";

        // 가격이 아직 0이면 임시 테스트 구매를 무료로 허용합니다.
        bool canPay = player != null && player.money >= data.Price;
        buyButton.interactable = data.Price >= 0 && canPay;
    }

    /// <summary>재료 하나의 가격을 지불하고 매장 재고를 한 개 늘립니다.</summary>
    private void BuyIngredient(Define.FoodIngredientType type)
    {
        if (Managers.Ingredient == null || player == null)
            return;

        IngredientCatalog data = Managers.Ingredient.GetIngredientData(type);
        if (data == null || data.Price < 0)
            return;

        // 유료 구매는 먼저 잔액을 확인하고 차감해 재고 변경 알림 전에 결제를 확정합니다.
        if (data.Price > 0 && !player.TrySpendMoney(data.Price))
        {
            RefreshMarket();
            return;
        }

        if (!Managers.Ingredient.AddIngredients(type, 1))
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
    }

    #endregion
}
