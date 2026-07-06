using NUnit.Framework.Internal;
using System;
using TMPro;
using UnityEngine;

public class Managers : MonoBehaviour
{
    static bool _isQuitting = false;

    private static Managers _instance;
    public static Managers Instance
    {
        get
        {
            if (_instance == null)
            {
                Init();
            }
            return _instance;
        }
    }
    // ========================================================== //
    // 각 관리자들 (필요한 매니저들을 여기에 추가)
    private CameraManager _camera;
    private DataManager _data;
    private EffectManager _effect;
    private EventManager _event;
    private InitManager _init;
    private InputManager _input;
    private LoginManager _login;
    private PoolingManager _pool;       
    private ResourceManager _resource;
    private SceneManagerEx _scene;
    private SettingManager _setting;
    private SoundManager _sound;
    private UIManager _ui;
    private UpdateManager _update;
    private WebServerManager _webServer;
    private CoroutineHelper _coroutine;

    // ========================================================== //
    // 프로퍼티를 통해 외부에서 접근하도록 설정
    public static CameraManager Camera => Instance._camera;
    public static DataManager Data => Instance._data;
    public static EffectManager Effect => Instance._effect;
    public static EventManager Event => Instance._event;
    public static InitManager Initialize => Instance._init;
    public static InputManager Input => Instance._input;
    public static LoginManager Login => Instance._login;
    public static PoolingManager Pool => Instance._pool;
    public static ResourceManager Resource => Instance._resource;
    public static SceneManagerEx Scene => Instance._scene;
    public static SettingManager Setting => Instance._setting;
    public static SoundManager Sound => Instance._sound;
    public static UIManager UI => Instance._ui;
    public static UpdateManager VersionUpdate => Instance._update;
    public static WebServerManager WebServer => Instance._webServer;
    public static CoroutineHelper Coroutine => Instance._coroutine;
    // ========================================================== //

    private void OnApplicationQuit()
    {
        _isQuitting = true;
    }
    static void Init()
    {
        if (_isQuitting) return;

        if (_instance == null)
        {
            GameObject go = GameObject.Find("@Managers");
            if (go == null)
            {
                go = new GameObject { name = "@Managers" };
                go.AddComponent<Managers>();
            }

            DontDestroyOnLoad(go);
            _instance = go.GetComponent<Managers>();

            // 컴포넌트로 붙일 필요없는 매니저 초기화
            _instance._camera = new CameraManager();
            _instance._data = new DataManager();
            _instance._effect = new EffectManager();
            _instance._event = new EventManager();
            _instance._init = new InitManager();
            _instance._input = new InputManager();
            _instance._login = new LoginManager();
            _instance._pool = new PoolingManager();
            _instance._resource = new ResourceManager();
            _instance._scene = new SceneManagerEx();
            _instance._setting = new SettingManager();
            _instance._sound = new SoundManager();
            _instance._ui = new UIManager();
            _instance._update = new UpdateManager();
            _instance._webServer = new WebServerManager();


            // 매니저들 초기화 함수
            // Core
            // InitManager초기화는 처음 Scene에서 진행하기
            Pool.Init();
            //Input.Init();
            Sound.Init();
            Effect.Init();
            Scene.Init();
            UI.Init();
            Setting.Init();

            // 로그 안보이게
            Debug.unityLogger.logEnabled = Debug.isDebugBuild;
            Application.targetFrameRate = 60; // 60프레임 고정 (부드러운 화면)

        }
    }
    private void Update()
    {
        
    }
    private void LateUpdate()
    {
        Effect.OnLateUpdate();
    }
    public void Clear()
    {
        _scene.Clear();
        _sound.Clear();
        _ui.Clear();
        if (_coroutine != null)
        {
            _coroutine.StopAllCoroutines();
        }
    }
}
