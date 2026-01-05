public interface IStressEstimator
{
    float Stress01 { get; }
    void Tick(float dt);
    void Reset();
}

