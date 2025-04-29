using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioChangeVolume : MonoBehaviour
{
    public AudioMixer group;
    public string floatParam = "MyExposedParam";

    public void ChangeValue(float f)
    {
        group.SetFloat(floatParam, f);
    }

    public void MuteVolume()
    {
        group.SetFloat(floatParam, -80f);
    }

    public void UnMuteVolume()
    {
        group.SetFloat(floatParam, 0f);
    }
}
