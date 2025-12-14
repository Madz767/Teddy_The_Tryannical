using UnityEngine;

public class Cameramanager : MonoBehaviour
{
    private static Cameramanager instance;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
