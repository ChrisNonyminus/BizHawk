﻿using System;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sound
{
	/// <summary>
	/// A simple 1-bit (mono) beeper/buzzer implementation using blipbuffer
	/// Simulating the piezzo-electric buzzer found in many old computers (such as the ZX Spectrum or Amstrad CPC)
	/// Sound is generated by toggling the single input line ON and OFF rapidly
	/// </summary>
	public sealed class OneBitBeeper : ISyncSoundProvider
	{
		private int _sampleRate;
		private int _clocksPerFrame;
		private int _framesPerSecond;
		private readonly BlipBuffer _blip;
		private readonly string _beeperId;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="blipSampleRate">The sample rate to pass to blipbuffer (this should be 44100 for ISoundProvider)</param>
		/// <param name="clocksPerFrame">The number of (usually CPU) clocked cycles in one frame</param>
		/// <param name="framesPerSecond">The number of frames per second (usually either 60 or 50)</param>
		/// <param name="beeperId">Unique name for this instance (needed for serialization as some cores have more than one active instance of the beeper)</param>
		public OneBitBeeper(int blipSampleRate, int clocksPerFrame, int framesPerSecond, string beeperId)
		{
			_beeperId = beeperId;
			_sampleRate = blipSampleRate;
			_clocksPerFrame = clocksPerFrame;
			_framesPerSecond = framesPerSecond;
			_blip = new BlipBuffer(blipSampleRate / framesPerSecond);
			_blip.SetRates(clocksPerFrame * 50, blipSampleRate);
		}

		private int clockCounter;

		/// <summary>
		/// Option to clock the beeper every CPU clock
		/// </summary>
		public void Clock(int clocksToAdd = 1)
		{
			clockCounter += clocksToAdd;
		}

		/// <summary>
		/// Option to directly set the current clock position within the frame
		/// </summary>
		public void SetClock(int currentFrameClock)
		{
			clockCounter = currentFrameClock;
		}

		private bool lastPulse;

		/// <summary>
		/// Processes an incoming pulse value
		/// </summary>
		public void ProcessPulseValue(bool pulse, bool renderSound = true)
		{
			if (!renderSound)
				return;

			if (lastPulse == pulse)
			{
				// no change
				_blip.AddDelta((uint)clockCounter, 0);
			}

			else
			{
				if (pulse)
					_blip.AddDelta((uint)clockCounter, (short)(_volume));
				else
					_blip.AddDelta((uint)clockCounter, -(short)(_volume));

				lastVolume = _volume;
			}

			lastPulse = pulse;
		}

		/// <summary>
		/// Beeper volume
		/// Accepts an int 0-100 value
		/// </summary>
		public int Volume
		{
			get => VolumeConverterOut(_volume);
			set
			{
				var newVol = VolumeConverterIn(value);
				if (newVol != _volume)
					_blip.Clear();
				_volume = VolumeConverterIn(value);
			}
		}
		private int _volume;

		/// <summary>
		/// The last used volume (used to modify blipbuffer delta values)
		/// </summary>
		private int lastVolume;


		/// <summary>
		/// Takes an int 0-100 and returns the relevant short volume to output
		/// </summary>
		private int VolumeConverterIn(int vol)
		{
			int maxLimit = short.MaxValue / 3;
			int increment = maxLimit / 100;

			return vol * increment;
		}

		/// <summary>
		/// Takes an short volume and returns the relevant int value 0-100
		/// </summary>
		private int VolumeConverterOut(int shortvol)
		{
			int maxLimit = short.MaxValue / 3;
			int increment = maxLimit / 100;

			if (shortvol > maxLimit)
				shortvol = maxLimit;

			return shortvol / increment;
		}

		public void DiscardSamples()
		{
			_blip.Clear();
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			_blip.EndFrame((uint)_clocksPerFrame);
			nsamp = _blip.SamplesAvailable();
			samples = new short[nsamp * 2];
			_blip.ReadSamples(samples, nsamp, true);
			for (int i = 0; i < nsamp * 2; i += 2)
			{
				samples[i + 1] = samples[i];
			}

			clockCounter = 0;
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("Beeper_" + _beeperId);
			ser.Sync(nameof(_sampleRate), ref _sampleRate);
			ser.Sync(nameof(_clocksPerFrame), ref _clocksPerFrame);
			ser.Sync(nameof(_framesPerSecond), ref _framesPerSecond);
			ser.Sync(nameof(clockCounter), ref clockCounter);
			ser.Sync(nameof(lastPulse), ref lastPulse);
			ser.EndSection();
		}
	}
}
