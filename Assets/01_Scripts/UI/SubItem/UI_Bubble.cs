using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using static Define;

public class UI_Bubble : UI_Base
{
    public enum Images
    {
        Image_Bg,
        Image_Progress,
        Image_Food,
        Image_Clean,
        Image_Payment,
        Image_Beer,
        Image_BeerEmpty
    }

    private Image progressImg;
    private RectTransform bubbleRect;
    private Vector3 shownScale;
    private Tween bubbleTween;
    private bool isShowing;

    [Header("말풍선 애니메이션")]
    [SerializeField] private float showDuration = 0.25f;
    [SerializeField] private float hideDuration = 0.16f;
    [SerializeField] private float showStartScale = 0.65f;
    [SerializeField] private float hideEndScale = 0.75f;

    public override void Init()
    {
        if (_init)
            return;

        base.Init();

        Bind<Image>(typeof(Images));

        progressImg = GetImage((int)Images.Image_Progress);
        bubbleRect = transform as RectTransform;
        shownScale = transform.localScale;

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

        // None 요청은 현재 말풍선을 부드럽게 닫고, 나머지 상태는 표시 준비를 합니다.
        if (type == BubbleType.None)
        {
            Hide();
            return;
        }

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        HideAll();

        GetImage((int)Images.Image_Bg)?.gameObject.SetActive(true);

        switch (type)
        {
            //case BubbleType.Order:
            case BubbleType.FoodReady:
                GetImage((int)Images.Image_Beer)
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

            // 맥주 기계에 깨끗한 빈 잔이 없을 때 전용 그림을 보여줍니다.
            case BubbleType.BeerMissing:
                GetImage((int)Images.Image_BeerEmpty)
                    ?.gameObject.SetActive(true);
                break;

            case BubbleType.None:
            default:
                break;
        }

        // 숨겨져 있던 말풍선만 작게 시작해 탄력 있게 펼쳐서 상태 전환 때 깜빡이지 않게 합니다.
        if (!isShowing)
        {
            isShowing = true;
            PlayShow();
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

        // 진행 중인 표시 애니메이션을 정리한 뒤, 축소가 끝났을 때 오브젝트를 비활성화합니다.
        bubbleTween?.Kill();
        isShowing = false;
        if (!gameObject.activeSelf)
        {
            HideAll();
            return;
        }

        // 그림은 축소가 끝날 때까지 켜 두어 말풍선이 사라지는 모습이 보이게 합니다.
        if (bubbleRect == null || hideDuration <= 0f)
        {
            HideAll();
            transform.localScale = Vector3.zero;
            gameObject.SetActive(false);
            return;
        }

        bubbleTween = bubbleRect
            .DOScale(shownScale * hideEndScale, hideDuration)
            .SetEase(Ease.InBack)
            .SetLink(gameObject)
            .OnComplete(() =>
            {
                // 축소가 끝난 뒤 그림을 끄고 오브젝트를 비활성화합니다.
                HideAll();
                transform.localScale = Vector3.zero;
                gameObject.SetActive(false);
                bubbleTween = null;
            });
    }

    /// <summary>
    /// 말풍선의 원래 크기까지 튀어 오르듯 확대하는 표시 애니메이션을 재생합니다.
    /// </summary>
    private void PlayShow()
    {
        // 이전 축소 트윈이 남아 있다면 제거하고, 같은 크기 기준으로 애니메이션을 시작합니다.
        bubbleTween?.Kill();
        transform.localScale = shownScale * showStartScale;

        if (bubbleRect == null || showDuration <= 0f)
        {
            transform.localScale = shownScale;
            return;
        }

        bubbleTween = bubbleRect
            .DOScale(shownScale, showDuration)
            .SetEase(Ease.OutBack)
            .SetLink(gameObject)
            .OnComplete(() => bubbleTween = null);
    }
}
