
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
        // 1. 새로운 씬을 로드하기 전에 현재 씬의 데이터 및 이벤트를 정리합니다.
        Managers.Instance.Clear();
        
        // 2. Enum을 바탕으로 로드할 씬의 이름을 가져옵니다.
        string sceneName = GetSceneName(type);

        // 3. 비동기로 씬을 로드합니다.
        // 일반 LoadScene과 달리 게임이 멈추지 않고 백그라운드에서 씬을 불러옵니다.
        SceneManager.LoadSceneAsync(sceneName);
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
