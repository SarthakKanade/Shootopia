namespace VelocityWars.Core
{
    /// <summary>
    /// Future-proofing interface for the Momentum Gauge system (Phase 2).
    /// Not implemented yet — define it now so systems can reference it safely.
    /// </summary>
    public interface IMomentum
    {
        float CurrentCharge { get; }
        void AddCharge(float amount);
        void ConsumeCharge(float amount);
    }
}
