using Robust.Shared.Map;

namespace Content.Server.Movement.Components;

[RegisterComponent]
public sealed class LagCompensationComponent : Component
{
    [ViewVariables]
    public readonly Queue<ValueTuple<TimeSpan, EntityCoordinates, Angle>> Positions = new();
}
