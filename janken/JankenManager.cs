using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using UnityEngine;
using Random = UnityEngine.Random;
using static JKSoundManager;

public class JankenManager : MonoBehaviour
{
    [SerializeField] public  int                      _intHand;  //モニターに表示される手を数字で管理する
    [SerializeField] public  int                      _intOrder; //指示を数字で管理する(0=勝て,1=負けろ,2=あいこ,3=勝つな,4=負けるな,5=あいこ以外,6=空白)
    [SerializeField]         Display                  _display;  //モニターのアタッチ場所
    [SerializeField]         SignBord                 _signBord; //看板のアタッチ場所
    [SerializeField] private List<JKPlayerController> _JKPlayerControllers;
    [SerializeField] private JKAlien                  _jkAlien;
    [SerializeField]         JKSoundManager           _JKSoundManager;
    [SerializeField]         BeatCounter              _beatCounter;

    [Header("フェーズ、ターン")]
    [SerializeField] private int maxCycle = 8;

    public int currentCycle = 1;
    public int Phase2Cycle  = 3;
    // public int Phase3Cycle  = 6;

    private JKCycleState _currentCycleState = JKCycleState.none;

    //11/28変更:順番の判定用
    private int _isHit = 0;

    private enum JKCycleState
    {
        none,
        start,         // 4 beat 
        reset,         // 0 beat 
        issue,         // 4 beat 
        Answer,        // 6 beat 
        correctAnswer, // 6 beat 
        Pause,         // フェーズ変更時の待機
    }
    
    /// <summary>
    /// じゃんけん指示（エイリアン）
    /// </summary>
    [SerializeField] private List<int> _randomOrder = new List<int>(); //始めに指示を全部出して格納する用
    private                  List<int> _firstList   = new List<int>(); //第一フェーズで選ばれる番号リスト
    //ランダム指示だし用のリスト
    [SerializeField] private List<int> _fullOrderList    = new List<int>();
    private                  int[]     addfullOrderLists = { 0, 1, 2, 3, 4, 5};
    //第三フェーズの確率リスト
    private List<int> _orderProbabilitys   = new List<int>();
    private int[]     addOrderProbabilitys = { 13, 13, 13, 23, 23, 15 };
    
    // BPM
    private const float _bpm1      = 100f;
    private const float _bpm2      = 121f;
    private       float currentBpm = 100f;

    public  double _beatInterval;                   // 1ビートの秒数
    private double _nextBeatTime;                   // 次の処理の時間
    private int    _nextBeatStep = 1;               // steps (how many beats) to when an action is performed
    private int[]  _beatStepArr1 = { 6, 0, 3, 7, 6, 0 }; // phase 1 beat steps

    private bool      IsPhaseStart(int PhaseCycle) => currentCycle == PhaseCycle;
    private Coroutine countdownCoroutine;

    // 11//20 木村が追加（ラウンド数を表示するのに使用）
    private int _currentRoundCount = 1;

    [SerializeField]
    private RoundCountManager _roundCountManager;
    
    void Start()
    {
        RandomOrder();
        _roundCountManager.RoundCountTextUpdate(_currentRoundCount, maxCycle);
        SetBPM(_bpm1);
        StartCoroutine(GameStart());
    }

    /// <summary>
    /// 音楽が始まる前のバチからサイクルスタートまで
    /// </summary>
    private IEnumerator GameStart()
    {
        _beatCounter.StartCounting();
        Instance.PlaySe(SeAudioClipNames.Stick);
        yield return new WaitForSeconds((float)_beatInterval);
        Instance.PlaySe(SeAudioClipNames.Stick);
        yield return new WaitForSeconds((float)_beatInterval);
        Instance.PlayBgm(BgmAudioClipNames.BGM_Janken100);
        yield return new WaitForSeconds((float)_beatInterval * 2);
        SetCycleState(JKCycleState.start);
    }

    private IEnumerator PhaseStart()
    {
        if(currentCycle == Phase2Cycle)
        {
            yield return new WaitForSeconds((float)_beatInterval * 4);
            SetBeatInterval(true, true);
            SetCycleState(JKCycleState.issue);
            Debug.Log("issue");
        }
        else
        {
            yield return new WaitForSeconds((float)_beatInterval * 2);
            SetBeatInterval(true, true);
            SetCycleState(JKCycleState.reset);
        }
    }
    
