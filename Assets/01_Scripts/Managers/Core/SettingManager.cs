using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class SettingManager
{
    // --- [저장 데이터 키값] ---
    private const string Key_BGM = "Setting_BGM";
    private const string Key_SFX = "Setting_SFX";
    private const string Key_Vibrate = "Setting_Vibrate";
    private const string Key_Lang = "Setting_Lang";

    // --- [현재 설정 상태] ---
    public bool IsBGMOn { get; private set; }
    public bool IsSFXOn { get; private set; }
    public bool IsVibrationOn { get; private set; }
    public string Language { get; private set; } // "KR", "EN" 등

    public void Init()
    {
        // 1. 데이터 로드 (저장된 게 없으면 기본값은 True/KR)
        IsBGMOn = PlayerPrefs.GetInt(Key_BGM, 1) == 1;
        IsSFXOn = PlayerPrefs.GetInt(Key_SFX, 1) == 1;
        IsVibrationOn = PlayerPrefs.GetInt(Key_Vibrate, 1) == 1;
        Language = PlayerPrefs.GetString(Key_Lang, "KR");

        // 2. 초기 로드 시 실제 시스템에 적용
        ApplyAllSettings();
    }

    // --- [설정 변경 함수들] ---

    public void ToggleBGM(bool isOn)
    {
        IsBGMOn = isOn;
        PlayerPrefs.SetInt(Key_BGM, isOn ? 1 : 0);
        Managers.Sound.SetBGMVolume(isOn ? 1.0f : 0.0f); // 사운드 매니저와 연동
    }

    public void ToggleSFX(bool isOn)
    {
        IsSFXOn = isOn;
        PlayerPrefs.SetInt(Key_SFX, isOn ? 1 : 0);
        Managers.Sound.SetSFXVolume(isOn ? 1.0f : 0.0f);// 효과음 매니저 연동 (볼륨 조절 혹은 뮤트 처리)
    }

    public void ToggleVibration(bool isOn)
    {
        IsVibrationOn = isOn;
        PlayerPrefs.SetInt(Key_Vibrate, isOn ? 1 : 0);

        // 설정 켰을 때 짧게 한 번 진동 (테스트용)
        if (isOn) Vibrate();
    }
    public void Vibrate()
    {
        // 설정이 꺼져있으면 무시
        if (!IsVibrationOn) return;

#if UNITY_ANDROID || UNITY_IOS
        // 유니티 기본 진동 (약 0.5초)
        Handheld.Vibrate();
#endif
    }
    public void SetLanguage(string langCode)
    {
        // langCode 예: "ko", "en", "ja"
        Language = langCode;
        PlayerPrefs.SetString(Key_Lang, langCode);

        // Localization 패키지 실제 적용
        Locale targetLocale = LocalizationSettings.AvailableLocales.GetLocale(langCode);
        if (targetLocale != null)
        {
            LocalizationSettings.SelectedLocale = targetLocale;
            Debug.Log($"[Setting] 언어 변경 완료: {langCode}");
        }
        else
        {
            Debug.LogWarning($"[Setting] 해당 언어 코드를 찾을 수 없습니다: {langCode}");
        }
    }

    private void ApplyAllSettings()
    {
        // 사운드 매니저 등에 현재 값을 한 번에 적용
        Managers.Sound.SetBGMVolume(IsBGMOn ? 1.0f : 0.0f);
        Managers.Sound.SetSFXVolume(IsSFXOn ? 1.0f : 0.0f);// 효과음 매니저 연동 (볼륨 조절 혹은 뮤트 처리)

        SetLanguage(Language);
    }
}
