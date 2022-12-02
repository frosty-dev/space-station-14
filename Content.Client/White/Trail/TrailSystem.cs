using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Linq;

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
        if (comp.Settings.СreationMethod != PointCreationMethod.OnMove ||
            _gameTiming.InPrediction ||
            args.NewPosition.InRange(EntityManager, args.OldPosition, comp.Settings.СreationDistanceThreshold))
            return;
        comp.LastMovement = args.NewPosition.ToMapPos(EntityManager) - args.OldPosition.ToMapPos(EntityManager);
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

    private static void UpdateTrailData(TrailData data, float frameTime)
    {
        if (data.Segments.Last == null)
            data.LifetimeAccumulator = 0;
        else
            data.LifetimeAccumulator += frameTime;
    }

    private static void RemoveExpiredPoints(LinkedList<TrailSegment> points, float trailLifetime)
    {
        while (points.First?.Value.ExistTil < trailLifetime)
            points.RemoveFirst();
    }

    private static void ProcessPoints(TrailData data)
    {
        foreach (var point in data.Segments)
            point.Coords = point.Coords.Offset(data.Settings.Gravity/* + comp.PointRandomWalk*/);
    }

    private static void TryAddPoint(TrailComponent comp, TransformComponent xform)
    {
        if (xform.MapID == MapId.Nullspace)
            return;
        var newPos = xform.MapPosition;
        var data = comp.Data;
        var pointsList = data.Segments;

        if (pointsList.Last == null)
        {
            pointsList.AddLast(new TrailSegment(newPos, GetComponentSegmentCreationAngle(comp), data.LifetimeAccumulator + data.Settings.Lifetime));
            return;
        }

        var coords = pointsList.Last.Value.Coords;
        if (newPos.InRange(coords, comp.Settings.СreationDistanceThreshold))
            return;
        
        pointsList.AddLast(new TrailSegment(newPos, GetComponentSegmentCreationAngle(comp), data.LifetimeAccumulator + data.Settings.Lifetime));
    }

    public static (Vector2, Vector2) GetComponentTrailPoints(TrailComponent comp, TransformComponent xform)
    {
        var globalPos = xform.MapPosition.Position;
        var offsetForward = GetComponentSegmentCreationAngle(comp).RotateVec(comp.Settings.Offset);
        return (
            globalPos - offsetForward,
            globalPos + offsetForward
            );
    }

    public static (Vector2, Vector2) GetSegmentTrailPoints(TrailSegment segment, TrailSettings settings, float lifetimePercent)
    {
        var globalPos = segment.Coords.Position;
        var offsetForward = segment.Forward.RotateVec(settings.Offset);
        var finalWidthMod = 1f;
        if(settings.LifetimeWidthMod.HasValue)
            finalWidthMod = settings.LifetimeWidthMod.Value * lifetimePercent;
        return (
            globalPos - offsetForward * finalWidthMod,
            globalPos + offsetForward * finalWidthMod
            );
    }

    public static Angle GetComponentSegmentCreationAngle(TrailComponent comp)
    {
        var res = comp.LastMovement + comp.Settings.Gravity;
        if (res == Vector2.Zero)
            res = Vector2.UnitY;
        return res.ToWorldAngle();
    }

}