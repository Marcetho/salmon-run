using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AudioManager : MonoBehaviour
{
    [SerializeField] List<SfxData> sfxList;
    [SerializeField] AudioSource musicPlayer;
    [SerializeField] AudioSource sfxPlayer;
    [SerializeField] float fadeDuration = 1f;
    Dictionary<SfxId, SfxData> sfxLookup;

    public static AudioManager i {get; private set;}
    private void Awake()
    {
        i = this;
    }

    private void Start()
    {
        sfxLookup = sfxList.ToDictionary(x => x.id);
    }

    public IEnumerator UnpauseMusic(float delay)
    {
        yield return new WaitForSeconds(delay);
        musicPlayer.UnPause();
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
        if (sfxId == SfxId.FishFlopping)
        {
            int flop = Random.Range(1, 5);
            switch (flop)
            {
                case 1:
                    sfxId = SfxId.FishFlop1;
                    break;
                case 2:
                    sfxId = SfxId.FishFlop2;
                    break;
                case 3:
                    sfxId = SfxId.FishFlop3;
                    break;
                default:
                    sfxId = SfxId.FishFlop4;
                    break;
            }
        }
        if (!sfxLookup.ContainsKey(sfxId))
            return;
        if (priority)
            sfxPlayer.Stop();
        var audioData = sfxLookup[sfxId];
        PlaySfx(audioData.clip, pauseMusic);
    }
    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null)
            return;

        musicPlayer.clip = clip;
        musicPlayer.loop = loop;
        musicPlayer.Play();
    }
}

public enum SfxId
{
    Splash, Crunch, FishFlopping, FishFlop1, FishFlop2, FishFlop3, FishFlop4
}

[System.Serializable]
public class SfxData
{
    public SfxId id;
    public AudioClip clip;

}
