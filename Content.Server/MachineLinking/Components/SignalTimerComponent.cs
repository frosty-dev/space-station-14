﻿using Content.Shared.MachineLinking;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.MachineLinking.Components
{
    [RegisterComponent]
    public sealed class SignalTimerComponent : Component
    {
        [DataField("delay")]
        [ViewVariables(VVAccess.ReadWrite)]
        public double Delay = 5;

        [DataField("user")] public EntityUid? User;

        /// <summary>
        ///     This shows the Label: text box in the UI.
        /// </summary>
        [DataField("canEditLabel")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanEditLabel = true;

        /// <summary>
        ///     The label, used for TextScreen visuals currently.
        /// </summary>
        [DataField("label")] public string Label = "";

        /// <summary>
        ///     port that gets signaled when the timer triggers, so something happens!
        /// </summary>
        [DataField("triggerPort", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
        public string TriggerPort = "Timer";

        /// <summary>
        ///     port that gets signaled when the timer starts, so something happens!
        /// </summary>
        [DataField("startPort", customTypeSerializer: typeof(PrototypeIdSerializer<TransmitterPortPrototype>))]
        public string StartPort = "Start";

        /// <summary>
        ///     this timer will play this sound when ends.
        /// </summary>
        [DataField("doneSound")] public SoundSpecifier? DoneSound= new SoundPathSpecifier("/Audio/Machines/doneSound.ogg");

        [DataField("soundParams")] public AudioParams SoundParams = AudioParams.Default.WithVolume(-2f);
    }
}
