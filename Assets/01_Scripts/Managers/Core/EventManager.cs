using System;
using System.Collections.Generic;
using UnityEngine;

public class EventManager
{
    // Dictionary를 이용해 각 이벤트 타입마다 Action들을 관리
    private Dictionary<Define.ActionEvent, Delegate> _events = new Dictionary<Define.ActionEvent, Delegate>();

    public void Subscribe<T>(Define.ActionEvent eventType, Action<T> listener)
    {
        if (_events.ContainsKey(eventType))
        {
            // 기존에 해당 이벤트가 있으면 기존 델리게이트에 추가
            _events[eventType] = (Action<T>)_events[eventType] + listener;
        }
        else
        {
            // 처음 등록되는 이벤트라면 새로 추가
            _events[eventType] = listener;
        }
    }

    public void UnSubscribe<T>(Define.ActionEvent eventType, Action<T> listener)
    {
        if (_events.ContainsKey(eventType))
        {
            var currentDelegate = (Action<T>)_events[eventType] - listener;

            if (currentDelegate == null)
                _events.Remove(eventType);
            else
                _events[eventType] = currentDelegate;
        }
    }

    public void PostEvent<T>(Define.ActionEvent eventType, T data)
    {
        if (_events.TryGetValue(eventType, out Delegate del))
        {
            // 저장된 Delegate를 Action<T>로 캐스팅하여 실행
            // 이 과정에서 박싱이 일어나지 않습니다!
            Action<T> action = del as Action<T>;
            action?.Invoke(data);
        }
    }
    // 매개변수가 없는 이벤트를 위한 PostEvent
    public void PostEvent(Define.ActionEvent eventType)
    {
        if (_events.TryGetValue(eventType, out Delegate del))
        {
            // Action<T>가 아닌 Action으로 캐스팅
            Action action = del as Action;
            action?.Invoke();
        }
    }

    // 매개변수가 없는 이벤트를 위한 Subscribe
    public void Subscribe(Define.ActionEvent eventType, Action listener)
    {
        if (_events.ContainsKey(eventType))
            _events[eventType] = (Action)_events[eventType] + listener;
        else
            _events[eventType] = listener;
    }

    // 매개변수가 없는 이벤트를 위한 Unsubscribe
    public void UnSubscribe(Define.ActionEvent eventType, Action listener)
    {
        if (_events.ContainsKey(eventType))
        {
            var currentDelegate = (Action)_events[eventType] - listener;
            if (currentDelegate == null) _events.Remove(eventType);
            else _events[eventType] = currentDelegate;
        }
    }

    
}