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
    // Core
    private CameraManager _camera;
    private DataManager _data;
    private EffectManager _effect;
    private EventManager _event;
    private InitManager _init;
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

    // Content
    private GameManager _game;
    private UnlockManager _unlock;
    private CustomerManager _customer;

    // ========================================================== //
    // Core
    public static CameraManager Camera => Instance._camera;
    public static DataManager Data => Instance._data;
    public static EffectManager Effect => Instance._effect;
    public static EventManager Event => Instance._event;
    public static InitManager Initialize => Instance._init;
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

    // Content
    public static GameManager Game => Instance._game;
    public static UnlockManager Unlock => Instance._unlock;
    public static CustomerManager Customer => Instance._customer;
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

            if (Application.isPlaying)
            {
                DontDestroyOnLoad(go);
            }
            _instance = go.GetComponent<Managers>();

            // ������Ʈ�� ���� �ʿ���� �Ŵ��� �ʱ�ȭ
            _instance._camera = new CameraManager();
            _instance._data = new DataManager();
            _instance._effect = new EffectManager();
            _instance._event = new EventManager();
            _instance._init = new InitManager();
            _instance._login = new LoginManager();
            _instance._pool = new PoolingManager();
            _instance._resource = new ResourceManager();
            _instance._scene = new SceneManagerEx();
            _instance._setting = new SettingManager();
            _instance._sound = new SoundManager();
            _instance._ui = new UIManager();
            _instance._update = new UpdateManager();
            _instance._webServer = new WebServerManager();


            // Core
            _instance._game = new GameManager();
            _instance._unlock = new UnlockManager();
            _instance._customer = new CustomerManager();

            // �Ŵ����� �ʱ�ȭ �Լ�
            // Core
            // InitManager�ʱ�ȭ�� ó�� Scene���� �����ϱ�
            Pool.Init();
            Resource.Init();
            Sound.Init();
            Effect.Init();
            Scene.Init();
            UI.Init();
            Setting.Init();


            Customer.Init();

            // �α� �Ⱥ��̰�
            Debug.unityLogger.logEnabled = Debug.isDebugBuild;
            Application.targetFrameRate = 60; // 60������ ���� (�ε巯�� ȭ��)

        }
    }
    public void Test()
    {
        Debug.Log("test");
    }
    private void Update()
    {
        Customer.OnUpdate();
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
