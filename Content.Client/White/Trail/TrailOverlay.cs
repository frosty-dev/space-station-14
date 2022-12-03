using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.White.Trail;

public sealed class TrailOverlay : Overlay
{
    private readonly TrailSystem _system;
    private readonly IEntityManager _entManager;
    private readonly IPrototypeManager _protoManager;
    private readonly IResourceCache _cache;

    private readonly Dictionary<string, ShaderInstance?> _shaderDict;
    private readonly Dictionary<string, Texture?> _textureDict;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    public TrailOverlay(TrailSystem system, IEntityManager entManager, IPrototypeManager protoManager, IResourceCache cache)
    {
        _system = system;
        _entManager = entManager;
        _protoManager = protoManager;
        _cache = cache;
        _shaderDict = new();
        _textureDict = new();

        ZIndex = (int) Shared.DrawDepth.DrawDepth.Effects;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;

        foreach (var (comp, xform) in _entManager.EntityQuery<TrailComponent, TransformComponent>())
        {

            comp.Data.LastParentCoords = xform.MapPosition;
            ProcessTrailData(handle, comp.Data);
        }

        foreach (var data in _system.DetachedTrails)
            ProcessTrailData(handle, data);
    }

    private void ProcessTrailData(DrawingHandleBase handle, TrailData data)
    {
        if (data.Segments.Last == null)
            return;

        var mapId = data.LastParentCoords.MapId;
        if (mapId == MapId.Nullspace)
            return;

        var settings = data.Settings;
        if (settings.ShaderSettings != null)
            handle.UseShader(GetCachedShader(settings.ShaderSettings.ShaderId));

        (Vector2, Vector2) prevPointsTuple = OffsetByCoordDiff(
            settings.Offset,
            data.LastParentCoords.Position,
            data.Segments.Last.Value.Coords.Position,
            -1f
            );

        var nextNode = data.Segments.Last;
        while (nextNode != null)
        {
            var curNode = nextNode;
            nextNode = nextNode.Previous;

            var curSegment = curNode.Value;
            if (curSegment.Coords.MapId != mapId)
                continue;

            var lifetimePercent = (curSegment.ExistTil - data.LifetimeAccumulator) / settings.Lifetime;
            (Vector2, Vector2) curPointsTuple = OffsetByCoordDiff(
                settings.Offset,
                curSegment.Coords.Position,
                curNode.Next?.Value.Coords.Position ?? data.LastParentCoords.Position
                );

            var color = Color.InterpolateBetween(settings.ColorLifetimeMod, settings.ColorBase, lifetimePercent);

            var tex = GetCachedTexture(settings.TexurePath);
            if (tex != null)
                RenderTrailTexture(handle, prevPointsTuple, curPointsTuple, tex, color);
            //RenderTrailDebugBox(handle, prevPointsTuple.Value, curPointsTuple);

            prevPointsTuple = curPointsTuple;
        }
        handle.UseShader(null);
    }

    //влепить на ети два метода мемори кеш со слайдинг експирейшоном вместо дикта если проблемы будут
    private ShaderInstance? GetCachedShader(string id)
    {
        ShaderInstance? shader;
        if (_shaderDict.TryGetValue(id, out shader))
            return shader;
        if (_protoManager.TryIndex<ShaderPrototype>(id, out var shaderRes))
            shader = shaderRes?.InstanceUnique();
        _shaderDict.Add(id, shader);
        return shader;
    }

    private Texture? GetCachedTexture(string path)
    {
        Texture? texture;
        if (_textureDict.TryGetValue(path, out texture))
            return texture;
        if (_cache.TryGetResource<TextureResource>(path, out var texRes))
            texture = texRes;
        _textureDict.Add(path, texture);
        return texture;
    }

    private static (Vector2, Vector2) OffsetByCoordDiff(Vector2 offset, Vector2 curPos, Vector2 nextPos, float signSwapMultiplier = 1f)
    {
        var rotatedOffset = (nextPos - curPos).ToWorldAngle().RotateVec(offset) * signSwapMultiplier;
        return (curPos - rotatedOffset, curPos + rotatedOffset);
    }

    private static void RenderTrailTexture(DrawingHandleBase handle, (Vector2, Vector2) from, (Vector2, Vector2) to, Texture tex, Color color)
    {
        var verts = new DrawVertexUV2D[] {
            new (from.Item1, Vector2.Zero),
            new (from.Item2, Vector2.UnitY),
            new (to.Item2, Vector2.One),
            new (to.Item1, Vector2.UnitX),
        };

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, tex, verts, color);
    }

    private static void RenderTrailDebugBox(DrawingHandleBase handle, (Vector2, Vector2) from, (Vector2, Vector2) to)
    {
        handle.DrawLine(from.Item1, from.Item2, Color.White);
        handle.DrawLine(from.Item1, to.Item1, Color.White);
        handle.DrawLine(from.Item2, to.Item2, Color.White);
        handle.DrawLine(to.Item1, to.Item2, Color.Red);
    }
}
