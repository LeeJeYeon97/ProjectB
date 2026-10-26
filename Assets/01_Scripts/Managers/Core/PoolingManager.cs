using System.Collections.Generic;
using System.Collections;
using System.Xml.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.Pool;

public class PoolingManager
{
    
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
            // 씬 정리와 소유 객체 비활성화가 연달아 일어나도 같은 인스턴스를 두 번 넣지 않습니다.
            if (!poolable.IsUsing)
                return;

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
