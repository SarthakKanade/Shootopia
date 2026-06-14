namespace VelocityWars.Core
{
    /// <summary>
    /// Implement on anything that has a death state.
    /// Called by the object itself when health reaches zero.
    /// </summary>
    public interface IKillable
    {
        void OnDeath();
    }
}
