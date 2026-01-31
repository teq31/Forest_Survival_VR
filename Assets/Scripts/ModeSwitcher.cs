using UnityEngine;
using UnityEngine.XR;

public class ModeSwitcher : MonoBehaviour
{
    public GameObject xrOrigin;
    public GameObject desktopPlayer;
    public bool forceDesktopInEditor = true;

    void Start()
    {
        bool hasHMD = XRSettings.isDeviceActive;

#if UNITY_EDITOR
        if (forceDesktopInEditor)
            hasHMD = false;
#endif

        xrOrigin.SetActive(hasHMD);
        desktopPlayer.SetActive(!hasHMD);
    }
}
