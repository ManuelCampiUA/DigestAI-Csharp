using DigestAI_Csharp;

namespace DigestAI_C_
{
    public class Program
    {
        // cd Digest e poi dotnet run . 
        public static async Task Main(string[] args)
        {
            if (args.Length == 0 || args.Any(a => a.Equals("--help", StringComparison.OrdinalIgnoreCase) || a.Equals("-h", StringComparison.OrdinalIgnoreCase)))
            {
                PrintHelp();
                return;
            }

            string projectPathArg = args.FirstOrDefault(arg => !arg.StartsWith("-"));
            var projectPath = string.IsNullOrEmpty(projectPathArg) || projectPathArg == "." || projectPathArg == "./" ?
                Directory.GetCurrentDirectory() :
                Path.GetFullPath(projectPathArg);

            var outputFile = GetArgumentValue(args, "--output") ?? "ai-digest.md";
            var outputFullPath = Path.GetFullPath(outputFile); // Per mostrare il percorso completo nel messaggio finale

            try
            {
                var generator = new ProjectDigestGenerator();
                await generator.GenerateAsync(projectPath, outputFullPath);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✅ Digest generated successfully: {outputFullPath}");
                Console.ResetColor();
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Error: Project directory not found.");
                Console.Error.WriteLine($"Details: {ex.Message}");
                Console.ResetColor();
                Environment.ExitCode = 1; // Codice di errore per directory non trovata
            }
            catch (IOException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Error: An I/O problem occurred (e.g., cannot write output file).");
                Console.Error.WriteLine($"Details: {ex.Message}");
                Console.ResetColor();
                Environment.ExitCode = 2; // Codice di errore per problemi I/O
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Error: Access denied. Check permissions for reading the project or writing the output file.");
                Console.Error.WriteLine($"Details: {ex.Message}");
                Console.ResetColor();
                Environment.ExitCode = 3; // Codice di errore per accesso negato
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("An unexpected error occurred:");
                Console.Error.WriteLine(ex.ToString()); // Fornisce lo stack trace completo per debug
                Console.ResetColor();
                Environment.ExitCode = 4; // Codice di errore generico
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("AI Digest Generator");
            Console.WriteLine("-------------------");
            Console.WriteLine("Generates a Markdown digest of a software project codebase.");
            Console.WriteLine("\nUsage: AiDigest.exe [path] [options]");
            Console.WriteLine("  path                     Project path (default: current directory if first arg is an option or omitted).");
            Console.WriteLine("\nOptions:");
            Console.WriteLine("  --output <filename>      Output file name (default: ai-digest.md).");
            Console.WriteLine("  --help, -h               Show this help message.");
        }

        private static string? GetArgumentValue(string[] args, string argumentName)
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
    }
}