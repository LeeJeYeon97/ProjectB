using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerEx
{
    public BaseScene CurrentScene
    {
        get
        {
            // 변수가 비어있을 때만 찾습니다.
            if (_currentScene == null)
                _currentScene = GameObject.FindAnyObjectByType<BaseScene>();
            return _currentScene;
        }
    }
    private BaseScene _currentScene; // 캐싱용 변수

    public void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    public void Clear()
    {
        //SceneManager.sceneLoaded -= OnSceneLoaded;
        // 절대 이벤트를 해제(-=)하면 안 됩니다! 
        // 씬 매니저가 살아있는 한 씬 로드 감지는 계속되어야 합니다.
        if (CurrentScene != null)
        {
            CurrentScene.Clear();
        }
        _currentScene = null;
    }
    public void LoadScene(Define.Scene type)
    {
        
        LoadSceneWithFade(GetSceneName(type));
    }
    public void LoadSceneWithFade(string sceneName)
    {
        UI_Transition transition = Managers.UI.Transition;

        // 1. 화면 가리기 시작!
        transition.FadeOut(() =>
        {
            //  [추가된 핵심 코드: 황금 타이밍의 KillAll] 
            // 이 중괄호 안쪽은 FadeOut 트윈은 끝났고, FadeIn 트윈은 큐에 들어가지도 않은 상태입니다.
            // Transition을 예외 처리할 필요조차 없이, 파괴될 씬의 모든 찌꺼기 트윈만 완벽하게 청소합니다!
            DG.Tweening.DOTween.KillAll();

            // 2. 동기(LoadScene) 대신 비동기(LoadSceneAsync)로 씬을 불러옵니다!
            // 이렇게 하면 로딩 중에도 유니티가 멈추지 않고 애니메이션을 부드럽게 유지합니다.
            AsyncOperation asyncOper = SceneManager.LoadSceneAsync(sceneName);

            Managers.Instance.Clear();
            // 3. 씬 로드가 100% "완전히" 끝났을 때(completed) FadeIn을 부르도록 예약!
            asyncOper.completed += (AsyncOperation obj) =>
            {
                Debug.Log("씬로드 완료 페이드인 부름");

                if (sceneName == "GameScene")
                {

                    //transition.FadeIn( () => Managers.UI.ShowPopupUI<UI_StartCountDownPopup>());
                }
                else
                {
                    transition.FadeIn();
                }
            };
        });
    }
    string GetSceneName(Define.Scene type)
    {
        string name = System.Enum.GetName(typeof(Define.Scene), type);
        return name;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1. 현재 씬에 BaseScene을 상속받은 객체가 있는지 확인
        BaseScene sceneObj = GameObject.FindAnyObjectByType<BaseScene>();

        // 2. 없다면 생성
        if (sceneObj == null)
        {
            // @Scene이라는 이름으로 빈 오브젝트 생성
            GameObject go = new GameObject { name = "@Scene" };

            // 씬 타입에 맞는 컴포넌트를 동적으로 붙여줍니다. (예: GameScene, LobbyScene 등)
            // 씬 이름이 "GameScene"이라면 "GameScene"이라는 클래스를 찾아 붙입니다.
            string sceneName = scene.name;
            System.Type sceneType = System.Type.GetType(sceneName);

            if (sceneType != null)
                go.AddComponent(sceneType);
            else
                Debug.LogWarning($"{sceneName} 타입의 스크립트를 찾을 수 없습니다.");
        }
    }

}
