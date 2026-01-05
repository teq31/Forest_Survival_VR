using UnityEngine;

public class VRStressEstimator : IStressEstimator
{
    public float Stress01 { get; private set; }

    readonly Transform _hmd;
    readonly Transform _left;
    readonly Transform _right;

    // Tune ranges in playtests
    readonly float _maxHeadAngularDegPerSec;
    readonly float _maxHeadJitter;
    readonly float _maxHandJerk;

    readonly float _alpha;

    Quaternion _hmdRotPrev;
    float _headOmegaSmoothed;

    Vector3 _lPosPrev, _rPosPrev;
    Vector3 _lVelPrev, _rVelPrev;
    Vector3 _lAccPrev, _rAccPrev;

    public VRStressEstimator(
        Transform hmd,
        Transform left,
        Transform right,
        float alpha = 0.12f,
        float maxHeadAngularDegPerSec = 220f,
        float maxHeadJitter = 60f,
        float maxHandJerk = 35f)
    {
        _hmd = hmd;
        _left = left;
        _right = right;

        _alpha = Mathf.Clamp(alpha, 0.01f, 0.3f);

        _maxHeadAngularDegPerSec = maxHeadAngularDegPerSec;
        _maxHeadJitter = maxHeadJitter;
        _maxHandJerk = maxHandJerk;
    }

    public void Reset()
    {
        Stress01 = 0f;

        if (_hmd != null) _hmdRotPrev = _hmd.rotation;
        _headOmegaSmoothed = 0f;

        if (_left != null) _lPosPrev = _left.position;
        if (_right != null) _rPosPrev = _right.position;

        _lVelPrev = _rVelPrev = Vector3.zero;
        _lAccPrev = _rAccPrev = Vector3.zero;
    }

    public void Tick(float dt)
    {
        dt = Mathf.Max(dt, 0.0001f);

        // 1) Head angular velocity (deg/s)
        float headOmega = 0f;
        if (_hmd != null)
        {
            float angle = Quaternion.Angle(_hmdRotPrev, _hmd.rotation);
            headOmega = angle / dt;
            _hmdRotPrev = _hmd.rotation;
        }

        // 2) Head jitter = |current omega - smoothed omega|
        _headOmegaSmoothed = Mathf.Lerp(_headOmegaSmoothed, headOmega, _alpha);
        float headJitter = Mathf.Abs(headOmega - _headOmegaSmoothed);

        // 3) Hand jerk (max of both)
        float leftJerk = ComputeHandJerk(_left, ref _lPosPrev, ref _lVelPrev, ref _lAccPrev, dt);
        float rightJerk = ComputeHandJerk(_right, ref _rPosPrev, ref _rVelPrev, ref _rAccPrev, dt);
        float handJerk = Mathf.Max(leftJerk, rightJerk);

        // Normalize to 0..1
        float headOmega01 = Mathf.Clamp01(headOmega / _maxHeadAngularDegPerSec);
        float headJitter01 = Mathf.Clamp01(headJitter / _maxHeadJitter);
        float handJerk01 = Mathf.Clamp01(handJerk / _maxHandJerk);

        // Weighted sum (same as doc)
        float stressRaw = 0.45f * headOmega01 + 0.25f * headJitter01 + 0.30f * handJerk01;

        // Smooth output to avoid flicker
        Stress01 = Mathf.Lerp(Stress01, stressRaw, _alpha);
    }

    static float ComputeHandJerk(
        Transform hand,
        ref Vector3 posPrev,
        ref Vector3 velPrev,
        ref Vector3 accPrev,
        float dt)
    {
        if (hand == null) return 0f;

        Vector3 pos = hand.position;
        Vector3 vel = (pos - posPrev) / dt;
        Vector3 acc = (vel - velPrev) / dt;
        Vector3 jerk = (acc - accPrev) / dt;

        posPrev = pos;
        velPrev = vel;
        accPrev = acc;

        return jerk.magnitude;
    }
}

