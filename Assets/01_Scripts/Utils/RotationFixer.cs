using UnityEngine;

public class RotationFixer : MonoBehaviour
{
    // 고정하고 싶은 각도 (기본적으로 0, 0, 0이면 정면을 봅니다)
    private Quaternion _initialRotation;

    void Awake()
    {
        // 시작할 때의 각도를 저장 (혹은 그냥 Quaternion.identity 사용 가능)
        _initialRotation = Quaternion.identity;
    }

    // 부모의 회전이 반영된 후 마지막에 내 회전을 고정시켜야 하므로 LateUpdate 사용
    void LateUpdate()
    {
        // 부모가 어떻게 돌든 나는 항상 0도(정면)를 유지합니다.
        transform.rotation = _initialRotation;
    }
}