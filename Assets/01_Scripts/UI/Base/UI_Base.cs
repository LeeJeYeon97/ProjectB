using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public abstract class UI_Base : MonoBehaviour
{
    protected Dictionary<Type, UnityEngine.Object[]> _objects = new Dictionary<Type, UnityEngine.Object[]>();

    // 추가: 초기화 여부를 기억하는 변수
    protected bool _init = false;
    public RenderMode canvasRenderMode = RenderMode.ScreenSpaceOverlay;

    public virtual void Init()
    {
        if (_init)
            return; // 이미 초기화가 끝났다면 그냥 돌아감

        // 1. 내 자식들 중에 Button 컴포넌트가 있는 걸 싹 다 찾습니다. (비활성화된 것 포함 true)
        Button[] buttons = GetComponentsInChildren<Button>(true);

        foreach (Button btn in buttons)
        {
            // 핵심 방어막: 내 위로 가장 가까운 UI_Base를 찾습니다.
            // 그게 '나 자신(this)'이 아니라면, 내 자식 UI의 버튼이므로 건너뜁니다!
            UI_Base owner = btn.GetComponentInParent<UI_Base>(true);
            if (owner != this)
                continue;

            btn.onClick.AddListener(() => Managers.Sound.Play(Define.SoundID.Sfx_UIButtonClick));
        }
        _init = true; // 이제 초기화 완료!
    }

    public virtual void Clear()
    {
        // 기본적으로는 아무것도 안 함
    }
    private void Awake()
    {
        Init();
    }
    private void OnDestroy()
    {
        Clear();
    }

    protected void Bind<T>(Type type) where T : UnityEngine.Object
    {
        string[] names = Enum.GetNames(type);
        UnityEngine.Object[] objects = new UnityEngine.Object[names.Length];
        _objects.Add(typeof(T), objects);

        for (int i = 0; i < names.Length; i++)
        {
            if (typeof(T) == typeof(GameObject))
                objects[i] = Util.FindChild(gameObject, names[i], true);
            else
                objects[i] = Util.FindChild<T>(gameObject, names[i], true);

            if (objects[i] == null)
                Debug.Log($"Failed to bind({names[i]})");
        }
    }

    protected T Get<T>(int idx) where T : UnityEngine.Object
    {
        UnityEngine.Object[] objects = null;
        if (_objects.TryGetValue(typeof(T), out objects) == false)
            return null;

        return objects[idx] as T;
    }

    protected GameObject GetObject(int idx) { return Get<GameObject>(idx); }
    protected Text GetText(int idx) { return Get<Text>(idx); }
    protected TMP_Text GetTMP(int idx) { return Get<TMP_Text>(idx); }
    protected Button GetButton(int idx) { return Get<Button>(idx); }
    protected Image GetImage(int idx) { return Get<Image>(idx); }
    protected Slider GetSlider(int idx) { return Get<Slider>(idx); }

    //public static void BindEvent(GameObject go, Action<PointerEventData> action, Define.UIEvent type = Define.UIEvent.Click)
    //{
    //    UI_EventHandler evt = Util.GetOrAddComponent<UI_EventHandler>(go);

    //    switch (type)
    //    {
    //        case Define.UIEvent.Click:
    //            evt.OnClickHandler -= action;
    //            evt.OnClickHandler += action;
    //            break;
    //        case Define.UIEvent.Drag:
    //            evt.OnDragHandler -= action;
    //            evt.OnDragHandler += action;
    //            break;
    //    }
    //}
}
