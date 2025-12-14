using Unity.Cinemachine;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private void Start()
    {
        var vcam = GetComponent<CinemachineCamera>();
        var player = GameObject.FindGameObjectWithTag("Player");

        if (vcam != null && player != null)
        {
            vcam.Follow = player.transform;
        }
    }
}
