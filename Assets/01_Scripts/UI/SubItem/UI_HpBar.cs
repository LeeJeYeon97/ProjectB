using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UI_HpBar : MonoBehaviour
{
    [SerializeField] private Image _hpSlider;
    [SerializeField] private Vector3 _offset = new Vector3(0, 1.2f, 0); // 몬스터 머리 위 높이

    private Transform _target;
    private Camera _mainCam;
    private bool _isVisible = false;

    private RectTransform _rectTransform;
    private RectTransform _parentCanvasRect;

    // 풀에서 꺼낼 때 초기화
    public void SetTarget(Transform target)
    {

        _hpSlider.DOKill(); // 이전 애니메이션이 실행 중이면 중지
        _hpSlider.fillAmount = 1.0f;
        _mainCam = Camera.main;
        _rectTransform = GetComponent<RectTransform>();

        Transform uiCanvas = GameObject.Find("UI_GameScene").transform;
        transform.SetParent(uiCanvas, false);

        // 추가 2: 부모를 설정했으니, 이제 그 부모(UI_GameScene)의 RectTransform을 기억해둡니다!
        _parentCanvasRect = uiCanvas.GetComponent<RectTransform>();

        _target = target;
        _isVisible = false;
        gameObject.SetActive(false); // 처음에는 숨겨둡니다 (최적화 꿀팁)
    }

    // 체력 갱신 (몬스터가 데미지를 입었을 때만 호출됨)
    public void UpdateHP(float currentHp, float maxHp)
    {
        //  한 대라도 맞으면 그때서야 화면에 보여줍니다.
        if (!_isVisible)
        {
            _isVisible = true;
            gameObject.SetActive(true);
        }

        // 현재 체력 비율 (0.0 ~ 1.0)
        float ratio = currentHp / maxHp;

        // DOFillAmount(목표값, 시간) 사용
        _hpSlider.DOKill(); // 이전 애니메이션이 실행 중이면 중지
        _hpSlider.DOFillAmount(ratio, 0.2f).SetEase(Ease.OutCubic);
    }

    private void LateUpdate()
    {
        if (_target == null || !_target.gameObject.activeInHierarchy || _parentCanvasRect == null) return;

        // 1. 몬스터의 월드 좌표를 스크린 좌표로 변환
        Vector3 screenPos = _mainCam.WorldToScreenPoint(_target.position + _offset);
        if (screenPos.z < 0) return;

        // =======================================================
        // 자동 감지 마법: 내 부모 캔버스가 어떤 모드인지 스스로 판단합니다.
        // =======================================================
        Canvas rootCanvas = GetComponentInParent<Canvas>();
        Camera uiCamera = null; // 기본은 Overlay 모드 (null)

        // 만약 캔버스가 Camera 모드라면, 캔버스를 찍고 있는 카메라를 가져옵니다.
        if (rootCanvas != null && rootCanvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            uiCamera = rootCanvas.worldCamera;
        }

        // 2. 정확한 UI 로컬 좌표 계산
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _parentCanvasRect,
            screenPos,
            uiCamera, // 이제 null 대신 알아서 알맞은 카메라를 넣습니다!
            out Vector2 localPos
        );

        // 3. 변환된 값을 적용
        _rectTransform.anchoredPosition = localPos;
    }
    private void OnDisable()
    {
        _hpSlider.DOKill(); // 이전 애니메이션이 실행 중이면 중지
    }
}
