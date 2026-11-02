using UnityEngine;

/// <summary>미리 만들어 둔 바닥 표시를 BoxCollider의 가로·세로 범위에 맞춥니다.</summary>
[ExecuteAlways]
public class BoxAreaDisplay : MonoBehaviour
{
    #region 표시 연결

    [SerializeField] private BoxCollider areaCollider;
    [SerializeField] private Transform fill;
    [SerializeField] private Transform left;
    [SerializeField] private Transform right;
    [SerializeField] private Transform front;
    [SerializeField] private Transform back;

    #endregion

    #region 유니티 함수

    private void LateUpdate()
    {
        RefreshArea();
    }

    #endregion

    #region 범위 표시

    /// <summary>콜라이더의 중심과 크기를 읽어 바닥과 네 테두리를 배치합니다.</summary>
    private void RefreshArea()
    {
        if (areaCollider == null) return;
        if (transform.parent != areaCollider.transform) return;

        // 높이는 기존 바닥 표시를 유지하고 가로·세로 중심만 콜라이더와 맞춥니다.
        Vector3 position = transform.localPosition;
        position.x = areaCollider.center.x;
        position.z = areaCollider.center.z;
        if (transform.localPosition != position) transform.localPosition = position;

        float width = areaCollider.size.x;
        float depth = areaCollider.size.z;
        SetPart(fill, 0f, 0f, width, depth);
        SetPart(left, -width * 0.5f, 0f, -1f, depth);
        SetPart(right, width * 0.5f, 0f, -1f, depth);
        SetPart(front, 0f, -depth * 0.5f, width, -1f);
        SetPart(back, 0f, depth * 0.5f, width, -1f);
    }

    /// <summary>테두리의 두께와 높이는 유지하면서 필요한 축만 조정합니다.</summary>
    private void SetPart(Transform part, float x, float z, float width, float depth)
    {
        if (part == null) return;

        Vector3 position = part.localPosition;
        position.x = x;
        position.z = z;
        if (part.localPosition != position) part.localPosition = position;

        Vector3 scale = part.localScale;
        if (width >= 0f) scale.x = width;
        if (depth >= 0f) scale.z = depth;
        if (part.localScale != scale) part.localScale = scale;
    }

    #endregion
}
