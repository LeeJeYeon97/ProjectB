using TMPro;
using UnityEngine;

public class UI_GameScene : UI_Scene
{
    private enum Texts
    {
        Text_Money,
    }

    private TMP_Text _moneyText;
    private PlayerController _player;

    public override void Init()
    {
        base.Init();

        Bind<TMP_Text>(typeof(Texts));
        _moneyText = GetTMP((int)Texts.Text_Money);

        BindPlayer();
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
