
using UnityEngine;
using UnityEngine.UI;

public class UI_closePopup : UI_Base
{
    private Button closeButton;
    public override void Init()
    {
        base.Init();
        closeButton = GetComponent<Button>();
        closeButton.onClick.AddListener(CloseParentPopup);
    }
    public void CloseParentPopup()
    {
        // 내 부모에게서 팝업 찾기
        UI_Popup parentPopup = GetComponentInParent<UI_Popup>();
        if(parentPopup != null)
        {
            Managers.UI.ClosePopupUI(parentPopup);
        }
    }
}
