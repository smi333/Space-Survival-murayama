using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using NaughtyAttributes;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEditorInternal.VersionControl.ListControl;

public class PauseManager : MonoBehaviour
{
    private CursorPopup cursorpopup;
    //optionボタンを設定する
    [SerializeField]private InputAction _fadeInputAction;
    //方向スティックのy軸を設定する
    [SerializeField] private InputAction _selectAction;
    //決定ボタンを設定する
    [SerializeField] private InputAction _decisionAction;
    //ポーズ画面の人数を制限するための変数
    private int _pausePlayer = 0;
    //フェードに関する動きのスピード指定用
    private float _fadeTime = 0.2f;
    //ポーズ中はtrueにする
    private bool _isPause = false;
    //クレジット中はtrueにする
    private bool _isCredit = false;
    //タイトルに戻るか確認中はtrueにする
    public bool _isConfirmTitle = false;
    //スティックのvalueが０かを判定する用
    private bool _isDecisionZero;
    [SerializeField] private Transform _pauseCanvas;
    //ゲーム画面を薄暗くする用
    [SerializeField] private CanvasGroup _backGroundFadeImage;
    //タイトル遷移時にフェードする用
    [SerializeField] private GameObject _fadeImage;
    //青い背景を設定する
    [SerializeField] private GameObject _SlideImage;
    //紫のヘッダーを設定する
    [SerializeField] private GameObject _headerImage;
    //ヘッダーのtextを設定する
    [SerializeField] private GameObject _headerCredit;
    //クレジット用のテキストを設定する
    [SerializeField] private GameObject _creditText;
    //どれを選択しているか数える用
    [SerializeField] private int _cursorIndex = 0;
    //選択してないバージョンのボタンを設定する
    [SerializeField] private List<GameObject> _buttons = new List<GameObject>();
    //↑のimageをとる用
    private List<UnityEngine.UI.Image> _buttonImages = new List<Image>();
    //選択中のUIを出すための位置の設定
    [SerializeField] private List<Transform> _buttonImagePosition = new List<Transform>();
    //選択中のUIを設定する
    [SerializeField] private List<GameObject> _selectButtonImages = new List<GameObject>();
    //Instantiateした選択中のUIを数える、削除用
    [SerializeField, Label("SelectButton保存、削除用")] private List<GameObject> _selectButtonList = new List<GameObject>();
    //ConfirmTitleImage（タイトルに戻るか確認するUI）を入れる
    [SerializeField] private GameObject _confirmTitle;
    //↑を保存する用
    [SerializeField,Label("ConfirmTitle保存、削除用")] private List<GameObject> _ConfirmTitleKeep;
    //optionボタンを押したデバイスを記録する
    public InputDevice _device;
    // Start is called before the first frame update
    void Awake()
    {
        //選択ボタンのimage取得
        for (int i = 0; i < _buttons.Count; i++)
        {
            _buttonImages.Add(_buttons[i].GetComponent<Image>());
        }
        _fadeInputAction.Enable(); //InputSystemを有効化する
        _fadeInputAction.performed += OnJoin; //ポーズボタンが押されたらOnPauseを呼び出す
        _decisionAction.Enable();//InputSystemを有効化する
        _decisionAction.performed += OnDecision;//設定したボタンが押されたらOnDecisinを呼び出す
        _selectAction.performed += SelectAction;//設定したボタンが押されたらSelectActionを呼び出す
        _selectAction.canceled += SelectAction;//操作が行われなくなった時（valueが0になったとき）に呼び出す
    }

    private void OnDestroy()
    {
        _fadeInputAction.performed -= OnJoin; //アタッチしたOnPauseを破棄する
        _selectAction.performed -= SelectAction;//アタッチしたSelectActinoを破棄する
        _selectAction.canceled -= SelectAction;//アタッチしたSelectActinを破棄する
        _decisionAction.performed -= OnDecision;//アタッチしたOnDecisionを破棄する
    }
    //InputActinの有効化
    private void OnEnable() => _selectAction.Enable();
    //inputActionの無効化
    private void OnDisable() => _selectAction.Disable();

