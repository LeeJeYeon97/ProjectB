using UnityEngine;

public interface IStatusTarget
{
    // 화상 등의 틱 데미지 처리
    void TakeStatusDamage(float damage);

    // 슬로우를 위한 속도 배율 변경
    void AddSpeedMultiplier(float multiplier);
    void SubSpeedMultiplier(float multiplier);

    // 빙결을 위한 강제 정지
    void SetForceZeroSpeed(bool isZero);

    // 시각적 효과 (색상 변경)
    void SetStatusColor(Color color);
    void ResetStatusColor();
}