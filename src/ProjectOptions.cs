namespace DigestAICsharp;

/// <summary>
/// Opzioni di configurazione per la generazione del digest del progetto.
/// </summary>
public record ProjectOptions
{
    public required string ProjectPath { get; init; }
    public required string OutputFilePath { get; init; }
    public List<string> ExcludePatterns { get; init; } = [];
    public List<string> IncludeExtensions { get; init; } = [];
    public int MaxFileSizeMb { get; init; } = 10;
    public bool Verbose { get; init; } = false;
}
