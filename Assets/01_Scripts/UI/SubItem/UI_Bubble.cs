using UnityEngine;
using UnityEngine.UI;

public class UI_Bubble : UI_Base
{
    public enum Images
    {
        Image_1,
        Image_2,
        Image_3,
    }

    public override void Init()
    {
        if (_init)
            return;

        base.Init();
        Bind<Image>(typeof(Images));

        GetImage((int)Images.Image_1)?.gameObject.SetActive(false);
        GetImage((int)Images.Image_2)?.gameObject.SetActive(false);
        GetImage((int)Images.Image_3)?.gameObject.SetActive(false);
    }

    public void SetImage()
    {
        Init();
        GetImage((int)Images.Image_1)?.gameObject.SetActive(false);
        GetImage((int)Images.Image_2)?.gameObject.SetActive(true);
        GetImage((int)Images.Image_3)?.gameObject.SetActive(false);
    }

    public void SetCleanImage()
    {
        Init();
        GetImage((int)Images.Image_1)?.gameObject.SetActive(false);
        GetImage((int)Images.Image_2)?.gameObject.SetActive(false);
        GetImage((int)Images.Image_3)?.gameObject.SetActive(true);
    }
}
