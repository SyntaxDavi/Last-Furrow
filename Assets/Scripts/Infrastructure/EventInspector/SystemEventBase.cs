using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastFurrow.EventInspector
{
    /// <summary>
    /// Base class for all system events.
    /// Events represent decisions made by the system, not raw input or visual effects.
    /// </summary>
    [Serializable]
    public abstract class SystemEventBase
    {
        /// <summary>
        /// Unique type identifier for this event.
        /// Used as discriminator for deserialization.
        /// </summary>
        public abstract string Type { get; }

        /// <summary>
        /// Metadata for traceability and debugging.
        /// </summary>
        public EventMetadata Metadata { get; set; }

        protected SystemEventBase()
        {
            Metadata = new EventMetadata();
        }

        protected SystemEventBase(string source)
        {
            Metadata = new EventMetadata(source);
        }
    }

    /// <summary>
    /// Metadata attached to every event for traceability.
    /// </summary>
    [Serializable]
    public class EventMetadata
    {
        /// <summary>
        /// Unique identifier for this event instance.
        /// </summary>
        public string Id;

        /// <summary>
        /// Timestamp when the event was created (ISO 8601).
        /// </summary>
        public string Timestamp;

        /// <summary>
        /// Identifier of the system component that emitted this event.
        /// </summary>
        public string Source;

        /// <summary>
        /// Optional correlation ID to link related events.
        /// </summary>
        public string CorrelationId;

        /// <summary>
        /// Optional sequence number for ordering.
        /// </summary>
        public int? SequenceNumber;

        public EventMetadata()
        {
            Id = $"evt_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            Timestamp = DateTime.UtcNow.ToString("o");
            Source = "system";
        }

        public EventMetadata(string source) : this()
        {
            Source = source;
        }
    }

    // =========================================================================
    // BUILT-IN EVENT TYPES
    // =========================================================================

    /// <summary>
    /// Event emitted when the system state is initialized.
    /// </summary>
    [Serializable]
    public class StateInitializedEvent : SystemEventBase
    {
        public override string Type => "STATE_INITIALIZED";

        public StateInitializedPayload Payload;

        public StateInitializedEvent(string description, Dictionary<string, object> initialValues, string source = "system")
            : base(source)
        {
            Payload = new StateInitializedPayload
            {
                Description = description,
                InitialValues = initialValues
            };
        }
    }

    [Serializable]
    public class StateInitializedPayload
    {
        public string Description;
        public Dictionary<string, object> InitialValues;
    }

    /// <summary>
    /// Event emitted when a property changes.
    /// </summary>
    [Serializable]
    public class PropertyChangedEvent : SystemEventBase
    {
        public override string Type => "PROPERTY_CHANGED";

        public PropertyChangedPayload Payload;

        public PropertyChangedEvent(string path, object previousValue, object newValue, string reason, string source = "system")
            : base(source)
        {
            Payload = new PropertyChangedPayload
            {
                Path = path,
                PreviousValue = previousValue,
                NewValue = newValue,
                Reason = reason
            };
        }
    }

    [Serializable]
    public class PropertyChangedPayload
    {
        public string Path;
        public object PreviousValue;
        public object NewValue;
        public string Reason;
    }

    /// <summary>
    /// Event emitted when a system rule is evaluated.
    /// </summary>
    [Serializable]
    public class RuleResolvedEvent : SystemEventBase
    {
        public override string Type => "RULE_RESOLVED";

        public RuleResolvedPayload Payload;

        public RuleResolvedEvent(string ruleId, Dictionary<string, object> inputs, string outcome, bool triggeredChange, string source = "system")
            : base(source)
        {
            Payload = new RuleResolvedPayload
            {
                RuleId = ruleId,
                Inputs = inputs,
                Outcome = outcome,
                TriggeredChange = triggeredChange
            };
        }
    }

    [Serializable]
    public class RuleResolvedPayload
    {
        public string RuleId;
        public Dictionary<string, object> Inputs;
        public string Outcome;
        public bool TriggeredChange;
    }

    // =========================================================================
    // CUSTOM EVENT HELPER
    // =========================================================================

    /// <summary>
    /// Generic event for custom event types.
    /// Use this when you need to create game-specific events without defining new classes.
    /// </summary>
    [Serializable]
    public class CustomEvent : SystemEventBase
    {
        private readonly string _type;
        public override string Type => _type;

        public Dictionary<string, object> Payload;

        public CustomEvent(string eventType, Dictionary<string, object> payload, string source = "system")
            : base(source)
        {
            _type = eventType;
            Payload = payload ?? new Dictionary<string, object>();
        }
    }
}
