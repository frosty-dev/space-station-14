using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using System.Linq;

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

        foreach (var (comp, xform) in _entManager.EntityQuery<TrailComponent, TransformComponent>(true))
            ProcessTrailData(handle, comp.Data, comp, xform);

        foreach (var data in _system.DetachedTrails)
            ProcessTrailData(handle, data);
    }

    private void ProcessTrailData(DrawingHandleBase handle, TrailData data, TrailComponent? comp = null, TransformComponent? xform = null)
    {
        if(data.ShaderSettings != null)
            handle.UseShader(GetCachedShader(data.ShaderSettings.ShaderId));

        var curNode = data.Points.Last;
        while (curNode != null)
        {
            var curPoint = curNode.Value;
            var prevPoint = curNode.Next?.Value;
            if(prevPoint == null && comp != null && xform != null)
                prevPoint = new TrailPoint(TrailSystem.GetMapCoordinatesWithOffset(comp, xform).ToArray(), data.LifetimeAccumulator + data.PointLifetime);
            if(prevPoint != null)
            {
                var lifetimePercent = (curPoint.ExistTil - data.LifetimeAccumulator) / data.PointLifetime;
                var color = Color.White;
                if(data.ShaderSettings != null && data.ShaderSettings.EncodeLifetimeAsB)
                    color.B = lifetimePercent;
                else
                    color.A = lifetimePercent;

                var tex = GetCachedTexture(data.TexurePath);
                if (tex != null)
                    RenderTrailTexture(handle, prevPoint.Coords, curPoint.Coords, tex, color);
                //RenderTrailDebugBox(handle, prevPoint.Coords, curPoint.Coords);
            }
            curNode = curNode.Previous;
        }
        handle.UseShader(null);
    }

    //влепить на ети два метода мемори кеш со слайдинг експирейшоном вместо дикта если проблемы будут
    private ShaderInstance? GetCachedShader(string id)
    {
        ShaderInstance? shader = null;
        if (_shaderDict.TryGetValue(id, out shader))
            return shader;
        if (_protoManager.TryIndex<ShaderPrototype>(id, out var shaderRes))
            shader = shaderRes?.InstanceUnique();
        _shaderDict.Add(id, shader);
        return shader;
    }

    private Texture? GetCachedTexture(string path)
    {
        Texture? texture = null;
        if (_textureDict.TryGetValue(path, out texture))
            return texture;
        if(_cache.TryGetResource<TextureResource>(path, out var texRes))
            texture = texRes;
        _textureDict.Add(path, texture);
        return texture;
    }

    private static void RenderTrailTexture(DrawingHandleBase handle, ReadOnlySpan<MapCoordinates> from, ReadOnlySpan<MapCoordinates> to, Texture tex, Color color)
    {
        var verts = new DrawVertexUV2D[] { //TODO: сделать нормально
            new (from[0].Position, Vector2.Zero),
            new (from[1].Position, Vector2.UnitY),
            new (to[1].Position, Vector2.One),
            new (to[0].Position, Vector2.UnitX),
        };

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, tex, verts, color);
    }

    private static void RenderTrailDebugBox(DrawingHandleBase handle, ReadOnlySpan<MapCoordinates> from, ReadOnlySpan<MapCoordinates> to)
    {
        handle.DrawLine(from[0].Position, from[1].Position, Color.White);
        handle.DrawLine(from[0].Position, to[0].Position, Color.White);
        handle.DrawLine(from[1].Position, to[1].Position, Color.White);
        handle.DrawLine(to[0].Position, to[1].Position, Color.Red);
    }
}
