using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client.White.Trail;

public sealed class TrailSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public LinkedList<TrailData> DetachedTrails { get; } = new LinkedList<TrailData>();

    public override void Initialize()
    {
        base.Initialize();

        IoCManager.Resolve<IOverlayManager>().AddOverlay(
            new TrailOverlay(
                this,
                EntityManager,
                IoCManager.Resolve<IPrototypeManager>(),
                IoCManager.Resolve<IResourceCache>()
                ));

        SubscribeLocalEvent<TrailComponent, MoveEvent>(OnTrailMove);
        SubscribeLocalEvent<TrailComponent, ComponentRemove>(OnTrailRemove);
    }

    private void OnTrailRemove(EntityUid uid, TrailComponent comp, ComponentRemove args)
    {
        DetachedTrails.AddLast(comp.Data);
    }

    private void OnTrailMove(EntityUid uid, TrailComponent comp, ref MoveEvent args)
    {
        if (
            comp.Settings.СreationMethod != PointCreationMethod.OnMove
            || _gameTiming.InPrediction
            || args.NewPosition.InRange(EntityManager, args.OldPosition, comp.Settings.СreationDistanceThreshold)
            )
            return;

        TryAddPoint(comp, args.Component);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        foreach (var (comp, xform) in EntityQuery<TrailComponent, TransformComponent>())
        {
            var data = comp.Data;

            UpdateTrailData(data, frameTime);

            if (comp.Settings.СreationMethod == PointCreationMethod.OnFrameUpdate)
                TryAddPoint(comp, xform);

            RemoveExpiredPoints(data.Segments, data.LifetimeAccumulator);
            ProcessPoints(data);
        }

        var nextNode = DetachedTrails.First;
        while (nextNode != null)
        {
            var curNode = nextNode;
            nextNode = nextNode.Next;

            var data = curNode.Value;
            UpdateTrailData(data, frameTime);
            RemoveExpiredPoints(data.Segments, data.LifetimeAccumulator);
            if (data.Segments.Count > 0)
                ProcessPoints(data);
            else
                DetachedTrails.Remove(curNode);
        }
    }

    private void UpdateTrailData(TrailData data, float frameTime)
    {
        if (data.Segments.Last == null)
            data.LifetimeAccumulator = 0;
        else
            data.LifetimeAccumulator += frameTime;
    }

    private void RemoveExpiredPoints(LinkedList<TrailSegment> points, float trailLifetime)
    {
        while (points.First?.Value.ExistTil < trailLifetime)
            points.RemoveFirst();
    }

    private void ProcessPoints(TrailData data)
    {
        var lifetime = data.Settings.Lifetime;
        var gravity = data.Settings.Gravity;
        var maxRandomWalk = data.Settings.MaxRandomWalk;
        foreach (var segment in data.Segments)
        {
            var offset = gravity;
            if (maxRandomWalk != Vector2.Zero)
                offset += new Vector2(
                    maxRandomWalk.X * _random.NextFloat(0.0f, 1.0f),
                    maxRandomWalk.Y * _random.NextFloat(0.0f, 1.0f)
                    )
                    * (segment.ExistTil - data.LifetimeAccumulator) / lifetime;

            segment.Coords = segment.Coords.Offset(offset);
        }
    }

    private void TryAddPoint(TrailComponent comp, TransformComponent xform)
    {
        if (xform.MapID == MapId.Nullspace)
            return;

        var data = comp.Data;

        if (data.LastParentCoords.MapId != xform.MapID)
        {
            DetachedTrails.AddLast(data);
            comp.Data = new();
            comp.Settings = data.Settings;
        }

        var newPos = xform.MapPosition;
        var pointsList = data.Segments;

        if (pointsList.Last == null)
        {
            pointsList.AddLast(new TrailSegment(newPos, data.LifetimeAccumulator + data.Settings.Lifetime));
            return;
        }

        var coords = pointsList.Last.Value.Coords;
        if (newPos.InRange(coords, comp.Settings.СreationDistanceThreshold))
            return;

        pointsList.AddLast(new TrailSegment(newPos, data.LifetimeAccumulator + data.Settings.Lifetime));
    }
}
