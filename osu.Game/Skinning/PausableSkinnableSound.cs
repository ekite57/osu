// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Threading;
using osu.Game.Audio;
using osu.Game.Screens.Play;

namespace osu.Game.Skinning
{
    public class PausableSkinnableSound : SkinnableSound
    {
        public double Length => !DrawableSamples.Any() ? 0 : DrawableSamples.Max(sample => sample.Length);

        protected bool RequestedPlaying { get; private set; }

        protected IBindable<bool> SamplePlaybackDisabled => samplePlaybackDisabled;

        private readonly Bindable<bool> samplePlaybackDisabled = new Bindable<bool>();

        public PausableSkinnableSound()
        {
        }

        public PausableSkinnableSound([NotNull] IEnumerable<ISampleInfo> samples)
            : base(samples)
        {
        }

        public PausableSkinnableSound([NotNull] ISampleInfo sample)
            : base(sample)
        {
        }

        private ScheduledDelegate scheduledStart;

        [BackgroundDependencyLoader(true)]
        private void load(ISamplePlaybackDisabler samplePlaybackDisabler)
        {
            // if in a gameplay context, pause sample playback when gameplay is paused.
            if (samplePlaybackDisabler != null)
            {
                SamplePlaybackDisabled.BindTo(samplePlaybackDisabler.SamplePlaybackDisabled);
                SamplePlaybackDisabled.BindValueChanged(disabled =>
                {
                    if (!RequestedPlaying) return;

                    // let non-looping samples that have already been started play out to completion (sounds better than abruptly cutting off).
                    if (!Looping) return;

                    cancelPendingStart();

                    if (disabled.NewValue)
                        base.Stop();
                    else
                    {
                        // schedule so we don't start playing a sample which is no longer alive.
                        scheduledStart = Schedule(() =>
                        {
                            if (RequestedPlaying)
                                base.Play();
                        });
                    }
                });
            }
        }

        public override void Play(bool restart = true)
        {
            cancelPendingStart();
            RequestedPlaying = true;

            if (SamplePlaybackDisabled.Value)
                return;

            base.Play(restart);
        }

        public override void Stop()
        {
            cancelPendingStart();
            RequestedPlaying = false;
            base.Stop();
        }

        private void cancelPendingStart()
        {
            scheduledStart?.Cancel();
            scheduledStart = null;
        }
    }
}
