using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using static Define;

public class UI_Bubble : UI_Base
{/// <summary>말풍선 프리팹에서 이름으로 연결할 이미지 요소입니다.</summary>
    public enum Images
    {
        Image_Bg,
        Image_Progress,
        Image_Food,
        Image_Clean,
        Image_Payment,
        Image_Beer,
        Image_BeerEmpty,
    }

    /// <summary>말풍선 프리팹에서 이름으로 연결할 글자 요소입니다.</summary>
    public enum Texts
    {
        Text_Disturbance,
    }
    private Image progressImg;
    private Image foodImg;
    private Sprite defaultFoodIcon;
    private TMP_Text disturbanceText;
    private RectTransform bubbleRect;
    private Canvas bubbleCanvas;
    private Transform bubbleAnchor;
    private Vector3 prefabWorldScale;
    private Vector3 shownScale;
    private Tween bubbleTween;
    private bool isShowing;

    [Header("말풍선 애니메이션")]
    [SerializeField] private float showDuration = 0.25f;
    [SerializeField] private float hideDuration = 0.16f;
    [SerializeField] private float showStartScale = 0.65f;
    [SerializeField] private float hideEndScale = 0.75f;

    public void ResetForReuse()
    {
        Init();
        bubbleTween?.Kill();
        bubbleTween = null;
        isShowing = false;
        HideAll();
        // 다시 사용할 때 직전 손님의 메뉴 그림과 진행 상태가 남지 않게 정리합니다.
        if (foodImg != null)
            foodImg.sprite = defaultFoodIcon;

        // 현재 소유자 아래에 둔 채 숨겨 다음 활성화 때 같은 말풍선을 다시 사용합니다.
        transform.localScale = shownScale;
        gameObject.SetActive(false);
    }

    public override void Init()
    {
        if (_init)
            return;

        base.Init();

        BindImage(typeof(Images));
        BindText(typeof(Texts));

        progressImg = GetImage((int)Images.Image_Progress);
        foodImg = GetImage((int)Images.Image_Food);
        if (foodImg != null)
            defaultFoodIcon = foodImg.sprite;
        disturbanceText = GetText((int)Texts.Text_Disturbance);
        bubbleRect = transform as RectTransform;
        bubbleCanvas = GetComponent<Canvas>();
        prefabWorldScale = transform.lossyScale;
        shownScale = transform.localScale;

        HideAll();
    }

    /// <summary>말풍선을 소유 객체의 기준점 아래로 옮기고 그 위치에 맞춥니다.</summary>
    public void SetAnchor(Transform anchor)
    {
        // 원래 크기를 먼저 기억한 뒤 부모를 바꾸어 부모별 스케일이 기본값이 되지 않게 합니다.
        Init();
        bubbleAnchor = anchor;

        if (bubbleAnchor == null)
            return;

        // 풀에 남은 이전 부모의 localScale을 버리고 프리팹의 원래 월드 크기로 맞춥니다.
        SetWorldScale(prefabWorldScale);

        // 공용 UI 루트에서 생성되거나 풀에서 재사용된 말풍선을 실제 소유자의 자식으로 옮깁니다.
        transform.SetParent(bubbleAnchor, true);

        // 기준점 자체가 말풍선의 위치이므로 프리팹의 임시 위치값을 제거합니다.
        transform.position = bubbleAnchor.position;
        // 애니메이션은 현재 앵커에 맞춘 localScale을 사용합니다.
        shownScale = transform.localScale;
        // 첫 화면에서도 세로 비율이 눌리지 않도록 카메라와 같은 방향을 바라봅니다.
        Camera viewCamera = Camera.main;
        transform.rotation = viewCamera != null ? viewCamera.transform.rotation : Quaternion.identity;
    }

