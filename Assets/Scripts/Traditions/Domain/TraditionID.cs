using System;
using UnityEngine;

namespace LastFurrow.Traditions
{
    /// <summary>
    /// Identificador fortemente tipado para Tradições.
    /// Evita bugs de string e permite validação em compile-time.
    /// </summary>
    [Serializable]
    public struct TraditionID : IEquatable<TraditionID>
    {
        [SerializeField] private string _value;
        
        public string Value => _value;
        public bool IsValid => !string.IsNullOrEmpty(_value);
        
        public TraditionID(string value)
        {
            _value = value?.ToUpperInvariant().Replace(" ", "_") ?? string.Empty;
        }
        
        public static TraditionID None => new TraditionID(string.Empty);
        
        public static implicit operator string(TraditionID id) => id._value;
        public static explicit operator TraditionID(string value) => new TraditionID(value);
        
        public bool Equals(TraditionID other) => _value == other._value;
        public override bool Equals(object obj) => obj is TraditionID other && Equals(other);
        public override int GetHashCode() => _value?.GetHashCode() ?? 0;
        public override string ToString() => _value ?? "INVALID";
        
        public static bool operator ==(TraditionID a, TraditionID b) => a.Equals(b);
        public static bool operator !=(TraditionID a, TraditionID b) => !a.Equals(b);
    }
}
