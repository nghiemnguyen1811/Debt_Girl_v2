using UnityEngine;

public abstract class SingletonMonobehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;

    public static T Instance
    {
        get
        {
            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (instance == null)
            instance = this as T;

        else Destroy(gameObject);
    }

    protected virtual void OnDestroy()
    {
        // When scene unloads or object removed → clear instance
        if (instance == this)
            instance = null;
    }
}
