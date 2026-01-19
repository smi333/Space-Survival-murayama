using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartUIPopup : MonoBehaviour
{
    [SerializeField,Label("Linear")] private Ease _ease;　//イージング用今はライナーにしておく


    // Start is called before the first frame update
    void Start()
    {
        popup();
    }
    [Button]
    private async void popup()
    {
        //ちょっと大きく膨らむ
        await LMotion.Create(new Vector3(0.2f, 0.2f, 0.2f), new Vector3(0.8f, 0.8f, 0.8f), 0.1f).WithEase(_ease).BindToLocalScale(transform).AddTo(gameObject);
        //ちょっと戻る
        await LMotion.Create(new Vector3(0.8f, 0.8f, 0.8f), new Vector3(0.6f, 0.6f, 0.6f), 0.1f).WithEase(_ease).BindToLocalScale(transform).AddTo(gameObject);
        await UniTask.Delay(500);//少し間を空ける
        //引っ込む
        await LMotion.Create(new Vector3(0.6f, 0.6f, 0.6f), new Vector3(0f, 0f, 0f), 0.1f).WithEase(_ease).BindToLocalScale(transform).AddTo(gameObject);
    }
}
