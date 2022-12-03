using Content.Shared.White.Trail;
using Robust.Shared.Map;
using System.Linq;

namespace Content.Client.White.Trail;

[RegisterComponent]
public sealed class TrailComponent : SharedTrailComponent
{
    [ViewVariables]
    public TrailData? Data { get; set; } = null;
    [ViewVariables]
    public override TrailSettings Settings { get; set; } = TrailSettings.Default;
}

[DataDefinition]
public sealed class TrailData
{

    [ViewVariables]
    public TrailSettings Settings { get; set; } = default!;
    [ViewVariables]
    public LinkedList<TrailSegment> Segments { get; } = new();
    [ViewVariables]
    public float LifetimeAccumulator { get; set; } //не доживет до ошибок с плавающей точкой надеюсь
    [ViewVariables]
    public MapCoordinates? LastParentCoords { get; private set; } = null;
    [ViewVariables]
    public IEnumerable<TrailSegmentDrawData> CalculatedDrawData { get; private set; } = Enumerable.Empty<TrailSegmentDrawData>();

    public TrailData(TrailSettings settings)
    {
        Settings = settings;
    }

    public void UpdateDrawData(MapCoordinates? parentCoords = null)
    {
        if (parentCoords != null && parentCoords != MapCoordinates.Nullspace)
            LastParentCoords = parentCoords.Value;
        if (LastParentCoords.HasValue)
            CalculatedDrawData = EnumerateDrawData().ToArray();
    }

    private IEnumerable<TrailSegmentDrawData> EnumerateDrawData()
    {
        if (Segments.Last == null || LastParentCoords == null)
            yield break;

        var parentCoords = LastParentCoords.Value;
        var mapId = parentCoords.MapId;

        var baseOffset = Settings.Offset;

        yield return ConstructDrawData(
            0f,
            baseOffset,
            parentCoords.Position,
            Segments.Last.Value.Coords.Position,
            true);

        var curNode = Segments.Last;
        while (curNode != null)
        {
            var curSegment = curNode.Value;
            if (curSegment.Coords.MapId == mapId)
                yield return ConstructDrawData(
                    (curSegment.ExistTil - LifetimeAccumulator) / Settings.Lifetime,
                    baseOffset,
                    curSegment.Coords.Position,
                    curNode.Next?.Value.Coords.Position ?? parentCoords.Position,
                    segment: curSegment
                    );
            curNode = curNode.Previous;
        }
    }

    private static TrailSegmentDrawData ConstructDrawData(
        float lifetimePercent,
        Vector2 offset,
        Vector2 curPos,
        Vector2 nextPos,
        bool flipAngle = false,
        TrailSegment? segment = null
        )
    {
        var angle = (nextPos - curPos).ToWorldAngle();
        if (flipAngle)
            angle = angle.Opposite();
        var rotatedOffset = angle.RotateVec(offset);
        return new TrailSegmentDrawData(curPos - rotatedOffset, curPos + rotatedOffset, angle, lifetimePercent, segment);
    }
}

public struct TrailSegmentDrawData
{
    public readonly Vector2 Point1;
    public readonly Vector2 Point2;
    public readonly Angle AngleRight;
    public readonly float LifetimePercent;
    public readonly TrailSegment? Segment;

    public TrailSegmentDrawData(Vector2 point1, Vector2 point2, Angle angleLeft, float lifetimePercent, TrailSegment? segment = null)
    {
        Point1 = point1;
        Point2 = point2;
        AngleRight = angleLeft;
        LifetimePercent = lifetimePercent;
        Segment = segment;
    }
}

public sealed class TrailSegment
{
    public TrailSegment(MapCoordinates coords, float existTil)
    {
        Coords = coords;
        ExistTil = existTil;
    }

    [ViewVariables]
    public MapCoordinates Coords { get; set; }
    [ViewVariables]
    public float ExistTil { get; set; }
}
