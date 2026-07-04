using UnityEngine;
using UnityEngine.EventSystems;

public class UI_Rotate3DModel : MonoBehaviour, IDragHandler
{
    [Tooltip("회전시킬 실제 3D 모델의 Transform을 넣으세요")]
    public Transform targetModel;

    public float rotationSpeed = -0.5f; // 회전 속도 (음수면 드래그 방향과 반대)

    public string targetObjectName = "Spaceship_Hero_F019";

    // RawImage 영역을 드래그할 때마다 호출됨
    public void Awake()
    {
        // 1. 인스펙터에 이미 할당되어 있다면 굳이 찾지 않고 넘어갑니다.
        if (targetModel != null) return;

        // 2. 씬에서 이름으로 게임 오브젝트를 찾습니다.
        GameObject foundObj = GameObject.Find(targetObjectName);

        // 3. 찾았다면 Transform을 할당하고, 못 찾았다면 경고 로그를 띄웁니다.
        if (foundObj != null)
        {
            targetModel = foundObj.transform;
        }
        else
        {
            Debug.LogWarning($"[UI_Rotate3DModel] 씬에서 '{targetObjectName}' 이름의 오브젝트를 찾을 수 없습니다! 하이러키 이름을 확인해주세요.");
        }
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (targetModel != null)
        {
            // 마우스/터치의 좌우 이동량(eventData.delta.x)만큼 Y축으로 회전
            targetModel.Rotate(0, eventData.delta.x * rotationSpeed, 0, Space.World);
        }
    }
}