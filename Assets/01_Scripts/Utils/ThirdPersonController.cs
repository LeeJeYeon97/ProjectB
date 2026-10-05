using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    public enum MovementControlMode
    {
        JoystickAndWASD, // 가상 조이스틱 및 키보드(WASD) 직접 조작
        ClickToMove,     // 마우스 클릭 및 터치 탭 목적지 이동
        Both             // 하이브리드 (직접 조작 입력 시 클릭 이동 자동 취소)
    }

    [Header("Movement Settings")]
    [Tooltip("플레이어 이동 제어 모드를 설정합니다.")]
    public MovementControlMode controlMode = MovementControlMode.Both;
    
    [Tooltip("이동 속도")]
    [SerializeField] private float moveSpeed = 5f;
    private float growthSpeedBonus;
    
    [Tooltip("캐릭터 회전 속도 (부드럽게 회전하는 보간 감도)")]
    [SerializeField] private float rotationSpeed = 10f;
    
    [Tooltip("적용할 중력의 크기")]
    [SerializeField] private float gravity = -9.81f;
    
    [Tooltip("Click to Move 사용 시, 바닥으로 인식할 지면 레이어")]
    [SerializeField] private LayerMask groundLayer;

    [Header("Input System Actions (Decoupled)")]
    [Tooltip("조이스틱, 키보드 WASD, 게임패드 스틱을 입력받을 Vector2 액션")]
    [SerializeField] public InputActionProperty moveAction;

    [Tooltip("클릭 또는 모바일 화면 탭을 감지할 Button 액션")]
    [SerializeField] public InputActionProperty clickAction;

    [Tooltip("마우스 포인터 또는 모바일 터치 좌표를 감지할 Vector2 액션")]
    [SerializeField] public InputActionProperty pointerPositionAction;

    // 캐싱 컴포넌트
    private CharacterController characterController;
    private Animator animator;
    private Camera mainCamera;
    
    // 상태 제어 변수
    private Vector3 clickTargetDestination;
    private bool hasClickTarget = false;
    private float verticalVelocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;
        
        if (mainCamera == null)
        {
            Debug.LogError("씬에 Main Camera 태그가 지정된 카메라가 없습니다. 카메라 기준 정렬을 위해 필요합니다!");
        }
    }

    private void OnEnable()
    {
        // 사용되지 않는 에셋이더라도 개별적으로 활성화하여 예외를 방지합니다.
        moveAction.action?.Enable();
        clickAction.action?.Enable();
        pointerPositionAction.action?.Enable();

        // 클릭 이벤트 등록
        if (clickAction.action != null)
        {
            clickAction.action.performed += OnClickPerformed;
        }
    }

    private void OnDisable()
    {
        // 클릭 이벤트 해제
        if (clickAction.action != null)
        {
            clickAction.action.performed -= OnClickPerformed;
        }

        moveAction.action?.Disable();
        clickAction.action?.Disable();
        pointerPositionAction.action?.Disable();
    }

    private void Update()
    {
        ApplyGravity();
        HandleMovement();
        UpdateAnimator();
    }

    /// <summary>업그레이드로 얻은 이동 속도 증가량을 즉시 적용합니다.</summary>
    public void SetGrowthSpeedBonus(float bonus)
    {
        growthSpeedBonus = Mathf.Max(0f, bonus);
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        // X, Z 축의 실제 물리적 속도 크기를 구합니다 (Y축 중력 속도 제외)
        Vector3 horizontalVelocity = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        // Animator의 "Speed" 파라미터에 값을 설정해 줍니다.
        animator.SetFloat("Speed", currentSpeed);
    }

    private void HandleMovement()
    {
        Vector2 input = Vector2.zero;
        if (controlMode == MovementControlMode.JoystickAndWASD || controlMode == MovementControlMode.Both)
        {
            input = moveAction.action != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        }

        // 1. 직접 조작 입력(조이스틱 / WASD)이 들어오는 경우
        if (input.sqrMagnitude > 0.01f)
        {
            // 직접 움직이면 이전 클릭 이동 타겟을 즉시 해제합니다.
            hasClickTarget = false;

            // 카메라가 틸트되어 있더라도 지면 평면 기준(XZ)으로 부드럽게 앞/우 벡터를 계산합니다.
            Vector3 cameraForward = mainCamera.transform.forward;
            Vector3 cameraRight = mainCamera.transform.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            // 카메라 정렬 반영된 방향 계산
            Vector3 movementDirection = (cameraForward * input.y + cameraRight * input.x).normalized;

            // 중력 및 이동 벡터 계산
            Vector3 moveVector = movementDirection * (moveSpeed + growthSpeedBonus);
            moveVector.y = verticalVelocity;

            characterController.Move(moveVector * Time.deltaTime);

            // 이동 방향으로 스무스한 회전
            Quaternion targetRotation = Quaternion.LookRotation(movementDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        // 2. 직접 조작 입력이 없고 클릭 이동 타겟이 존재하는 경우
        else if (hasClickTarget && (controlMode == MovementControlMode.ClickToMove || controlMode == MovementControlMode.Both))
        {
            Vector3 currentPos = transform.position;
            currentPos.y = clickTargetDestination.y; // 높이 오차 배제

            float distance = Vector3.Distance(currentPos, clickTargetDestination);

            if (distance > 0.15f)
            {
                Vector3 direction = (clickTargetDestination - currentPos).normalized;

                Vector3 moveVector = direction * (moveSpeed + growthSpeedBonus);
                moveVector.y = verticalVelocity;

                characterController.Move(moveVector * Time.deltaTime);

                // 목적지 방향으로 스무스한 회전
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            else
            {
                hasClickTarget = false;
            }
        }
        // 3. 아무 입력이 없을 때는 중력에 의해 떨어지도록 처리
        else
        {
            // 조건문(if)을 지우고 매 프레임 무조건 중력 방향으로 Move를 호출합니다.
            // X, Z 값에 0이 들어가므로 characterController.velocity 값이 즉시 0으로 갱신됩니다.
            characterController.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
        }
    }

    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        // 조이스틱 온리 모드이면 클릭 이동 무시
        if (controlMode == MovementControlMode.JoystickAndWASD) return;

        if (pointerPositionAction.action == null) return;
        Vector2 pointerPos = pointerPositionAction.action.ReadValue<Vector2>();

        // 메인 카메라에서 스크린 좌표 기준 레이 발사
        Ray ray = mainCamera.ScreenPointToRay(pointerPos);
        RaycastHit hit;

        // 지상 레이어(Ground)와 충돌 시 해당 목적지로 이동 설정
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayer))
        {
            clickTargetDestination = hit.point;
            hasClickTarget = true;
        }
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded)
        {
            if (verticalVelocity < 0f)
            {
                verticalVelocity = -2f; // 지면에 완전히 붙어 있도록 음수 최소 속도 유지
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
    }
}
