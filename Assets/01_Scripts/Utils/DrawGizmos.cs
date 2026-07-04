using UnityEngine;

public class DrawGizmos : MonoBehaviour
{
    [Range(0,10.0f)]
    public float range;
    private void OnDrawGizmosSelected()
    {
        // 2. 기즈모 색상 설정 (눈에 확 띄는 빨간색 반투명으로 세팅)
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);

        // 3. 내 위치(총알 위치)를 중심으로 폭발 범위(반지름)만큼 구(Sphere)를 그립니다.
        // 유니티 2D 환경이라도 DrawWireSphere를 쓰면 아주 깔끔한 원형 테두리가 그려집니다!
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