    /// <summary>
    /// あらかじめ八ターン分選んで_randomOrderに格納しておく
    /// </summary>
    [Button]
    private void RandomOrder()
    {
        _orderProbabilitys.Clear();
        _orderProbabilitys.AddRange(addOrderProbabilitys);
        _fullOrderList.Clear();
        _fullOrderList.AddRange(addfullOrderLists);
        _firstList = Enumerable.Range(0, 3).ToList();
        for(int i = 0; i < 8; i++)
        {
            if (i < 3)
                RandomOrderFirst();
            else if (i == 3)
                RandomOrderfour();
            else
                RandomOrderFull();
        }
    }
    
    /// <summary>
    /// 1～3ターン目の指示を選んで_randomOrderに格納する
    /// </summary>
    private void RandomOrderFirst()
    {
        //0~_firstListの長さ(勝て、負けろ、あいこ（選ばれたものは除外）)からランダムの数字を選ぶ
        var rnd = Random.Range(0, _firstList.Count);
        //選んだ数字をnumに格納する
        var num = _firstList[rnd];
        //選ばれた数字をリストから削除
        _firstList.RemoveAt(rnd);
        //選ばれた数字を_randomOrderに格納する
        _randomOrder.Add(num);
        return;
    }
    /// <summary>
    /// ４ターン目の指示を選んで_randomOrderに格納する
    /// </summary>
    private void RandomOrderfour()
    {
        //3~5の中からランダムの数字を選ぶ（勝つな、負けるな、あいこ以外）
        var rnd = Random.Range(3, 6);
        //選ばれた数字を_randomOrderに格納する
        _randomOrder.Add(rnd);
        return;
    }
    /// <summary>
    /// 5~8ターン目の指示を選んで_randomOrderに格納する
    /// </summary>
    private void RandomOrderFull()
    {
        var fullRnd = 0;
       
        for (int i = 0; i < _orderProbabilitys.Count; i++)
        {
            fullRnd +=  _orderProbabilitys[i];
        }
        //0~99の数字をランダムに取得
        var rnd = Random.Range(0, fullRnd);

        //_orderProbabilitysの長さ分繰り返す
        for (int i = 0; i < _orderProbabilitys.Count; i++)
        {
            //rndが_orderProbabilitys[i]の数字より小さかったら_randomOrderに格納する
            if (rnd < _orderProbabilitys[i])
            {
                //選ばれた数字を_randomOrderに格納する
                _randomOrder.Add(_fullOrderList[i]);
                
                // 選ばれた数字を消す
                _fullOrderList.RemoveAt(i);
                _orderProbabilitys.RemoveAt(i);
                
                return;
            }
            //rndから_orderProbabilitys[i]の値を引く
            rnd -= _orderProbabilitys[i];
        }
        Debug.Log("数値適用外");
        return;
    }
    
    /// <summary>
    /// 次のステートに行く時間をBGM時間基準で予約する
    /// </summary>
    private void SetBeatInterval(bool reset, bool changedTrack)
    {
        if (_currentCycleState == JKCycleState.Pause)
        {
            //Pauseの時は何もしない
            return;
        }

        if (reset)
        {
            _nextBeatStep = 0;
            _nextBeatTime = 0;
        }

        _nextBeatTime = _JKSoundManager.BgmVirtualTime;

        _nextBeatStep =  _beatStepArr1[(int)_currentCycleState - 1];
        _nextBeatTime += _beatInterval * _nextBeatStep;
        if (changedTrack)
        {
            _nextBeatTime -= _beatInterval;
        }
    }
    
    void Update()
    {
        // // 予約された処理を実行
        // RunScheduler();

        if (_currentCycleState == JKCycleState.none || _currentCycleState == JKCycleState.Pause)
        {
            return;
        }

        if (_JKSoundManager.BgmVirtualTime > _nextBeatTime)
        {
            JKCycleState nextState;
            if (currentCycle == maxCycle + 1)
            {
                nextState = JKCycleState.none;
            }
            else if (_currentCycleState == JKCycleState.correctAnswer) // サイクルの開始点にリセット
            {
                nextState = JKCycleState.reset;
            }
            else
            {
                nextState = _currentCycleState + 1;
            }

            SetCycleState(nextState);
        }
    }
        
