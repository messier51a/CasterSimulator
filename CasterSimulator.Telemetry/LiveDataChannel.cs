using System.Text;
using System.Reactive.Linq;
using System.Text.RegularExpressions;

namespace CasterSimulator.Telemetry
{
    public class LiveDataChannel : IDisposable
    {
        private readonly Dictionary<string, Signals> _measurements = new();
        private readonly LiveDataSender _liveDataSender;
        private readonly IDisposable _subscription;
        private readonly string _url;
        private readonly string _token;

        public LiveDataChannel(string channelName)
        {
          
            _url = $"{Configuration.Telemetry.GrafanaLiveUrl}/{channelName}";
            _token = Configuration.Telemetry.GrafanaLiveToken;
            _liveDataSender = new LiveDataSender();

            // Periodic auto-update every 1 second
            _subscription = Observable.Interval(TimeSpan.FromSeconds(1))
                .Subscribe(_ => SendPendingUpdates());
        }

        /// <summary>
        /// Retrieves a Signals object for the given area.
        /// </summary>
        public Signals GetSignals(string measurementName)
        {
            if (!_measurements.ContainsKey(measurementName))
                _measurements[measurementName] = new Signals(measurementName);

            return _measurements[measurementName];
        }

        /// <summary>
        /// Sends only changed signals.
        /// </summary>
        public void Push()
        {
            foreach (var payload in _measurements.Values
                         .Select(signalArea => signalArea.GetPendingUpdates())
                         .Where(payload => !string.IsNullOrEmpty(payload)))
            {
                //Console.WriteLine($"Sending payload {payload}");
                SendUpdate(payload);
            }
        }

        public void Push(string payload)
        {
            //Console.WriteLine($"Push payload (url, payload): {_url},{payload}");
            _liveDataSender.SendAsync(_url, _token, payload);
        }

        /// <summary>
        /// Sends all pending updates from all areas in InfluxDB format.
        /// </summary>
        private void SendPendingUpdates()
        {
            var combinedPayload = new StringBuilder();

            foreach (var signals in _measurements.Values)
            {
                string updates = signals.GetPendingUpdates();
                if (!string.IsNullOrEmpty(updates))
                {
                    combinedPayload.AppendLine(updates);
                }
            }

            string payload = combinedPayload.ToString().Trim();
            if (!string.IsNullOrEmpty(payload))
            {
                _liveDataSender.SendAsync(_url, _token, payload);
            }
        }

        /// <summary>
        /// Allows manual updates for a specific area.
        /// </summary>
        internal void SendUpdate(string payload)
        {
            if (!string.IsNullOrEmpty(payload))
            {
                _liveDataSender.SendAsync(_url, _token, payload);
            }
        }

        /// <summary>
        /// Disposes of the reactive subscription.
        /// </summary>
        public void Dispose()
        {
            _subscription.Dispose();
        }
    }

    public partial class Signals
    {
        public string MeasurementName { get; }
        private readonly Dictionary<string, object> _currentValues = new();

        public Signals(string measurementName)
        {
            MeasurementName = measurementName;
        }

        /// <summary>
        /// Sets a signal value.
        /// </summary>
        public void Set<T>(string key, T value) where T : notnull
        {
            _currentValues[key] = value;
        }

        /// <summary>
        /// Retrieves signals that have changed since the last update.
        /// </summary>
        internal string GetPendingUpdates()
        {
            /*var changedSignals = _currentValues
                .Where(kv => !_previousValues.ContainsKey(kv.Key) || !Equals(_previousValues[kv.Key], kv.Value))
                .ToDictionary(kv => kv.Key, kv => kv.Value);*/

            //if (changedSignals.Count == 0) return string.Empty;

            var signalsPayload = new StringBuilder();
            foreach (var (key, value) in _currentValues)
            {
                if (value is null) continue;
                var formattedValue = value switch
                {
                    string s => $"\"{s}\"", // Ensure string values are enclosed in double quotes
                    _ => value.ToString()
                };

                signalsPayload.Append($"{key}={formattedValue},");
            }

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000; // Convert to nanoseconds

            var formattedPayload = $"{MeasurementName} {signalsPayload.ToString().TrimEnd(',')} {timestamp}";

            //_previousValues = new Dictionary<string, object>(_currentValues); // Update history
            return formattedPayload;
        }


        [GeneratedRegex(@"([a-z0-9])([A-Z])")]
        private static partial Regex MyRegex();
    }
}