
using System;
using UnityEditor;
using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;

public class UI_UnlockStation : UI_Base
{

    private enum Images
    {
        Image_Fill, // 금액 누적 게이지 이미지 (Filled 타입)
    }

    private enum Texts
    {
        Text_Price, // 남은 금액 표시용 TextMeshProUGUI
    }

    [Header("인스펙터 직접 할당 (비워둘 경우 자동 Bind 검색)")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Transform uiContainer;

    [Header("Punch 애니메이션 설정")]
    [SerializeField] private float punchScaleRatio = 0.2f; // 원래 크기 대비 튕기는 비율 (20%)
    [SerializeField] private float punchDuration = 0.2f;

    private Vector3 _initialScale = Vector3.one;
    private Tween _bounceTween;

    public override void Init()
    {
        if (_init) return;
        base.Init();

        // 1. UI_Base 자동 바인딩 시도
        Bind<Image>(typeof(Images));
        Bind<TMP_Text>(typeof(Texts));

        // 2. 인스펙터에 수동 할당되지 않았으면 바인딩된 오브젝트로 매핑
        if (fillImage == null)
            fillImage = GetImage((int)Images.Image_Fill);

        if (progressText == null)
            progressText = GetTMP((int)Texts.Text_Price);

        if (uiContainer == null)
        {
            uiContainer = transform;
        }

        if (uiContainer != null)
        {
            _initialScale = uiContainer.localScale;
        }
    }

    /// <summary>
    /// 현재 누적 지불액과 목표 금액을 바탕으로 게이지와 텍스트를 갱신합니다.
    /// </summary>
    /// <param name="currentPaid">현재까지 지불된 누적 금액</param>
    /// <param name="targetPrice">총 필요 금액</param>
    /// <param name="animate">돈이 들어올 때 튕기는 애니메이션 재생 여부</param>
    public void UpdateUI(int currentPaid, int targetPrice, bool animate = true)
    {
        Init();

        if (targetPrice <= 0) return;

        // 1. 게이지 비율 반영 (0.0 ~ 1.0)
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Clamp01((float)currentPaid / targetPrice);
        }

        // 2. 남은 금액 표시
        if (progressText != null)
        {
            int remainingPrice = Mathf.Max(0, targetPrice - currentPaid);
            progressText.text = remainingPrice.ToString();
        }

        // 3. 돈이 누적될 때 통통 튀는 Punch 애니메이션
        if (animate && uiContainer != null)
        {
            _bounceTween?.Kill();
            uiContainer.localScale = _initialScale;

            Vector3 punchAmount = _initialScale * punchScaleRatio;
            _bounceTween = uiContainer.DOPunchScale(punchAmount, punchDuration, 10, 1f)
                .OnKill(() =>
                {
                    if (uiContainer != null)
                        uiContainer.localScale = _initialScale;
                });
        }
    }

    /// <summary>
    /// 오브젝트 파괴 또는 비활성화 시 트윈 안전 제거
    /// </summary>
    public void KillTweens()
    {
        _bounceTween?.Kill();
    }

    private void OnDisable()
    {
        KillTweens();
    }

    public override void Clear()
    {
        base.Clear();
        KillTweens();
    }
}
