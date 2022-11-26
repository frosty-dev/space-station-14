using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.MobState;
using JetBrains.Annotations;
using Robust.Shared.GameStates;

namespace Content.Server.DoAfter
{
    [UsedImplicitly]
    public sealed class DoAfterQTESystem : EntitySystem
    {
        // We cache these lists as to not allocate them every update tick...
        private readonly Queue<DoAfterQTE> _cancelled = new();
        private readonly Queue<DoAfterQTE> _finished = new();

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DoAfterQTEComponent, DamageChangedEvent>(OnDamage);
            SubscribeLocalEvent<DoAfterQTEComponent, MobStateChangedEvent>(OnStateChanged);
            SubscribeLocalEvent<DoAfterQTEComponent, ComponentGetState>(OnDoAfterGetState);
            SubscribeNetworkEvent<TriggeredDoAfterQTEMessage>(OnTriggeredDoAfter);
        }

        private void OnTriggeredDoAfter(TriggeredDoAfterQTEMessage msg)
        {
            var component = EntityManager.GetComponent<DoAfterQTEComponent>(msg.Uid);

            DoAfterQTE? doAfter = null;
            foreach (var (k, v) in component.DoAfters)
            {
                if (v == msg.ID)
                {
                    if(k.QTEScore == null)
                        doAfter = k;
                    break;
                }
            }
            if (doAfter == null)
                return;
            Triggered(component, doAfter, msg.PercentComplete);
        }

        private void Triggered(DoAfterQTEComponent component, DoAfterQTE doAfter, float percentComplete)
        {
            var qtes = doAfter.EventArgs.QTEs;
            var qteScore = qtes.Max(x => x.InRange(percentComplete));
            doAfter.QTEScore = qteScore;

            if (qteScore > 0)
                doAfter.Finish();
        }

        public void Add(DoAfterQTEComponent component, DoAfterQTE doAfter)
        {
            component.DoAfters.Add(doAfter, component.RunningIndex);
            EnsureComp<ActiveDoAfterQTEComponent>(component.Owner);
            component.RunningIndex++;
            Dirty(component);
        }

        public void Cancelled(DoAfterQTEComponent component, DoAfterQTE doAfter)
        {
            if (!component.DoAfters.TryGetValue(doAfter, out var index))
                return;

            component.DoAfters.Remove(doAfter);

            if (component.DoAfters.Count == 0)
            {
                RemComp<ActiveDoAfterQTEComponent>(component.Owner);
            }

            RaiseNetworkEvent(new CancelledDoAfterQTEMessage(component.Owner, index));
        }

        /// <summary>
        ///     Call when the particular DoAfter is finished.
        /// </summary>
        public void Finished(DoAfterQTEComponent component, DoAfterQTE doAfter)
        {
            if (!component.DoAfters.TryGetValue(doAfter, out var index))
                return;

            component.DoAfters.Remove(doAfter);

            if (component.DoAfters.Count == 0)
            {
                RemComp<ActiveDoAfterQTEComponent>(component.Owner);
            }
        }

        private void OnDoAfterGetState(EntityUid uid, DoAfterQTEComponent component, ref ComponentGetState args)
        {
            var toAdd = new List<ClientDoAfterQTE>(component.DoAfters.Count);

            foreach (var (doAfter, _) in component.DoAfters)
            {
                // THE ALMIGHTY PYRAMID
                var clientDoAfter = new ClientDoAfterQTE(
                    component.DoAfters[doAfter],
                    doAfter.UserGrid,
                    doAfter.TargetGrid,
                    doAfter.StartTime,
                    doAfter.EventArgs.Delay,
                    doAfter.EventArgs.QTEs,
                    doAfter.EventArgs.QTETriggers,
                    doAfter.EventArgs.BreakOnUserMove,
                    doAfter.EventArgs.BreakOnTargetMove,
                    doAfter.EventArgs.MovementThreshold,
                    doAfter.EventArgs.DamageThreshold,
                    doAfter.EventArgs.Target);

                toAdd.Add(clientDoAfter);
            }

            args.State = new DoAfterQTEComponentState(toAdd);
        }

        private void OnStateChanged(EntityUid uid, DoAfterQTEComponent component, MobStateChangedEvent args)
        {
            if (!args.CurrentMobState.IsIncapacitated())
                return;

            foreach (var (doAfter, _) in component.DoAfters)
            {
                doAfter.Cancel();
            }
        }

