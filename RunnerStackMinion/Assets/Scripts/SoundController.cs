using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SoundType : int
{
    Spawn = 0,
    Despawn,
    Finish
}

public interface ISoundController : IService
{
    void PlaySound(SoundType soundType);
}

public class SoundController : MonoBehaviour, ISoundController
{
    [SerializeField] AudioSource AudioSource;
    [SerializeField] AudioClip[] AudioClips;

    void Awake()
    {
        ServiceLocator.Instance.RegisterService(this);
    }

    public void PlaySound(SoundType soundType)
    {
        AudioSource.clip = AudioClips[(int)soundType];
        AudioSource.Play();
    }
}
