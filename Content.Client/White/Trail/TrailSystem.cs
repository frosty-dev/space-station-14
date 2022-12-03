using Content.Shared.White.Trail;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameStates;
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
        SubscribeLocalEvent<TrailComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, TrailComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not TrailComponentState state)
            return;

        component.Settings = state.Settings;
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

        TryAddSegment(comp, args.Component);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        foreach (var (comp, xform) in EntityQuery<TrailComponent, TransformComponent>())
        {
            var data = comp.Data;

            UpdateTrailData(data, frameTime, xform.MapPosition);

            if (comp.Settings.СreationMethod == PointCreationMethod.OnFrameUpdate)
                TryAddSegment(comp, xform);

            RemoveExpiredPoints(data.Segments, data.LifetimeAccumulator);
            ProcessSegmentMovement(data);
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
                ProcessSegmentMovement(data);
            else
                DetachedTrails.Remove(curNode);
        }
    }

    private void UpdateTrailData(TrailData data, float frameTime, MapCoordinates? parentCoord = null)
    {
        if (data.Segments.Last == null)
        {
            data.LifetimeAccumulator = 0;
            return;
        }
        data.LifetimeAccumulator += frameTime;
        data.UpdateDrawData(parentCoord);
    }

    private void RemoveExpiredPoints(LinkedList<TrailSegment> segment, float trailLifetime)
    {
        while (segment.First?.Value.ExistTil < trailLifetime)
            segment.RemoveFirst();
    }

    private void ProcessSegmentMovement(TrailData data)
    {
        var gravity = data.Settings.Gravity;
        var maxRandomWalk = data.Settings.MaxRandomWalk;

        foreach (var cur in data.CalculatedDrawData)
        {
            if (cur.Segment == null)
                continue;
            var curSegment = cur.Segment;

            var offset = gravity;
            if (maxRandomWalk != Vector2.Zero && cur.AngleRight != float.NaN)
            {
                var alignedWalk = cur.AngleRight.RotateVec(maxRandomWalk);
                offset += new Vector2(alignedWalk.X * _random.NextFloat(-1.0f, 1.0f), alignedWalk.Y * _random.NextFloat(-1.0f, 1.0f)) * cur.LifetimePercent;
            }

            curSegment.Coords = curSegment.Coords.Offset(offset);
        }
    }

    private void TryAddSegment(TrailComponent comp, TransformComponent xform)
    {
        if (xform.MapID == MapId.Nullspace)
            return;

        var data = comp.Data;
        var settings = data.Settings;
        if (data.LastParentCoords.HasValue && data.LastParentCoords.Value.MapId != xform.MapID)
        {
            DetachedTrails.AddLast(data);
            comp.Data = new();
            comp.Settings = data.Settings;
        }

        var newPos = xform.MapPosition.Offset(settings.Gravity * 0.01f);
        var segmentsList = data.Segments;

        if (segmentsList.Last == null)
        {
            segmentsList.AddLast(new TrailSegment(newPos, data.LifetimeAccumulator + data.Settings.Lifetime));
            return;
        }

        var coords = segmentsList.Last.Value.Coords;
        if (newPos.InRange(coords, comp.Settings.СreationDistanceThreshold))
            return;



        segmentsList.AddLast(new TrailSegment(newPos, data.LifetimeAccumulator + data.Settings.Lifetime));
    }
}
