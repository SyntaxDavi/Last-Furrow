using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace LastFurrow.EventInspector
{
    /// <summary>
    /// Event logger for capturing and exporting system events from Unity.
    /// Attach this to a GameObject to collect events during gameplay.
    /// </summary>
    public class EventLogger : MonoBehaviour
    {
        // =====================================================================
        // CONFIGURATION
        // =====================================================================

        [Header("Logging Settings")]
        [Tooltip("Enable event logging")]
        public bool IsEnabled = true;

        [Tooltip("Maximum events to keep in memory")]
        public int MaxEvents = 10000;

        [Tooltip("Auto-export on application quit")]
        public bool ExportOnQuit = false; // TEMPORARIAMENTE DESABILITADO

        [Header("Export Settings")]
        [Tooltip("Output directory for exported logs")]
        public string ExportDirectory = @"C:\Users\davi_\OneDrive\Área de Trabalho\json";

        [Tooltip("Include timestamp in filename")]
        public bool TimestampFilename = true;

        // =====================================================================
        // SINGLETON
        // =====================================================================

        private static EventLogger _instance;
        public static EventLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<EventLogger>();
                    if (_instance == null)
                    {
                        var go = new GameObject("EventLogger");
                        _instance = go.AddComponent<EventLogger>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // =====================================================================
        // STATE
        // =====================================================================

        private List<SystemEventBase> _events = new List<SystemEventBase>();
        private int _sequenceCounter = 0;
        private string _sessionId;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            _sessionId = Guid.NewGuid().ToString("N").Substring(0, 8);
            Debug.Log($"[EventLogger] Session started: {_sessionId}");
        }

        private void OnApplicationQuit()
        {
            if (ExportOnQuit && _events.Count > 0)
            {
                ExportToFile();
            }
        }

        // =====================================================================
        // PUBLIC API
        // =====================================================================

        /// <summary>
        /// Log a system event.
        /// </summary>
        public void Log(SystemEventBase evt)
        {
            if (!IsEnabled) return;

            // Add sequence number
            evt.Metadata.SequenceNumber = _sequenceCounter++;

            // Add to list
            _events.Add(evt);

            // Enforce max limit
            if (_events.Count > MaxEvents)
            {
                _events.RemoveAt(0);
            }

            Debug.Log($"[EventLogger] {evt.Type}: {evt.Metadata.Source}");
        }

        /// <summary>
        /// Log a property change.
        /// </summary>
        public void LogPropertyChange(string path, object oldValue, object newValue, string reason, string source = "system")
        {
            Log(new PropertyChangedEvent(path, oldValue, newValue, reason, source));
        }

        /// <summary>
        /// Log a rule resolution.
        /// </summary>
        public void LogRuleResolved(string ruleId, Dictionary<string, object> inputs, string outcome, bool changed, string source = "system")
        {
            Log(new RuleResolvedEvent(ruleId, inputs, outcome, changed, source));
        }

        /// <summary>
        /// Log a custom event.
        /// </summary>
        public void LogCustom(string eventType, Dictionary<string, object> payload, string source = "system")
        {
            Log(new CustomEvent(eventType, payload, source));
        }

        /// <summary>
        /// Log a connection between two scripts for the Architecture Graph.
        /// </summary>
        /// <param name="from">Origin script/module name</param>
        /// <param name="to">Target script/module name</param>
        public void LogConnection(string from, string to)
        {
            var payload = new Dictionary<string, object>
            {
                { "from", from },
                { "to", to }
            };
            LogCustom("script_connection", payload, "architecture_tracker");
        }

        /// <summary>
        /// Log state initialization.
        /// </summary>
        public void LogStateInitialized(string description, Dictionary<string, object> initialValues, string source = "system")
        {
            Log(new StateInitializedEvent(description, initialValues, source));
        }

        /// <summary>
        /// Get all logged events.
        /// </summary>
        public List<SystemEventBase> GetEvents()
        {
            return new List<SystemEventBase>(_events);
        }

        /// <summary>
        /// Get event count.
        /// </summary>
        public int EventCount => _events.Count;

        /// <summary>
        /// Clear all logged events.
        /// </summary>
        public void Clear()
        {
            _events.Clear();
            _sequenceCounter = 0;
            Debug.Log("[EventLogger] Events cleared");
        }

        // =====================================================================
        // EXPORT
        // =====================================================================

        /// <summary>
        /// Export events to a JSON file.
        /// </summary>
        public string ExportToFile(string filename = null)
        {
            if (_events.Count == 0)
            {
                Debug.LogWarning("[EventLogger] No events to export");
                return null;
            }

            // Build filename
            if (string.IsNullOrEmpty(filename))
            {
                filename = TimestampFilename
                    ? $"events_{_sessionId}_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                    : $"events_{_sessionId}.json";
            }

            // Ensure directory exists
            var directory = ExportDirectory;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var filepath = Path.Combine(directory, filename);

            // Serialize
            var json = SerializeEvents();
            File.WriteAllText(filepath, json, Encoding.UTF8);

            Debug.Log($"[EventLogger] Exported {_events.Count} events to: {filepath}");
            return filepath;
        }

        /// <summary>
        /// Get events as JSON string.
        /// </summary>
        public string ToJson()
        {
            return SerializeEvents();
        }

        // =====================================================================
        // SERIALIZATION
        // =====================================================================

        private string SerializeEvents()
        {
            // Using Unity's JsonUtility with a wrapper since it doesn't support List directly
            var wrapper = new EventListWrapper { events = _events };
            
            // Manual JSON building for better compatibility
            var sb = new StringBuilder();
            sb.Append("[\n");

            for (int i = 0; i < _events.Count; i++)
            {
                sb.Append(SerializeEvent(_events[i]));
                if (i < _events.Count - 1)
                {
                    sb.Append(",");
                }
                sb.Append("\n");
            }

            sb.Append("]");
            return sb.ToString();
        }

        private string SerializeEvent(SystemEventBase evt)
        {
            // Build JSON manually for consistent output
            var sb = new StringBuilder();
            sb.Append("  {\n");
            sb.Append($"    \"type\": \"{evt.Type}\",\n");
            sb.Append("    \"metadata\": {\n");
            sb.Append($"      \"id\": \"{evt.Metadata.Id}\",\n");
            sb.Append($"      \"timestamp\": \"{evt.Metadata.Timestamp}\",\n");
            sb.Append($"      \"source\": \"{evt.Metadata.Source}\"");
            
            if (!string.IsNullOrEmpty(evt.Metadata.CorrelationId))
            {
                sb.Append($",\n      \"correlationId\": \"{evt.Metadata.CorrelationId}\"");
            }
            if (evt.Metadata.SequenceNumber.HasValue)
            {
                sb.Append($",\n      \"sequenceNumber\": {evt.Metadata.SequenceNumber}");
            }
            
            sb.Append("\n    },\n");
            sb.Append("    \"payload\": ");
            sb.Append(SerializePayload(evt));
            sb.Append("\n  }");

            return sb.ToString();
        }

        private string SerializePayload(SystemEventBase evt)
        {
            try
            {
                // Use reflection to get the Payload field
                var payloadField = evt.GetType().GetField("Payload");
                if (payloadField != null)
                {
                    var payload = payloadField.GetValue(evt);
                    return JsonUtility.ToJson(payload);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[EventLogger] Payload serialization failed: {e.Message}");
            }

            return "{}";
        }

        [Serializable]
        private class EventListWrapper
        {
            public List<SystemEventBase> events;
        }
    }
}


