using UnityEngine;

public class FadeSingleton<T> : MonoBehaviour where T : MonoBehaviour
{

    // インスタンス設定
    private static T i;
    public static T I
    {
        get
        {
            if (i == null)
            {
                i = (T)FindObjectOfType(typeof(T));

                if (i == null)
                {
                    Debug.LogError(typeof(T) + " is nothing");
                }
            }

            return i;
        }
    }

    // DontDestroyOnLoadで永続化、その他の場合は破棄する
    protected virtual void Awake()
    {
        if (this != I)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

}