    /// <summary>
    /// 現在のフェーズのサイクルを設定
    /// ReSharper で Unity.PerformanceAnalysis を無効にする
    /// </summary>
    private void SetCycleState(JKCycleState state)
    {
        _currentCycleState = state;
        
        // ClearSchedule();

        switch (_currentCycleState)
        {
            case JKCycleState.none: // MaxCycleの時だけ
                EndGame();
                return;
            case JKCycleState.start:
                StartCoroutine(PhaseStart());
                break;
            case JKCycleState.reset: // Cycleの最初、リセット目的
                Debug.Log("reset");
                currentCycle++;

                // 11/20 木村が追加（ラウンド数加算とUI反映）
                if (currentCycle >= 2) _currentRoundCount++;
                if (_currentRoundCount != maxCycle + 1)
                {
                    _roundCountManager.RoundCountTextUpdate(_currentRoundCount, maxCycle);
                }

                JKClear();
    
                // Phaseの開始時だけポーズを入れる
                if (IsPhaseStart(Phase2Cycle))
                {
                    SetCycleState(JKCycleState.Pause);
                    SetBPM(_bpm2);
                    _beatCounter._targetBPM = 120;
                    StartCoroutine(PhaseChange());
                    return;
                }

                break;
            case JKCycleState.issue: // 問題の出題、ロック解除
                StartCoroutine(issueSequenceByBeat());
                break;
            case JKCycleState.Answer: // 回答時間
                StartCoroutine(AnswerSequence());
                break;
            case JKCycleState.correctAnswer: // コントローラーロック、順番に正解発表
                Instance.StopSe();
                TimeOver();
                StartCoroutine(AnswerPointView());
                break;
            case JKCycleState.Pause:
                break;
        }

        //ビート間隔を設定する。
        //リセット状態であればビートをリセットする。
        if (_currentCycleState == JKCycleState.issue)
        {
            SetBeatInterval(true, false);
            PlayBGM(true);
        }
        else
        {
            SetBeatInterval(false, false);
        }
    }

    /// <summary>
    /// 指定秒数待ってから指定ステートに移動
    /// </summary>
    private IEnumerator WaitAndResume(float waitSeconds, JKCycleState resumeState)
    {
        yield return new WaitForSeconds(waitSeconds);
        Debug.Log("ポーズ終了。次のステートへ移行：" + resumeState);
        SetCycleState(resumeState);
    }
    
    /// <summary>
    /// 早押しリセット、回答席の正誤リセット、指示のリセット、出した手のリセット、
    /// </summary>
    private void JKClear()
    {
        _isHit = 0; //11/28変更:ターン開始時に早押しリセット
        _display.ClearDisplay();
        _signBord.ClearBord();
        _jkAlien.reset();
        foreach (JKPlayerController jkPlayer in _JKPlayerControllers)
        {
            jkPlayer.StopBlink();
            jkPlayer.JKHandClear();
            jkPlayer.SetInit(IsFirstHit);
        }
    }

    /// <summary>
    /// bool->intに変更順番を記録し細かく得点が変化するようにする
    /// 11/28追加：呼び出されるたびに１加算された値を返す
    /// </summary>
    public int IsFirstHit()
    {
        /*if(_isHit == false)
        {
            _isHit = true;
            return true;
        }*/
        switch (_isHit)
        {
            case 0:
                _isHit++;
                return 1; //1を返す
            case 1:
                _isHit++;
                return 2; //2を返す
            case 2:
                _isHit++;
                return 3; //3を返す
            case 3:
                _isHit++;
                return 4; //4を返す
        }

        return 0;
    }

    /// <summary>
    /// ボタンを押せるようにする
    /// </summary>
    private void JKReset()
    {
        foreach (JKPlayerController jkPlayer in _JKPlayerControllers)
        {
            jkPlayer.JKControllerReset();
        }
    }

    /// <summary>
    /// ボタンを押せないようにする
    /// </summary>
    private void TimeOver()
    {
        foreach (JKPlayerController jkPlayer in _JKPlayerControllers)
        {
            jkPlayer.TimeOver();
        }
    }
    
