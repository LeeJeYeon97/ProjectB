using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera _mainCamera;

    void Start()
    {
        // 씬에 있는 메인 카메라를 찾아 캐싱해둡니다. (성능 최적화)
        _mainCamera = Camera.main;
    }

    // Update가 아닌 LateUpdate를 사용하는 것이 핵심입니다!
    void LateUpdate()
    {
        if (_mainCamera == null) return;

        // 카메라가 바라보는 앞(Forward) 방향과 UI가 바라보는 앞 방향을 똑같이 맞춰줍니다.
        transform.forward = _mainCamera.transform.forward;

        // (참고) 만약 UI가 위아래로 끄덕이지 않고 좌우로만 카메라를 보게 하려면 아래 코드를 씁니다.
        // Vector3 forward = _mainCamera.transform.forward;
        // forward.y = 0;
        // transform.forward = forward;
    }
}