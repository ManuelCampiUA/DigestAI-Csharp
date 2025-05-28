using System.Text;

namespace DigestAICsharp;

public class ProjectDigestGenerator
{
    // Estensioni dei file da includere (sempre lowercase per confronto normalizzato)
    private readonly HashSet<string> _includedExtensions =
    [
        ".cs", ".js", ".ts", ".jsx", ".tsx", ".py", ".java", ".cpp", ".h", ".hpp", ".hh", ".c", ".cc",
        ".html", ".css", ".scss", ".vue", ".php", ".rb", ".go", ".rs",
        ".sql", ".json", ".xml", ".yaml", ".yml", ".md", ".txt", ".sh", ".ps1", ".csproj", ".sln"
        // NOTA: .csproj e .sln sono stati aggiunti qui, valuta se spostarli in _priorityIncludesIfNeeded
    ];

    // Cartelle da escludere (confronto case-insensitive)
    private readonly HashSet<string> _excludedFolders =
    [
        // .NET/C#
        "bin", "obj", "packages", "TestResults", ".vs", "publish",
        // Frontend generale
        "node_modules", "dist", "build", "out", ".next", ".nuxt", ".svelte-kit",
        // Cache e temp
        ".vscode", ".idea", "temp", "tmp", ".cache", ".parcel-cache",
        // Version control
        ".git", ".svn", ".hg",
        // OS
        ".DS_Store", "Thumbs.db",
        // Coverage/Testing
        "coverage", "nyc_output", ".nyc_output", "TestCoverage",
        // Logs
        "logs", "log"
    ];

    // Pattern di nomi file da escludere (confronto case-insensitive, wildcard semplici)
    private readonly HashSet<string> _excludedFilePatterns =
    [
        // .NET/C# - Config sensibili
        "appsettings.json", "appsettings.*.json", "secrets.json", "web.config", "app.config", "connectionstrings.json",
        // .NET - Generated/Build (esclusi file binari, di debug e assembly info generati)
        "*.dll", "*.pdb", "*.exe", "*.msi", "*.nupkg", "*.snupkg",
        "GlobalAssemblyInfo.cs", "AssemblyInfo.cs", "*.Generated.cs",
        // Frontend - Dipendenze/Lock files
        "package-lock.json", "yarn.lock", "pnpm-lock.yaml", "composer.lock",
        // Frontend - Build/Generated
        "*.min.js", "*.min.css", "*.map",
        "*.bundle.js", "*.chunk.js", "manifest.json",
        // Frontend - Alcuni file di configurazione (se si vogliono escludere, altrimenti rimuovere da qui)
        // "tsconfig.json", "package.json", "angular.json", "vue.config.js", "next.config.js",
        // "webpack.config.js", "tailwind.config.js", "jest.config.js", "eslint.config.js",
        // ".eslintrc.*", "prettier.config.js", ".prettierrc.*",
        // Commentati perché spesso utili per l'AI. Se non desiderati, decommentare.

        // Environment/Secrets
        ".env", ".env.*", "*.pem", "*.key", "*.crt", "*.pfx",
        // Git
        ".gitignore", ".gitattributes", ".gitmodules",
        // Editor/IDE
        "*.swp", "*.swo", "*~", "*.user", "*.suo", "*.userprefs",
        // Testing/Coverage
        "*.coverage", "coverage.xml", "*.lcov",
        // Logs
        "*.log", "npm-debug.log*", "yarn-debug.log*", "lerna-debug.log*",
        // OS
        /* ".DS_Store", "Thumbs.db", */ "ehthumbs.db", // Già coperti da _excludedFolders per le cartelle, ma qui per file singoli alla radice.
        // Documentation (ESCLUSI tranne README o file in _priorityIncludes)
        "CHANGELOG.md", "LICENSE", "LICENSE.txt", "CONTRIBUTING.md",
        // Database
        "*.db", "*.sqlite", "*.sqlite3", "*.mdf", "*.ldf",
        // Backup files
        "*.bak", "*.backup", "*.old", "*.orig"
    ];