    /// <summary>
    /// フェードイン
    /// </summary>
    private async void FadeIn()
    {
        //後ろの背景が暗くなる
        LMotion.Create(0.0f, 0.5f, _fadeTime).WithEase(Ease.Linear).BindToAlpha(_backGroundFadeImage).AddTo(gameObject);
        //水色の背景が左から飛び出してくる
        LMotion.Create(-3000f, -1800f, _fadeTime).WithEase(Ease.Linear).BindToLocalPositionX(_SlideImage.transform).AddTo(gameObject);
        //ヘッダーが左から飛び出してくる
        await LMotion.Create(-2250f, -750f, _fadeTime).WithEase(Ease.Linear).BindToLocalPositionX(_headerImage.transform).AddTo(gameObject);
        //少し間を置く
        await UniTask.WaitForSeconds(0.05f);
        //ボタンが上から順に左→右にスライド
        for(int i = 0; i < _buttons.Count-1; i++)
        {
            LMotion.Create(-1380f - 100 * i, -100f - 100*i, _fadeTime).WithEase(Ease.Linear).BindToLocalPositionX(_buttons[i].transform).AddTo(gameObject);
            await UniTask.WaitForSeconds(0.05f);
        }
        //少し間を置く
        await UniTask.WaitForSeconds(0.1f);
        CursorView(0,3);
    }
    /// <summary>
    /// フェードアウト
    /// </summary>
    private async void FadeOut()
    {
        //ボタンが上から順に右→左に引っ込む
        for (int i = 0; i < _buttons.Count - 1; i++)
        {
            LMotion.Create(-100f - 100 * i, -1380f - 100 * i, _fadeTime).WithEase(Ease.Linear).BindToLocalPositionX(_buttons[i].transform).AddTo(gameObject);
            await UniTask.WaitForSeconds(0.05f);
        }
        //少し間を置く
        await UniTask.WaitForSeconds(0.15f);
        //ヘッダーが左に引っ込む
        await LMotion.Create(-750f, -2250f, _fadeTime).WithEase(Ease.Linear).BindToLocalPositionX(_headerImage.transform).AddTo(gameObject);
        //水色の背景が左に引っ込む
        LMotion.Create(-1800f, -3000f, _fadeTime).WithEase(Ease.Linear).BindToLocalPositionX(_SlideImage.transform).AddTo(gameObject);
        //後ろの背景が明るくなる
        LMotion.Create(0.5f, 0.0f, _fadeTime).WithEase(Ease.Linear).BindToAlpha(_backGroundFadeImage).AddTo(gameObject);
    }
    
    /// <summary>
    /// optionボタンが押されたらデバイスを記録し、ポーズ画面を開く
    /// </summary>
    /// <param name="context"></param>
    private void OnJoin(InputAction.CallbackContext context)
    {
        //誰かがポーズ画面を開いているなら何もしない
        if (_pausePlayer >= 1) return;
        //optionボタンを押したデバイスを取得
        _device = context.control.device;
        Debug.Log($"{_device}");
        //_pausePlayerに1足す
        _pausePlayer++;
        Pause();
    }
    /// <summary>
    /// ポーズ画面を開く（もう開いてたら何もしない）
    /// </summary>
    private void Pause()
    {
        //ポーズ画面を開いているなら何もしない
        if (_isPause)
        {
            return;
        }
        //開いてなかったら開く
        else
        {
            //一番上を選択中にする
            _cursorIndex = 0;
            FadeIn();
            //Debug.Log("optionボタンを押した");
            //ポーズ画面を開いている状態にする
            _isPause = true;
        }
    }