    /// <summary>월드 공간 말풍선의 화면 표시 여부와 방향을 갱신합니다.</summary>
    private void LateUpdate()
    {
        // 부모의 회전이나 풀에서 가져온 RectTransform 위치값과 관계없이 기준점의 월드 위치에 고정합니다.
        // 카메라 각도가 바뀌어도 말풍선 중심은 항상 소유자의 같은 위치를 따라갑니다.
        if (bubbleAnchor != null)
            transform.position = bubbleAnchor.position;

        // 월드 위치를 확정한 뒤 화면 안에 있는지 확인합니다.
        Camera viewCamera = Camera.main;
        if (bubbleAnchor == null || viewCamera == null || bubbleCanvas == null)
        {
            if (bubbleCanvas != null)
                bubbleCanvas.enabled = false;
            return;
        }

        Vector3 screenPoint = viewCamera.WorldToScreenPoint(bubbleAnchor.position);
        bool canShow = screenPoint.z > 0f && screenPoint.x >= 0f && screenPoint.x <= Screen.width
            && screenPoint.y >= 0f && screenPoint.y <= Screen.height;

        // 카메라 뒤쪽이나 화면 밖에 있는 대상은 말풍선만 숨겨 둡니다.
        bubbleCanvas.enabled = canShow;
        if (bubbleRect != null)
        {
            // 월드 공간 Canvas의 정면을 카메라와 나란히 맞춰 비스듬한 투영으로 눌리지 않게 합니다.
            bubbleRect.rotation = viewCamera.transform.rotation;
        }
    }

    private void HideAll()
    {
        foreach (Images image in System.Enum.GetValues(typeof(Images)))
        {
            GetImage((int)image)?.gameObject.SetActive(false);
        }

        // 방해 손님 표시용 글자도 다른 말풍선에서는 보이지 않도록 함께 숨깁니다.
        disturbanceText?.gameObject.SetActive(false);
    }

    /// <summary>부모의 크기와 관계없이 지정한 월드 크기를 유지하도록 localScale을 환산합니다.</summary>
    private void SetWorldScale(Vector3 worldScale)
    {
        Transform currentParent = transform.parent;
        if (currentParent == null)
        {
            transform.localScale = worldScale;
            return;
        }

        Vector3 parentScale = currentParent.lossyScale;
        float scaleX = parentScale.x == 0f ? 1f : parentScale.x;
        float scaleY = parentScale.y == 0f ? 1f : parentScale.y;
        float scaleZ = parentScale.z == 0f ? 1f : parentScale.z;

        transform.localScale = new Vector3(
            worldScale.x / scaleX,
            worldScale.y / scaleY,
            worldScale.z / scaleZ);
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
                foodImg?.gameObject.SetActive(true);
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

            case BubbleType.Disturbance:
                // 해결 가능한 손님은 진행 막대 대신 느낌표로 먼저 구분합니다.
                disturbanceText?.gameObject.SetActive(true);
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

    /// <summary>주문한 레시피의 이미지를 공용 음식 이미지 요소에 표시합니다.</summary>
    public void SetOrder(FoodRecipeData recipe)
    {
        SetBubble(BubbleType.FoodReady);

        if (foodImg == null)
            return;

        // 레시피에 이미지가 없으면 프리팹에 지정된 기본 음식 이미지를 사용합니다.
        if (recipe == null || recipe.OrderIcon == null)
        {
            foodImg.sprite = defaultFoodIcon;
            return;
        }

        foodImg.sprite = recipe.OrderIcon;
    }

    public void SetProgress(float ratio)
    {
        Init();

        if (progressImg != null)
        {
            progressImg.fillAmount = Mathf.Clamp01(ratio);
        }
    }

    /// <summary>말풍선을 닫고 비활성화한 뒤 선택한 완료 동작을 호출합니다.</summary>
    public void Hide(Action onHidden = null)
    {
        Init();

        // 진행 중인 표시 애니메이션을 정리한 뒤, 축소가 끝났을 때 오브젝트를 비활성화합니다.
        bubbleTween?.Kill();
        isShowing = false;
        if (!gameObject.activeSelf)
        {
            HideAll();
            // 이미 닫힌 말풍선도 호출자가 다음 처리를 이어가도록 완료를 알립니다.
            onHidden?.Invoke();
            return;
        }

        // 그림은 축소가 끝날 때까지 켜 두어 말풍선이 사라지는 모습이 보이게 합니다.
        if (bubbleRect == null || hideDuration <= 0f)
        {
            HideAll();
            transform.localScale = Vector3.zero;
            gameObject.SetActive(false);
            // 애니메이션을 생략해도 비활성화한 다음 완료 동작을 호출합니다.
            onHidden?.Invoke();
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
                // 닫힘 연출이 끝난 시점에 소유자가 다음 처리를 이어가도록 알립니다.
                onHidden?.Invoke();
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
