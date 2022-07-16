using UnityEngine;

namespace TSUtils.Sounds
{
    public class PlaySoundOnStart : MonoBehaviour
    {
        [SerializeField] private SoundAsset Sound;

        private void Start()
        {
            SoundManager.Instance.Play(Sound);
        }
    }
}