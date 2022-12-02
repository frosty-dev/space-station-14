using Robust.Shared.Map;

namespace Content.Client.White.Trail;

[RegisterComponent]
public sealed class TrailComponent : Component
{
    [ViewVariables]
    public TrailData Data { get; } = new();
    [ViewVariables]
    public Vector2 LastMovement { get; set; } = Vector2.Zero;
    [ViewVariables]
    [DataField("settings", required: true)]
    public TrailSettings Settings { get => Data.Settings; set => Data.Settings = value; } //капец впадлу пилить кастомный сериализатор
}

[DataDefinition]
public sealed class TrailData
{
    [ViewVariables]
    public LinkedList<TrailSegment> Segments { get; } = new();
    [ViewVariables]
    public float LifetimeAccumulator { get; set; } //не доживет до ошибок с плавающей точкой надеюсь
    [ViewVariables]
    public TrailSettings Settings { get; set; } = default!;
}

public sealed class TrailSegment
{
    public TrailSegment(MapCoordinates coords, Angle forward, float existTil)
    {
        Coords = coords;
        Forward = forward;
        ExistTil = existTil;
    }

    [ViewVariables]
    public MapCoordinates Coords { get; set; }
    [ViewVariables]
    public Angle Forward { get; set; }
    [ViewVariables]
    public float ExistTil { get; set; }
}
