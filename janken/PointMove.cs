using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitMotion;
using LitMotion.Extensions;
using Unity.VisualScripting;
using Cysharp.Threading.Tasks;

public class PointMove : MonoBehaviour
{
    [SerializeField] private Ease _ease;
    [SerializeField] private Ease _ease2;
    // Start is called before the first frame update
    void Start()
    {
        Popup();
    }

    private async void Popup()
    {
        LMotion.Create(transform.position.y, transform.position.y + 2f, 2f).WithEase(_ease).BindToLocalPositionY(transform).AddTo(gameObject);//ポイントオブジェクトを上に動かす
        await UniTask.Delay(500);//少し間を空ける
        await LMotion.Create(new Color(1, 1, 1, 1), new Color(1, 1, 1, 0), 1f).WithEase(_ease2).BindToColor(this.GetComponent<SpriteRenderer>()).AddTo(gameObject);//オブジェクトを徐々に透明にする
        Destroy(this.gameObject);//自身を削除する
    }
}
