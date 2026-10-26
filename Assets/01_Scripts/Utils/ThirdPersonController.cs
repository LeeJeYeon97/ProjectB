using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("플레이어 이동 제어 모드를 설정합니다.")]
    public Define.MovementControlMode controlMode = Define.MovementControlMode.Both;
    
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
    [Tooltip("키보드 WASD와 게임패드 스틱을 입력받을 Vector2 액션")]
    [SerializeField] public InputActionProperty moveAction;

    [Tooltip("클릭 또는 모바일 화면 탭을 감지할 Button 액션")]
    [SerializeField] public InputActionProperty clickAction;

    [Tooltip("마우스 포인터 또는 모바일 터치 좌표를 감지할 Vector2 액션")]
    [SerializeField] public InputActionProperty pointerPositionAction;

    // 캐싱 컴포넌트
    private CharacterController characterController;
    private Animator animator;
    private StackingSystem stackingSystem;
    private Camera mainCamera;
    private ScreenMoveInput screenMoveInput;
    
    // 상태 제어 변수
    private Vector3 clickTargetDestination;
    private bool hasClickTarget = false;
    private float verticalVelocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        stackingSystem = GetComponentInChildren<StackingSystem>();
        mainCamera = Camera.main;
        screenMoveInput = GetComponent<ScreenMoveInput>();
        
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
        // 가구 배치처럼 이동을 잠시 끄는 동안 이전 터치가 남지 않게 합니다.
        screenMoveInput?.Clear();

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

        // 플레이어가 쌓을 수 있는 아이템을 하나 이상 들고 있으면 운반 상태를 켭니다.
        // 돈처럼 스택에 포함되지 않는 아이템은 운반 애니메이션을 바꾸지 않습니다.
        bool isCarrying = stackingSystem != null && stackingSystem.StackCount > 0;
        animator.SetBool("IsCarrying", isCarrying);
    }

    private void HandleMovement()
    {
        Vector2 input = Vector2.zero;
        if (controlMode == Define.MovementControlMode.Both)
        {
            input = moveAction.action != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        }

        // 현재 모드에서 다른 직접 조작 입력이 없으면 화면 드래그 방향을 사용합니다.
        if (input.sqrMagnitude <= 0.01f)
        {
            if (TryGetScreenDirection(out Vector2 dragDirection))
                input = dragDirection;
        }

        // 입력 방향을 카메라 기준의 지면 방향으로 바꿉니다.
        if (input.sqrMagnitude > 0.01f)
        {
            hasClickTarget = false;
            Vector3 cameraForward = mainCamera.transform.forward;
            Vector3 cameraRight = mainCamera.transform.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();
            Vector3 movementDirection = (cameraForward * input.y + cameraRight * input.x).normalized;
            MoveInDirection(movementDirection);
        }
        else if (hasClickTarget && (controlMode == Define.MovementControlMode.ClickToMove || controlMode == Define.MovementControlMode.Both))
        {
            Vector3 currentPos = transform.position;
            currentPos.y = clickTargetDestination.y; // 높이 오차 배제

            float distance = Vector3.Distance(currentPos, clickTargetDestination);

            if (distance > 0.15f)
            {
                Vector3 direction = (clickTargetDestination - currentPos).normalized;

                MoveInDirection(direction);
            }
            else
            {
                hasClickTarget = false;
                MoveWithGravity();
            }
        }
        else
        {
            MoveWithGravity();
        }
    }

    /// <summary>직접 조작 모드에서만 화면 드래그의 방향을 읽습니다.</summary>
    private bool TryGetScreenDirection(out Vector2 direction)
    {
        direction = Vector2.zero;
        if (controlMode == Define.MovementControlMode.ClickToMove)
            return false;

        if (screenMoveInput == null)
            return false;

        return screenMoveInput.TryGetDirection(out direction);
    }

    /// <summary>입력 종류와 상관없이 같은 속도와 회전 방식으로 플레이어를 움직입니다.</summary>
    private void MoveInDirection(Vector3 direction)
    {
        Vector3 moveVector = direction * (moveSpeed + growthSpeedBonus);
        moveVector.y = verticalVelocity;
        characterController.Move(moveVector * Time.deltaTime);

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    /// <summary>화면 입력이 없거나 목표 지점에 도착하면 수평 이동 없이 중력만 적용합니다.</summary>
    private void MoveWithGravity()
    {
        characterController.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
    }

    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        // 화면 터치 직접 이동 모드이면 별도의 클릭 이동을 시작하지 않습니다.
        if (controlMode == Define.MovementControlMode.DragOnly) return;

        // 화면 이동을 사용하는 모드에서는 손을 뗀 뒤 탭 이동이 다시 시작되지 않게 합니다.
        if (controlMode == Define.MovementControlMode.Both)
        {
            if (screenMoveInput != null)
            {
                if (context.control.device is Touchscreen)
                    return;
            }
        }

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
