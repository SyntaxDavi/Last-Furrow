using System;
using UnityEngine;

[Serializable]
public struct CropID : IEquatable<CropID>
{
    [SerializeField] private string _value;

    public string Value => _value;
    public bool IsValid => !string.IsNullOrEmpty(_value);
    public static readonly CropID Empty = new CropID(string.Empty);

    public CropID(string value) => _value = value;

    // Conversão Explícita (Segurança): string -> CropID
    public static explicit operator CropID(string value) => new CropID(value);

    // Comparadores
    public override string ToString() => _value;
    public bool Equals(CropID other) => _value == other._value;
    public override bool Equals(object obj) => obj is CropID other && Equals(other);
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;
    public static bool operator ==(CropID a, CropID b) => a.Equals(b);
    public static bool operator !=(CropID a, CropID b) => !a.Equals(b);
}

[Serializable]
public struct CardID : IEquatable<CardID>
{
    [SerializeField] private string _value;
    public string Value => _value;
    public bool IsValid => !string.IsNullOrEmpty(_value);
    public static readonly CardID Empty = new CardID(string.Empty);

    public CardID(string value) => _value = value;

    public static explicit operator CardID(string value) => new CardID(value);

    public override string ToString() => _value;
    public bool Equals(CardID other) => _value == other._value;
    public override bool Equals(object obj) => obj is CardID other && Equals(other);
    public override int GetHashCode() => _value?.GetHashCode() ?? 0;
    public static bool operator ==(CardID a, CardID b) => a.Equals(b);
    public static bool operator !=(CardID a, CardID b) => !a.Equals(b);
}