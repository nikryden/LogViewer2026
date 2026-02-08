using LogViewer2026.Core.TestUtilities;

Console.WriteLine("LogViewer2026 Test Data Generator");
Console.WriteLine("=================================");
Console.WriteLine();

var testDataDir = Path.Combine(Environment.CurrentDirectory, "TestData");
Directory.CreateDirectory(testDataDir);

Console.WriteLine("Generating test files...");

Console.Write("- Small JSON (100 lines)... ");
LogFileGenerator.GenerateJsonLogFile(Path.Combine(testDataDir, "small.json"), 100);
Console.WriteLine("Done");

Console.Write("- Medium JSON (10,000 lines)... ");
LogFileGenerator.GenerateJsonLogFile(Path.Combine(testDataDir, "medium.json"), 10_000);
Console.WriteLine("Done");

Console.Write("- Large JSON (1,000,000 lines)... ");
LogFileGenerator.GenerateJsonLogFile(Path.Combine(testDataDir, "large.json"), 1_000_000);
Console.WriteLine("Done");

Console.Write("- Small Text (100 lines)... ");
LogFileGenerator.GenerateTextLogFile(Path.Combine(testDataDir, "small.log"), 100);
Console.WriteLine("Done");

Console.Write("- Medium Text (10,000 lines)... ");
LogFileGenerator.GenerateTextLogFile(Path.Combine(testDataDir, "medium.log"), 10_000);
Console.WriteLine("Done");

Console.Write("- Large Text (1,000,000 lines)... ");
LogFileGenerator.GenerateTextLogFile(Path.Combine(testDataDir, "large.log"), 1_000_000);
Console.WriteLine("Done");

Console.WriteLine();
Console.WriteLine($"Test files generated in: {testDataDir}");
Console.WriteLine();
Console.WriteLine("Files created:");
foreach (var file in Directory.GetFiles(testDataDir))
{
    var fileInfo = new FileInfo(file);
    Console.WriteLine($"  {Path.GetFileName(file)} ({fileInfo.Length:N0} bytes)");
}
