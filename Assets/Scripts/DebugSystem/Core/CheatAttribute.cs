using System;

[AttributeUsage(AttributeTargets.Class)]
public class CheatAttribute : Attribute
{
    public string Id { get; }
    public string Category { get; }
    public string Description { get; }
    public string DefaultHotkey { get; set; }

    public CheatAttribute(string id, string category, string description)
    {
        Id = id;
        Category = category;
        Description = description;
    }
}