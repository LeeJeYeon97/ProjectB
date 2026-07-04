using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraResolution : MonoBehaviour
{
    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    // 에디터에서 기기를 바꿀 때마다 실시간으로 비율을 다시 계산합니다.
    void Update()
    {
        SetResolution();
    }

    private void SetResolution()
    {
        Rect rect = cam.rect;
        float targetRatio = 9f / 19.5f;
        float currentRatio = (float)Screen.width / Screen.height;
        float scaleHeight = currentRatio / targetRatio;

        if (scaleHeight < 1f)
        {
            // 위아래 레터박스 (폴드 등 길쭉한 기기)
            rect.width = 1f; // 가로는 꽉 채움
            rect.height = scaleHeight;
            rect.x = 0f;
            rect.y = (1f - scaleHeight) / 2f;
        }
        else
        {
            // 양옆 필러박스 (아이패드 등 넙적한 기기)
            float scaleWidth = 1f / scaleHeight;
            rect.width = scaleWidth;
            rect.height = 1f; // 세로는 꽉 채움
            rect.x = (1f - scaleWidth) / 2f;
            rect.y = 0f;
        }

        cam.rect = rect;
    }
}