using System.Collections.Generic;
using UnityEngine;


public class UIManager
{
    int _order = 10;

    Stack<UI_Popup> _popupStack = new Stack<UI_Popup>();
    UI_Scene _sceneUI = null;
    public UI_Transition Transition;

    public GameObject Root
    {
        get
        {
            GameObject root = GameObject.Find("@UI_Root");
            if (root == null)
                root = new GameObject { name = "@UI_Root" };
            return root;
        }
    }

    public void Init()
    {
        // 씬 전환 애니메이션용 프리팹
        GameObject go  = Managers.Resource.Instantiate("UI/SubItem/UI_Transition");
        Object.DontDestroyOnLoad(go);
        Transition = go.GetComponent<UI_Transition>();
    }
    public void Clear()
    {
        CloseAllPopupUI();
        if(_sceneUI != null)
        {
            Managers.Resource.Destroy(_sceneUI.gameObject);
        }
    }
    public void SetCanvas(GameObject go, bool sort = true)
    {
        Canvas canvas = Util.GetOrAddComponent<Canvas>(go);

        //  1. UI 오브젝트에 붙어있는 스크립트(UI_Base)를 찾아서, 대표님이 설정한 모드를 알아냅니다.
        UI_Base uiBase = go.GetComponent<UI_Base>();
        RenderMode targetMode = RenderMode.ScreenSpaceCamera; // 기본값

        if (uiBase != null)
        {
            targetMode = uiBase.canvasRenderMode; // 프리팹에서 설정한 값 가져오기
        }

        // 2. 유니티가 멋대로 바꿨든 말든, 우리가 원하는 모드로 강제 세팅해버립니다!
        canvas.renderMode = targetMode;

        //  3. Camera 모드라면 메인 카메라를 찰칵!
        if (targetMode == RenderMode.ScreenSpaceCamera)
        {
            canvas.worldCamera = Camera.main;
            if (canvas.worldCamera == null)
            {
                Debug.LogWarning($"[UI 매니저] {go.name}에 할당할 MainCamera를 찾을 수 없습니다.");
            }
            else
            {
                
                //[추가 1] 카메라가 할당되었다면, Plane Distance를 코드로 강제 고정! (메테오 뚫림 방지)
                canvas.planeDistance = 1f;
                
            }
        }

        canvas.overrideSorting = true;
        // [추가 2] 아까 만든 "UI" Sorting Layer를 기본값으로 강제 지정!
        // (만약 유니티 에디터에서 "UI"라는 레이어를 안 만드셨다면 이 줄은 빼셔도 됩니다)
        //canvas.sortingLayerName = "UI";

        if (sort)
        {
            canvas.sortingOrder = _order;
            _order++;
        }
        else
        {
            canvas.sortingOrder = 0;
        }
    }
    public T MakeSubItem<T>(Transform parent = null, string name = null) where T : UI_Base
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        GameObject go = Managers.Resource.Instantiate($"UI/SubItem/{name}");
        if (parent != null)
            go.transform.SetParent(parent);

        return Util.GetOrAddComponent<T>(go);
    }
    public T ShowSceneUI<T>(string name = null) where T : UI_Scene
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        GameObject go = Managers.Resource.Instantiate($"UI/Scene/{name}");
        T sceneUI = Util.GetOrAddComponent<T>(go);

        _sceneUI = sceneUI;

        go.transform.SetParent(Root.transform);

        return sceneUI;
    }
    public T ShowPopupUI<T>(string name = null) where T : UI_Popup
    {
        if (string.IsNullOrEmpty(name))
            name = typeof(T).Name;

        GameObject go = Managers.Resource.Instantiate($"UI/Popup/{name}");
        T popup = Util.GetOrAddComponent<T>(go);
        _popupStack.Push(popup);

        go.transform.SetParent(Root.transform);

        return popup;
    }
    public void ClosePopupUI(UI_Popup popup)
    {
        if (_popupStack.Count == 0)
            return;

        // 1. 만약 닫으려는 팝업이 최상단(Peek)이 아니라면?
        if (_popupStack.Peek() != popup)
        {
            Debug.Log("최상단 팝업이 아니어서 스택을 재배치하고 강제 삭제합니다.");

            // Stack을 임시 리스트로 변환 (ToList를 하면 최상단 요소가 0번 인덱스에 옵니다)
            List<UI_Popup> tempList = System.Linq.Enumerable.ToList(_popupStack);

            // 리스트에서 해당 팝업을 찾아서 지움
            if (tempList.Contains(popup))
            {
                tempList.Remove(popup);

                // 기존 스택을 싹 비우고
                _popupStack.Clear();

                // 리스트를 역순으로 뒤집은 뒤 다시 스택에 쌓아줌 (원래 순서 복구)
                tempList.Reverse();
                foreach (UI_Popup p in tempList)
                {
                    _popupStack.Push(p);
                }

                // 팝업 오브젝트 파괴
                Managers.Resource.Destroy(popup.gameObject);
            }
            return;
        }

        // 2. 최상단 팝업이 맞다면 기존 로직대로 닫기
        ClosePopupUI();
    }
    public void ClosePopupUI()
    {
        if (_popupStack.Count == 0)
            return;

        UI_Popup popup = _popupStack.Pop();

        Managers.Resource.Destroy(popup.gameObject);
        popup = null;
        _order--;
    }
    public void CloseAllPopupUI()
    {
        while (_popupStack.Count > 0)
            ClosePopupUI();
    }

}
