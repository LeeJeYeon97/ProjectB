using System;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : BaseController
{
    public static PlayerController Instance { get; private set; }

    public int money = 100;
    public event Action<int> OnMoneyChanged;

    public float throwInterval = 0.1f; // 0.1초마다 1번씩 돈을 날림
    public int throwAmount = 1;       // 한 번에 1원(또는 지정된 금액)씩 날림

    public StackingSystem Stacking;

    private void Awake()
    {
        Instance = this;
        Stacking = GetComponentInChildren<StackingSystem>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    public void UpdateMoney(int addMoney)
    {
        money += addMoney;
        OnMoneyChanged?.Invoke(money);
    }

    public void SetMoney(int newMoney)
    {
        money = newMoney;
        OnMoneyChanged?.Invoke(money);
    }
}
