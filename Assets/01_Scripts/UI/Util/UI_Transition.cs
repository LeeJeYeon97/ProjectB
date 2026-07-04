using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UI_Transition : MonoBehaviour
{
    // 씬을 이동할 때 이 함수를 부릅니다!
    public Image fadeImage;

    private void Awake()
    {
        if(fadeImage.gameObject.activeSelf == true)
        {
            fadeImage.gameObject.SetActive(false);
        }
    }
    // 화면을 까맣게 덮는 함수 (완료되면 onComplete 실행)
    public void FadeOut(Action onComplete)
    {
        fadeImage.gameObject.SetActive(true);
        fadeImage.color = new Color(0, 0, 0, 0);

        // 0.5초 동안 까매지고, 끝나면 감독(SceneManager)에게 알려줌
        fadeImage.DOFade(1f, 1.0f).SetLink(gameObject).SetUpdate(true).OnComplete(() =>
        {
            if (onComplete != null) onComplete.Invoke();
        });
    }

    public void FadeIn(Action onComplete = null)
    {
        StartCoroutine(WaitAndFadeInRoutine(onComplete));
    }

    private IEnumerator WaitAndFadeInRoutine(Action onComplete)
    {
        yield return null;
        yield return null;
        yield return null;

        //  OnComplete 부분에 전달받은 신호를 실행하도록 추가합니다.
        fadeImage.DOFade(0f, 1.0f).SetLink(gameObject).SetUpdate(true).OnComplete(() =>
        {
            fadeImage.gameObject.SetActive(false);

            // 신호가 있다면 실행!
            if (onComplete != null)
                onComplete.Invoke();
        });
    }
    private void OnDestroy()
    {
        // 이 스크립트(또는 이미지)에 걸려있는 모든 DOTween 애니메이션을 즉시 정지 및 삭제
        fadeImage.DOKill();

        // 또는 transform 기준으로 걸려있는 모든 트윈을 끌 수도 있습니다.
        // transform.DOKill();
    }
}
