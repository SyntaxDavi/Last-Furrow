
[AttributeUsage(AttributeTargets.Class,)]
public class CheatAttribute : Attribute
{
    public string ID { get; }
    public string Category { get; }
    public string Description { get; }
    public string DefaultHotkey { get; set;}

    public CheatAttribute(string id, string category, string description)
    {
        ID = id;
        Category = category;
        Description = description;
    }
}