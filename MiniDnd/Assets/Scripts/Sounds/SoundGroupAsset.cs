using UnityEngine;

namespace TSUtils.Sounds
{
    [CreateAssetMenu(fileName = "Sound Group", menuName = "Audio/Sound Group")]
    public class SoundGroupAsset : ScriptableObject, ISoundGroup
    {
        public int MaxSoundsInGroup;
        
        public int GetMaxConcurrentSounds()
        {
            return MaxSoundsInGroup;
        }

        public string GetId()
        {
            return name;
        }
    }
}