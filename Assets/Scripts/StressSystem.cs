using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

public class StressSystem : MonoBehaviour
{
    [Header("VR references (XR Origin)")]
    public Transform hmd;
    public Transform leftController;
    public Transform rightController;

    [Header("Non-VR reference")]
    public Transform desktopCamera;

    [Header("Debug")]
    public bool forceNonVR = false;
    public bool forceVR = false;

    public float Stress01 => _active?.Stress01 ?? 0f;
    public bool IsVRActive { get; private set; }

    IStressEstimator _vr;
    IStressEstimator _nonVr;
    IStressEstimator _active;

    void Start()
    {
        _vr = new VRStressEstimator(hmd, leftController, rightController);
        _nonVr = new NonVRStressEstimator(desktopCamera);

        SelectMode();
        _active?.Reset();
    }

    void Update()
    {
        // Re-check occasionally (you can also do it on events)
        SelectMode();

        _active?.Tick(Time.deltaTime);
    }

    void SelectMode()
    {
        bool xrActive = IsXRRunning();

        if (forceNonVR) xrActive = false;
        if (forceVR) xrActive = true;

        if (xrActive && _active != _vr)
        {
            IsVRActive = true;
            _active = _vr;
            _active.Reset();
        }
        else if (!xrActive && _active != _nonVr)
        {
            IsVRActive = false;
            _active = _nonVr;
            _active.Reset();
        }
    }

    static bool IsXRRunning()
    {
        // Combination is more robust across setups
        bool deviceActive = XRSettings.isDeviceActive;

        bool loaderActive = false;
        var gs = XRGeneralSettings.Instance;
        if (gs != null && gs.Manager != null)
            loaderActive = gs.Manager.activeLoader != null;

        return deviceActive || loaderActive;
    }
}
