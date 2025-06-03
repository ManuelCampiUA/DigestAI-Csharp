using System.Text;

namespace DigestAICsharp;

public class ProjectDigestGenerator
{
    // File importanti da includere sempre (case-insensitive)
    private readonly HashSet<string> _priorityFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "README.md", "README.txt", "README.rst", "README", "ReadMe.md",
    };

    // Estensioni file da includere NEL DIGEST (non nella struttura)
    private readonly HashSet<string> _allowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // .NET/C#
        ".cs", ".csx", ".csproj", ".sln", ".props", ".targets",
        
        // Web Frontend
        ".js", ".ts", ".jsx", ".tsx", ".vue", ".svelte", ".html", ".htm", ".css", ".scss", ".sass", ".less",
        
        // Backend Languages
        ".py", ".java", ".php", ".rb", ".go", ".rs", ".kt", ".cpp", ".cxx", ".cc", ".c++", ".h", ".hpp", ".hh", ".c", ".fs", ".fsi", ".fsx",
        
        // Config/Data
        ".json", ".xml", ".yaml", ".yml", ".toml", ".ini", ".cfg", ".sql", ".graphql", ".proto",
        
        // Documentation/Scripts
        ".md", ".markdown", ".txt", ".rst", ".sh", ".bash", ".zsh", ".ps1", ".bat", ".cmd",
        
        // Docker/Container
        ".dockerfile", ".dockerignore"
    };

    // Cartelle da ignorare NEL DIGEST (non nella struttura)
    private readonly HashSet<string> _ignoredFolders = new(StringComparer.OrdinalIgnoreCase)
    {
        // .NET Build/Packages
        "bin", "obj", "packages", "TestResults", ".vs", "publish", "artifacts", "output", "release", "debug",
        
        // .NET Migrations
        "Migrations", "migrations",
        
        // Node.js/Frontend
        "node_modules", "dist", "build", "out", ".next", ".nuxt", "public/build", "wwwroot/dist", "assets/build",
        
        // IDEs/Editors
        ".vscode", ".idea", ".vs", "*.xcworkspace", "*.xcodeproj",
        
        // Temporary/Cache
        "temp", "tmp", ".cache", ".temp", "cache",
        
        // Version Control
        ".git", ".svn", ".hg", ".bzr",
        
        // Testing/Coverage
        "coverage", "TestCoverage", "CodeCoverage", "test-results",
        
        // Logs
        "logs", "log", "LogFiles",

        // Other common
        "target", // Rust/Java/Scala
        "__pycache__", ".pytest_cache", // Python
        ".nuget" // NuGet local cache
    };

    // Pattern file da ignorare NEL DIGEST (non nella struttura)
    private readonly HashSet<string> _ignoredFilePatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        // Speciali
        "digestedCode.txt",
        
        // .NET/C# - Config sensitive
        "appsettings.json", "appsettings.*.json", "secrets.json", "web.config", "app.config",
        "connectionstrings.json", "launchSettings.json",
        
        // .NET - Migration files specifici
        "*Migration.cs", "*Migration.Designer.cs", "*.Designer.cs",
        
        // .NET - Generated/Build artifacts
        "*.dll", "*.pdb", "*.exe", "*.msi", "*.nupkg", "*.snupkg", "GlobalAssemblyInfo.cs",
        "AssemblyInfo.cs", "*.Generated.cs", "*.g.cs", "*.g.i.cs", "TemporaryGeneratedFile_*",
        
        // Frontend - Lock files & build artifacts
        "package-lock.json", "yarn.lock", "pnpm-lock.yaml", "composer.lock", "*.min.js",
        "*.min.css", "*.map", "*.bundle.js", "*.chunk.js", "manifest.json", "asset-manifest.json",
        
        // Environment/Secrets
        ".env", ".env.*", "*.pem", "*.key", "*.crt", "*.pfx", "*.p12",
        
        // Git files
        ".gitignore", ".gitattributes", ".gitmodules", ".gitkeep",
        
        // Editor/IDE specifici
        "*.swp", "*.swo", "*~", "*.user", "*.suo", "*.userprefs",
        ".DS_Store", "Thumbs.db", "desktop.ini",
        
        // Testing/Coverage
        "*.coverage", "coverage.xml", "*.lcov", "*.trx", "*.runsettings",
        
        // Logs
        "*.log", "npm-debug.log*", "yarn-debug.log*", "lerna-debug.log*",
        
        // Database files
        "*.db", "*.sqlite", "*.sqlite3", "*.mdf", "*.ldf", "*.bak",
        
        // Backup e temporanei
        "*.backup", "*.old", "*.orig", "*.tmp", "*~", "*.swp",
        
        // OS specific
        ".DS_Store", "Thumbs.db", "desktop.ini", "*.lnk",
        
        // Docker/Container 
        ".dockerignore", 
        
        // Archive files
        "*.zip", "*.rar", "*.7z", "*.tar", "*.gz", "*.bz2",
        
        // Media files (solitamente non utili per AI)
        "*.jpg", "*.jpeg", "*.png", "*.gif", "*.bmp", "*.ico", "*.mp4", "*.avi",
        "*.mov", "*.wmv", "*.mp3", "*.wav"
    };

    // Cartelle da nascondere NELLA STRUTTURA AD ALBERO (molto limitato)
    private readonly HashSet<string> _hiddenFromTree = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".vs", ".vscode", ".idea", // Solo le più invasive
        "bin", "obj" // Solo quelle che creano troppo rumore
    };

    public async Task GenerateAsync(string projectPath, string outputFilePath)
    {
        // Verifica che la cartella esista
        if (!Directory.Exists(projectPath))
            throw new DirectoryNotFoundException($"Project directory not found: {projectPath}");

        // Gestisci file di output esistente
        await HandleExistingOutputFileAsync(outputFilePath);

        Console.WriteLine($"🔍 Scanning project: {projectPath}");

        // Trova tutti i file per il digest (CON filtri)
        var allFiles = Directory.GetFiles(projectPath, "*.*", SearchOption.AllDirectories);
        var filesToDigest = new List<string>();

        // Filtra i file PER IL DIGEST
        foreach (var file in allFiles)
        {
            if (ShouldIncludeInDigest(file, projectPath))
                filesToDigest.Add(file);
        }

        if (filesToDigest.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️ No relevant files found to generate the digest.");
            Console.ResetColor();
            return;
        }

        Console.WriteLine($"📁 Found {filesToDigest.Count} relevant files to include in digest.");

        // Crea il contenuto markdown
        var content = await CreateMarkdownContentAsync(filesToDigest, projectPath);

        // Scrivi il file
        try
        {
            await File.WriteAllTextAsync(outputFilePath, content);
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to write digest file to '{outputFilePath}'.", ex);
        }
    }

    private async Task HandleExistingOutputFileAsync(string outputFilePath)
    {
        if (File.Exists(outputFilePath))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"🗑️ Removing existing output file: {outputFilePath}");
            Console.ResetColor();

            try
            {
                File.Delete(outputFilePath);
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to delete existing output file '{outputFilePath}': {ex.Message}", ex);
            }
        }
    }

    // METODO SEPARATO: Include file nel digest (CON filtri)
    private bool ShouldIncludeInDigest(string filePath, string projectPath)
    {
        var fileName = Path.GetFileName(filePath);
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        // 1. File prioritari sono sempre inclusi (se non in cartelle ignorate)
        if (IsPriorityFile(fileName))
        {
            return !IsInIgnoredFolder(filePath, projectPath);
        }

        // 2. Ignora se in cartella ignorata
        if (IsInIgnoredFolder(filePath, projectPath))
            return false;

        // 3. Ignora se corrisponde a pattern ignorati
        if (MatchesIgnoredPattern(fileName))
            return false;

        // 4. Includi solo se ha estensione valida
        return _allowedExtensions.Contains(extension);
    }

    // METODO SEPARATO: Include nella struttura ad albero (SENZA filtri, tranne quelli minimi)
    private bool ShouldIncludeInTree(string path, string projectPath)
    {
        var itemName = Path.GetFileName(path);

        // Solo nascondere le cartelle più invasive
        if (Directory.Exists(path))
        {
            return !_hiddenFromTree.Contains(itemName);
        }

        // Includi tutti i file nella struttura
        return true;
    }

    private bool IsPriorityFile(string fileName)
    {
        return _priorityFiles.Any(priority => priority.Equals(fileName, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsInIgnoredFolder(string filePath, string projectPath)
    {
        var relativePath = Path.GetRelativePath(projectPath, filePath);
        var pathParts = relativePath.Split(Path.DirectorySeparatorChar);

        return pathParts.Any(part =>
            _ignoredFolders.Any(ignored =>
                ignored.Equals(part, StringComparison.OrdinalIgnoreCase)));
    }

    private bool MatchesIgnoredPattern(string fileName)
    {
        foreach (var pattern in _ignoredFilePatterns)
        {
            if (SimplePatternMatch(fileName, pattern))
                return true;
        }
        return false;
    }

    private static bool SimplePatternMatch(string fileName, string pattern)
    {
        if (fileName.Equals(pattern, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!pattern.Contains('*'))
            return false;

        if (pattern.StartsWith("*."))
        {
            var extension = pattern.Substring(1);
            return fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
        }

        if (pattern.EndsWith(".*"))
        {
            var baseName = pattern.Substring(0, pattern.Length - 2);
            return fileName.StartsWith(baseName, StringComparison.OrdinalIgnoreCase);
        }

        if (pattern.StartsWith("*") && !pattern.EndsWith("*"))
        {
            var suffix = pattern.Substring(1);
            return fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
        }

        if (pattern.Count(c => c == '*') == 1)
        {
            var parts = pattern.Split('*');
            if (parts.Length == 2)
            {
                var prefix = parts[0];
                var suffix = parts[1];

                var hasPrefix = string.IsNullOrEmpty(prefix) ||
                               fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
                var hasSuffix = string.IsNullOrEmpty(suffix) ||
                               fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);

                return hasPrefix && hasSuffix;
            }
        }

        return false;
    }

    private async Task<string> CreateMarkdownContentAsync(List<string> filesToDigest, string projectPath)
    {
        var markdown = new StringBuilder();

        // Header
        markdown.AppendLine($"Total files included: {filesToDigest.Count}");
        markdown.AppendLine();

        // **STRUTTURA AD ALBERO - SENZA FILTRI (eccetto minimi)**
        var projectTree = GenerateProjectTree(projectPath);
        markdown.AppendLine("## Project Structure");
        markdown.AppendLine();
        markdown.AppendLine("```");
        markdown.AppendLine(projectTree);
        markdown.AppendLine("```");
        markdown.AppendLine();

        markdown.AppendLine("---");

        // Processa ogni file (CON filtri applicati)
        for (int i = 0; i < filesToDigest.Count; i++)
        {
            var filePath = filesToDigest[i];
            var relativePath = Path.GetRelativePath(projectPath, filePath);

            Console.WriteLine($"📄 Processing ({i + 1}/{filesToDigest.Count}): {relativePath}");

            string fileContent;
            try
            {
                fileContent = await File.ReadAllTextAsync(filePath);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"   Skipping file {relativePath} due to read error: {ex.Message}");
                Console.ResetColor();
                continue;
            }

            var language = GetLanguageFromExtension(Path.GetExtension(filePath));

            markdown.AppendLine();
            markdown.AppendLine($"## File: `{relativePath}`");
            markdown.AppendLine($"Language: `{language}`");
            markdown.AppendLine();
            markdown.AppendLine($"```{language}");
            markdown.AppendLine(fileContent);
            markdown.AppendLine("```");
            markdown.AppendLine();
            markdown.AppendLine("---");
        }

        return markdown.ToString();
    }

    // **METODO AGGIORNATO: Genera struttura senza filtri pesanti**
    private string GenerateProjectTree(string projectPath)
    {
        var projectName = Path.GetFileName(projectPath);
        var tree = new StringBuilder();

        tree.AppendLine($"{projectName}/");

        try
        {
            GenerateDirectoryTree(projectPath, tree, "", projectPath, isRoot: true);
        }
        catch (Exception ex)
        {
            tree.AppendLine($"Error generating tree: {ex.Message}");
        }

        return tree.ToString();
    }

    private void GenerateDirectoryTree(string directoryPath, StringBuilder tree, string indent, string rootPath, bool isRoot = false)
    {
        try
        {
            // Ottieni tutti gli elementi (directory + file)
            var directories = Directory.GetDirectories(directoryPath)
                .Where(dir => ShouldIncludeInTree(dir, rootPath)) // Filtri MINIMI
                .Select(dir => new { Path = dir, Name = Path.GetFileName(dir), IsDirectory = true })
                .OrderBy(item => item.Name);

            var files = Directory.GetFiles(directoryPath)
                .Where(file => ShouldIncludeInTree(file, rootPath)) // Filtri MINIMI
                .Select(file => new { Path = file, Name = Path.GetFileName(file), IsDirectory = false })
                .OrderBy(item => item.Name);

            var allItems = directories.Concat(files).ToList();

            for (int i = 0; i < allItems.Count; i++)
            {
                var item = allItems[i];
                var isLast = i == allItems.Count - 1;
                var currentIndent = isRoot ? "" : indent;
                var connector = isLast ? "└── " : "├── ";
                var nextIndent = isRoot ? "" : indent + (isLast ? "    " : "│   ");

                if (item.IsDirectory)
                {
                    tree.AppendLine($"{currentIndent}{connector}{item.Name}");

                    // Ricorsione per sottodirectory
                    GenerateDirectoryTree(item.Path, tree, nextIndent, rootPath);
                }
                else
                {
                    tree.AppendLine($"{currentIndent}{connector}{item.Name}");
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            tree.AppendLine($"{indent}└── [Access Denied]");
        }
        catch (Exception ex)
        {
            tree.AppendLine($"{indent}└── [Error: {ex.Message}]");
        }
    }

    private static string GetLanguageFromExtension(string extension)
    {
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
            ".h" or ".hpp" or ".hh" => "cpp",
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
            ".sh" => "bash",
            ".ps1" => "powershell",
            ".fs" or ".fsi" or ".fsx" => "fsharp",
            ".csproj" => "xml",
            ".sln" => "text",
            ".txt" => "text",
            _ => "text"
        };
    }
}
