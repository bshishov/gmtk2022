using UnityEngine;
using UnityEngine.Audio;

namespace TSUtils.Sounds
{
    public interface ISound
    {
        AudioClip GetAudioClip();
        float GetPitch();
        float GetDelay();
        AudioMixerGroup GetMixedGroup();
        bool IsOnLoop();
        float GetVolumeModifier();
        bool ShouldIgnoreListenerPause();
        ISoundGroup GetGroup();
    }
}