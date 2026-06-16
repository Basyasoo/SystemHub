using System;
using NAudio.Dsp;
using NAudio.Wave;

namespace SystemHub.Services
{
    public class EqualizerSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider _sourceProvider;
        private readonly float[] _frequencies;
        private readonly float[] _gains;
        private readonly int _bandsCount;
        private readonly BiQuadFilter[] _filters;
        private readonly object _lockObject = new();
        private bool _isEnabled = true;

        public WaveFormat WaveFormat => _sourceProvider.WaveFormat;

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                lock (_lockObject)
                {
                    _isEnabled = value;
                }
            }
        }

        private float _masterGainDb = 0f;
        public float MasterGainDb
        {
            get => _masterGainDb;
            set
            {
                lock (_lockObject)
                {
                    _masterGainDb = value;
                }
            }
        }

        public EqualizerSampleProvider(ISampleProvider sourceProvider, float[] frequencies, float[] gains)
        {
            _sourceProvider = sourceProvider ?? throw new ArgumentNullException(nameof(sourceProvider));
            _frequencies = frequencies;
            _gains = gains;
            _bandsCount = frequencies.Length;
            _filters = new BiQuadFilter[_bandsCount * WaveFormat.Channels];
            CreateFilters();
        }

        private void CreateFilters()
        {
            lock (_lockObject)
            {
                for (int channel = 0; channel < WaveFormat.Channels; channel++)
                {
                    for (int band = 0; band < _bandsCount; band++)
                    {
                        int filterIndex = channel * _bandsCount + band;
                        _filters[filterIndex] = BiQuadFilter.PeakingEQ(WaveFormat.SampleRate, _frequencies[band], 0.8f, _gains[band]);
                    }
                }
            }
        }

        public void UpdateBand(int bandIndex, float gainDb)
        {
            if (bandIndex < 0 || bandIndex >= _bandsCount) return;
            lock (_lockObject)
            {
                _gains[bandIndex] = gainDb;
                for (int channel = 0; channel < WaveFormat.Channels; channel++)
                {
                    int filterIndex = channel * _bandsCount + bandIndex;
                    _filters[filterIndex] = BiQuadFilter.PeakingEQ(WaveFormat.SampleRate, _frequencies[bandIndex], 0.8f, gainDb);
                }
            }
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = _sourceProvider.Read(buffer, offset, count);
            lock (_lockObject)
            {
                if (!_isEnabled) return samplesRead;

                float multiplier = (float)Math.Pow(10, _masterGainDb / 20.0);
                for (int n = 0; n < samplesRead; n++)
                {
                    int channel = n % WaveFormat.Channels;
                    float sample = buffer[offset + n];
                    for (int band = 0; band < _bandsCount; band++)
                    {
                        int filterIndex = channel * _bandsCount + band;
                        if (_filters[filterIndex] != null)
                        {
                            sample = _filters[filterIndex].Transform(sample);
                        }
                    }
                    buffer[offset + n] = sample * multiplier;
                }
            }
            return samplesRead;
        }
    }
}

