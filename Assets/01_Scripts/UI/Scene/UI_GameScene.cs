using TMPro;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_GameScene : UI_Scene
{
    public enum Texts
    {
        Text_Money,
    }
    public enum Buttons
    {
        Button_Menu,
    }
    public List<TMP_Text> TextList = new();

    public override void Init()
    {
        base.Init();
        BindText(typeof(Texts));
        BindButton(typeof(Buttons));

        GetButton((int)Buttons.Button_Menu).onClick.AddListener(ShowMenuPopup);
        Managers.Game.Player.OnMoneyChanged += UpdateMoneyUI;
        UpdateMoneyUI(Managers.Game.Player.money);
    }
    public void UpdateMoneyUI(int money)
    {
        GetText((int)Texts.Text_Money).text = $"{Managers.Game.Player.money}";
    }
    public void ShowMenuPopup()
    {
        Managers.UI.ShowPopupUI<UI_MenuPopup>();
    }
    public override void Clear()
    {
        base.Clear();
        Managers.Game.Player.OnMoneyChanged -= UpdateMoneyUI;
    }
}