    /// <summary>
    /// ポーズ画面からクレジット画面へのフェードイン
    /// </summary>
    private async void CreditFadeIn()
    {
        //ボタンのalphaを1にする
        _buttonImages[_cursorIndex].color = new Color(1, 1, 1, 1f);
        //水色の背景が真ん中から右へ移動
        LMotion.Create(-1800f, 0f, _fadeTime).WithEase(Ease.Linear).BindToLocalPositionX(_SlideImage.transform).AddTo(gameObject);
        //ヘッダーを左画面外へ
        LMotion.Create(-750f, -2250f, _fadeTime).WithEase(Ease.Linear).BindToLocalPositionX(_headerImage.transform).AddTo(gameObject);
        //ヘッダーテキストをクレジットへ変更
        //_headerText.text = "クレジット";
        await UniTask.WaitForSeconds(0.2f);
        //ボタンを上から順に真ん中から左画面外へ
        for (int i = 0; i < _buttons.Count - 1; i++)
        {
            LMotion.Create(-100f - 100 * i, 1500f - 100 * i, _fadeTime).WithEase(Ease.Linear).BindToLocalPositionX(_buttons[i].transform).AddTo(gameObject);
            await UniTask.WaitForSeconds(0.05f);
        }
        await UniTask.WaitForSeconds(0.15f);
        //クレジット用テキストを左画面外から真ん中へ
        await LMotion.Create(-1500f, 0f, _fadeTime).WithEase(Ease.Linear).BindToLocalPositionX(_creditText.transform).AddTo(gameObject);
        //戻るボタンを左画面外から真ん中へ
        await LMotion.Create(-1500f, 0f, _fadeTime).WithEase(Ease.Linear).BindToLocalPositionX(_buttons[3].transform).AddTo(gameObject);
        //ヘッダーを左画面外から右にスライド
        await LMotion.Create(-2250f, -750f, _fadeTime).WithEase(Ease.Linear).BindToLocalPositionX(_headerCredit.transform).AddTo(gameObject);
        await UniTask.WaitForSeconds(0.1f);
        CursorView(3,4);

    }
    /// <summary>
    /// クレジット画面からポーズ画面へのフェードアウト
    /// </summary>
    private async void CreditFadeOut()
    {
        //ボタンのalphaを全部1にする
        for (int i = 0; i < 3; i++)
        {
            _buttonImages[i].color = new Color(1, 1, 1, 1f);
        }
        //ヘッダーを左画面外へ
        LMotion.Create(-750f, -2250f, _fadeTime).WithEase(Ease.Linear).BindToLocalPositionX(_headerCredit.transform).AddTo(gameObject);
        //ヘッダーテキストをメニューに変更
        //_headerText.text = "メニュー";
        //戻るボタンを左画面外へ
        await LMotion.Create(0f, -1500f, _fadeTime).WithEase(Ease.Linear).BindToLocalPositionX(_buttons[3].transform).AddTo(gameObject);
        //クレジット用テキストを左画面外へ
        LMotion.Create(0f, -1500f, _fadeTime).WithEase(Ease.Linear).BindToLocalPositionX(_creditText.transform).AddTo(gameObject);
        //水色の背景を全画面から左半分へ
        LMotion.Create(0f, -1800f, _fadeTime).WithEase(Ease.Linear).BindToLocalPositionX(_SlideImage.transform).AddTo(gameObject);
        //ヘッダーを左画面から右にスライド
        LMotion.Create(-2250f, -750f, _fadeTime).WithEase(Ease.Linear).BindToLocalPositionX(_headerImage.transform).AddTo(gameObject);
        await UniTask.WaitForSeconds(0.2f);
        //ボタンを上から順に右画面外から真ん中へ
        for (int i = 0; i < _buttons.Count - 1; i++)
        {
            LMotion.Create(1500f - 100 * i, -100f - 100 * i, _fadeTime).WithEase(Ease.Linear).BindToLocalPositionX(_buttons[i].transform).AddTo(gameObject);
            await UniTask.WaitForSeconds(0.05f);
        }
        //少し間を置く
        await UniTask.WaitForSeconds(0.1f);
        CursorView(0,3);
    }
    /// <summary>
    /// スティックの上下操作で選択ボタンを変える
    /// </summary>
    /// <param name="context"></param>
    private void SelectAction(InputAction.CallbackContext context)
    {
        //操作デバイスがoptionボタンのデバイスと同じなら
        if (_isPause && _device == context.control.device)
        {
            //スティックの操作方向（強さ）を記録する
            var direction = context.ReadValue<float>();
            //方向確認用
            //print($"Direction: {direction}");
            //クレジット画面を開いているなら何もしない
            if (_isCredit) return;
            if (_isConfirmTitle) return;
            //上操作かつ最初の操作なら
            if(direction >= 0.5f && _isDecisionZero)
            {
                //値が0に戻るまで操作させないようにする
                _isDecisionZero = false;
                //選択中のボタンが１番上なら１番下にする
                if (_cursorIndex <= 0) _cursorIndex = 2;
                //選択中のボタンを１つ上に変更する
                else _cursorIndex--;
                CursorView(0, 3);
            }
            //下操作かつ最初の操作なら
            if(direction <= -0.5f && _isDecisionZero)
            {
                //値が0に戻るまで操作させないようにする
                _isDecisionZero = false;
                //選択中のボタンが１番下なら１番上にする
                if (_cursorIndex >= 2) _cursorIndex = 0;
                //選択中のボタンを１つ下に変更する
                else _cursorIndex++;
                CursorView(0, 3);
            }
            //スティックの方向が0になったら
            if(direction == 0f)
            {
                //再度操作できるようにする
                _isDecisionZero = true;
            }

        }
        //操作デバイスがoptionボタンのデバイスと同じじゃないなら何もしない
        else
        {
            return;
        }
    }

