using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    private static object syncObject = new object();

    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                lock (syncObject)
                {
                    instance = FindAnyObjectByType<T>();
                    if (instance == null)
                    {
                        GameObject obj = new GameObject();
                        obj.name = typeof(T).Name;
                        instance = obj.AddComponent<T>();
                    }
                }
            }

            return instance;
        }
    }

    protected virtual void Awake()
    {
        lock (syncObject)
        {
            if (instance == null)
            {
                instance = this as T;
            }
            else if (instance != this) 
            {
                Debug.LogWarning($"Multiple instances of {typeof(T).Name} detected. Destroying duplicate: {gameObject.name}");
                Destroy(gameObject);
            }
        }
    }

    private void OnDestroy()
    {
        lock (syncObject)
        {
            if (instance != null && instance == this)
            {
                instance = null;
            }
        }
    }
}