using DigestAICsharp;
using Cocona;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = CoconaApp.CreateBuilder();

// Configura i servizi
builder.Services.AddSingleton<ProjectDigestGenerator>();
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

var app = builder.Build();

// Comando principale e unico
app.AddCommand(async (

    ILogger<Program> logger,

    ProjectDigestGenerator generator,

    [Argument(Description = "Project path to analyze")]
    string? projectPath = null,

    [Option('o', Description = "Output file name")]
    string output = "digestedCode.txt",

    [Option(Description = "Exclude additional file patterns (comma-separated)")]
    string? excludePatterns = null,

    [Option(Description = "Include additional file extensions (comma-separated)")]
    string? includeExtensions = null,

    [Option(Description = "Maximum file size in MB to process")]
    int maxFileSizeMb = 10,

    [Option(Description = "Show verbose output")]
    bool verbose = false) =>
{
    // Normalizza il project path
    var normalizedPath = NormalizeProjectPath(projectPath);

    // Valida che il path esista
    if (!Directory.Exists(normalizedPath))
    {
        logger.LogError("Project directory not found: {ProjectPath}", normalizedPath);
        return 1;
    }

    // Configurazione opzioni
    var options = new ProjectOptions
    {
        ProjectPath = normalizedPath,
        OutputFilePath = Path.GetFullPath(output),
        ExcludePatterns = ParseCommaSeparatedValues(excludePatterns),
        IncludeExtensions = ParseCommaSeparatedValues(includeExtensions),
        MaxFileSizeMb = maxFileSizeMb,
        Verbose = verbose
    };

    // Genera il digest
    await generator.GenerateAsync(options);

    // Messaggio di successo
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"✅ Digest generated successfully: {options.OutputFilePath}");
    Console.ResetColor();

    return 0;
});

await app.RunAsync();

static string NormalizeProjectPath(string? projectPath)
{
    return string.IsNullOrWhiteSpace(projectPath) || projectPath is "." or "./"
        ? Directory.GetCurrentDirectory()
        : Path.GetFullPath(projectPath);
}

static List<string> ParseCommaSeparatedValues(string? values)
{
    return string.IsNullOrWhiteSpace(values)
        ? []
        : [.. values.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
}
