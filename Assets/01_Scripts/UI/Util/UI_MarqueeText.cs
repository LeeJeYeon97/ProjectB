using UnityEngine;
using TMPro;
using DG.Tweening;

[RequireComponent(typeof(TextMeshProUGUI))]
public class UI_MarqueeText : UI_Base
{
    private TextMeshProUGUI _tmp;
    private RectTransform _rectTransform;

    // Tween 대신 Sequence로 변경하여 복잡한 타이밍 대본을 짭니다!
    private Sequence _scrollSequence;

    [Header("세팅")]
    public float scrollSpeed = 50f;
    public float startDelay = 1.5f; // 처음 시작할 때 대기 시간
    public float endDelay = 1.0f;   // 다 지나가고 나서 뿅 돌아가기 전 대기 시간 (새로 추가!)

    // 아까 1단계에서 만든 부모 마스크 영역(DescMask)을 인스펙터에서 넣어주세요!
    public RectTransform maskRect;

    public override void Init()
    {
        if (_init)
        {
            return;
        }
        base.Init();

        _tmp = GetComponent<TextMeshProUGUI>();
        _rectTransform = GetComponent<RectTransform>();
    }
    public void PlayMarquee(string newText)
    {
        Init();

        // 1. 기존 시퀀스 확실하게 죽이기 & 텍스트/위치 초기화
        _scrollSequence?.Kill();
        _tmp.text = newText;
        _rectTransform.anchoredPosition = Vector2.zero;

        // [추가/수정] UI 크기가 아직 계산되지 않았을 때를 대비한 강제 새로고침!
        Canvas.ForceUpdateCanvases();
        _tmp.ForceMeshUpdate();

        // [수정] preferredWidth 대신 화면에 찍힌 '진짜(렌더링된) 글자 테두리 길이'를 가져옵니다.
        float textWidth = _tmp.textBounds.size.x;
        float maskWidth = maskRect.rect.width;

        // (디버그용: 왜 굴러갔는지 범인을 찾아줍니다. 확인 후 지우셔도 됩니다!)
        //Debug.Log($"[마키 텍스트] 글자 진짜 길이: {textWidth} / 마스크 가로 길이: {maskWidth}");

        if (textWidth > maskWidth)
        {
            // [추가] 글자가 길어서 스크롤이 필요할 때 -> 왼쪽 정렬
            // (만약 수직 정렬이 위로 붙는다면 TextAlignmentOptions.MidlineLeft 로 변경하세요)
            _tmp.alignment = TextAlignmentOptions.Left;

            float moveDistance = textWidth - maskWidth + 20f;
            float duration = moveDistance / scrollSpeed;

            //  4. 마법의 시퀀스 대본 작성!
            _scrollSequence = DOTween.Sequence();
            _scrollSequence.SetUpdate(true);
            // [대본 1장] 처음에 원래 위치에서 글자 읽을 시간 주기
            _scrollSequence.AppendInterval(startDelay);

            // [대본 2장] 왼쪽으로 설정한 속도만큼 스르륵 밀기
            _scrollSequence.Append(_rectTransform.DOAnchorPosX(-moveDistance, duration).SetEase(Ease.Linear));

            // [대본 3장] 끝까지 다 밀렸을 때 잠깐 멈춰서 마지막 단어 읽을 시간 주기
            _scrollSequence.AppendInterval(endDelay);

            // [대본 4장] 원래 위치(0)로 0초 만에 뿅! 하고 순간이동 시키기
            _scrollSequence.Append(_rectTransform.DOAnchorPosX(0, 0));

            // 이 전체 4장짜리 대본을 무한 반복!
            _scrollSequence.SetLoops(-1);
        }
        else
        {
            _tmp.alignment = TextAlignmentOptions.Center;
        }
    }

    private void OnDisable()
    {
        // 팝업이 꺼지면 애니메이션 확실히 죽이기 (에러 방지!)
        _scrollSequence?.Kill();
    }
}