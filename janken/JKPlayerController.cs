using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;
using TMPro;
using static JKSoundManager;

public class JKPlayerController : MonoBehaviour
{
    [SerializeField] private JankenManager  _jankenManager;
    [SerializeField] private GameObject     Plate;
    [SerializeField] private Material       _Pmat; // 解答台の○×
    [SerializeField] private Material       _Lmat; //ライトマテリアル
    [SerializeField] private Material       _Hmat;
    [SerializeField] private int            _HideHandPhase;
    [SerializeField] private ParticleSystem _particle; //正解した時に出すパーティクル
    private static readonly  int            BaseMap     = Shader.PropertyToID("_MainTex");
    private static readonly  int            MainTex2    = Shader.PropertyToID("_Main2ndTex");
    private static readonly  int            UseEmission = Shader.PropertyToID("_UseEmission");
    private                  Coroutine      _blinkCoroutine; // コルーチンを止めるためのハンドル
    
    //内部参照
    private PlayerInput playerInput; // PlayerInput参照
    //11/28変更:コールバック用
    private System.Func<int> _selectFuncCallback;

    // 入力アクション
    private InputAction RockAction;     // グー        四角     D
    private InputAction ScissorsAction; // チョキ      バツ     S
    private InputAction PaperAction;    // パー        丸       A
    private InputAction ResetAction;    //リセット              SPACE

    [SerializeField]public int Hand = 4;
    //11/28変更:表示する獲得ポイントのオブジェクトをまとめる場所
    // 下    記のプレハブを当てはめる（prefabs->Janken->Murayama内にあります）
    // _AddPointObject[0]->"Add100 1" , [1]->"Add80" , [2]->"Add60" , [3]->"Add50" , [4]->"mainas50"
    [SerializeField] private List<GameObject> _AddPointObject;

    [SerializeField] private int _changePoint; //毎ターンごとに獲得したポイントを保存する場所
    [SerializeField] private Transform _pointTransform; //毎ターンごとの獲得ポイントを表示する場所

    [Header("保持ポイント")]
    [SerializeField] public int _point = 0;
    [SerializeField] private TextMeshProUGUI _scoreText;
    [Header("加減ポイント")]
    [SerializeField] private int _bonusPoint = 50;
    [SerializeField] private int _AddPoint = 100;

    [Header("ボーナス関連")]
    [SerializeField, Header("このプレイヤーがグーを出した数")] private int _rockCount = 0;
    [SerializeField, Header("このプレイヤーがチョキを出した数")] private int _ScissorsCount = 0;
    [SerializeField, Header("このプレイヤーがパーを出した数")] private int _paperCount = 0;
    [SerializeField, Header("正誤かかわらず勝った数")] private int _WinnerCount = 0;
    [SerializeField, Header("正誤かかわらず負けた数")] private int _loserCount = 0;
    [SerializeField, Header("正誤かかわらず引き分けた数")] private int _drawCount = 0;

    // 11/20 木村が追加(マイナスポイント)
    [SerializeField]
    private int _minusPoint = 50;
    // 仮
    [SerializeField]
    private string _playerName;

    //11/28村山bool->intに変更
    //このプレイヤーが正解のとき何番目に手を出したか
    /// <summary>
    /// このプレイヤーが正解のとき何番目に手を出したかをカウントする
    /// </summary>
    public int _isFirstPlayerHit = 0;
    
    // アニメーションコントローラー
    private                 Animator JK_Animator;
    private static readonly int      Answer        = Animator.StringToHash("Answer");
    private static readonly int      Correct       = Animator.StringToHash("Correct");
    private static readonly int      InCorrect     = Animator.StringToHash("InCorrect");
    

    private void Awake()
    {
        JK_Animator     = GetComponent<Animator>();
        _scoreText.text = _point.ToString();
        if (playerInput != null)
        {
            _Pmat = GetComponent<Renderer>().material;
            _Lmat = GetComponent<Renderer>().material;
            _Hmat = GetComponent<Renderer>().material;
        }
        matClear();
    }

    void Start()
    {
        _Lmat.SetFloat(UseEmission, 0f);
    }

    private void matClear()
    {
        Vector2 offset = new Vector2((float)0, (float)0.5);
        _Pmat.SetTextureOffset(MainTex2, offset);
        
        _Lmat.SetFloat(UseEmission, 0f);
        
        Plate.SetActive(false);
    }

    private void PmatCorrect()
    {
        Vector2 offset = new Vector2((float)0, (float)0);
        _Pmat.SetTextureOffset(MainTex2, offset);
    }

    private void PmatIncorrect()
    {
        Vector2 offset = new Vector2((float)0.5, (float)0);
        _Pmat.SetTextureOffset(MainTex2, offset);
    }

