using UnityEngine;
using UnityEngine.UI;

public class UI_LoginScene : UI_Scene
{
    enum Buttons
    {
        Button_Start,
    }
    public override void Init()
    {
        base.Init();

        Bind<Button>(typeof(Buttons));
        GetButton((int)Buttons.Button_Start).onClick.AddListener(StartGame);
    }
    public void StartGame()
    {
        Managers.Scene.LoadScene(Define.Scene.GameScene);
    }
}
