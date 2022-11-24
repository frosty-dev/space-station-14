using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.DoAfter
{
    [NetworkedComponent()]
    public abstract class SharedDoAfterQTEComponent : Component
    {
    }

    [Serializable, NetSerializable]
    public sealed class DoAfterQTEComponentState : ComponentState
    {
        public List<ClientDoAfterQTE> DoAfters { get; }

        public DoAfterQTEComponentState(List<ClientDoAfterQTE> doAfters)
        {
            DoAfters = doAfters;
        }
    }

    [Serializable, NetSerializable]
    public sealed class CancelledDoAfterQTEMessage : EntityEventArgs
    {
        public EntityUid Uid;
        public byte ID { get; }

        public CancelledDoAfterQTEMessage(EntityUid uid, byte id)
        {
            Uid = uid;
            ID = id;
        }
    }

    [Serializable, NetSerializable]
    public sealed class FinishedDoAfterQTEMessage : EntityEventArgs
    {
        public EntityUid Uid;
        public byte ID { get; }

        public FinishedDoAfterQTEMessage(EntityUid uid, byte id)
        {
            Uid = uid;
            ID = id;
        }
    }

    // TODO: Merge this with the actual DoAfter
    /// <summary>
    ///     We send a trimmed-down version of the DoAfter for the client for it to use.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class ClientDoAfterQTE
    {
        public bool Cancelled = false;

        /// <summary>
        /// Accrued time when cancelled.
        /// </summary>
        public float CancelledAccumulator;

        // To see what these do look at DoAfter and DoAfterEventArgs
        public byte ID { get; }

        public TimeSpan StartTime { get; }

        public EntityCoordinates UserGrid { get; }

        public EntityCoordinates TargetGrid { get; }

        public EntityUid? Target { get; }

        public float Accumulator;

        public float Delay { get; }

        public QTEWindow[] QTEs { get; }

        // TODO: The other ones need predicting
        public bool BreakOnUserMove { get; }

        public bool BreakOnTargetMove { get; }

        public float MovementThreshold { get; }

        public FixedPoint2 DamageThreshold { get; }

        public ClientDoAfterQTE(byte id, EntityCoordinates userGrid, EntityCoordinates targetGrid, TimeSpan startTime,
            float delay, QTEWindow[] qtes, bool breakOnUserMove, bool breakOnTargetMove, float movementThreshold, FixedPoint2 damageThreshold, EntityUid? target = null)
        {
            ID = id;
            UserGrid = userGrid;
            TargetGrid = targetGrid;
            StartTime = startTime;
            Delay = delay;
            QTEs = qtes;
            BreakOnUserMove = breakOnUserMove;
            BreakOnTargetMove = breakOnTargetMove;
            MovementThreshold = movementThreshold;
            DamageThreshold = damageThreshold;
            Target = target;
        }
    }

    [Serializable, NetSerializable]
    public sealed class QTEWindow
    {
        public float Start { get; }
        public float? PerfectStart { get; }
        public float End { get; }

        /// <summary>
        /// не ну тупо ети реальные числа ето % от всей длительности действия
        /// </summary>
        public QTEWindow(float start, float end, float? perfectStart = null)
        {
            Start = start;
            PerfectStart = perfectStart;
            End = end;
        }

        /// <summary>
        /// 0 - не попадает
        /// 1 - попадает
        /// 2 - попадает в зону приколов
        /// </summary>
        public int InRange(float percentComplete)
        {
            if(percentComplete > End)
                return 0;
            if (percentComplete >= PerfectStart)
                return 2;
            if (percentComplete >= Start)
                return 1;
            return 0;
        }

    }

}
