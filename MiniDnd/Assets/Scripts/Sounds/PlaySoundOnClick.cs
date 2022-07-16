using UnityEngine;
using UnityEngine.UI;

namespace TSUtils.Sounds
{
    [RequireComponent(typeof(Button))]
    public class PlaySoundOnClick : MonoBehaviour
    {
        [SerializeField] private SoundAsset Sound;
        private Button _button;

        private void Awake() => _button = GetComponent<Button>();

        private void OnEnable() => _button.onClick.AddListener(Play);

        private void OnDisable() => _button.onClick.RemoveListener(Play);

        private void Play() => SoundManager.Instance.Play(Sound);
    }
}