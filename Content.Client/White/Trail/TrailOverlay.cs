using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using System.Linq;
using TerraFX.Interop.Windows;

namespace Content.Client.White.Trail;

public sealed class TrailOverlay : Overlay
{
    private readonly IEntityManager _entManager;
    //private readonly ShaderInstance _shader;
    private readonly Texture _barTexture;
    private readonly TrailSystem _system;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    public TrailOverlay(TrailSystem system, IEntityManager entManager, IPrototypeManager protoManager, IResourceCache cache)
    {
        ZIndex = 99;
        _system = system;
        _entManager = entManager;
        //_shader = protoManager.Index<ShaderPrototype>("TrailSmoothstepAlpha").InstanceUnique();
        _barTexture = cache.GetTexture("/Textures/White/Effects/Trails/trail.png");
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;

        //handle.UseShader(_shader);

        foreach (var (comp, xform) in _entManager.EntityQuery<TrailComponent, TransformComponent>(true))
            ProcessTrailData(handle, comp.Data, comp, xform);

        foreach (var data in _system.DetachedTrails)
            ProcessTrailData(handle, data);

        //handle.UseShader(null);
    }

    private void ProcessTrailData(DrawingHandleBase handle, TrailData data, TrailComponent? comp = null, TransformComponent? xform = null)
    {
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
                RenderTrailTexture(handle, prevPoint.Coords, curPoint.Coords, _barTexture, lifetimePercent);
                //RenderTrailDebugBox(handle, prevPoint.Coords, curPoint.Coords);
            }
            curNode = curNode.Previous;
        }
    }

    private static void RenderTrailTexture(DrawingHandleBase handle, ReadOnlySpan<MapCoordinates> from, ReadOnlySpan<MapCoordinates> to, Texture tex, float lifetimePercent)
    {
        var verts = new DrawVertexUV2D[] { //TODO: сделать нормально
            new (from[0].Position, Vector2.Zero),
            new (from[1].Position, Vector2.UnitY),
            new (to[1].Position, Vector2.One),
            new (to[0].Position, Vector2.UnitX),
        };

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, tex, verts, new Color(1f, 1f, 1f, lifetimePercent));
    }

    private static void RenderTrailDebugBox(DrawingHandleBase handle, ReadOnlySpan<MapCoordinates> from, ReadOnlySpan<MapCoordinates> to)
    {
        handle.DrawLine(from[0].Position, from[1].Position, Color.White);
        handle.DrawLine(from[0].Position, to[0].Position, Color.White);
        handle.DrawLine(from[1].Position, to[1].Position, Color.White);
        handle.DrawLine(to[0].Position, to[1].Position, Color.Red);
    }
}
