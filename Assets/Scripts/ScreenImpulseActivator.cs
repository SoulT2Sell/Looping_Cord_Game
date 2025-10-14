using Unity.Cinemachine;
using UnityEngine;

public class ScreenImpulseActivator : MonoBehaviour
{
    private CinemachineImpulseSource impulseSource;

    private void Start()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();

        impulseSource.GenerateImpulse();
    }

}
