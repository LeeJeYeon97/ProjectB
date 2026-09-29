using UnityEngine;
using UnityEngine.UI;
using static Define;

public class UI_Bubble : UI_Base
{
    public enum Images
    {
        Image_Progress,
        Image_Food,
        Image_Clean,
        Image_Payment,
    }

    private Image progressImg;

    public override void Init()
    {
        if (_init)
            return;

        base.Init();

        Bind<Image>(typeof(Images));

        progressImg = GetImage((int)Images.Image_Progress);

        HideAll();
    }

    private void HideAll()
    {
        foreach (Images image in System.Enum.GetValues(typeof(Images)))
        {
            GetImage((int)image)?.gameObject.SetActive(false);
        }
    }

    public void SetBubble(BubbleType type)
    {
        Init();

        HideAll();

        switch (type)
        {
            //case BubbleType.Order:
            case BubbleType.FoodReady:
                GetImage((int)Images.Image_Food)
                    ?.gameObject.SetActive(true);
                break;

            case BubbleType.Payment:
                GetImage((int)Images.Image_Payment)
                    ?.gameObject.SetActive(true);
                break;

            case BubbleType.Cleaning:
                GetImage((int)Images.Image_Clean)
                    ?.gameObject.SetActive(true);
                break;

            case BubbleType.Progress:
                GetImage((int)Images.Image_Progress)
                    ?.gameObject.SetActive(true);
                break;

            case BubbleType.None:
            default:
                break;
        }
    }

    public void SetProgress(float ratio)
    {
        Init();

        if (progressImg != null)
        {
            progressImg.fillAmount = Mathf.Clamp01(ratio);
        }
    }

    public void Hide()
    {
        Init();
        HideAll();
    }
}
