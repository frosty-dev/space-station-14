using Robust.Shared.Map;

namespace Content.Client.White.Trail;

[RegisterComponent]
public sealed class TrailComponent : Component
{
    [ViewVariables]
    public TrailData Data { get; } = new TrailData();
    [ViewVariables(VVAccess.ReadWrite)]
    public float PointCreationDistanceThreshold { get; set; } = 0.1f;
    [ViewVariables(VVAccess.ReadWrite)]
    public PointCreationMethod PointCreationMethod { get; set; } = PointCreationMethod.OnMove;
}

public sealed class TrailData
{
    [ViewVariables]
    public LinkedList<TrailPoint> Points { get; } = new LinkedList<TrailPoint>();
    [ViewVariables]
    public float LifetimeAccumulator { get; set; } //не доживет до ошибок с плавающей точкой надеюсь
    [ViewVariables(VVAccess.ReadWrite)]
    public MapCoordinates LastPosition { get; set; }
    [ViewVariables(VVAccess.ReadWrite)]
    public Angle LastRotation { get; set; }
    [ViewVariables(VVAccess.ReadWrite)]
    public Vector2[] PointOffset { get; set; } = new Vector2[] { new(0.1f, 0.0f), new(-0.1f, 0.0f) };
    [ViewVariables(VVAccess.ReadWrite)]
    public Vector2 PointGravity { get; set; } = Vector2.Zero;// new Vector2(-0.001f, -0.005f);
    [ViewVariables(VVAccess.ReadWrite)]
    public Vector2 PointRandomWalk { get; set; } = Vector2.Zero; //TODO: доделать
    [ViewVariables(VVAccess.ReadWrite)]
    public float PointLifetime { get; set; } = 0.3f;
    [ViewVariables(VVAccess.ReadWrite)]
    public Vector2 KostilOffset0 { get => PointOffset[0]; set => PointOffset[0] = value; }
    [ViewVariables(VVAccess.ReadWrite)]
    public Vector2 KostilOffset1 { get => PointOffset[1]; set => PointOffset[1] = value; }
}

public sealed class TrailPoint
{
    public TrailPoint(MapCoordinates[] coords, float existTil)
    {
        Coords = coords;
        ExistTil = existTil;
    }

    [ViewVariables]
    public MapCoordinates[] Coords { get; set; }
    [ViewVariables]
    public float ExistTil { get; set; }
}

public enum PointCreationMethod : byte
{
    InFrameUpdate,
    OnMove
}