    // File da includere prioritariamente (es. README), bypassano alcune esclusioni (non l'esclusione di cartelle)
    // Confronto case-insensitive
    private readonly HashSet<string> _priorityIncludes =
    [
        "README.md", "README.txt", "README", "ReadMe.md"
        // Aggiungere qui altri file specifici che si vogliono sempre includere,
        // come "ARCHITECTURE.md", "TODO.md", ecc.
    ];

    public void Generate(string projectPath, string outputFilePath)
    {
        if (!Directory.Exists(projectPath))
            throw new DirectoryNotFoundException($"Project directory not found: {projectPath}");

        Console.WriteLine($"🔍 Scanning project: {projectPath}");

        var filesToDigest = GetRelevantFiles(projectPath).ToList();

        if (filesToDigest.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️ No relevant files found to generate the digest.");
            Console.ResetColor();
            // Crea comunque un file vuoto o con un messaggio? Per ora, esce senza creare il file.
            // Se si vuole un file vuoto: File.WriteAllText(outputFilePath, "# AI Project Digest\n\nNo relevant files found.");
            return;
        }

        Console.WriteLine($"📁 Found {filesToDigest.Count} relevant files to include.");

        var markdownBuilder = new StringBuilder();

        // Header del Markdown
        markdownBuilder.AppendLine($"Total files included: {filesToDigest.Count}\n");
        markdownBuilder.AppendLine("---");

        // Contenuto dei file
        int processedCount = 0;
        foreach (var filePath in filesToDigest)
        {
            processedCount++;
            var relativePath = Path.GetRelativePath(projectPath, filePath);
            Console.WriteLine($"📄 Processing ({processedCount}/{filesToDigest.Count}): {relativePath}");

            string content;
            try
            {
                content = File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"   Skipping file {relativePath} due to read error: {ex.Message}");
                Console.ResetColor();
                content = $"Error reading file: {ex.Message}"; // Includi un messaggio di errore nel digest
            }

            var extension = Path.GetExtension(filePath);
            var language = GetLanguageFromExtension(extension);

            markdownBuilder.AppendLine($"\n## File: `{relativePath}`");
            markdownBuilder.AppendLine($"Language: `{language}`");
            markdownBuilder.AppendLine($"\n```{language}");
            markdownBuilder.AppendLine(content);
            markdownBuilder.AppendLine("```");
            markdownBuilder.AppendLine("\n---");
        }