        /// <summary>
        /// Cancels DoAfter if it breaks on damage and it meets the threshold
        /// </summary>
        /// <param name="_">
        /// The EntityUID of the user
        /// </param>
        /// <param name="component"></param>
        /// <param name="args"></param>
        public void OnDamage(EntityUid _, DoAfterQTEComponent component, DamageChangedEvent args)
        {
            if (!args.InterruptsDoAfters || !args.DamageIncreased || args.DamageDelta == null)
                return;

            foreach (var (doAfter, _) in component.DoAfters)
            {
                if (doAfter.EventArgs.BreakOnDamage && args.DamageDelta?.Total.Float() > doAfter.EventArgs.DamageThreshold)
                {
                    doAfter.Cancel();
                }
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var (_, comp) in EntityManager.EntityQuery<ActiveDoAfterQTEComponent, DoAfterQTEComponent>())
            {
                foreach (var (doAfter, _) in comp.DoAfters.ToArray())
                {
                    doAfter.Run(frameTime, EntityManager);

                    switch (doAfter.Status)
                    {
                        case DoAfterQTEStatus.Running:
                            break;
                        case DoAfterQTEStatus.Cancelled:
                            _cancelled.Enqueue(doAfter);
                            break;
                        case DoAfterQTEStatus.Finished:
                            _finished.Enqueue(doAfter);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                while (_cancelled.TryDequeue(out var doAfter))
                {
                    Cancelled(comp, doAfter);

                    if(EntityManager.EntityExists(doAfter.EventArgs.User) && doAfter.EventArgs.UserCancelledEvent != null)
                        RaiseLocalEvent(doAfter.EventArgs.User, doAfter.EventArgs.UserCancelledEvent, false);

                    if (doAfter.EventArgs.Used is {} used && EntityManager.EntityExists(used) && doAfter.EventArgs.UsedCancelledEvent != null)
                        RaiseLocalEvent(used, doAfter.EventArgs.UsedCancelledEvent);

                    if(doAfter.EventArgs.Target is {} target && EntityManager.EntityExists(target) && doAfter.EventArgs.TargetCancelledEvent != null)
                        RaiseLocalEvent(target, doAfter.EventArgs.TargetCancelledEvent, false);

                    if(doAfter.EventArgs.BroadcastCancelledEvent != null)
                        RaiseLocalEvent(doAfter.EventArgs.BroadcastCancelledEvent);
                }

                while (_finished.TryDequeue(out var doAfter))
                {
                    Finished(comp, doAfter);

                    if(EntityManager.EntityExists(doAfter.EventArgs.User) && doAfter.EventArgs.UserFinishedEvent != null)
                        RaiseLocalEvent(doAfter.EventArgs.User, doAfter.EventArgs.UserFinishedEvent, false);

                    if(doAfter.EventArgs.Used is {} used && EntityManager.EntityExists(used) && doAfter.EventArgs.UsedFinishedEvent != null)
                        RaiseLocalEvent(used, doAfter.EventArgs.UsedFinishedEvent);

                    if(doAfter.EventArgs.Target is {} target && EntityManager.EntityExists(target) && doAfter.EventArgs.TargetFinishedEvent != null)
                        RaiseLocalEvent(target, doAfter.EventArgs.TargetFinishedEvent, false);

                    if(doAfter.EventArgs.BroadcastFinishedEvent != null)
                        RaiseLocalEvent(doAfter.EventArgs.BroadcastFinishedEvent);
                }
            }
        }

        /// <summary>
        ///     Tasks that are delayed until the specified time has passed
        ///     These can be potentially cancelled by the user moving or when other things happen.
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        public async Task<DoAfterQTEStatus> WaitDoAfter(DoAfterQTEEventArgs eventArgs)
        {
            var doAfter = CreateDoAfter(eventArgs);

            await doAfter.AsTask;

            return doAfter.Status;
        }

        /// <summary>
        ///     Creates a DoAfter without waiting for it to finish. You can use events with this.
        ///     These can be potentially cancelled by the user moving or when other things happen.
        /// </summary>
        /// <param name="eventArgs"></param>
        public void DoAfter(DoAfterQTEEventArgs eventArgs)
        {
            CreateDoAfter(eventArgs);
        }

        private DoAfterQTE CreateDoAfter(DoAfterQTEEventArgs eventArgs)
        {
            // Setup
            var doAfter = new DoAfterQTE(eventArgs, EntityManager);
            // Caller's gonna be responsible for this I guess
            var doAfterComponent = Comp<DoAfterQTEComponent>(eventArgs.User);
            Add(doAfterComponent, doAfter);
            return doAfter;
        }
    }

    public enum DoAfterQTEStatus : byte
    {
        Running,
        Cancelled,
        Finished,
    }
}
