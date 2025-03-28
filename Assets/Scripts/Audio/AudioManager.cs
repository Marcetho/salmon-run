using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Linq;

public class AudioManager : MonoBehaviour
{
    [SerializeField] List<SfxData> sfxList;
    [SerializeField] AudioSource musicPlayer;
    [SerializeField] AudioSource sfxPlayer;
    [SerializeField] float fadeDuration = 1f;
    float originalMusicVolume;
    Dictionary<SfxId, SfxData> sfxLookup;

    public static AudioManager i {get; private set;}
    private void Awake()
    {
        i = this;
    }

    private void Start()
    {
        originalMusicVolume = musicPlayer.volume;
        sfxLookup = sfxList.ToDictionary(x => x.id);
    }

    public IEnumerator UnpauseMusic(float delay)
    {
        yield return new WaitForSeconds(delay);
        musicPlayer.volume = 0;
        musicPlayer.UnPause();
        musicPlayer.DOFade(0.1f, fadeDuration);
    }

    public void ResumeMusic()
    {
        musicPlayer.UnPause();
    }

    public void FreezeMusic()
    {
        musicPlayer.Pause();
    }

    public void PlaySfx(AudioClip clip, bool pauseMusic = false)
    {
        if (clip == null)
            return;
        if (pauseMusic)
        {
            musicPlayer.Pause();
            StartCoroutine(UnpauseMusic(clip.length));
        }
        sfxPlayer.PlayOneShot(clip);
    }

    public void PlaySfx(SfxId sfxId, bool pauseMusic = false, bool priority = false)
    {
        if (!sfxLookup.ContainsKey(sfxId))
            return;
        if (priority)
            sfxPlayer.Stop();
        var audioData = sfxLookup[sfxId];
        PlaySfx(audioData.clip, pauseMusic);
    }
    public void PlayMusic(AudioClip clip, bool loop = true, bool fade = false)
    {
        if (clip == null)
            return;

        StartCoroutine(PlayMusicAsync(clip, loop, fade));
    }

    IEnumerator PlayMusicAsync(AudioClip clip, bool loop, bool fade)
    {
        if (fade)
           yield return musicPlayer.DOFade(0, fadeDuration).WaitForCompletion();

        musicPlayer.clip = clip;
        musicPlayer.loop = loop;
        musicPlayer.Play();

        if (fade)
           yield return musicPlayer.DOFade(originalMusicVolume, fadeDuration).WaitForCompletion();
    }
}

public enum SfxId
{
    BearAngry, BearAlert, SealAngry, SealAlert, FishFlop
}

[System.Serializable]
public class SfxData
{
    public SfxId id;
    public AudioClip clip;

}
