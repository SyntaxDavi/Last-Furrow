public interface ICheatCommand
{
    string Id { get; }
    string Category { get; }
    string Description { get; }
    bool Execute(string[] args, out string feedback);
    bool ValidateArgs(string[] args, out string error);
}
