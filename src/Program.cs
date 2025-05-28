using DigestAICsharp;

if (
    args.Length == 0
    || args.Any(a => a.Equals("--help", StringComparison.OrdinalIgnoreCase)
    || a.Equals("-h", StringComparison.OrdinalIgnoreCase))
)
{
    var help = @"
AI Digest Generator
-------------------
Generates a Markdown digest of a software project codebase.
Usage: DigestAICsharp.exe [path] [options]
path                     Project path (default: current directory if first arg is an option or omitted).
Options:
--output <filename>      Output file name (default: digestedCode.txt).
--help, -h               Show this help message.";

    Console.WriteLine(help);
    return;
}

var projectPathArg = args.FirstOrDefault(arg => !arg.StartsWith("-"));
var projectPath = string.IsNullOrEmpty(projectPathArg) || projectPathArg == "." || projectPathArg == "./"
    ? Directory.GetCurrentDirectory()
    : Path.GetFullPath(projectPathArg);

var outputFile = GetArgumentValue(args, "--output") ?? "digestedCode.txt";
var outputFullPath = Path.GetFullPath(outputFile); // Per mostrare il percorso completo nel messaggio finale

try
{
    var generator = new ProjectDigestGenerator();
    generator.Generate(projectPath, outputFullPath);
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"✅ Digest generated successfully: {outputFullPath}");
    Console.ResetColor();
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine("An unexpected error occurred:");
    Console.Error.WriteLine(ex.ToString()); // Fornisce lo stack trace completo per debug
    Console.ResetColor();
    Environment.ExitCode = 1; // Codice di errore generico
}

static string? GetArgumentValue(string[] args, string argumentName)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (args[i].Equals(argumentName, StringComparison.OrdinalIgnoreCase))
        {
            if (!args[i + 1].StartsWith("-"))
            {
                return args[i + 1];
            }
            Console.Error.WriteLine($"Warning: Argument '{argumentName}' is missing a value.");
            return null;
        }
    }
    return null;
}