    private void LmatEmission()
    {
        _Lmat.SetFloat(UseEmission,      1.0f);
    }

    private void Lmatblink()
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
        }
        _blinkCoroutine = StartCoroutine(BlinkEmission(_Lmat, 10f, 0.2f));
    }
   
    /// <summary>
    /// 点滅コールチン
    /// </summary>
    private IEnumerator BlinkEmission(Material targetMat, float duration, float interval)
    {
        float timer = 0f;

        // lilToonのエミッションを有効化
        targetMat.EnableKeyword("_EMISSION");

        while (timer < duration)
        {
            // 点灯
            targetMat.SetFloat(UseEmission,1.0f);
            yield return new WaitForSeconds(interval);

            // 消灯
            targetMat.SetFloat(UseEmission,0f);
            yield return new WaitForSeconds(interval);

            timer += interval * 2; // ON + OFFで合計時間を進める
        }

        // 点滅終了後、最終的に消灯
        targetMat.SetFloat(UseEmission,0f);
    }
    
    public void StopBlink()
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
            
            _Lmat.SetFloat(UseEmission,0f);
        }
    }

    private void Lmatreset()
    {
        _Lmat.SetFloat(UseEmission,   0f);
    }
    
    /// <summary>
    /// 11/28変更:呼ばれた時に値を返すコールバック
    /// </summary>
    /// <param name="selectFuncCallback"></param>
    public void SetInit(System.Func<int> selectFuncCallback)
    {
        _selectFuncCallback = selectFuncCallback;
    }
    
    public void OnRock(InputAction.CallbackContext context)         //グーが押されたら
    {
        if(Hand == 0)
        {
            Vector2 offset = new Vector2((float)0.0, (float)0.0);
            _Hmat.SetTextureOffset(BaseMap, offset);
            Plate.SetActive(true);
            _rockCount++;
            Hand = 1;
            Judge(0);
            Instance.PlaySe(SeAudioClipNames.Ansewer);
            JK_Animator.SetTrigger(Answer);
            LmatEmission();
        }
        return;
    }
    public void OnScissors(InputAction.CallbackContext context)     //チョキが押されたら
    { 
        if(Hand == 0)
        {
            Vector2 offset = new Vector2((float)0.0, (float)0.67);
            _Hmat.SetTextureOffset(BaseMap, offset);
            Plate.SetActive(true);
            _ScissorsCount++;
            Hand = 2;
            Judge(1);
            Instance.PlaySe(SeAudioClipNames.Ansewer);
            JK_Animator.SetTrigger(Answer);
            LmatEmission();
        }
        return;
    }
    public void OnPaper(InputAction.CallbackContext context)        //パーが押されたら
    {
        if (Hand == 0)
        {            
            Vector2 offset = new Vector2((float)0.0, (float)0.34);
            _Hmat.SetTextureOffset(BaseMap, offset);
            Plate.SetActive(true);
            _paperCount++;
            Hand = 3;
            Judge(2);
            Instance.PlaySe(SeAudioClipNames.Ansewer);
            JK_Animator.SetTrigger(Answer);
            LmatEmission();
        }
        return;
    }

    /// <summary>
    /// 時間内に手を出さなかった場合操作できないようにする
    /// </summary>
    public void TimeOver()
    {
        switch (Hand)
        {
            case 0: 
                Hand = 4;
                NoEntry();
                break;
            case 1:
                Lmatreset();
                break;
            case 2:
                Lmatreset();
                break;
            case 3:
                Lmatreset();
                break;
        }

    }
    //
    // public void OnReset(InputAction.CallbackContext context)
    // {
    //     Rock.SetActive(false);
    //     Scissors.SetActive(false);
    //     Paper.SetActive(false);
    //     judge.text = Monitor[2];
    //     Hand = 0;
    // }
    public void JKHandClear()
    {
        //出した手を消す
        // Rock.SetActive(false);
        // Scissors.SetActive(false);
        // Paper.SetActive(false);
        
        //ジャッジを空白にする
        matClear();
        Hand       = 4;
    }
    public void JKControllerReset()
    {
        Hand = 0;
    }

    /// <summary>
    /// 11/28変更:ターン内に獲得した点数をポップアップさせる
    /// 獲得点数に応じて_addPointObjectから点数のスプライトを呼び出す。
    /// _AddPointObject[0]->"Add100 1" , [1]->"Add80" , [2]->"Add60" , [3]->"Add50" , [4]->"mainas50"のプレハブを当てはめる（prefabs->Janken->Murayama内にあります）
    /// monitor[0]->〇 , monitor[1]->× , monitor[2]->空白
    /// </summary>
    public void PointView()
    {
        switch (_changePoint)
        {
            case 100:
                PmatCorrect();
                Lmatblink();//正解したらライトを光らせる
                Instance.PlaySe(SeAudioClipNames.Correct_ansewer);
                _particle.Play(); //正解した時のエフェクトを出す
                Instantiate(_AddPointObject[0], _pointTransform); //プレイヤーの頭上に獲得得点をポップアップさせる
                JK_Animator.SetTrigger(Correct);
                Plate.SetActive(false);
                break;
            case 80:
                PmatCorrect();
                Lmatblink();//正解したらライトを光らせる
                Instance.PlaySe(SeAudioClipNames.Correct_ansewer);
                _particle.Play(); //正解した時のエフェクトを出す
                Instantiate(_AddPointObject[1], _pointTransform); //プレイヤーの頭上に獲得得点をポップアップさせる
                JK_Animator.SetTrigger(Correct);
                Plate.SetActive(false);
                break;
            case 60:
                PmatCorrect();
                Lmatblink();//正解したらライトを光らせる
                Instance.PlaySe(SeAudioClipNames.Correct_ansewer);
                _particle.Play();                                 //正解した時のエフェクトを出す
                Instantiate(_AddPointObject[2], _pointTransform); //プレイヤーの頭上に獲得得点をポップアップさせる
                JK_Animator.SetTrigger(Correct);
                Plate.SetActive(false);
                break;
            case 50:
                PmatCorrect();
                Lmatblink();//正解したらライトを光らせる
                Instance.PlaySe(SeAudioClipNames.Correct_ansewer);
                _particle.Play(); //正解した時のエフェクトを出す
                Instantiate(_AddPointObject[3], _pointTransform); //プレイヤーの頭上に獲得得点をポップアップさせる
                JK_Animator.SetTrigger(Correct);
                Plate.SetActive(false);
                break;
            case -50:
                PmatIncorrect();
                Instance.PlaySe(SeAudioClipNames.InCorrect);
                Instantiate(_AddPointObject[4], _pointTransform);//プレイヤーの頭上に獲得得点をポップアップさせる
                JK_Animator.SetTrigger(InCorrect);
                Plate.SetActive(false);
                break;
        }
        _scoreText.text = _point.ToString(); //現在のポイント数を表示する
    }


    /// <summary>
    /// 正誤判定
    /// </summary>
    /// <param name="hand">0=グー、1=チョキ、2=パー</param>
    private void Judge(int hand)
    {
        // モニターの手（0ぐー、１ちょき、２ぱー）＋ 3 - 自分の手（0ぐー,1ちょき、2ぱー）を3で割ったあまり
        // モニターと自分の手の値を引いて絶対値出す
        /*int diff = Mathf.Abs(_jankenManager._intHand   - (hand));;
        Debug.Log($"diff:{diff} = {_jankenManager._intHand} - {hand} ");*/
        var result = ((_jankenManager._intHand + 3) - hand) % 3;

        switch (_jankenManager._intOrder)
        {
            case 0: //  win?
                if (result == 1)
                {
                    Debug.Log($"指示は勝て{_jankenManager._intOrder}でモニターは{_jankenManager._intHand}:{_playerName}のては{hand}で加算 {result}");
                    _isFirstPlayerHit = _jankenManager.IsFirstHit(); //何番目に正解したかが帰ってくる
                    AddPoint();
                }
                else
                {
                    Debug.Log($"指示は勝て{_jankenManager._intOrder}でモニターは{_jankenManager._intHand}:{_playerName}のては{hand}で減算{result}");
                    MainasPoint();
                }
                break;
            case 1: //  lose?
                if (result == 2)
                {
                    Debug.Log($"指示は負けろ{_jankenManager._intOrder}でモニターは{_jankenManager._intHand}:{_playerName}のては{hand}で加算{result}");
                    _isFirstPlayerHit = _jankenManager.IsFirstHit();    //何番目に正解したかが帰ってくる
                    AddPoint();
                }
                else
                {
                    Debug.Log($"指示は負けろ{_jankenManager._intOrder}でモニターは{_jankenManager._intHand}:{_playerName}のては{hand}で減算{result}");
                    MainasPoint();
                }
                break;
            case 2: //  draw?
                if (result == 0)
                {
                    Debug.Log($"指示はあいこ{_jankenManager._intOrder}でモニターは{_jankenManager._intHand}:{_playerName}のては{hand}で加算{result}");
                    _isFirstPlayerHit = _jankenManager.IsFirstHit();    //何番目に正解したかが帰ってくる
                    AddPoint();
                }
                else
                {
                    Debug.Log($"指示はあいこ{_jankenManager._intOrder}でモニターは{_jankenManager._intHand}:{_playerName}のては{hand}で減算算{result}");
                    MainasPoint();
                }
                break;
            case 3: //  not win?
                if (result == 1)
                {
                    Debug.Log($"指示は勝つな{_jankenManager._intOrder}でモニターは{_jankenManager._intHand}:{_playerName}のては{hand}で減算{result}");
                    MainasPoint();
                }
                else
                {
                    Debug.Log($"指示は勝つな{_jankenManager._intOrder}でモニターは{_jankenManager._intHand}:{_playerName}のては{hand}で加算 {result}");
                    _isFirstPlayerHit = _jankenManager.IsFirstHit(); //何番目に正解したかが帰ってくる
                    AddPoint();
                }
                break;
            case 4: //  not lose?
                if (result == 2)
                {
                    Debug.Log($"指示は負けるな{_jankenManager._intOrder}でモニターは{_jankenManager._intHand}:{_playerName}のては{hand}で減算{result}");
                    MainasPoint();
                }
                else
                {
                    Debug.Log($"指示は負けるな{_jankenManager._intOrder}でモニターは{_jankenManager._intHand}:{_playerName}のては{hand}で加算 {result}");
                    _isFirstPlayerHit = _jankenManager.IsFirstHit(); //何番目に正解したかが帰ってくる
                    AddPoint();
                }
                break;
            case 5: //  not draw?
                if (result == 0)
                {
                    Debug.Log($"指示はあいこ以外{_jankenManager._intOrder}でモニターは{_jankenManager._intHand}:{_playerName}のては{hand}で減算{result}");
                    MainasPoint();
                }
                else
                {
                    Debug.Log($"指示はあいこ以外{_jankenManager._intOrder}でモニターは{_jankenManager._intHand}:{_playerName}のては{hand}で加算 {result}");
                    _isFirstPlayerHit = _jankenManager.IsFirstHit(); //何番目に正解したかが帰ってくる
                    AddPoint();
                }
                break;
            default:
                PmatIncorrect();
                break;
        }

        if (result == 1)
        {
            _WinnerCount++; //正誤かかわらず勝っていたら＋１する
            Debug.Log("勝ち");
        }
        if (result == 2)
        {
            _loserCount++; //正誤かかわらず負けていたら＋１する
            Debug.Log("負け");
        }
        if (result == 0)
        {
            _drawCount++; //正誤かかわらず引き分けていたら＋１する
            Debug.Log("引き分け");
        }


        #region 前のJudge
        /*
        //object judgeText = null;
        switch (_jankenManager._intHand)
        {
            case 0:
                //勝ち処理 計算 出した手から問題で表示された手を引いた値
                if (hand - _JMintHand == -1 || hand - _JMintHand == 2)
                {
                    //true;
                    judge.text = Monitor[0];
                }
                else
                {
                    judge.text = Monitor[1];
                    break;
                }
                break;
        
            case 1:
                //負け処理 計算 出した手から問題で表示された手を引いた値
                if (hand - _JMintHand == -2 || hand - _JMintHand == 1)
                {
                    //true;
                    judge.text = Monitor[0];
                }
                else
                {
                    judge.text = Monitor[1];
                    break;
                }
                break;
            case 2:
                //あいこ処理 計算 出した手から問題で表示された手を引いた値
                if (hand - _JMintHand == 0)
                {
                    //true;
                    judge.text = Monitor[0];
                }
                else
                {
                    judge.text = Monitor[1];
                    break;
                }
                break;
        }
        */
        #endregion
    }
    /// <summary>
    /// 制限時間内に回答していないなら-50する
    /// </summary>
    private void NoEntry()
    {
        MainasPoint();
    }

    /// <summary>
    /// 回答が間違っていた場合-50、現在の所持ポイントが0なら0を下回らないようにする
    /// </summary>
    private void MainasPoint()
    {
        if (_point <= 0)
        {
            _point = 0;
            _changePoint = -50;
        }
        else if (_point >= _AddPoint)
        {
            _point -= _minusPoint;
            _changePoint = -50;
        }
    }

    /// <summary>
    /// 11/28追加:プレイヤーが獲得したポイント数を返す
    /// </summary>
    /// <returns></returns>
    private int SetPoint()
    {
        switch(_isFirstPlayerHit)
        {
            //一番目
            case 1:
                return 100;
            //二番目
            case 2:
                return 80;
            //三番目
            case 3:
                return 60;
            //四番目
            case 4:
                return 50;
            //それ以外は0を返す
            default:
                return 0;
        }
    }

    /// <summary>
    /// 11/28変更:プレイヤーが１ターンに獲得したポイントを現在のポイントに足す
    /// </summary>
    private void AddPoint()
    {
        _AddPoint = SetPoint();//獲得したポイントを_AddPointに渡す
        _point += _AddPoint; //現在のポイントに獲得ポイントを足す
        _changePoint = _AddPoint; //何点獲得したかを表示するために保存しておく
    }
}