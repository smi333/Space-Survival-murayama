using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class StartCameraMove : MonoBehaviour
{
    [SerializeField] private Camera _mainCamera; //メインカメラ
    [SerializeField] private Camera _startCamera; //始めに周りを見渡す動きをするカメラ
    [SerializeField,Label("P1の座標を入れる")] private Transform _startTarget; //始めにスタートカメラが向いているキャラクター
    [SerializeField,Label("Linearを設定")] private Ease _ease;//イージング用後で設定変えるかも
    [SerializeField,Label("JankenStart_UI")] private GameObject _popupObject;//ポップアップさせるUI入れる用
    [SerializeField] JankenManager _jankenManager;
    [SerializeField] private MiniGameHandlerSpawner _miniGameManager;
    // Start is called before the first frame update
    void Start()
    {
        _jankenManager.enabled   = false;
        _mainCamera.enabled      = false; //メインカメラを一時的に切る
        _startCamera.enabled     = true;
        _startCamera.fieldOfView = 5f;               //スタートカメラでキャラクターをズームする
        _startCamera.transform.LookAt(_startTarget); //スターをカメラを始めのキャラクターに向ける
        CameraMove();
    }

    private async void CameraMove()
    {
        //プレイヤー1からプレイヤー4まで視点移動する
        await LMotion.Create(new Vector3(4.903f, -3.917f, 0f), new Vector3(4.903f, 3.735f, 0f), 1f).WithEase(_ease).BindToEulerAngles(transform).AddTo(_startCamera);
        await UniTask.Delay(500);//少し間を空ける
        //カメラの座標を０に戻す
            LMotion.Create(new Vector3(4.903f, 3.735f, 0f), new Vector3(3.647f, 0f, 0f), 0.5f).WithEase(_ease).BindToEulerAngles(transform).AddTo(_startCamera);
            LMotion.Create(5f, 10f, 0.5f).WithEase(_ease).BindToFieldOfView(_startCamera).AddTo(_startCamera);
        await UniTask.Delay(1000);//少し間を空ける
        StartPopup();
        _startCamera.enabled = false;//スタートカメラを切る
        _mainCamera.enabled = true;//メインカメラを映す
        await UniTask.Delay(500);
        StartGame();
    }

    // 12/14木村が修正　privateをpublicに変更 
    /// <summary>
    /// スタートのUIを呼び出す
    /// </summary>
    public void StartPopup()
    {
        Instantiate(_popupObject,new Vector3(0f, 0f, 0f),Quaternion.identity);
    }

    private void StartGame()
    {
        _jankenManager.enabled = true;
        _miniGameManager.enabled = true;
        _miniGameManager.SpawnPlayerHandler();
    }
}
