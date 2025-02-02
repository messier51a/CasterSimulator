using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reactive.Linq;
using System.Text.RegularExpressions;

namespace CasterSimulator.Streaming
{
    public class Channel : IDisposable
    {
        private readonly Dictionary<string, Signals> _signalsByArea = new();
        private readonly LiveDataSender _liveDataSender;
        private readonly IDisposable _subscription;

        public Channel(string channelName, string url, string token)
        {
            _liveDataSender = new LiveDataSender($"{url}/{channelName}", token);

            // Periodic auto-update every 1 second
            _subscription = Observable.Interval(TimeSpan.FromSeconds(1))
                .Subscribe(_ => SendPendingUpdates());
        }

        /// <summary>
        /// Retrieves a Signals object for the given area.
        /// </summary>
        public Signals GetSignals(string area)
        {
            if (!_signalsByArea.ContainsKey(area))
                _signalsByArea[area] = new Signals(area, this);

            return _signalsByArea[area];
        }

        /// <summary>
        /// Sends all pending updates from all areas.
        /// </summary>
        private void SendPendingUpdates()
        {
            var combinedPayload = new StringBuilder();

            foreach (var signals in _signalsByArea.Values)
            {
                string updates = signals.GetPendingUpdates();
                if (!string.IsNullOrEmpty(updates))
                {
                    combinedPayload.AppendLine($"# {signals.Area}");
                    combinedPayload.Append(updates);
                }
            }

            string finalPayload = combinedPayload.ToString().Trim();
            if (!string.IsNullOrEmpty(finalPayload))
            {
                _liveDataSender.Send(finalPayload);
            }
        }

        /// <summary>
        /// Allows manual updates for a specific area.
        /// </summary>
        internal void SendUpdate(string area, string payload)
        {
            if (!string.IsNullOrEmpty(payload))
            {
                _liveDataSender.Send( $"# {area}\n{payload}");
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
        public string Area { get; }
        private readonly Channel _channel;
        private readonly Dictionary<string, object> _currentValues = new();
        private Dictionary<string, object> _previousValues = new();

        public Signals(string area, Channel channel)
        {
            Area = area;
            _channel = channel;
        }

        /// <summary>
        /// Sets a signal value.
        /// </summary>
        public void Set<T>(string key, T value) where T : notnull
        {
            string formattedKey = FormatKey(key);
            _currentValues[formattedKey] = value;
        }
        
        /// <summary>
        /// Converts CamelCase or PascalCase to lowercase_with_underscores.
        /// </summary>
        private string FormatKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return key;

            // Use regex to insert underscores before uppercase letters (except the first one)
            var formatted = MyRegex().Replace(key, "$1_$2").ToLower();
            return formatted;
        }

        /// <summary>
        /// Sends only changed signals.
        /// </summary>
        public void Update()
        {
            var payload = GetPendingUpdates();
            if (!string.IsNullOrEmpty(payload))
            {
                _channel.SendUpdate(Area, payload);
            }
        }

        /// <summary>
        /// Retrieves signals that have changed since the last update.
        /// </summary>
        internal string GetPendingUpdates()
        {
            var changedSignals = _currentValues
                .Where(kv => !_previousValues.ContainsKey(kv.Key) || !Equals(_previousValues[kv.Key], kv.Value))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            if (changedSignals.Count == 0) return string.Empty;

            var signalsPayload = new StringBuilder();
            foreach (var (key, value) in changedSignals)
            {
                var formattedValue = value switch
                {
                    string s => $"value=\"{s}\"",
                    _ => $"value={value}"
                };
                signalsPayload.AppendLine($"{key} {formattedValue}");
            }

            _previousValues = new Dictionary<string, object>(_currentValues); // Update history
            return signalsPayload.ToString().Trim();
        }

        [GeneratedRegex(@"([a-z0-9])([A-Z])")]
        private static partial Regex MyRegex();
    }
}
