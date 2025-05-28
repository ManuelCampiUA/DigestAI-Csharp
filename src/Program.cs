using DigestAICsharp;

if (ShouldShowHelp(args))
{
    DisplayHelp();
    return;
}

var options = ParseArguments(args);

try
{
    var generator = new ProjectDigestGenerator();
    generator.Generate(options.ProjectPath, options.OutputFilePath);

    DisplaySuccess(options.OutputFilePath);
}
catch (Exception ex)
{
    DisplayError(ex);
    Environment.ExitCode = 1;
}

static bool ShouldShowHelp(string[] args)
{
    return args.Length == 0 ||
           args.Any(a => a.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
                        a.Equals("-h", StringComparison.OrdinalIgnoreCase));
}

static void DisplayHelp()
{
    const string help = """
        AI Digest Generator
        -------------------
        Generates a Markdown digest of a software project codebase.
        Usage: DigestAICsharp.exe [path] [options]
        
        Arguments:
          path                     Project path (default: current directory)
        
        Options:
          --output <filename>      Output file name (default: digestedCode.txt)
          --help, -h               Show this help message
        """;

    Console.WriteLine(help);
}

static ProjectOptions ParseArguments(string[] args)
{
    var projectPathArg = args.FirstOrDefault(arg => !arg.StartsWith("-"));
    var projectPath = DetermineProjectPath(projectPathArg);
    var outputFile = GetArgumentValue(args, "--output") ?? "digestedCode.txt";

    return new ProjectOptions(projectPath, Path.GetFullPath(outputFile));
}

static string DetermineProjectPath(string? projectPathArg)
{
    return string.IsNullOrEmpty(projectPathArg) || projectPathArg is "." or "./"
        ? Directory.GetCurrentDirectory()
        : Path.GetFullPath(projectPathArg);
}

static void DisplaySuccess(string outputFilePath)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"✅ Digest generated successfully: {outputFilePath}");
    Console.ResetColor();
}

static void DisplayError(Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine("An unexpected error occurred:");
    Console.Error.WriteLine(ex.ToString());
    Console.ResetColor();
}

static string? GetArgumentValue(string[] args, string argumentName)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (args[i].Equals(argumentName, StringComparison.OrdinalIgnoreCase))
        {
            return !args[i + 1].StartsWith("-")
                ? args[i + 1]
                : LogWarningAndReturnNull(argumentName);
        }
    }
    return null;
}

static string? LogWarningAndReturnNull(string argumentName)
{
    Console.Error.WriteLine($"Warning: Argument '{argumentName}' is missing a value.");
    return null;
}

// Record per le opzioni del progetto
public record ProjectOptions(string ProjectPath, string OutputFilePath);
