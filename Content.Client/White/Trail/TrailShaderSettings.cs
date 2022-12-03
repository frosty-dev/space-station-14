namespace Content.Client.White.Trail;

[DataDefinition]
public sealed class TrailShaderSettings
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("shaderId", required: true)]
    public string ShaderId { get; set; } = string.Empty;
    /*
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("encodeFlowmapAsRG")]
    public bool EncodeFlowmapAsRG { get; set; } = false; //TODO: доделать когда надо будет
    */

}
