using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PressBattleStartCameraManager : MonoBehaviour
{
    [SerializeField] private Camera _startCamera; //始めに周りを見渡す動きをするカメラ
    [SerializeField] private float _cameraSpeed = 2f;
    [SerializeField] private GameObject _MoveChara;
    [SerializeField] private Camera _mainCamera;
    // Start is called before the first frame update
    [Button]
    void Start()
    {
        //ここ、もしくはゲームマネージャーの最初にこのスクリプトが終わるまで待機するscriptを書いてください

        _mainCamera.enabled = false; //メインカメラを一時的に切る
        _startCamera.enabled = true;//移動用カメラをonにする
        Instantiate(_MoveChara, new Vector3(4f, -1f, 4f), Quaternion.identity);//宇宙人を召喚
        StartCamera();
    }
    /// <summary>
    /// カメラ移動
    /// </summary>
    private async void StartCamera()
    {
        _startCamera.transform.position = new Vector3(0f,0f,1f); //スタートカメラのポジションリセット
        await UniTask.Delay(1000);//少し間を空ける
        await LMotion.Create(1f, -50f, _cameraSpeed).WithEase(Ease.Linear).BindToLocalPositionZ(transform).AddTo(_startCamera);//カメラをズームアウト
        StartGame();
    }
    /// <summary>
    /// ムーブが終わった後のカメラ処理
    /// ここの末尾にゲームを始める処理や、天井が落ちる処理を書いてください
    /// </summary>
    private void StartGame()
    {
        //メインカメラをonにする
        _mainCamera.enabled = true;
        //スタートカメラを切る
        _startCamera.enabled = false;
    }
}
