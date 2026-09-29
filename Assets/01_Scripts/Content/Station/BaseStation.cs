using UnityEngine;

public class BaseStation : MonoBehaviour
{
    [Header("전략 연결 (컴포넌트로 주입하거나 인스펙터에서 할당)")]
    [SerializeField] private MonoBehaviour strategyComponent;
    private IInteractable _strategy;


    [Header("발판 UI")]
    [SerializeField] private UI_BaseStation progressUI;

    public bool isOperational = true;

    private GameObject _currentActor;
    private float _timer = 0f;

    private void Awake()
    {
        // Inspector에 컴포넌트가 지정되어 있으면 우선 사용
        if (strategyComponent != null)
        {
            _strategy = strategyComponent as IInteractable;
        }

        // 지정되지 않았으면 자신 또는 부모에서 검색
        if (_strategy == null)
        {
            _strategy = GetComponentInParent<IInteractable>();
        }
        if (_strategy == null)
        {
            Debug.LogError(
                $"{gameObject.name}: IInteractable을 찾을 수 없습니다.",
                this
            );
        }
        

        if (progressUI == null)
            progressUI = GetComponentInChildren<UI_BaseStation>(true);

        if(_strategy.InteractionType == Define.InterationType.Proximity)
        {
            progressUI.gameObject.SetActive(false);
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isOperational || _strategy == null) return;

        // [개선] 부모/자식 어디에 콜라이더가 있어도 PlayerController를 정확히 감지
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (other.CompareTag("Player") || player != null)
        {
            GameObject actor = player != null ? player.gameObject : other.gameObject;

            // 이미 동일한 플레이어가 상호작용 중이면 무시
            if (_currentActor == actor)
                return;

            // 다른 플레이어가 상호작용 중이면 무시
            if (_currentActor != null)
                return;

            if (_strategy.CanInteract(actor))
            {
                _currentActor = actor;
                _timer = 0f;
                _strategy.OnStartInteract(_currentActor);

                if (_strategy.Duration <= 0f)
                {
                    _strategy.OnCompleteInteract(_currentActor);
                    _currentActor = null;
                }
            }
        }
    }

    private void Update()
    {
        if (_currentActor == null || _strategy == null) return;

        // 조건이 여전히 유효한지 체크
        if (!_strategy.CanInteract(_currentActor))
        {
            CancelInteraction();
            return;
        }

        _timer += Time.deltaTime;
        float progress = Mathf.Clamp01(_timer / _strategy.Duration);

        _strategy.OnUpdateInteract(_currentActor, progress);

        // UI 게이지 업데이트
        if (progressUI != null)
        {
            progressUI.SetProgress(progress);
        }

        // 상호작용 완료
        if (_timer >= _strategy.Duration)
        {
            _strategy.OnCompleteInteract(_currentActor);
            _timer = 0f;

            // 완료 후 연속으로 계속 상호작용이 가능한지 확인 (예: 맥주를 연속으로 계속 뽑을 때)
            if (!_strategy.CanInteract(_currentActor))
            {
                CancelInteraction();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (_currentActor == null) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        GameObject exitingActor = player != null ? player.gameObject : other.gameObject;

        if (exitingActor == _currentActor)
        {
            CancelInteraction();
        }
    }

    private void CancelInteraction()
    {
        if (_currentActor != null && _strategy != null)
        {
            _strategy.OnCancelInteract(_currentActor);
        }

        _currentActor = null;
        _timer = 0f;

        if (progressUI != null)
        {
            progressUI.SetProgress(0f);
        }
    }
}