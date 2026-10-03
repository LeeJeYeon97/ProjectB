using TMPro;
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

    public override void Init()
    {
        base.Init();

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
        if (_player != null && _player.money >= cost)
        {
            _player.UpdateMoney(-cost);
            Managers.Employee.HireEmployee();
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
    }
}
