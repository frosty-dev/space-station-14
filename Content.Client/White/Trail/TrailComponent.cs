using Robust.Shared.Map;
using System.Linq;

namespace Content.Client.White.Trail;

[RegisterComponent]
public sealed class TrailComponent : Component
{
    [ViewVariables]
    [DataField("trailData", required: true)]
    public TrailData Data { get; } = default!;
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("pointCreationDistanceThreshold")]
    public float PointCreationDistanceThreshold { get; set; } = 0.1f;
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("pointCreationMethod")]
    public PointCreationMethod PointCreationMethod { get; set; } = PointCreationMethod.OnMove;
}

[DataDefinition]
public sealed class TrailData
{
    private Vector2[] _pointOffsets = default!;

    [ViewVariables]
    public LinkedList<TrailPoint> Points { get; } = new();
    [ViewVariables]
    public float LifetimeAccumulator { get; set; } //не доживет до ошибок с плавающей точкой надеюсь
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("pointOffsets", required: true)]
    public Vector2[] PointOffsets
    {
        get => _pointOffsets;
        set {
            var offsets = new[] { Vector2.Zero, Vector2.Zero };
            for (int i = 0; i < offsets.Length; i++)
            {
                var el = value.ElementAtOrDefault(i);
                if (el != default)
                    offsets[i] = el;
            }
            _pointOffsets = offsets;
        }
    }
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("pointGravity")]
    public Vector2 PointGravity { get; set; } = Vector2.Zero;
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("pointRandomWalk")]
    public Vector2 PointRandomWalk { get; set; } = Vector2.Zero; //TODO: доделать
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("pointLifetime", required: true)]
    public float PointLifetime { get; set; }
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("texturePath", required: true)]
    public string TexurePath { get; set; } = string.Empty;
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("shaderSettings")]
    public TrailShaderSettings? ShaderSettings { get; set; }
    [ViewVariables(VVAccess.ReadWrite)]
    public Vector2 KostilOffset0 { get => PointOffsets[0]; set => PointOffsets[0] = value; }
    [ViewVariables(VVAccess.ReadWrite)]
    public Vector2 KostilOffset1 { get => PointOffsets[1]; set => PointOffsets[1] = value; }
}

[DataDefinition]
public sealed class TrailShaderSettings
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("shaderId", required: true)]
    public string ShaderId { get; set; } = string.Empty;
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("encodeFlowmapAsRG")]
    public bool EncodeFlowmapAsRG { get; set; } = false; //TODO: доделать когда надо будет
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("encodeLifetimeAsB")]
    public bool EncodeLifetimeAsB { get; set; } = false;
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
