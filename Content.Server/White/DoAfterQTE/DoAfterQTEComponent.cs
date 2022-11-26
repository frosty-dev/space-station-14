using Content.Shared.DoAfter;

namespace Content.Server.DoAfter
{
    [RegisterComponent, Access(typeof(DoAfterQTESystem))]
    public sealed class DoAfterQTEComponent : SharedDoAfterQTEComponent
    {
        public readonly Dictionary<DoAfterQTE, byte> DoAfters = new();

        // So the client knows which one to update (and so we don't send all of the do_afters every time 1 updates)
        // we'll just send them the index. Doesn't matter if it wraps around.
        public byte RunningIndex;
    }

    /// <summary>
    ///     Added to entities that are currently performing any doafters.
    /// </summary>
    [RegisterComponent]
    public sealed class ActiveDoAfterQTEComponent : Component {}
}
