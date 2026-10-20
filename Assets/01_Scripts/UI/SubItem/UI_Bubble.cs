using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using static Define;

public class UI_Bubble : UI_Base
{
    private Image progressImg;
    private TMP_Text disturbanceText;
    private RectTransform bubbleRect;
    private Canvas bubbleCanvas;
    private Transform bubbleAnchor;
    private Vector3 shownScale;
    private Tween bubbleTween;
    private bool isShowing;

    [Header("말풍선 애니메이션")]
    [SerializeField] private float showDuration = 0.25f;
    [SerializeField] private float hideDuration = 0.16f;
    [SerializeField] private float showStartScale = 0.65f;
    [SerializeField] private float hideEndScale = 0.75f;

    /// <summary>다른 상호작용 객체가 다시 쓰기 전에 이전 그림과 애니메이션을 정리합니다.</summary>
    public void ResetForPool()
    {
        Init();
        bubbleTween?.Kill();
        bubbleTween = null;
        isShowing = false;
        HideAll();
        transform.localScale = shownScale;
        gameObject.SetActive(false);
    }

    public override void Init()
    {
        if (_init)
            return;

        base.Init();

        Bind<Image>(typeof(Define.BubbleImages));
        Bind<TMP_Text>(typeof(Define.BubbleTexts));

        progressImg = GetImage((int)Define.BubbleImages.Image_Progress);
        disturbanceText = GetTMP((int)Define.BubbleTexts.Text_Disturbance);
        bubbleRect = transform as RectTransform;
        bubbleCanvas = GetComponent<Canvas>();
        shownScale = transform.localScale;

        HideAll();
    }

    /// <summary>말풍선을 소유 객체의 기준점 아래로 옮기고 그 위치에 맞춥니다.</summary>
    public void SetAnchor(Transform anchor)
    {
        bubbleAnchor = anchor;

        if (bubbleAnchor == null)
            return;

        // 공용 UI 루트에서 생성되거나 풀에서 재사용된 말풍선도 실제 소유자의 자식으로 옮깁니다.
        // 기존 월드 크기를 유지한 채 부모를 바꿔, 서로 다른 객체의 크기 설정에 영향을 받지 않게 합니다.
        transform.SetParent(bubbleAnchor, true);

        // 기준점 자체가 말풍선의 위치이므로 프리팹의 임시 위치값을 제거합니다.
        transform.position = bubbleAnchor.position;
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
        foreach (Define.BubbleImages image in System.Enum.GetValues(typeof(Define.BubbleImages)))
        {
            GetImage((int)image)?.gameObject.SetActive(false);
        }

        // 방해 손님 표시용 글자도 다른 말풍선에서는 보이지 않도록 함께 숨깁니다.
        disturbanceText?.gameObject.SetActive(false);
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

        GetImage((int)Define.BubbleImages.Image_Bg)?.gameObject.SetActive(true);

        switch (type)
        {
            //case BubbleType.Order:
            case BubbleType.FoodReady:
                GetImage((int)Define.BubbleImages.Image_Beer)
                    ?.gameObject.SetActive(true);
                break;

            case BubbleType.Payment:
                GetImage((int)Define.BubbleImages.Image_Payment)
                    ?.gameObject.SetActive(true);
                break;

            case BubbleType.Cleaning:
                GetImage((int)Define.BubbleImages.Image_Clean)
                    ?.gameObject.SetActive(true);
                break;

            case BubbleType.Progress:
                GetImage((int)Define.BubbleImages.Image_Progress)
                    ?.gameObject.SetActive(true);
                break;

            // 맥주 기계에 깨끗한 빈 잔이 없을 때 전용 그림을 보여줍니다.
            case BubbleType.BeerMissing:
                GetImage((int)Define.BubbleImages.Image_BeerEmpty)
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

    public void SetProgress(float ratio)
    {
        Init();

        if (progressImg != null)
        {
            progressImg.fillAmount = Mathf.Clamp01(ratio);
        }
    }

    /// <summary>말풍선을 닫고 실제로 비활성화된 뒤 요청한 완료 동작을 호출합니다.</summary>
    public void Hide(Action onHidden = null)
    {
        Init();

        // 진행 중인 표시 애니메이션을 정리한 뒤, 축소가 끝났을 때 오브젝트를 비활성화합니다.
        bubbleTween?.Kill();
        isShowing = false;
        if (!gameObject.activeSelf)
        {
            HideAll();
            // 이미 닫힌 말풍선도 호출자가 즉시 풀에 반납할 수 있도록 완료를 알립니다.
            onHidden?.Invoke();
            return;
        }

        // 그림은 축소가 끝날 때까지 켜 두어 말풍선이 사라지는 모습이 보이게 합니다.
        if (bubbleRect == null || hideDuration <= 0f)
        {
            HideAll();
            transform.localScale = Vector3.zero;
            gameObject.SetActive(false);
            // 애니메이션을 생략한 경우에도 비활성화가 끝난 뒤에만 반환합니다.
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
                // 닫힘 연출이 끝난 시점에 소유자가 말풍선을 풀로 돌려보낼 수 있습니다.
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