    /// <summary>
    /// ビートインターバルでディレイをかけてテンポよく指示出し
    /// </summary>
    private IEnumerator issueSequenceByBeat()
    {
        Instance.PlaySe(SeAudioClipNames.Pi);
        yield return new WaitForSeconds((float)_beatInterval * 1);
        Instance.PlaySe(SeAudioClipNames.Pi);
        yield return new WaitForSeconds((float)_beatInterval * 1);
        RandomHand();
        Instance.PlaySe(SeAudioClipNames.Piron);
        yield return new WaitForSeconds((float)_beatInterval * 1);
        Instance.PlaySe(SeAudioClipNames.Piron);
        TurnOrder();
        JKReset();
    }

    /// <summary>
    /// モニターに表示される手をランダムに選ぶ
    /// </summary>
    private void RandomHand()
    {
        // 0＝グー、1＝チョキ、2＝パー、3＝空白
        _intHand = Random.Range(0, 3);
        _display.JKDisplay();
    }

    private void TurnOrder()
    {
        int turnIndex = currentCycle - 1;
        _intOrder = _randomOrder[turnIndex];
        _jkAlien.issue();
        _signBord.JKSignBord();
    }
    
    /// <summary>
    /// そのまま流すと直前の音と被るので少しディレイ
    /// </summary>
    private IEnumerator AnswerSequence()
    {
        yield return new WaitForSeconds((float)_beatInterval * 1);
        if (currentCycle >= Phase2Cycle)
        {
            Instance.PlaySe(SeAudioClipNames.thinkingtime120);
        }
        else
        {
            Instance.PlaySe(SeAudioClipNames.thinkingtime100);
        }
    }
    
    /// <summary>
    /// スコアをP1から順番に表示する
    /// </summary>
    /// <returns></returns>
    private IEnumerator AnswerPointView()
    {
        
        // プレイヤーを順番に処理する
        for (int i = 0; i < _JKPlayerControllers.Count; i++)
        {
            _JKPlayerControllers[i].PointView();
            yield return new WaitForSeconds((float)_beatInterval * 1);
        }
    }


    /// <summary>
    /// 1拍の秒数を作る
    /// </summary>
    private void SetBPM(float bpm)
    {
        currentBpm    = bpm;
        _beatInterval = 60.0 / currentBpm;
        if (Mathf.Approximately(currentBpm, _bpm2))
        {
            SetBeatInterval(true, true); 
        }
    }

    private void PlayBGM(bool play)
    {
        if (play)
        {
            if(!Instance.bgmSource.isPlaying)
                Instance.bgmSource.Play();
        }
        else
        {
            Instance.bgmSource.Stop();
        }
    }
    
    private IEnumerator PhaseChange()
    {
        if (currentCycle == Phase2Cycle)
        {
            Instance.StopBgm();
            
            yield return new WaitForSeconds((float)_beatInterval * 2);
            Instance.PlaySe(SeAudioClipNames.Stick);
            yield return new WaitForSeconds((float)_beatInterval);
            Instance.PlaySe(SeAudioClipNames.Stick);
            yield return new WaitForSeconds((float)_beatInterval);
            Instance.PlayBgm(BgmAudioClipNames.BGM_Janken120);
            SetCycleState(JKCycleState.start);
        }
        // // Phase3開始演出
        // if (currentCycle == Phase3Cycle)
        // {
        //     Debug.Log("出題範囲追加");
        //     SetCycleState(JKCycleState.start);
        // }
    }

    /// <summary>
    /// 最終フェーズ終了時に呼び出される
    /// </summary>
    private void EndGame()
    {
        // プレイヤーのスコアをPlayerDataに受け渡す
        for(int i = 0; i < _JKPlayerControllers.Count; i++)
        {
            PlayerDataManager.Instance.GetPlayer(i + 1).DataSetter(PlayerData.PlayerDataField.JkScore, _JKPlayerControllers[i]._point);
        }
        Debug.Log("GAME ENDED");
        PlayBGM(false);
        // リザルトシーンに移動
        SceneTransitonManager.Instance.FadeSceneMove(SceneTransitonManager.MoveSeneName.MiniGameResult);
    }

}
