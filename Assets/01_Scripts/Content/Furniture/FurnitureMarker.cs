using UnityEngine;

/// <summary>플레이어가 배치한 가구인지와 저장용 ID를 표시합니다.</summary>
public class FurnitureMarker : MonoBehaviour
{
    public string furnitureId;

    /// <summary>씬에 원래 있던 가구인지 표시해 이동 위치를 원본 오브젝트에 복원합니다.</summary>
    public bool isSceneFurniture;

    /// <summary>씬 가구를 다시 찾을 때 사용할 계층 경로를 저장합니다.</summary>
    public string scenePath;
}
