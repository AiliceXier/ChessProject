using UnityEngine;

namespace Chess.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Background Music")]
        public AudioClip bgmClip;
        [Range(0f, 1f)] public float bgmVolume = 0.3f;

        [Header("Sound Effects")]
        public AudioClip pieceLightTouchClip;
        public AudioClip pieceHeavyTouchClip;
        public AudioClip captureClip;
        public AudioClip checkClip;
        public AudioClip gameOverClip;
        [Range(0f, 1f)] public float sfxVolume = 0.7f;

        [Header("Game End")]
        public AudioClip winClip;
        public AudioClip loseClip;

        private AudioSource _bgmSource;
        private AudioSource _sfxSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;
            _bgmSource.volume = bgmVolume;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;
            _sfxSource.volume = sfxVolume;
        }

        public void PlayBGM()
        {
            if (bgmClip == null || _bgmSource == null) return;
            if (_bgmSource.isPlaying) return;
            _bgmSource.clip = bgmClip;
            _bgmSource.Play();
        }

        public void StopBGM()
        {
            if (_bgmSource != null && _bgmSource.isPlaying)
                _bgmSource.Stop();
        }

        public void PlayPieceSelect()
        {
            PlaySFX(pieceLightTouchClip);
        }

        public void PlayPieceMove()
        {
            PlaySFX(pieceHeavyTouchClip);
        }

        public void PlayCapture()
        {
            PlaySFX(captureClip != null ? captureClip : pieceHeavyTouchClip, 1.2f);
        }

        public void PlayCheck()
        {
            PlaySFX(checkClip);
        }

        public void PlayGameOver()
        {
            PlaySFX(gameOverClip);
        }

        public void PlayWin()
        {
            PlaySFX(winClip);
        }

        public void PlayLose()
        {
            PlaySFX(loseClip);
        }

        private void PlaySFX(AudioClip clip, float pitch = 1f)
        {
            if (clip == null || _sfxSource == null) return;
            _sfxSource.pitch = pitch;
            _sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }
}
