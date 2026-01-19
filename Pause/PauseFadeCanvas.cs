using LitMotion;
using LitMotion.Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseFadeCanvas : MonoBehaviour
{
    private Image _fadeImage;
    // Start is called before the first frame update
    void Start()
    {
        //透明度をいじるためにImageを取る
        _fadeImage =  GetComponent<Image>();
        FadeOut();
    }


    /// <summary>
    /// 画面が暗くなってタイトルシーンに遷移する
    /// </summary>
    private async void FadeOut()
    {
        //フェードアウト
        await LMotion.Create(0f, 1f, 0.2f).WithEase(Ease.Linear).BindToColorA(_fadeImage).AddTo(gameObject);
        //タイトルシーンに移動する
        SceneManager.LoadScene("Main_TitleScene");
    }
}
