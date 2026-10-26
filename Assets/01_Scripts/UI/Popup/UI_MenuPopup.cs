using UnityEngine;

public class UI_MenuPopup : UI_Popup
{
    // 메뉴 패널들
    public enum GameObjects
    {
        Panel_MainMenu
    }
    public override void Init()
    {
        base.Init();

        BindObject(typeof(GameObjects));

    }

}
