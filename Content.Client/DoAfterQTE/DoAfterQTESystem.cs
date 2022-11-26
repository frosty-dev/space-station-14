using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System;
using System.Linq;

namespace Content.Client.DoAfter
{
    /// <summary>
    /// Handles events that need to happen after a certain amount of time where the event could be cancelled by factors
    /// such as moving.
    /// </summary>
    [UsedImplicitly]
    public sealed class DoAfterQTESystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IPlayerManager _player = default!;

        /// <summary>
        ///     We'll use an excess time so stuff like finishing effects can show.
        /// </summary>
        public const float ExcessTime = 0.5f;

        public override void Initialize()
        {
            base.Initialize();
            UpdatesOutsidePrediction = true;
            SubscribeNetworkEvent<CancelledDoAfterQTEMessage>(OnCancelledDoAfter);
            SubscribeLocalEvent<DoAfterQTEComponent, ComponentHandleState>(OnDoAfterHandleState);
            IoCManager.Resolve<IOverlayManager>().AddOverlay(
                new DoAfterQTEOverlay(
                    EntityManager,
                    IoCManager.Resolve<IPrototypeManager>(),
                    IoCManager.Resolve<IResourceCache>()));

            //SubscribeLocalEvent<DoAfterQTEComponent, UseInHandEvent>((a,b,c) => OnTriggerEvent(a, b, c, QTETriggerEventTypes.UseInHand, c.User));
            SubscribeLocalEvent<DoAfterQTEComponent, InteractUsingEvent>((a, b, c) => OnQTETriggerEvent(a, b, c, QTETriggerEventTypes.InteractUsing, c.User, c.Used, c.Target));
        }

        public override void Shutdown()
        {
            base.Shutdown();
            IoCManager.Resolve<IOverlayManager>().RemoveOverlay<DoAfterQTEOverlay>();
        }

        private void OnDoAfterHandleState(EntityUid uid, DoAfterQTEComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not DoAfterQTEComponentState state)
                return;

            var toRemove = new RemQueue<ClientDoAfterQTE>();

            foreach (var (id, doAfter) in component.DoAfters)
            {
                var found = false;

                foreach (var clientdoAfter in state.DoAfters)
                {
                    if (clientdoAfter.ID == id)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    toRemove.Add(doAfter);
                }
            }

            foreach (var doAfter in toRemove)
            {
                Remove(component, doAfter);
            }

