namespace Content.Client.White.Trail;

[DataDefinition]
public sealed class TrailShaderSettings
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("shaderId", required: true)]
    public string ShaderId { get; set; } = string.Empty;
}
