using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ResourceManager
{
    #region 기존 Resources 폴더 사용방식
    //public T Load<T>(string path) where T : Object
    //{
    //    if (typeof(T) == typeof(GameObject))
    //    {
    //        string name = path;
    //        int index = name.LastIndexOf('/');
    //        if (index >= 0)
    //            name = name.Substring(index + 1);

    //        GameObject go = Managers.Pool.GetOriginal(name);
    //        if (go != null)
    //            return go as T;
    //    }
    //    return Resources.Load<T>(path);
    //}
    //public T[] LoadAll<T>(string path) where T : Object
    //{
    //    return Resources.LoadAll<T>(path);
    //}

    //// [추가] 프리팹 원본(GameObject)을 직접 받는 Instantiate
    //public GameObject Instantiate(GameObject original, Transform parent = null)
    //{
    //    if (original == null)
    //    {
    //        Debug.Log($"Failed to instantiate : Original is null");
    //        return null;
    //    }

    //    // 풀링이 필요한 아이템인지 체크 (Poolable 컴포넌트 유무)
    //    if (original.GetComponent<Poolable>() != null)
    //        return Managers.Pool.Get(original, parent).gameObject;

    //    // 일반 생성
    //    GameObject go = Object.Instantiate(original, parent);
    //    go.name = original.name;
    //    return go;
    //}

    //// [리팩토링] 기존 경로 기반 함수가 위 함수를 호출하도록 변경
    //public GameObject Instantiate(string path, Transform parent = null)
    //{
    //    GameObject original = Load<GameObject>($"Prefabs/{path}");
    //    if (original == null)
    //    {
    //        Debug.Log($"Failed to load prefab : {path}");
    //        return null;
    //    }

    //    return Instantiate(original, parent);
    //}

    ////  [추가됨] 프리팹 원본 + 위치 + 회전값을 받는 Instantiate
    //public GameObject Instantiate(GameObject original, Vector3 position, Quaternion rotation, Transform parent = null)
    //{
    //    if (original == null)
    //    {
    //        Debug.Log($"Failed to instantiate : Original is null");
    //        return null;
    //    }

    //    // 풀링이 필요한 아이템인지 체크
    //    if (original.GetComponent<Poolable>() != null)
    //    {
    //        // 풀에서 꺼낸 뒤 원하는 위치와 회전값으로 즉시 덮어씌웁니다.
    //        GameObject go = Managers.Pool.Get(original, parent).gameObject;
    //        go.transform.SetPositionAndRotation(position, rotation);
    //        return go;
    //    }

    //    // 일반 생성 시 유니티 기본 기능으로 좌표를 넣어서 생성
    //    GameObject clone = Object.Instantiate(original, position, rotation, parent);
    //    clone.name = original.name;
    //    return clone;
    //}
    ////  [추가됨] 경로(String) + 위치 + 회전값을 받는 Instantiate
    //public GameObject Instantiate(string path, Vector3 position, Quaternion rotation, Transform parent = null)
    //{
    //    GameObject original = Load<GameObject>($"Prefabs/{path}");
    //    if (original == null)
    //    {
    //        Debug.Log($"Failed to load prefab : {path}");
    //        return null;
    //    }

    //    return Instantiate(original, position, rotation, parent);
    //}

    //public void Destroy(GameObject go)
    //{
    //    if (go == null) return;

    //    //Poolable poolable = go.GetComponent<Poolable>();
    //    //if (poolable != null)
    //    //{
    //    //    Managers.Pool.Release(go);
    //    //    return;
    //    //}

    //    //Object.Destroy(go);

    //    // GetComponent 연산 생략 가능
    //    if (go.TryGetComponent<Poolable>(out var p) && p.IsPooled)
    //    {
    //        Managers.Pool.Release(go);
    //    }
    //    else
    //    {
    //        Object.Destroy(go);
    //    }
    //}

    #endregion

    #region 어드레서블 방식
    // 로드 완료된 에셋 캐시 저장소
    private Dictionary<string, UnityEngine.Object> _resources = new Dictionary<string, UnityEngine.Object>();

    // 메모리 해제(Release)를 위한 어드레서블 핸들 저장소
    private Dictionary<string, AsyncOperationHandle> _handles = new Dictionary<string, AsyncOperationHandle>();

    // 1. 현재 로드 완료 상태를 저장할 프로퍼티 추가
    public bool IsLoaded { get; private set; } = false;

    public event Action assetLoaded;
    public void Init()
    {
        // "Preload" 라벨을 가진 모든 UnityEngine.Object 타입 에셋들을 사전 로드하여 캐싱을 준비합니다.
        LoadAllAsync<UnityEngine.Object>("Preload",
            (key, current, total) =>
            {
                // 각 에셋이 하나씩 로드될 때마다 실행될 진행 상태 로그
                Debug.Log($"[ResourceManager] Preloading... {key} ({current}/{total})");
            },
            (result) =>
            {
                assetLoaded?.Invoke();
                IsLoaded = true;
                //Managers.UI.ShowSceneUI<UI_LoginScene>();
            });
    }
    #region 에셋 로딩
    // [1] 단일 에셋 비동기 로드 (Callback 방식)
    public void LoadAsync<T>(string key, Action<T> callback = null) where T : UnityEngine.Object
    {
        // 1. 이미 캐싱되어 있다면 즉시 반환
        if (_resources.TryGetValue(key, out UnityEngine.Object resource))
        {
            callback?.Invoke(resource as T);
            return;
        }

        // 2. 어드레서블을 통해 로드 시작
        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(key);
        handle.Completed += (opRef) =>
        {
            if (opRef.Status == AsyncOperationStatus.Succeeded)
            {
                _resources.Add(key, opRef.Result);
                _handles.Add(key, opRef);
                callback?.Invoke(opRef.Result);
            }
            else
            {
                Debug.LogError($"[ResourceManager] Failed to load addressable asset: {key}");
                callback?.Invoke(null);
            }
        };
    }
    // [3] 특정 라벨(Label)의 에셋들을 전부 사전 로딩 (기존 LoadAll<T> 대체)
    public void LoadAllAsync<T>(string label, Action<string, int, int> progressCallback = null, Action<Dictionary<string, UnityEngine.Object>> completeCallback = null) where T : UnityEngine.Object
    {
        // 라벨에 해당하는 리소스 위치들을 비동기로 검색
        var handle = Addressables.LoadResourceLocationsAsync(label, typeof(T));
        handle.Completed += (opRef) =>
        {
            int totalCount = opRef.Result.Count;
            int loadedCount = 0;

            if (totalCount == 0)
            {
                completeCallback?.Invoke(null);
                return;
            }

            foreach (var location in opRef.Result)
            {
                string key = location.PrimaryKey;
                LoadAsync<T>(key, (obj) =>
                {
                    loadedCount++;
                    // 로드 진행률 콜백 (현재 로드된 에셋 키, 진행 수, 총 수)
                    progressCallback?.Invoke(key, loadedCount, totalCount);

                    if (loadedCount == totalCount)
                    {
                        completeCallback?.Invoke(_resources);
                    }
                });
            }
        };
    }
    #endregion

    // [2] 캐시에서 동기식으로 에셋 꺼내기 (기존 Load<T> 대체)
    // *주의: 사전에 LoadAsync 등을 통해 사전 로드가 완료된 상태여야 합니다.
    public T Load<T>(string key) where T : UnityEngine.Object
    {
        if (typeof(T) == typeof(GameObject))
        {
            string name = key;
            int index = name.LastIndexOf('/');
            if (index >= 0)
                name = name.Substring(index + 1);

            // 오브젝트 풀링 매니저에 원본이 등록되어 있다면 그것을 우선 사용
            GameObject go = Managers.Pool.GetOriginal(name);
            if (go != null)
                return go as T;
        }

        if (_resources.TryGetValue(key, out UnityEngine.Object resource))
        {
            return resource as T;
        }

        Debug.LogWarning($"[ResourceManager] 에셋이 사전 로드되지 않았습니다: {key}. 비동기 로드를 먼저 수행하세요.");
        return null;
    }

    /// <summary>Resources 폴더 에셋도 리소스 관리자를 거쳐 한 곳에서 불러옵니다.</summary>
    public T LoadResource<T>(string path) where T : UnityEngine.Object
    {
        // 같은 키가 Addressables 캐시에 있으면 경고 없이 바로 반환합니다.
        if (_resources.TryGetValue(path, out UnityEngine.Object cached))
            return cached as T;

        // 아직 Addressables에 등록되지 않은 기존 Resources 에셋을 불러옵니다.
        T resource = Resources.Load<T>(path);
        if (resource == null)
            Debug.LogWarning($"[ResourceManager] 리소스를 찾을 수 없습니다: {path}");
        return resource;
    }

    public T[] LoadAll<T>(string prefix = "") where T : UnityEngine.Object
    {
        List<T> result = new List<T>();

        foreach (var kvp in _resources)
        {
            // 1. 키(주소)가 prefix로 시작하는지 확인 (prefix가 비어있으면 모두 통과)
            if (string.IsNullOrEmpty(prefix) || kvp.Key.StartsWith(prefix))
            {
                // 2. 해당 에셋이 원하는 타입(T)으로 형변환이 가능한지 확인
                if (kvp.Value is T asset)
                {
                    result.Add(asset);
                }
            }
        }

        // 찾은 에셋들을 배열로 변환하여 반환
        return result.ToArray();
    }

    // [4] 에셋 하나 개별 해제 (메모리 관리용)
    public void Release(string key)
    {
        if (_handles.TryGetValue(key, out AsyncOperationHandle handle))
        {
            Addressables.Release(handle);
            _handles.Remove(key);
            _resources.Remove(key);
        }
    }

    // [5] 씬 전환 시 등 모든 캐시 클리어
    public void Clear()
    {
        foreach (var handle in _handles.Values)
        {
            Addressables.Release(handle);
        }
        _handles.Clear();
        _resources.Clear();
    }

    // ========================================================== //
    // 아래의 Instantiate 및 Destroy 함수들은 Load<T> 구조가 캐시를 활용하게 되면서
    // 기존의 코딩 스타일과 풀링 연동 방식을 그대로 안전하게 유지할 수 있습니다.

    public GameObject Instantiate(GameObject original, Transform parent = null)
    {
        if (original == null)
        {
            Debug.Log($"Failed to instantiate : Original is null");
            return null;
        }

        if (original.GetComponent<Poolable>() != null)
            return Managers.Pool.Get(original, parent).gameObject;

        GameObject go = UnityEngine.Object.Instantiate(original, parent);
        go.name = original.name;
        return go;
    }

    /// <summary>풀에 넣으면 안 되는 가구나 배치 미리보기를 원본 복사본으로 생성합니다.</summary>
    public GameObject InstantiateUnpooled(GameObject original, Transform parent = null)
    {
        if (original == null)
        {
            Debug.LogWarning("[ResourceManager] 생성할 원본 오브젝트가 없습니다.");
            return null;
        }

        GameObject go = UnityEngine.Object.Instantiate(original, parent);
        go.name = original.name;
        return go;
    }

    /// <summary>풀을 사용하지 않고 원하는 위치와 회전으로 오브젝트를 생성합니다.</summary>
    public GameObject InstantiateUnpooled(GameObject original, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (original == null)
        {
            Debug.LogWarning("[ResourceManager] 생성할 원본 오브젝트가 없습니다.");
            return null;
        }

        GameObject go = UnityEngine.Object.Instantiate(original, position, rotation, parent);
        go.name = original.name;
        return go;
    }

    /// <summary>풀링 프리팹도 풀에 반환하지 않고 실제로 제거합니다.</summary>
    public void DestroyUnpooled(GameObject go)
    {
        if (go != null)
            UnityEngine.Object.Destroy(go);
    }

    public GameObject Instantiate(string path, Transform parent = null)
    {
        GameObject original = Load<GameObject>(path); // 이제 어드레서블 키값(주소)을 전달합니다.
        if (original == null)
        {
            Debug.Log($"Failed to load prefab : {path}");
            return null;
        }

        return Instantiate(original, parent);
    }

    public GameObject Instantiate(GameObject original, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (original == null)
        {
            Debug.Log($"Failed to instantiate : Original is null");
            return null;
        }

        if (original.GetComponent<Poolable>() != null)
        {
            GameObject go = Managers.Pool.Get(original, parent).gameObject;
            go.transform.SetPositionAndRotation(position, rotation);
            return go;
        }

        GameObject clone = UnityEngine.Object.Instantiate(original, position, rotation, parent);
        clone.name = original.name;
        return clone;
    }

    public GameObject Instantiate(string path, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        GameObject original = Load<GameObject>(path);
        if (original == null)
        {
            Debug.Log($"Failed to load prefab : {path}");
            return null;
        }

        return Instantiate(original, position, rotation, parent);
    }

    public void Destroy(GameObject go)
    {
        if (go == null) return;

        if (go.TryGetComponent<Poolable>(out var p) && p.IsPooled)
        {
            Managers.Pool.Release(go);
        }
        else
        {
            UnityEngine.Object.Destroy(go);
        }
    }
    #endregion
}

