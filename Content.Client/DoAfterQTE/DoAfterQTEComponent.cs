using Content.Shared.DoAfter;

namespace Content.Client.DoAfter
{
    [RegisterComponent, Access(typeof(DoAfterQTESystem))]
    public sealed class DoAfterQTEComponent : SharedDoAfterQTEComponent
    {
        public readonly Dictionary<byte, ClientDoAfterQTE> DoAfters = new();

        public readonly Dictionary<byte, ClientDoAfterQTE> CancelledDoAfters = new();
    }
}
