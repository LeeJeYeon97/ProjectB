using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class SoundManager
{
    // 사운드 재생 해줄 오디오 소스들
    private AudioSource[] _audioSources = new AudioSource[(int)Sound.MaxCount];

    // String 경로 대신 Enum(SoundID)을 키값으로 사용합니다!
    private Dictionary<SoundID, AudioClip> _audioClips = new Dictionary<SoundID, AudioClip>();

    // [추가] 마스터 볼륨 계수 (0.0 ~ 1.0)
    private float _bgmVolumeMultiplier = 1.0f;
    private float _sfxVolumeMultiplier = 1.0f;

    // [추가] 각 사운드별 마지막 재생 시간을 기억하는 딕셔너리
    private Dictionary<Define.SoundID, float> _lastPlayTimes = new Dictionary<Define.SoundID, float>();

    // [추가] 같은 소리가 중복 재생되는 것을 막는 쿨타임 (0.05초 ~ 0.1초 권장)
    //private float _sfxCooldown = 0.05f;

    public void Init()
    {
        // 1. AudioSource 기본 세팅
        GameObject root = GameObject.Find("@Sound");
        if (root == null)
        {
            root = new GameObject { name = "@Sound" };
            Object.DontDestroyOnLoad(root);

            string[] soundNames = System.Enum.GetNames(typeof(Sound));
            for (int i = 0; i < soundNames.Length - 1; i++)
            {
                GameObject go = new GameObject { name = soundNames[i] };
                _audioSources[i] = go.AddComponent<AudioSource>();
                go.transform.parent = root.transform;
            }
            _audioSources[(int)Sound.Bgm].loop = true;
        }

        // 2. SO 데이터 로드 및 딕셔너리 캐싱 (핵심!)
        // (Managers.Data 쪽에 SO를 물려두셨다면 그걸 가져오셔도 됩니다.)
        //SoundDataSO data = Managers.Data.SoundData;
        //if (data != null)
        //{
        //    foreach (var info in data.soundList)
        //    {
        //        if (!_audioClips.ContainsKey(info.id))
        //        {
        //            _audioClips.Add(info.id, info.clip);
        //            _soundInfo.Add(info.id, info); // 볼륨 데이터 등을 위해 같이 저장
        //        }
        //    }
        //}
        //else
        //{
        //    Debug.LogError("SoundData ScriptableObject를 찾을 수 없습니다!");
        //}
    }

    // ==========================================================
    // [추가 1] AudioClip을 직접 받는 Play 함수
    // (ScriptableObject나 인스펙터에서 직접 할당한 클립을 재생할 때 사용)
    // ==========================================================
    public void Play(AudioClip clip, Sound type = Sound.Sfx, float pitch = 1.0f, float baseVolume = 1.0f)
    {
        if (clip == null) return;

        if (type == Sound.Bgm)
        {
            float finalVolume = baseVolume * _bgmVolumeMultiplier;
            AudioSource audioSource = _audioSources[(int)Sound.Bgm];

            // 1. 만약 지금 틀려는 브금이 이미 재생 중인 브금과 똑같다면 무시!
            if (audioSource.isPlaying && audioSource.clip == clip)
                return;

            // 2. 이전에 겹쳐서 실행 중이던 볼륨 애니메이션이 있다면 강제 종료
            audioSource.DOKill();

            // 3. 부드러운 전환 로직 (Fade Out -> Change -> Fade In)
            if (audioSource.isPlaying)
            {
                audioSource.DOFade(0f, 0.5f).OnComplete(() =>
                {
                    audioSource.clip = clip;
                    audioSource.pitch = pitch;
                    audioSource.Play();
                    audioSource.DOFade(finalVolume, 0.5f);
                });
            }
            else
            {
                audioSource.volume = 0f;
                audioSource.clip = clip;
                audioSource.pitch = pitch;
                audioSource.Play();
                audioSource.DOFade(finalVolume, 1.0f);
            }
        }
        else // SFX
        {

            float finalVolume = baseVolume * _sfxVolumeMultiplier;
            AudioSource audioSource = _audioSources[(int)Sound.Sfx];

            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip, finalVolume);
        }
    }

    // ==========================================================
    // [추가 2] String(경로)을 직접 받는 Play 함수
    // ("SFX/Explosion" 처럼 리소스 폴더 경로를 통해 재생할 때 사용)
    // ==========================================================
    public void Play(string path, Sound type = Sound.Sfx, float pitch = 1.0f, float baseVolume = 1.0f)
    {
        if (string.IsNullOrEmpty(path)) return;

        // Managers.Resource를 통해 AudioClip을 로드해옵니다. 
        // (주의: Resource 로더의 캐싱 기능이 없다면, 자주 쓰이는 효과음은 매번 Load되어 무거울 수 있습니다.)
        AudioClip clip = Managers.Resource.Load<AudioClip>(path);

        if (clip != null)
        {
            // 로드에 성공했다면 위에 만들어둔 AudioClip 전용 Play 함수로 넘겨줍니다.
            Play(clip, type, pitch, baseVolume);
        }
        else
        {
            Debug.LogWarning($"사운드 파일을 찾을 수 없습니다: {path}");
        }
    }

    public void Play(SoundID id, Sound type = Sound.Sfx, float pitch = 1.0f)
    {
        //if (id == SoundID.None) return;
        //
        //// [추가] 쿨타임 방어막: SFX일 경우에만 체크하여 중복 재생(씹힘)을 막습니다.
        //if (type == Sound.Sfx)
        //{
        //    if (_lastPlayTimes.TryGetValue(id, out float lastTime))
        //    {
        //        // 마지막 재생 시간으로부터 쿨타임(0.05초)이 지나지 않았다면 무시!
        //        if (Time.time - lastTime < _sfxCooldown)
        //        {
        //            return;
        //        }
        //    }
        //    // 쿨타임 통과 시 현재 시간으로 갱신
        //    _lastPlayTimes[id] = Time.time;
        //}
        //
        //if (_audioClips.TryGetValue(id, out AudioClip clip))
        //{
        //    // 딕셔너리에서 클립과 기본 볼륨을 찾은 뒤, AudioClip 전용 Play 함수로 토스!
        //    float baseVolume = _soundInfo[id].volume;
        //    Play(clip, type, pitch, baseVolume);
        //}
        //else
        //{
        //    Debug.LogWarning($"사운드 ID를 찾을 수 없습니다: {id}");
        //}
    }

    // 배경음 볼륨 설정 (SettingManager에서 호출)
    public void SetBGMVolume(float volume)
    {
        _bgmVolumeMultiplier = volume;
        AudioSource bgmSource = _audioSources[(int)Sound.Bgm];

        // 현재 배경음이 재생 중이라면 즉시 볼륨 반영
        if (bgmSource.isPlaying && bgmSource.clip != null)
        {
            // 현재 클립의 기본 볼륨 정보가 있는지 확인 후 적용
            // (SoundID를 알기 어렵다면 현재 소스 볼륨을 직접 조절하거나, 
            // 마지막 재생 정보를 저장해두었다가 사용하는 것이 좋습니다.)
            bgmSource.DOKill(); // 기존 페이드 애니메이션 중지
            bgmSource.volume = GetFinalBGMVolume(bgmSource.clip);
        }
    }

    // 효과음 볼륨 설정 (SettingManager에서 호출)
    public void SetSFXVolume(float volume)
    {
        _sfxVolumeMultiplier = volume;
    }

    // 클립의 원래 볼륨과 마스터 볼륨을 곱한 최종 볼륨 계산기
    private float GetFinalBGMVolume(AudioClip clip)
    {
        //// 현재 재생 중인 클립의 정보를 딕셔너리에서 역추적하거나 
        //// 마지막 재생 시 저장된 볼륨 값을 사용합니다.
        //foreach (var info in _soundInfo.Values)
        //{
        //    if (info.clip == clip)
        //        return info.volume * _bgmVolumeMultiplier;
        //}
        return _bgmVolumeMultiplier;
    }

    public void Clear()
    {
        foreach (AudioSource audioSource in _audioSources)
        {
            audioSource.clip = null;
            audioSource.Stop();
        }
        // 씬 전환 시 SO 데이터는 계속 쓸 거라면 _audioClips.Clear()는 안 하셔도 됩니다.
    }
}