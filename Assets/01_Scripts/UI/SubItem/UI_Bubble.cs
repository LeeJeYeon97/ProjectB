using UnityEngine;
using UnityEngine.UI;

public class UI_Bubble : UI_Base
{
    enum Images
    {
        Image_1,
        Image_2,
    }
    public override void Init()
    {
        base.Init();
        Bind<Image>(typeof(Images));

        GetImage((int)Images.Image_2).gameObject.SetActive(false);
    }
    public void SetImage()
    {
        GetImage((int)Images.Image_1).gameObject.SetActive(false);
        GetImage((int)Images.Image_2).gameObject.SetActive(true);
    }
}
