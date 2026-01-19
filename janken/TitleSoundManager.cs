using LitMotion;
using LitMotion.Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleSoundManager : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _bgmClip; // 流すBGM格納場所 
    [SerializeField] private float _bgmFadeDuration; //フェードにかかる時間
    // Start is called before the first frame update
    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    //BGMを再生する
    private async void BgmStart()
    {
        _audioSource.clip = _bgmClip; // 再生したいclipを指定する
        _audioSource.Play(); // BGM再生
        await LMotion.Create(0f, 1.0f, _bgmFadeDuration).WithEase(Ease.Linear).BindToVolume(_audioSource).AddTo(gameObject);　//BGMがフェードをかけて大きくなる
    }
    //BGMを止める
    private async void BgmStop()
    {
        await LMotion.Create(1.0f, 0f, _bgmFadeDuration).WithEase(Ease.Linear).BindToVolume(_audioSource).AddTo(gameObject); //BGMがフェードをかけて小さくなる
        _audioSource.Stop();
    }

    /*public void OnBgmStartButton()　 //ボタン確認用
    {
        BgmStart();
    }

    public void OnBgmStopButton() //ボタン確認用
    {
        BgmStop();
    }*/
}
