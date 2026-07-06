using UnityEngine;
using UnityEngine.EventSystems;

public class UI_ClickSound : MonoBehaviour, IPointerClickHandler
{
    [Header("사운드 설정")]
    [Tooltip("인스펙터에서 사운드 파일을 직접 드래그해서 넣으세요.")]
    public AudioClip clickSoundClip;

    [Tooltip("위에 클립을 넣지 않았다면, 이 경로의 사운드를 재생합니다.")]
    public string soundPath = "SFX/UI_Click";

    public void OnPointerClick(PointerEventData eventData)
    {
        // 1. 인스펙터에 직접 넣은 사운드 파일(AudioClip)이 있다면 그것을 우선 재생합니다.
        if (clickSoundClip != null)
        {
            // (주의: Managers.Sound 쪽에 AudioClip을 직접 받는 Play 오버로딩 함수가 있어야 합니다!)
            Managers.Sound.Play(clickSoundClip);
        }
        // 2. 직접 넣은 파일이 비어있다면, 기존처럼 string 경로를 이용해 재생합니다.
        else if (!string.IsNullOrEmpty(soundPath))
        {
            Managers.Sound.Play(soundPath);
        }
    }
}