using System;
using Xunit;
using System.IO;
using Microsoft.ApplicationInspector.Commands;
using Microsoft.ApplicationInspector.CLI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AppInspector.Tests.Commands
{
    public class TestMarkdownWriter
    {
        private readonly ILoggerFactory factory = new NullLoggerFactory();

        [Fact]
        public void MarkdownWriterTest()
        {
            // Arrange
            var testFilePath = Path.Combine("TestData", "TestAnalyzeCmd", "Samples", "FourWindowsOneLinux.js");
            var testRulesPath = Path.Combine("TestData", "TestAnalyzeCmd", "Rules", "FindWindows.json");
            var outputPath = Path.Combine(Path.GetTempPath(), $"test_markdown_{Guid.NewGuid()}.md");

            try
            {
                // Run analyze command
                AnalyzeOptions options = new()
                {
                    SourcePath = new[] { testFilePath },
                    CustomRulesPath = testRulesPath,
                    IgnoreDefaultRules = true
                };

                AnalyzeCommand command = new(options, factory);
                var result = command.GetResult();

                Assert.Equal(AnalyzeResult.ExitCode.Success, result.ResultCode);

                // Write markdown
                using var streamWriter = new StreamWriter(outputPath);
                var cliOptions = new CLIAnalyzeCmdOptions { OutputFilePath = outputPath, OutputFileFormat = "markdown" };
                var writer = new AnalyzeMarkdownWriter(streamWriter, factory);
                writer.WriteResults(result, cliOptions);

                // Verify output
                Assert.True(File.Exists(outputPath));
                var content = File.ReadAllText(outputPath);
                
                Assert.Contains("# Application Inspector Analysis Report", content);
                Assert.Contains("## Summary", content);
                Assert.Contains("## Key Statistics", content);
                Assert.Contains("## Key Features Detected", content);
                Assert.Contains("| Metric | Count |", content);
                Assert.Contains("| Total Files |", content);
                Assert.Contains("| Files Analyzed |", content);
            }
            finally
            {
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }
            }
        }
    }
}
