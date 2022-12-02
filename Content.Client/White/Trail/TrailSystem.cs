using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Client.White.Trail;

public sealed class TrailSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

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
        if (comp.PointCreationMethod != PointCreationMethod.OnMove ||
            _gameTiming.InPrediction ||
            args.NewPosition.InRange(EntityManager, args.OldPosition, comp.PointCreationDistanceThreshold))
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

            if (comp.PointCreationMethod == PointCreationMethod.InFrameUpdate)
                TryAddPoint(comp, xform);

            RemoveExpiredPoints(data.Points, data.LifetimeAccumulator);
            ProcessPoints(data);
        }

        var nextNode = DetachedTrails.First;
        while (nextNode != null)
        {
            var curNode = nextNode;
            nextNode = nextNode.Next;

            var data = curNode.Value;
            UpdateTrailData(data, frameTime);
            RemoveExpiredPoints(data.Points, data.LifetimeAccumulator);
            if (data.Points.Count > 0)
                ProcessPoints(data);
            else
                DetachedTrails.Remove(curNode);
        }
    }

    private static void UpdateTrailData(TrailData data, float frameTime)
    {
        if (data.Points.Last == null)
            data.LifetimeAccumulator = 0;
        else
            data.LifetimeAccumulator += frameTime;
    }

    private static void RemoveExpiredPoints(LinkedList<TrailPoint> points, float trailLifetime)
    {
        while (points.First?.Value.ExistTil < trailLifetime)
            points.RemoveFirst();
    }

    private static void ProcessPoints(TrailData data)
    {
        foreach (var point in data.Points)
            for (int i = 0; i < point.Coords.Length; i++)
                point.Coords[i] = point.Coords[i].Offset(data.PointGravity/* + comp.PointRandomWalk*/);
    }

    private static void TryAddPoint(TrailComponent comp, TransformComponent xform)
    {
        if (xform.MapID == MapId.Nullspace)
            return;

        var newPointPos = GetMapCoordinatesWithOffset(comp, xform);
        var data = comp.Data;
        var pointsList = data.Points;

        var newPoints = newPointPos.ToArray();
        if (pointsList.Last == null)
        {
            pointsList.AddLast(new TrailPoint(newPoints, data.LifetimeAccumulator + data.PointLifetime));
            return;
        }

        var coords = pointsList.Last.Value.Coords;
        if (coords.Length != newPoints.Length)
            return;
        for (int i = 0; i < coords.Length; i++)
            if (newPoints[i].InRange(coords[i], comp.PointCreationDistanceThreshold))
                return;
        pointsList.AddLast(new TrailPoint(newPoints, data.LifetimeAccumulator + data.PointLifetime));
    }

    public static IEnumerable<MapCoordinates> GetMapCoordinatesWithOffset(TrailComponent comp, TransformComponent xform)
        => comp.Data.UsedPointOffsets.Select(x => xform.MapPosition.Offset(xform.WorldRotation.RotateVec(x)));
}
