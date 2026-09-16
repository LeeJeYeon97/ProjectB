using System.Collections.Generic;
using System.Collections;
using System.Xml.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.Pool;

public class PoolingManager
{
    // ObjectPool API
    //// 2. ���� Ǯ�� �ý���
    //private Dictionary<string, IObjectPool<GameObject>> _pools = new Dictionary<string, IObjectPool<GameObject>>();
    //// Ǯ���� ������Ʈ���� ���̾��Ű���� ���������� �ʰ� ������ ��Ʈ
    //private Transform _root;

    //public void Init()
    //{
    //    if (_root == null)
    //    {
    //        _root = new GameObject("@Pool_Root").transform;
    //        Object.DontDestroyOnLoad(_root);
    //    }

    //    // �������� ������ ��� ��ųʸ�
    //    foreach (var pool in Managers.Data.poolingData.poolList)
    //    {
    //        if (!_prefabDic.ContainsKey(pool.type))
    //        {
    //            _prefabDic.Add(pool.type, pool.original);
    //        }
    //    }
    //}

    //// ���������� Ǯ�� �����ϴ� �Լ�
    //private void CreatePool(Define.Pool type)
    //{

    //    string key = type.ToString();
    //    GameObject originalPrefab = _prefabDic[type]; // �̸� �����ص� ���� ���

    //    IObjectPool<GameObject> pool = new ObjectPool<GameObject>(
    //        createFunc: () => {
    //            GameObject go = Object.Instantiate(originalPrefab);
    //            go.name = key;
    //            go.transform.SetParent(_root);
    //            return go;
    //        },
    //        actionOnGet: (go) => go.SetActive(true),
    //        actionOnRelease: (go) => go.SetActive(false),
    //        actionOnDestroy: (go) => Object.Destroy(go),
    //        maxSize: 100
    //    );
    //    _pools.Add(key, pool);
    //}

    //// �ٽ� �Լ�: ������ (������Ʈ Ÿ������ �ٷ� ������)
    //public T Get<T>(Define.Pool type, Transform parent = null) where T : UnityEngine.Object
    //{
    //    string key = type.ToString();
    //    // 1. �ش� ���(�̸�)�� Ǯ�� �ִ��� Ȯ���ϰ� ������ ����
    //    if (!_pools.ContainsKey(key))
    //    {
    //        // ���� ������ ��ϵ� �� �Ǿ� �ִٸ� ����
    //        if (!_prefabDic.ContainsKey(type))
    //        {
    //            Debug.LogError($"[PoolingManager] {type} Ÿ���� �������� SO�� ��ϵ��� �ʾҽ��ϴ�!");
    //            return null;
    //        }
    //        CreatePool(type);
    //    }

    //    GameObject go = _pools[key].Get();
    //    if (parent != null) go.transform.SetParent(parent);

    //    if (typeof(T) == typeof(GameObject)) return go as T;
    //    T component = go.GetComponent<T>();
    //    // 3. T�� ������Ʈ��� GetComponent�� ã�ƺ���

    //    // 4. ������Ʈ�� ���ٸ�? �ڵ�� ���� �ٿ��ֱ�!
    //    if (component == null)
    //    {
    //        // AddComponent<T>() ��� AddComponent(typeof(T))�� ����Ͽ� ���׸� ���� ���� ��ȸ
    //        component = go.AddComponent(typeof(T)) as T;
    //    }

    //    return component;

    //}

    //// �ٽ� �Լ�: �ݳ��ϱ�
    //public void Release(GameObject go)
    //{
    //    if (go == null || !go.activeSelf) return;
    //    string key = go.name;

    //    if (_pools.ContainsKey(key))
    //        _pools[key].Release(go);
    //    else
    //        Object.Destroy(go);
    //}

    //public void Clear()
    //{
    //    foreach (var pool in _pools.Values) pool.Clear();
    //    foreach (Transform child in _root) Object.Destroy(child.gameObject);
    //    _pools.Clear();
    //    _prefabDic.Clear();
    //}
    #region Pool
    class Pool
    {
        public GameObject Original { get; private set; }
        public Transform Root { get; set; }

        Stack<Poolable> _poolStack = new Stack<Poolable>();

        public void Init(GameObject original, int count = 5)
        {
            Original = original;
            Root = new GameObject().transform;
            Root.name = $"{original.name}_Root";

            for (int i = 0; i < count; i++)
                Push(Create());
        }

        Poolable Create()
        {
            GameObject go = Object.Instantiate<GameObject>(Original);
            go.name = Original.name;
            return go.GetOrAddComponent<Poolable>();
        }

        public void Push(Poolable poolable)
        {
            if (poolable == null)
                return;

            poolable.transform.SetParent(Root, false);
            poolable.gameObject.SetActive(false);
            poolable.IsUsing = false;

            _poolStack.Push(poolable);
        }

        public Poolable Pop(Transform parent)
        {
            Poolable poolable;

            if (_poolStack.Count > 0)
                poolable = _poolStack.Pop();
            else
                poolable = Create();

            poolable.gameObject.SetActive(true);

            // DontDestroyOnLoad ���� �뵵
            // DontDestroyOnLoad ���� �뵵
            if (parent == null)
            {
                // ���� ��: poolable.transform.parent = Managers.Scene.CurrentScene.transform;
                poolable.transform.SetParent(Managers.Scene.CurrentScene.transform, false);
            }

            poolable.transform.SetParent(parent, false);
            poolable.IsUsing = true;

            return poolable;
        }
    }
    #endregion

    Dictionary<string, Pool> _pool = new Dictionary<string, Pool>();
    Transform _root;

    public void Init()
    {
        if (_root == null)
        {
            _root = new GameObject { name = "@Pool_Root" }.transform;
            if (Application.isPlaying)
            {
                Object.DontDestroyOnLoad(_root);
            }
        }
    }

    public void CreatePool(GameObject original, int count = 5)
    {
        Pool pool = new Pool();
        pool.Init(original, count);
        pool.Root.parent = _root;

        _pool.Add(original.name, pool);
    }

    public void Release(GameObject go)
    {
        if (go == null) return;

        // 2. �����ſ� ����(Poolable)�� �ִ��� �Ŵ����� ���� Ȯ���մϴ�.
        if (go.TryGetComponent<Poolable>(out Poolable poolable))
        {
            string name = go.name;

            // ������ �ִµ� �� �ٱ��� ��Ͽ� ���ٸ�? (���� ��Ȳ) -> �׳� �ı�
            if (_pool.ContainsKey(name) == false)
            {
                Object.Destroy(go);
                return;
            }

            // �������� Ǯ�� ��ü��� �ٱ��Ͽ� �ֱ�
            _pool[name].Push(poolable);
        }
        else
        {
            // 3. ������ ���� ��ü(��ȸ�� ��ƼŬ, ���� ��)��� �̷� ���� ��� �ı�!
            Object.Destroy(go);
        }
    }

    public Poolable Get(GameObject original, Transform parent = null)
    {
        if (_pool.ContainsKey(original.name) == false)
            CreatePool(original);

        return _pool[original.name].Pop(parent);
    }

    public GameObject GetOriginal(string name)
    {
        if (_pool.ContainsKey(name) == false)
            return null;
        return _pool[name].Original;
    }

    public void Clear()
    {
        foreach (Transform child in _root)
            GameObject.Destroy(child.gameObject);

        _pool.Clear();
    }

}
