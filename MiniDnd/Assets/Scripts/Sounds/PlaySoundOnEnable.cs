using UnityEngine;

namespace TSUtils.Sounds
{
    public class PlaySoundOnEnable : MonoBehaviour
    {
        [SerializeField] private SoundAsset Sound;

        private void OnEnable()
        {
            SoundManager.Instance.Play(Sound);
        }
    }
}