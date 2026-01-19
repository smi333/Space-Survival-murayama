using LitMotion;
using LitMotion.Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorPopup : MonoBehaviour
{
    private void Start()
    {
        LMotion.Create(0.8f, 1f, 0.2f).WithEase(Ease.Linear).BindToLocalScaleX(this.transform).AddTo(gameObject);
        LMotion.Create(0.8f, 1f, 0.2f).WithEase(Ease.Linear).BindToLocalScaleY(this.transform).AddTo(gameObject);
    }
    public void destroy()
    {
        Destroy(this.gameObject);
    }
}
