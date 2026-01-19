using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static SceneTransitonManager;

public class FadeTransition : MonoBehaviour
{
    //アタッチしてるマテリアルを参照する
    [SerializeField]private Material _fadeTransitionMT;
    //↑にアタッチしてるシェーダーのfloatを取ってくる
    private readonly int _transitionbar = Shader.PropertyToID("_Transition");
    //_fadeTransitionMTにアタッチしているテクスチャをとってくる
    private readonly int _texture = Shader.PropertyToID("_Shape");
    //フェードのスピード
    [SerializeField] float _transitionTime = 1f;
    //ランダムで影の形を変えるため使うテクスチャを保存しておく
    [SerializeField] List<Texture2D> _fadeTexture = new List<Texture2D>();
    //　移動先のシーン名
    private string _moveSceneName;

    [SerializeField]
    private string[] _sceneNameList = { "Main_TitleScene",                  // タイトル
                                        "Main_PlayerJoin",                  // プレイヤー参加
                                        "Main_MiniGameExplanationScene",     // ミニゲーム説明
                                        "Main_MiniGameResultScene",         // ミニゲームのリザルト
                                        "Main_LastResultSceme",             // 最終リザルト
                                        "Main_Janken",                      // じゃんけん
                                        "Main_PressBattle",                 // プレス
                                        "Main_FallFloor"                   // フォール
                                       };

    public enum MoveSeneName
    {
        Title,                  // タイトル画面
        PlayerJoin,             // キャラ選択のシーン
        MiniGameExplanation,    // ミニゲームの説明シーン
        MiniGameResult,         // ミニゲームのリザルト
        TotalResult,            // 最後のリザルト
        Janken,                 // じゃんけんのテストシーン
        Press,　                // プレスバトルのテストシーン
        FallFloor,              // フォールフロアのテストシーン
    }

    private void Start()
    {
        //シーンをロードした時に呼び出す
        SceneManager.sceneLoaded += OnSceneLoaded;
        //シーンが変わっても保持する
        DontDestroyOnLoad(gameObject);
        //FadeIn();
    }
    //テスト用
    [Button]
    public void TitleFade()
    {
        FadeIn(FadeTransition.MoveSeneName.Title);
    }

    /// <summary>
    /// 別のscript等から呼び出す
    /// フェードイン
    /// </summary>
    [Button]
    public void FadeIn(MoveSeneName moveSceneType)
    {
        switch (moveSceneType)
        {
            case MoveSeneName.Title:
                _moveSceneName = _sceneNameList[0];
                break;
            case MoveSeneName.PlayerJoin:
                _moveSceneName = _sceneNameList[1];
                break;
            case MoveSeneName.MiniGameResult:
                _moveSceneName = _sceneNameList[2];
                break;
            case MoveSeneName.TotalResult:
                _moveSceneName = _sceneNameList[3];
                break;
            case MoveSeneName.Janken:
                _moveSceneName = _sceneNameList[4];
                break;
            case MoveSeneName.Press:
                _moveSceneName = _sceneNameList[5];
                break;
            case MoveSeneName.FallFloor:
                _moveSceneName = _sceneNameList[6];
                break;
            case MoveSeneName.MiniGameExplanation:
                _moveSceneName = _sceneNameList[7];
                break;
        }
        //格納しているテクスチャからランダムに選ぶ
        var rndTex = Random.Range(0, _fadeTexture.Count);
        //↑で選んだテクスチャをセットする
        _fadeTransitionMT.SetTexture(_texture, _fadeTexture[rndTex]);
        //コルーチンでフェードインを呼び出す
        StartCoroutine(TransitionFadeIn(_moveSceneName));
    }
    /// <summary>
    /// シーンが変わったときに呼び出される
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="mode"></param>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //await UniTask.WaitForSeconds(1);
        Debug.Log("シーン変わった");
        //コルーチンでフェードアウトを呼び出す
        StartCoroutine(TransitionFadeOut());
    }
    /// <summary>
    /// フェードアウト
    /// </summary>
    /// <returns></returns>
    IEnumerator TransitionFadeOut()
    {
        //スライドバーの値を0にする
        _fadeTransitionMT.SetFloat(_transitionbar, 0f);
        float t = 0;
        while (t < _transitionTime)
        {
            float progres = t / _transitionTime;
            //大きいときは遅くて小さいときは速くなる
            //y=1-(x*x*x*x)のグラフ参考
            float fadevalue = 1 - (1 - progres) * (1 - progres) * (1 - progres) * (1 - progres);
            //スライドバーに現在のfadevalueを代入
            _fadeTransitionMT.SetFloat(_transitionbar, fadevalue);
            yield return null;
            t += Time.deltaTime;
        }
        //スライドバーの値を1にする
        _fadeTransitionMT.SetFloat(_transitionbar, 1f);
    }

    IEnumerator TransitionFadeIn(string moveSceneName)
    {
        _fadeTransitionMT.SetFloat(_transitionbar, 1f);

        float t = 0;
        while (t < _transitionTime)
        {
            float progres = 1 - t / _transitionTime;
            //大きいときは遅くて小さいときは速くなる
            //y=1-(x*x*x*x)のグラフ参考
            //tにマイナスを掛けているので逆になる
            float fadevalue = 1 - (1 - progres) * (1 - progres) * (1 - progres) * (1 - progres);
            //スライドバーに現在のfadevalueを代入
            _fadeTransitionMT.SetFloat(_transitionbar, fadevalue);
            yield return null;
            t += Time.deltaTime;
        }
        //スライドバーの値を0にする
        _fadeTransitionMT.SetFloat(_transitionbar, 0f);
        SceneManager.LoadScene(moveSceneName);
    }
}
