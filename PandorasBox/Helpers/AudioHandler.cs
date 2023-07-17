using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace PandorasBox.Helpers
{
    public enum AudioTrigger
    {
        Light,
        Strong,
        Legendary
    }

    // Cached sound concept lovingly borrowed from: https://markheath.net/post/fire-and-forget-audio-playback-with
    class CachedSound
    {
        internal float[] AudioData { get; private set; }
        internal WaveFormat WaveFormat { get; private set; }
        internal CachedSound(string audioFileName)
        {
            using (var audioFileReader = new AudioFileReader(audioFileName))
            {
                WaveFormat = audioFileReader.WaveFormat;
                var wholeFile = new List<float>((int)(audioFileReader.Length / 4));
                var readBuffer = new float[audioFileReader.WaveFormat.SampleRate * audioFileReader.WaveFormat.Channels];
                int samplesRead;
                while ((samplesRead = audioFileReader.Read(readBuffer, 0, readBuffer.Length)) > 0)
                {
                    wholeFile.AddRange(readBuffer.Take(samplesRead));
                }
                AudioData = wholeFile.ToArray();
            }
        }
    }

    class CachedSoundSampleProvider : ISampleProvider
    {
        private readonly CachedSound cachedSound;
        private long position;

        public CachedSoundSampleProvider(CachedSound cachedSound)
        {
            this.cachedSound = cachedSound;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var availableSamples = cachedSound.AudioData.Length - position;
            var samplesToCopy = Math.Min(availableSamples, count);
            Array.Copy(cachedSound.AudioData, position, buffer, offset, samplesToCopy);
            position += samplesToCopy;
            return (int)samplesToCopy;
        }

        public WaveFormat WaveFormat { get { return cachedSound.WaveFormat; } }
    }

    public class AudioHandler
    {
        private readonly IWavePlayer outputDevice;
        private readonly Dictionary<AudioTrigger, CachedSound> sounds;
        private readonly VolumeSampleProvider sampleProvider;
        private readonly MixingSampleProvider mixer;

        public float Volume
        {
            get => sampleProvider.Volume;
            set
            {
                sampleProvider.Volume = value;

            }
        }

        public AudioHandler(string soundsPath)
        {
            outputDevice = new WaveOutEvent();
            sounds = new Dictionary<AudioTrigger, CachedSound>
            {
                { AudioTrigger.Light, new(System.IO.Path.Combine(soundsPath, "Light.wav")) },
                { AudioTrigger.Strong, new(System.IO.Path.Combine(soundsPath, "Strong.wav")) },
                { AudioTrigger.Legendary, new(System.IO.Path.Combine(soundsPath, "Legendary.wav")) }
            };
            mixer = new(WaveFormat.CreateIeeeFloatWaveFormat(48000, 2));
            mixer.ReadFully = true;
            sampleProvider = new(mixer);
            outputDevice.Init(sampleProvider);
            outputDevice.Play();
        }

        private ISampleProvider ConvertToRightChannelCount(ISampleProvider input)
        {
            if (input.WaveFormat.Channels == mixer.WaveFormat.Channels)
            {
                return input;
            }
            if (input.WaveFormat.Channels == 1 && mixer.WaveFormat.Channels == 2)
            {
                return new MonoToStereoSampleProvider(input);
            }
            throw new NotImplementedException("Not yet implemented this channel count conversion");
        }


        private void AddMixerInput(ISampleProvider input)
        {
            mixer.AddMixerInput(ConvertToRightChannelCount(input));
        }

        public void PlaySound(AudioTrigger trigger)
        {
            AddMixerInput(new WdlResamplingSampleProvider(new CachedSoundSampleProvider(sounds[trigger]), mixer.WaveFormat.SampleRate));
        }

    }
}
