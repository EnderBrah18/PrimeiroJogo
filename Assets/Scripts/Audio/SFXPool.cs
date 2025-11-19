using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ebac.Core.Singleton;
using UnityEngine.Audio;

public class SFXPool : Singleton<SFXPool>
{
    private List<AudioSource> _audioSourceList;

    public int poolSize = 10;

    private int _index = 0;

    public AudioMixer audioMixer;

    public string VolumeSFX = "SFX_Volume";


    private void Start()
    {
        CreatePool();
    }

    private void CreatePool()
    {
        _audioSourceList = new List<AudioSource>();

        for (int i = 0; i < poolSize; i++)
        {
            CreateAudioSourceItem();
        }
    }

    private void CreateAudioSourceItem()
    {
        GameObject go = new GameObject("SFX_Pool");
        go.transform.SetParent(gameObject.transform);
        AudioSource audioSource = go.AddComponent<AudioSource>();

        audioSource.outputAudioMixerGroup = audioMixer.FindMatchingGroups("SFX")[0];

        _audioSourceList.Add(audioSource);
    }

    public void Play(SFXType sfxType)   
    {
        if (sfxType == SFXType.NONE) return;
        var sfx = SoundManager.Instance.GetSFXByType(sfxType);


        _audioSourceList[_index].clip = sfx.audioClip;
        _audioSourceList[_index].Play();

        _index++;
        if(_index >= _audioSourceList.Count) _index = 0;
    }
}