    /// <summary>
    /// 本当にタイトルに戻るか確認する
    /// </summary>
    [Button]
    private void ConfirmTitle()
    {
        _isConfirmTitle = true;
        _ConfirmTitleKeep.Add(Instantiate(_confirmTitle, _pauseCanvas));
    }

    /// <summary>
    /// タイトルシーンへ遷移するfadeImageを呼び出す
    /// </summary>
    public void TitleScene()
    {
        Instantiate(_fadeImage, _pauseCanvas);
    }

    /// <summary>
    /// 決定ボタン（_decisionActionに設定したボタン）を押したら選択中のボタン先に遷移する
    /// </summary>
    /// <param name="context"></param>
    private void OnDecision(InputAction.CallbackContext context)
    {
        //もしポーズ中なら
        if(_isPause)
        {
            switch (_cursorIndex)
            {
                //続ける
                case 0:
                    //ポーズを解除する
                    _isPause = false;
                    //optionボタンが押せるようになる
                    _pausePlayer--;
                    FadeOut();
                    break;
                //クレジット
                case 1:
                    //クレジット表示中にする
                    _isCredit = true;
                    //戻るボタンにカーソルを合わせておく
                    _cursorIndex = 3;
                    CreditFadeIn();
                    break;
                //タイトル
                case 2:
                    if (_isConfirmTitle == false) ConfirmTitle();
                    else return;
                    break;
                //戻る
                case 3:
                    //クレジット中解除
                    _isCredit = false;
                    //カーソルを一番上に持ってくる
                    _cursorIndex = 0;
                    CreditFadeOut();
                    break;
                
            }
        }
        
    }

    /// <summary>
    /// 選択中のボタンの見た目を変える
    /// </summary>
    private void CursorView(int listStart,int listend)
    {
        //ボタンのalphaを全部1にする
        for (int i = listStart; i < listend; i++)
        {
            _buttonImages[i].color = new Color(1, 1, 1, 1f);
        }
        //前に選択中だったボタンに表示されているボタンを消す
        for (int i = 0; i < _selectButtonList.Count; i++)
        {
            Destroy(_selectButtonList[i]);
        }
        //リストを整理する
        _selectButtonList.Clear();
        //選択中のボタンのalphaを０にする
        _buttonImages[_cursorIndex].color = new Color(1, 1, 1, 0f);
        //選択用のボタンをInstantiateして_selectButtonListに入れる
        _selectButtonList.Add (Instantiate(_selectButtonImages[_cursorIndex], _buttonImagePosition[_cursorIndex]));
    }

}
