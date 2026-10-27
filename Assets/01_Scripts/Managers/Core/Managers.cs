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
    private PlayerPrefsManager _playerPrefs;
    private SoundManager _sound;
    private UIManager _ui;
    private UpdateManager _update;
    private WebServerManager _webServer;
    private QuestManager _quest;

    // Content
    [SerializeField]
    private GameManager _game;
    [SerializeField]
    private QuestCatalog _questCatalog;
    private UnlockManager _unlock;
    private CustomerManager _customer;
    private EmployeeManager _employee;
    private TaskManager _task;
    [SerializeField]
    private DishManager _dish;
    private GrowthManager _growth;
    private IngredientManager _ingredient;
    private ReviewManager _review;

    [SerializeField]
    private StationManager _station;

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
    public static PlayerPrefsManager PlayerPrefs => Instance._playerPrefs;
    public static SoundManager Sound => Instance._sound;
    public static UIManager UI => Instance._ui;
    public static UpdateManager VersionUpdate => Instance._update;
    public static WebServerManager WebServer => Instance._webServer;
    public static QuestManager Quest => Instance._quest;

    // Content
    public static GameManager Game => Instance._game;
    public static UnlockManager Unlock => Instance._unlock;
    public static CustomerManager Customer => Instance._customer;
    public static EmployeeManager Employee => Instance._employee;
    public static TaskManager Task => Instance._task;
    public static DishManager Dish => Instance._dish;
    public static GrowthManager Growth => Instance._growth;
    public static IngredientManager Ingredient => Instance._ingredient;
    public static ReviewManager Review => Instance._review;
    public static StationManager Station => Instance._station;
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

            // Core
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
            _instance._playerPrefs = new PlayerPrefsManager();
            _instance._sound = new SoundManager();
            _instance._ui = new UIManager();
            _instance._update = new UpdateManager();
            _instance._webServer = new WebServerManager();
            _instance._quest = new QuestManager();


            // Content
            _instance._game = new GameManager();
            _instance._unlock = new UnlockManager();
            _instance._customer = new CustomerManager();
            _instance._employee = new EmployeeManager();
            _instance._task = new TaskManager();
            _instance._dish = new DishManager();
            _instance._growth = new GrowthManager();
            _instance._ingredient = new IngredientManager();
            _instance._review = new ReviewManager();
            _instance._station = new StationManager();

            // Core
            _instance._camera.Init();
            Pool.Init();
            Resource.Init();
            Sound.Init();
            Effect.Init();
            Scene.Init();
            UI.Init();
            Setting.Init();
            Quest.Init(_instance._questCatalog);
            Ingredient.Init();


            // Content
            Customer.Init();
            Employee.Init();
            Dish.Init();
            Growth.Init();
            Review.Init();

            Debug.unityLogger.logEnabled = Debug.isDebugBuild;
            Application.targetFrameRate = 60; 

        }
    }
    private void Awake()
    {
        Game.OnAwake();
    }
    private void Update()
    {
        Customer.OnUpdate();
    }
    public void Clear()
    {
        _scene.Clear();
        _sound.Clear();
        _ui.Clear();
        _employee?.Clear();
        _task?.Clear();
    }
}
