using UnityEngine;

public class NonVRStressEstimator : IStressEstimator
{
    public float Stress01 { get; private set; }

    readonly Transform _camera;

    // Calibration ranges
    readonly float _maxCamAngularDegPerSec;
    readonly float _maxCamJitter;
    readonly float _maxDirChangeRate; // changes/sec

    readonly float _alpha;

    Vector2 _anglesPrev;       // (yaw, pitch)
    float _camOmegaSmoothed;

    // Direction change rate over a sliding window
    readonly float _windowSec;
    float _windowTimer;
    int _dirChangesInWindow;

    Vector2 _dirPrev;

    public NonVRStressEstimator(
        Transform cameraTransform,
        float alpha = 0.12f,
        float windowSec = 1.0f,
        float maxCamAngularDegPerSec = 600f,
        float maxCamJitter = 200f,
        float maxDirChangeRate = 8f)
    {
        _camera = cameraTransform;
        _alpha = Mathf.Clamp(alpha, 0.01f, 0.3f);

        _windowSec = Mathf.Max(0.25f, windowSec);

        _maxCamAngularDegPerSec = maxCamAngularDegPerSec;
        _maxCamJitter = maxCamJitter;
        _maxDirChangeRate = maxDirChangeRate;
    }

    public void Reset()
    {
        Stress01 = 0f;

        _anglesPrev = GetYawPitch(_camera);
        _camOmegaSmoothed = 0f;

        _windowTimer = 0f;
        _dirChangesInWindow = 0;

        _dirPrev = ReadMoveDir();
    }

    public void Tick(float dt)
    {
        dt = Mathf.Max(dt, 0.0001f);

        // 1) Camera angular velocity proxy
        Vector2 angles = GetYawPitch(_camera);
        Vector2 delta = DeltaAngles(angles, _anglesPrev);
        float camOmega = delta.magnitude / dt; // deg/s
        _anglesPrev = angles;

        // 2) Camera jitter
        _camOmegaSmoothed = Mathf.Lerp(_camOmegaSmoothed, camOmega, _alpha);
        float camJitter = Mathf.Abs(camOmega - _camOmegaSmoothed);

        // 3) Direction-change rate proxy
        Vector2 dir = ReadMoveDir();
        if (dir != _dirPrev)
        {
            _dirChangesInWindow++;
            _dirPrev = dir;
        }

        _windowTimer += dt;
        if (_windowTimer >= _windowSec)
        {
            // reset window (simple; you can make it true sliding later)
            _windowTimer = 0f;
            _dirChangesInWindow = 0;
        }

        // Approx changes/sec
        float dirChangeRate = (_windowSec > 0f) ? (_dirChangesInWindow / _windowSec) : 0f;

        // Normalize
        float camOmega01 = Mathf.Clamp01(camOmega / _maxCamAngularDegPerSec);
        float camJitter01 = Mathf.Clamp01(camJitter / _maxCamJitter);
        float dirRate01 = Mathf.Clamp01(dirChangeRate / _maxDirChangeRate);

        // Same weights as VR/doc
        float stressRaw = 0.45f * camOmega01 + 0.25f * camJitter01 + 0.30f * dirRate01;

        Stress01 = Mathf.Lerp(Stress01, stressRaw, _alpha);
    }

    static Vector2 ReadMoveDir()
    {
        // WASD -> (-1..1, -1..1). Uses old Input for compatibility.
        float x = 0f, y = 0f;
        if (Input.GetKey(KeyCode.A)) x -= 1f;
        if (Input.GetKey(KeyCode.D)) x += 1f;
        if (Input.GetKey(KeyCode.S)) y -= 1f;
        if (Input.GetKey(KeyCode.W)) y += 1f;
        return new Vector2(x, y);
    }

    static Vector2 GetYawPitch(Transform cam)
    {
        if (cam == null) return Vector2.zero;
        Vector3 e = cam.rotation.eulerAngles;
        return new Vector2(e.y, e.x); // yaw, pitch
    }

    static Vector2 DeltaAngles(Vector2 a, Vector2 b)
    {
        // Handle wrap-around at 360 deg
        float dy = Mathf.DeltaAngle(b.x, a.x);
        float dp = Mathf.DeltaAngle(b.y, a.y);
        return new Vector2(dy, dp);
    }
}
