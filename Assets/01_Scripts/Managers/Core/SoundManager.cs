using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class SoundManager
{
    // ���� ��� ���� ����� �ҽ���
    private AudioSource[] _audioSources = new AudioSource[(int)Sound.MaxCount];

    // String ��� ��� Enum(SoundID)�� Ű������ ����մϴ�!
    private Dictionary<SoundID, AudioClip> _audioClips = new Dictionary<SoundID, AudioClip>();

    // [�߰�] ������ ���� ��� (0.0 ~ 1.0)
    private float _bgmVolumeMultiplier = 1.0f;
    private float _sfxVolumeMultiplier = 1.0f;

    // [�߰�] �� ���庰 ������ ��� �ð��� ����ϴ� ��ųʸ�
    private Dictionary<Define.SoundID, float> _lastPlayTimes = new Dictionary<Define.SoundID, float>();

    // [�߰�] ���� �Ҹ��� �ߺ� ����Ǵ� ���� ���� ��Ÿ�� (0.05�� ~ 0.1�� ����)
    //private float _sfxCooldown = 0.05f;

    public void Init()
    {
        // 1. AudioSource �⺻ ����
        GameObject root = GameObject.Find("@Sound");
        if (root == null)
        {
            root = new GameObject { name = "@Sound" };
            if (Application.isPlaying)
            {
                Object.DontDestroyOnLoad(root);
            }

            string[] soundNames = System.Enum.GetNames(typeof(Sound));
            for (int i = 0; i < soundNames.Length - 1; i++)
            {
                GameObject go = new GameObject { name = soundNames[i] };
                _audioSources[i] = go.AddComponent<AudioSource>();
                go.transform.parent = root.transform;
            }
            _audioSources[(int)Sound.Bgm].loop = true;
        }

        // 2. SO ������ �ε� �� ��ųʸ� ĳ�� (�ٽ�!)
        // (Managers.Data �ʿ� SO�� �����μ̴ٸ� �װ� �������ŵ� �˴ϴ�.)
        //SoundDataSO data = Managers.Data.SoundData;
        //if (data != null)
        //{
        //    foreach (var info in data.soundList)
        //    {
        //        if (!_audioClips.ContainsKey(info.id))
        //        {
        //            _audioClips.Add(info.id, info.clip);
        //            _soundInfo.Add(info.id, info); // ���� ������ ���� ���� ���� ����
        //        }
        //    }
        //}
        //else
        //{
        //    Debug.LogError("SoundData ScriptableObject�� ã�� �� �����ϴ�!");
        //}
    }

    // ==========================================================
    // [�߰� 1] AudioClip�� ���� �޴� Play �Լ�
    // (ScriptableObject�� �ν����Ϳ��� ���� �Ҵ��� Ŭ���� ����� �� ���)
    // ==========================================================
    public void Play(AudioClip clip, Sound type = Sound.Sfx, float pitch = 1.0f, float baseVolume = 1.0f)
    {
        if (clip == null) return;

        if (type == Sound.Bgm)
        {
            float finalVolume = baseVolume * _bgmVolumeMultiplier;
            AudioSource audioSource = _audioSources[(int)Sound.Bgm];

            // 1. ���� ���� Ʋ���� ����� �̹� ��� ���� ��ݰ� �Ȱ��ٸ� ����!
            if (audioSource.isPlaying && audioSource.clip == clip)
                return;

            // 2. ������ ���ļ� ���� ���̴� ���� �ִϸ��̼��� �ִٸ� ���� ����
            audioSource.DOKill();

            // 3. �ε巯�� ��ȯ ���� (Fade Out -> Change -> Fade In)
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
    // [�߰� 2] String(���)�� ���� �޴� Play �Լ�
    // ("SFX/Explosion" ó�� ���ҽ� ���� ��θ� ���� ����� �� ���)
    // ==========================================================
    public void Play(string path, Sound type = Sound.Sfx, float pitch = 1.0f, float baseVolume = 1.0f)
    {
        if (string.IsNullOrEmpty(path)) return;

        // Managers.Resource�� ���� AudioClip�� �ε��ؿɴϴ�. 
        // (����: Resource �δ��� ĳ�� ����� ���ٸ�, ���� ���̴� ȿ������ �Ź� Load�Ǿ� ���ſ� �� �ֽ��ϴ�.)
        AudioClip clip = Managers.Resource.Load<AudioClip>(path);

        if (clip != null)
        {
            // �ε忡 �����ߴٸ� ���� ������ AudioClip ���� Play �Լ��� �Ѱ��ݴϴ�.
            Play(clip, type, pitch, baseVolume);
        }
        else
        {
            Debug.LogWarning($"���� ������ ã�� �� �����ϴ�: {path}");
        }
    }

    public void Play(SoundID id, Sound type = Sound.Sfx, float pitch = 1.0f)
    {
        //if (id == SoundID.None) return;
        //
        //// [�߰�] ��Ÿ�� ��: SFX�� ��쿡�� üũ�Ͽ� �ߺ� ���(����)�� �����ϴ�.
        //if (type == Sound.Sfx)
        //{
        //    if (_lastPlayTimes.TryGetValue(id, out float lastTime))
        //    {
        //        // ������ ��� �ð����κ��� ��Ÿ��(0.05��)�� ������ �ʾҴٸ� ����!
        //        if (Time.time - lastTime < _sfxCooldown)
        //        {
        //            return;
        //        }
        //    }
        //    // ��Ÿ�� ��� �� ���� �ð����� ����
        //    _lastPlayTimes[id] = Time.time;
        //}
        //
        //if (_audioClips.TryGetValue(id, out AudioClip clip))
        //{
        //    // ��ųʸ����� Ŭ���� �⺻ ������ ã�� ��, AudioClip ���� Play �Լ��� �佺!
        //    float baseVolume = _soundInfo[id].volume;
        //    Play(clip, type, pitch, baseVolume);
        //}
        //else
        //{
        //    Debug.LogWarning($"���� ID�� ã�� �� �����ϴ�: {id}");
        //}
    }

    // ����� ���� ���� (SettingManager���� ȣ��)
    public void SetBGMVolume(float volume)
    {
        _bgmVolumeMultiplier = volume;
        if (_audioSources == null || _audioSources.Length <= (int)Sound.Bgm) return;
        AudioSource bgmSource = _audioSources[(int)Sound.Bgm];
        if (bgmSource == null) return;

        // ���� ������� ��� ���̶�� ��� ���� �ݿ�
        if (bgmSource.isPlaying && bgmSource.clip != null)
        {
            // ���� Ŭ���� �⺻ ���� ������ �ִ��� Ȯ�� �� ����
            // (SoundID�� �˱� ��ƴٸ� ���� �ҽ� ������ ���� �����ϰų�, 
            // ������ ��� ������ �����صξ��ٰ� ����ϴ� ���� �����ϴ�.)
            bgmSource.DOKill(); // ���� ���̵� �ִϸ��̼� ����
            bgmSource.volume = GetFinalBGMVolume(bgmSource.clip);
        }
    }

    // ȿ���� ���� ���� (SettingManager���� ȣ��)
    public void SetSFXVolume(float volume)
    {
        _sfxVolumeMultiplier = volume;
    }

    // Ŭ���� ���� ������ ������ ������ ���� ���� ���� ����
    private float GetFinalBGMVolume(AudioClip clip)
    {
        //// ���� ��� ���� Ŭ���� ������ ��ųʸ����� �������ϰų� 
        //// ������ ��� �� ����� ���� ���� ����մϴ�.
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
        // �� ��ȯ �� SO �����ʹ� ��� �� �Ŷ�� _audioClips.Clear()�� �� �ϼŵ� �˴ϴ�.
    }
}