            foreach (var doAfter in state.DoAfters)
            {
                if (component.DoAfters.ContainsKey(doAfter.ID))
                    continue;

                component.DoAfters.Add(doAfter.ID, doAfter);
            }
        }

        private void OnCancelledDoAfter(CancelledDoAfterQTEMessage ev)
        {
            if (!TryComp<DoAfterQTEComponent>(ev.Uid, out var doAfter))
                return;

            Cancel(doAfter, ev.ID);
        }

        private void OnQTETriggerEvent(EntityUid uid, DoAfterQTEComponent component, HandledEntityEventArgs eventHandled, QTETriggerEventTypes triggerType, EntityUid user, EntityUid? used = null, EntityUid? target = null)
        {
            foreach (var (k, v) in component.DoAfters)
            {
                if (v.QTEScore.HasValue)
                    continue;
                if (!v.QTETriggers.HasFlag(triggerType))
                    continue;
                if (triggerType == QTETriggerEventTypes.InteractUsing && target != v.Target)
                    continue;
                eventHandled.Handled = true;
                TriggerQTE(component, v, k, v.Accumulator / v.Delay);
            }
        }

        public void TriggerQTE(DoAfterQTEComponent component, ClientDoAfterQTE doAfter, byte id, float percentComplete)
        {
            var qteScore = doAfter.QTEs.Max(x => x.InRange(percentComplete));
            doAfter.QTEScore = qteScore;

            RaiseNetworkEvent(new TriggeredDoAfterQTEMessage(component.Owner, id, percentComplete));

            if (qteScore > 0)
                Remove(component, doAfter);
        }

        /// <summary>
        ///     Remove a DoAfter without showing a cancellation graphic.
        /// </summary>
        public void Remove(DoAfterQTEComponent component, ClientDoAfterQTE clientDoAfter)
        {
            component.DoAfters.Remove(clientDoAfter.ID);

            var found = false;

            component.CancelledDoAfters.Remove(clientDoAfter.ID);

            if (!found)
                component.DoAfters.Remove(clientDoAfter.ID);
        }

        /// <summary>
        ///     Mark a DoAfter as cancelled and show a cancellation graphic.
        /// </summary>
        ///     Actual removal is handled by DoAfterEntitySystem.
        public void Cancel(DoAfterQTEComponent component, byte id)
        {
            if (component.CancelledDoAfters.ContainsKey(id))
                return;

            if (!component.DoAfters.ContainsKey(id))
                return;

            var doAfterMessage = component.DoAfters[id];
            doAfterMessage.Cancelled = true;
            component.CancelledDoAfters.Add(id, doAfterMessage);
        }

        // TODO separate DoAfter & ActiveDoAfter components for the entity query.
        public override void Update(float frameTime)
        {
            if (!_gameTiming.IsFirstTimePredicted)
                return;

            var playerEntity = _player.LocalPlayer?.ControlledEntity;

            foreach (var (comp, xform) in EntityQuery<DoAfterQTEComponent, TransformComponent>())
            {
                var doAfters = comp.DoAfters;

                if (doAfters.Count == 0)
                {
                    continue;
                }

                var userGrid = xform.Coordinates;
                var toRemove = new RemQueue<ClientDoAfterQTE>();

                // Check cancellations / finishes
                foreach (var (id, doAfter) in doAfters)
                {
                    // If we've passed the final time (after the excess to show completion graphic) then remove.
                    if ((doAfter.Accumulator + doAfter.CancelledAccumulator) > doAfter.Delay + ExcessTime)
                    {
                        toRemove.Add(doAfter);
                        continue;
                    }

                    if (doAfter.Cancelled)
                    {
                        doAfter.CancelledAccumulator += frameTime;
                        continue;
                    }

                    doAfter.Accumulator += frameTime;

                    // Well we finished so don't try to predict cancels.
                    if (doAfter.Accumulator > doAfter.Delay)
                    {
                        continue;
                    }

                    // Predictions
                    if (comp.Owner != playerEntity)
                        continue;

                    // TODO: Add these back in when I work out some system for changing the accumulation rate
                    // based on ping. Right now these would show as cancelled near completion if we moved at the end
                    // despite succeeding.
                    continue;

                    if (doAfter.BreakOnUserMove)
                    {
                        if (!userGrid.InRange(EntityManager, doAfter.UserGrid, doAfter.MovementThreshold))
                        {
                            Cancel(comp, id);
                            continue;
                        }
                    }

                    if (doAfter.BreakOnTargetMove)
                    {
                        if (!EntityManager.Deleted(doAfter.Target) &&
                            !Transform(doAfter.Target.Value).Coordinates.InRange(EntityManager, doAfter.TargetGrid,
                                doAfter.MovementThreshold))
                        {
                            Cancel(comp, id);
                            continue;
                        }
                    }
                }

                foreach (var doAfter in toRemove)
                {
                    Remove(comp, doAfter);
                }

                // Remove cancelled DoAfters after ExcessTime has elapsed
                var toRemoveCancelled = new List<ClientDoAfterQTE>();

                foreach (var (_, doAfter) in comp.CancelledDoAfters)
                {
                    if (doAfter.CancelledAccumulator > ExcessTime)
                    {
                        toRemoveCancelled.Add(doAfter);
                    }
                }

                foreach (var doAfter in toRemoveCancelled)
                {
                    Remove(comp, doAfter);
                }
            }
        }
    }
}
