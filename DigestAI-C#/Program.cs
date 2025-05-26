using DigestAI_Csharp;

if (args.Length == 0)
{
    Console.WriteLine("AI Digest Generator");
    Console.WriteLine("Usage: AiDigest.exe [path] [options]");
    Console.WriteLine("  path: Project path (default: current directory)");
    Console.WriteLine("  --output: Output file name (default: ai-digest.md)");
    return;
}

var projectPath = args.Length == 0 || args[0] == "." || args[0] == "./" ?
    Directory.GetCurrentDirectory() :
    Path.GetFullPath(args[0]);

var outputFile = GetArgumentValue(args, "--output") ?? "ai-digest.md";

var generator = new ProjectDigestGenerator();
await generator.GenerateAsync(projectPath, outputFile);

Console.WriteLine($"✅ Digest generated: {outputFile}");

static string? GetArgumentValue(string[] args, string argument)
{
    var index = Array.IndexOf(args, argument);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}