        try
        {
            File.WriteAllText(outputFilePath, markdownBuilder.ToString());
        }
        catch (Exception ex)
        {
            // Rilancia per essere catturata dal Main e loggata correttamente
            throw new IOException($"Failed to write digest file to '{outputFilePath}'.", ex);
        }
    }

    private IEnumerable<string> GetRelevantFiles(string projectPath)
    {
        return Directory.EnumerateFiles(projectPath, "*.*", SearchOption.AllDirectories)
            .Where(filePath =>
            {
                // 1. Controllo Esclusione Cartelle
                // Ottieni il percorso relativo della directory del file
                var relativeDirectory = Path.GetDirectoryName(Path.GetRelativePath(projectPath, filePath));
                if (!string.IsNullOrEmpty(relativeDirectory))
                {
                    // Separa il percorso in segmenti e controlla se uno di essi è escluso
                    var directoryParts = relativeDirectory.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
                    if (directoryParts.Any(part => _excludedFolders.Contains(part)))
                        return false; // File in una cartella esclusa
                }

                var fileName = Path.GetFileName(filePath);

                // 2. Controllo Inclusione Prioritaria (es. README.md)
                // Se un file è prioritario, lo includiamo subito a meno che non sia in una cartella esclusa (già controllato).
                // Non verrà sottoposto a esclusione per pattern o estensione.
                if (_priorityIncludes.Contains(fileName))
                    return true;

                // 3. Controllo Esclusione per Pattern di Nome File
                if (IsFileExcludedByPattern(fileName))
                    return false;

                // 4. Controllo Inclusione per Estensione
                var extension = Path.GetExtension(fileName); // es. ".cs"
                if (string.IsNullOrEmpty(extension))
                    return false; // File senza estensione non sono inclusi (a meno che non prioritari)

                return _includedExtensions.Contains(extension); // _includedExtensions usa StringComparer.OrdinalIgnoreCase
            })
            .OrderBy(filePath => filePath, StringComparer.Ordinal); // Ordina i percorsi
    }

    private bool IsFileExcludedByPattern(string fileName)
    {
        // Confronto case-insensitive grazie al comparer dell'HashSet _excludedFilePatterns
        if (_excludedFilePatterns.Contains(fileName)) // Match esatto
            return true;

        foreach (var pattern in _excludedFilePatterns)
        {
            if (pattern.Contains("*")) // Solo se il pattern contiene un wildcard
            {
                // Gestione pattern con wildcard (semplificata)
                if (pattern.StartsWith("*.") && fileName.EndsWith(pattern[1..], StringComparison.OrdinalIgnoreCase)) // Es. "*.log" -> ".log"
                    return true;
                else if (pattern.EndsWith(".*") && fileName.StartsWith(pattern.Substring(0, pattern.Length - 2), StringComparison.OrdinalIgnoreCase)) // Es. "file.*" -> "file"
                    return true;
                else if (pattern.StartsWith("*") && pattern.EndsWith("*") && pattern.Length > 2) // Es. "*Generated*"
                    if (fileName.Contains(pattern.Substring(1, pattern.Length - 2), StringComparison.OrdinalIgnoreCase))
                        return true;
                    else if (pattern.Contains("*") && pattern.Count(c => c == '*') == 1) // Es. "appsettings.*.json" o "file*.txt"
                    {
                        var parts = pattern.Split('*');
                        if (parts.Length == 2)
                        {
                            bool startsWith = string.IsNullOrEmpty(parts[0]) || fileName.StartsWith(parts[0], StringComparison.OrdinalIgnoreCase);
                            bool endsWith = string.IsNullOrEmpty(parts[1]) || fileName.EndsWith(parts[1], StringComparison.OrdinalIgnoreCase);
                            if (startsWith && endsWith && fileName.Length >= (parts[0].Length + parts[1].Length))
                                return true;
                        }
                    }
            }
        }

        return false;
    }

    private static string GetLanguageFromExtension(string extension)
    {
        // extension include il punto, es. ".cs". Converti a lowercase per il matching.
        return extension.ToLowerInvariant() switch
        {
            ".cs" => "csharp",
            ".js" => "javascript",
            ".ts" => "typescript",
            ".jsx" => "jsx",
            ".tsx" => "tsx",
            ".py" => "python",
            ".java" => "java",
            ".cpp" or ".cxx" or ".cc" or ".c++" => "cpp",
            ".h" or ".hpp" or ".hh" => "cpp", // Header C/C++ generalmente come cpp per highlighting
            ".c" => "c",
            ".html" or ".htm" => "html",
            ".css" => "css",
            ".scss" or ".sass" => "scss",
            ".vue" => "vue",
            ".php" => "php",
            ".rb" => "ruby",
            ".go" => "go",
            ".rs" => "rust",
            ".kt" or ".kts" => "kotlin",
            ".swift" => "swift",
            ".sql" => "sql",
            ".json" => "json",
            ".xml" => "xml",
            ".yaml" or ".yml" => "yaml",
            ".md" or ".markdown" => "markdown",
            ".sh" => "bash", // o shell
            ".ps1" => "powershell",
            ".fs" or ".fsi" or ".fsx" => "fsharp",
            ".csproj" => "xml", // I file Csproj sono XML
            ".sln" => "text",   // I file SLN hanno un formato custom, 'text' è un fallback sicuro
            ".txt" => "text",
            _ => "text" // Fallback generico
        };
    }
}
