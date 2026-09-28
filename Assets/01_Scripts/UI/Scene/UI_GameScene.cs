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
    private PlayerController _player;

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
    }
}
