using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ConfirmTitle : MonoBehaviour
{
    //pauseManagerを探して保存する用
    private GameObject _pauseManagerobj;
    //pauseManagerからscriptを探して保存する用
    [SerializeField]private PauseManager _pauseManagerScript;
    //方向スティックのx軸を設定する
    [SerializeField] private InputAction _TitleSelectAction;
    //決定ボタンを設定する
    [SerializeField] private InputAction _decisionAction;
    //選択してないバージョンのボタンを設定する
    [SerializeField] private List<GameObject> _buttons = new List<GameObject>();
    //↑のimageをとる用
    [SerializeField] private List<Image> _buttonImages;
    //選択中のUIを出すための位置の設定
    [SerializeField] private List<Transform> _buttonPositions = new List<Transform>();
    //選択中のUIを設定する
    [SerializeField] private List<GameObject> _selectButtonImages = new List<GameObject>();
    //Instantiateした選択中のUIを数える、削除用
    [SerializeField, Label("SelectButton保存、削除用")] private List<GameObject> _selectButtonList = new List<GameObject>();
    //どれを選択しているか数える用
    [SerializeField] private int _cursorIndex;
    //スティックのvalueが０かを判定する用
    private bool _isDecisionZero;
    //UIが出現しきるまで押せないようにする用
    private bool _isButton = false;

    private void Awake()
    {
        _decisionAction.Enable();//InputSystemを有効化する
        _decisionAction.performed += OnDecision;//設定したボタンが押されたらOnDecisinを呼び出す
        _TitleSelectAction.performed += SelectAction;//設定したボタンが押されたらSelectActionを呼び出す
        _TitleSelectAction.canceled += SelectAction;//操作が行われなくなった時（valueが0になったとき）に呼び出す
        //選択ボタンのimage取得
        for (int i = 0; i < _buttons.Count; i++)
        {
            _buttonImages.Add(_buttons[i].GetComponent<Image>());
        }
        //pauseManagerを探して保存する
        _pauseManagerobj = GameObject.Find("PauseManager");
        //pauseManagerからscriptを探して保存する
        _pauseManagerScript = _pauseManagerobj.GetComponent<PauseManager>();
    }
    //InputActinの有効化
    private void OnEnable() => _TitleSelectAction.Enable();
    //inputActionの無効化
    private void OnDisable() => _TitleSelectAction.Disable();

    private void OnDestroy()
    {
        _TitleSelectAction.performed -= SelectAction;//アタッチしたSelectActinoを破棄する
        _TitleSelectAction.canceled -= SelectAction;//アタッチしたSelectActinを破棄する
        _decisionAction.performed -= OnDecision;//アタッチしたOnDecisionを破棄する
    }

    // Start is called before the first frame update
    void Start()
    {
        FadeIn();
    }
    /// <summary>
    /// スティックの左右操作で選択ボタンを変える
    /// </summary>
    /// <param name="context"></param>
    private void SelectAction(InputAction.CallbackContext context)
    {
        //操作デバイスがoptionボタンのデバイスと同じなら
        if (_pauseManagerScript._isConfirmTitle && _pauseManagerScript._device == context.control.device)
        {
            //スティックの操作方向（強さ）を記録する
            var direction = context.ReadValue<float>();
            //方向確認用
            print($"Direction: {direction}");
            //右操作かつ最初の操作なら
            if (direction >= 0.5f && _isDecisionZero)
            {
                //値が0に戻るまで操作させないようにする
                _isDecisionZero = false;
                //選択中のボタンが１番上なら１番下にする
                if (_cursorIndex <= 0) _cursorIndex = 1;
                //選択中のボタンを１つ上に変更する
                else _cursorIndex--;
                CursorView(0, 2);
            }
            //左操作かつ最初の操作なら
            if (direction <= -0.5f && _isDecisionZero)
            {
                //値が0に戻るまで操作させないようにする
                _isDecisionZero = false;
                //選択中のボタンが１番下なら１番上にする
                if (_cursorIndex >= 1) _cursorIndex = 0;
                //選択中のボタンを１つ下に変更する
                else _cursorIndex++;
                CursorView(0, 2);
            }
            //スティックの方向が0になったら
            if (direction == 0f)
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
    /// 選択中のボタンの見た目を変える
    /// </summary>
    private void CursorView(int listStart, int listend)
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
        _selectButtonList.Add(Instantiate(_selectButtonImages[_cursorIndex], _buttonPositions[_cursorIndex]));
    }

    /// <summary>
    /// 決定ボタン（_decisionActionに設定したボタン）を押したら選択中のボタン先に遷移する
    /// </summary>
    private void OnDecision(InputAction.CallbackContext context)
    {
        switch(_cursorIndex)
        {
            //いいえ
            case 0:
                //_isButtonがtrueならポーズ画面に戻る
                if (_isButton == true) FadeOut();
                else return;
                break;
            //はい
            case 1:
                //_isButtonがtrueならタイトル画面に遷移
                if (_isButton == true)
                {
                    var _fadeTransition = GameObject.Find("FadeTransitionImage");
                    var _fadeTransitionScript = _fadeTransition.GetComponent<FadeTransition>();
                    _fadeTransitionScript.FadeIn(FadeTransition.MoveSeneName.Title);
                }
                else return;
                break;
        }
        
    }

    /// <summary>
    /// 確認UIがポップアップする
    /// </summary>
    private async void FadeIn()
    {
        LMotion.Create(0f, 1f, 0.2f).WithEase(Ease.Linear).BindToLocalScaleX(this.transform).AddTo(gameObject);
        LMotion.Create(0f, 1f, 0.2f).WithEase(Ease.Linear).BindToLocalScaleY(this.transform).AddTo(gameObject);
        await UniTask.WaitForSeconds(0.2f);
        CursorView(0, 2);
        //決定ボタンを押せるようにする
        _isButton = true;
    }

    /// <summary>
    /// 確認UIが縮んんで消える
    /// </summary>
    private async void FadeOut()
    {
        LMotion.Create(1f, 0f, 0.2f).WithEase(Ease.Linear).BindToLocalScaleX(this.transform).AddTo(gameObject);
        LMotion.Create(1f, 0f, 0.2f).WithEase(Ease.Linear).BindToLocalScaleY(this.transform).AddTo(gameObject);
        await UniTask.WaitForSeconds(0.2f);
        //確認中を解除する
        _pauseManagerScript._isConfirmTitle = false;
        //自身を消す
        Destroy(this.gameObject);
    }
}
