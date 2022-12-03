namespace Content.Client.White.Trail;

[DataDefinition]
public sealed class TrailSettings
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("offset", required: true)]
    public Vector2 Offset { get; set; } = Vector2.UnitY;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("сreationDistanceThreshold")]
    public float СreationDistanceThreshold { get; set; } = 0.1f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("сreationMethod")]
    public PointCreationMethod СreationMethod { get; set; } = PointCreationMethod.OnMove;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("gravity")]
    public Vector2 Gravity { get; set; } = Vector2.Zero;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("lifetimeWidthMod")]
    public float? LifetimeWidthMod { get; set; }

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("randomWalk")]
    public Vector2 MaxRandomWalk { get; set; } = Vector2.Zero;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("lifetime", required: true)]
    public float Lifetime { get; set; }

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("texturePath", required: true)]
    public string TexurePath { get; set; } = string.Empty;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("textureColor")]
    public Color TexureColor { get; set; } = Color.White;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("shaderSettings")]
    public TrailShaderSettings? ShaderSettings { get; set; }
}
public enum PointCreationMethod : byte
{
    OnFrameUpdate,
    OnMove
}
