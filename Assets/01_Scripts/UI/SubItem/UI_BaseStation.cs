using UnityEngine;
using UnityEngine.UI;

public class UI_BaseStation : UI_Base
{
    private enum Images
    {
        Image_Bg,
        Image_Progress,
    }

    Image progressImg;
    public override void Init()
    {
        if (_init) return;

        base.Init();
        Bind<Image>(typeof(Images));

        progressImg = GetImage((int)Images.Image_Progress);
        SetProgress(0f);
    }

    public void SetProgress(float ratio)
    {
        Init();
        if (progressImg != null)
        {
            progressImg.fillAmount = Mathf.Clamp01(ratio);
        }
    }


}
