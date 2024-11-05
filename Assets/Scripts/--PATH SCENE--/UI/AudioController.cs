using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioController : MonoBehaviour {

    private AudioSource _audioSource;

    private void Start() {
        this._audioSource = GetComponent<AudioSource>();
    }

    public void PlaySound(AudioClip audioClip) {
        _audioSource.clip = audioClip;
        _audioSource.Play();
    }

    public void HideSound() {
        _audioSource.Stop();
    }

    public float GetProgressRatio() {
        return _audioSource.time / _audioSource.clip.length;
    }

    public void PauseResume(bool pause) {
        if(pause)
            _audioSource.Pause();
        else
            _audioSource.Play();
    }
}