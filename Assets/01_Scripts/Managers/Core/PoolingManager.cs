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
    //// 2. 실제 풀링 시스템
    //private Dictionary<string, IObjectPool<GameObject>> _pools = new Dictionary<string, IObjectPool<GameObject>>();
    //// 풀링된 오브젝트들이 하이어라키에서 지저분하지 않게 정리할 루트
    //private Transform _root;

    //public void Init()
    //{
    //    if (_root == null)
    //    {
    //        _root = new GameObject("@Pool_Root").transform;
    //        Object.DontDestroyOnLoad(_root);
    //    }

    //    // 오리지널 프리팹 담는 딕셔너리
    //    foreach (var pool in Managers.Data.poolingData.poolList)
    //    {
    //        if (!_prefabDic.ContainsKey(pool.type))
    //        {
    //            _prefabDic.Add(pool.type, pool.original);
    //        }
    //    }
    //}

    //// 내부적으로 풀을 생성하는 함수
    //private void CreatePool(Define.Pool type)
    //{

    //    string key = type.ToString();
    //    GameObject originalPrefab = _prefabDic[type]; // 미리 저장해둔 원본 사용

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

    //// 핵심 함수: 꺼내기 (컴포넌트 타입으로 바로 가져옴)
    //public T Get<T>(Define.Pool type, Transform parent = null) where T : UnityEngine.Object
    //{
    //    string key = type.ToString();
    //    // 1. 해당 경로(이름)의 풀이 있는지 확인하고 없으면 만듦
    //    if (!_pools.ContainsKey(key))
    //    {
    //        // 만약 프리팹 등록도 안 되어 있다면 에러
    //        if (!_prefabDic.ContainsKey(type))
    //        {
    //            Debug.LogError($"[PoolingManager] {type} 타입의 프리팹이 SO에 등록되지 않았습니다!");
    //            return null;
    //        }
    //        CreatePool(type);
    //    }

    //    GameObject go = _pools[key].Get();
    //    if (parent != null) go.transform.SetParent(parent);

    //    if (typeof(T) == typeof(GameObject)) return go as T;
    //    T component = go.GetComponent<T>();
    //    // 3. T가 컴포넌트라면 GetComponent로 찾아보기

    //    // 4. 컴포넌트가 없다면? 코드로 직접 붙여주기!
    //    if (component == null)
    //    {
    //        // AddComponent<T>() 대신 AddComponent(typeof(T))를 사용하여 제네릭 제약 조건 우회
    //        component = go.AddComponent(typeof(T)) as T;
    //    }

    //    return component;

    //}

    //// 핵심 함수: 반납하기
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

            // DontDestroyOnLoad 해제 용도
            // DontDestroyOnLoad 해제 용도
            if (parent == null)
            {
                // 수정 전: poolable.transform.parent = Managers.Scene.CurrentScene.transform;
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
            Object.DontDestroyOnLoad(_root);
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

        // 2. 가슴팍에 명찰(Poolable)이 있는지 매니저가 직접 확인합니다.
        if (go.TryGetComponent<Poolable>(out Poolable poolable))
        {
            string name = go.name;

            // 명찰은 있는데 내 바구니 목록에 없다면? (에러 상황) -> 그냥 파괴
            if (_pool.ContainsKey(name) == false)
            {
                Object.Destroy(go);
                return;
            }

            // 정상적인 풀링 객체라면 바구니에 넣기
            _pool[name].Push(poolable);
        }
        else
        {
            // 3. 명찰이 없는 객체(일회용 파티클, 보스 등)라면 미련 없이 즉시 파괴!
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
