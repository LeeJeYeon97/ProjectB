using UnityEngine;

/// <summary>PlayerPrefs 읽기와 쓰기를 한 곳에서 제공하는 공용 저장 매니저입니다.</summary>
public class PlayerPrefsManager
{
    /// <summary>정수 값을 저장하고 변경 내용을 디스크에 기록합니다.</summary>
    public void SetInt(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
    }

    /// <summary>정수 값을 읽고 키가 없으면 지정한 기본값을 돌려줍니다.</summary>
    public int GetInt(string key, int defaultValue = 0)
    {
        return PlayerPrefs.GetInt(key, defaultValue);
    }

    /// <summary>실수 값을 저장하고 변경 내용을 디스크에 기록합니다.</summary>
    public void SetFloat(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
    }

    /// <summary>실수 값을 읽고 키가 없으면 지정한 기본값을 돌려줍니다.</summary>
    public float GetFloat(string key, float defaultValue = 0f)
    {
        return PlayerPrefs.GetFloat(key, defaultValue);
    }

    /// <summary>문자열 값을 저장하고 변경 내용을 디스크에 기록합니다.</summary>
    public void SetString(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
    }

    /// <summary>문자열 값을 읽고 키가 없으면 지정한 기본값을 돌려줍니다.</summary>
    public string GetString(string key, string defaultValue = "")
    {
        return PlayerPrefs.GetString(key, defaultValue);
    }

    /// <summary>자료를 JSON 문자열로 바꾸어 저장합니다.</summary>
    public void SetJson<T>(string key, T data)
    {
        if (data == null)
        {
            Debug.LogWarning($"저장할 자료가 비어 있습니다. 키: {key}");
            return;
        }

        SetString(key, JsonUtility.ToJson(data));
        Save();
    }

    /// <summary>JSON 문자열을 자료로 바꾸며, 키가 없거나 읽기에 실패하면 기본값을 돌려줍니다.</summary>
    public T GetJson<T>(string key, T defaultValue = default)
    {
        if (!HasKey(key))
            return defaultValue;

        try
        {
            string json = GetString(key);
            if (string.IsNullOrEmpty(json))
                return defaultValue;

            T data = JsonUtility.FromJson<T>(json);
            return data == null ? defaultValue : data;
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"저장 자료를 읽지 못했습니다. 키: {key}, 이유: {exception.Message}");
            return defaultValue;
        }
    }

    /// <summary>지정한 키가 저장되어 있는지 확인합니다.</summary>
    public bool HasKey(string key)
    {
        return PlayerPrefs.HasKey(key);
    }

    /// <summary>지정한 키의 저장 값을 삭제합니다.</summary>
    public void DeleteKey(string key)
    {
        PlayerPrefs.DeleteKey(key);
        Save();
    }

    /// <summary>모든 PlayerPrefs 변경 내용을 디스크에 기록합니다.</summary>
    public void Save()
    {
        PlayerPrefs.Save();
    }
}
