using System;
using System.IO;
using Xunit;

namespace AppInspector.Tests;

/// <summary>
///     Detects whether the machine running the tests can create symbolic links. Creating a symbolic link on Windows
///     requires either Developer Mode or the SeCreateSymbolicLinkPrivilege, and some file systems do not support
///     links at all, so symlink tests are skipped rather than failed in those environments.
/// </summary>
internal static class SymlinkTestSupport
{
    private static readonly Lazy<string?> LazySkipReason = new(GetSkipReason);

    /// <summary>
    ///     The reason symlink tests cannot run here, or null when they can run.
    /// </summary>
    public static string? SkipReason => LazySkipReason.Value;

    /// <summary>
    ///     Creates a uniquely named directory for a symlink test under the same root the probe uses, so that the
    ///     skip decision and the test itself exercise the same file system.
    /// </summary>
    /// <param name="prefix">A short prefix describing the test, used in the directory name.</param>
    /// <returns>The path of the created directory.</returns>
    public static string CreateTestRoot(string prefix)
    {
        var testRoot = Path.Combine(TestRootParent, $"{prefix}-{Guid.NewGuid()}");
        Directory.CreateDirectory(testRoot);
        return testRoot;
    }

    /// <summary>
    ///     Deletes a directory created by <see cref="CreateTestRoot" />. Cleanup failures are ignored so that they
    ///     cannot replace a genuine assertion failure when called from a finally block.
    /// </summary>
    /// <param name="testRoot">The directory to delete.</param>
    public static void TryDeleteTestRoot(string testRoot)
    {
        try
        {
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, true);
            }
        }
        catch (Exception)
        {
            // Cleanup is best effort. Leaving a directory behind in TestOutput must not mask a test result.
        }
    }

    // The tests write beneath the working directory, not the system temp directory, and the two can be different
    // file systems with different symlink support. Probing here keeps the skip decision accurate.
    private static string TestRootParent => "TestOutput";

    private static string? GetSkipReason()
    {
        string testRoot;

        try
        {
            Directory.CreateDirectory(TestRootParent);
            testRoot = Path.Combine(TestRootParent, $"SymlinkProbe-{Guid.NewGuid()}");
            Directory.CreateDirectory(testRoot);
        }
        catch (Exception ex)
        {
            return $"Could not create a directory to probe for symlink support: {ex.GetType().Name}: {ex.Message}";
        }

        try
        {
            var fileTarget = Path.Combine(testRoot, "target.txt");
            var directoryTarget = Path.Combine(testRoot, "target-directory");
            File.WriteAllText(fileTarget, string.Empty);
            Directory.CreateDirectory(directoryTarget);
            File.CreateSymbolicLink(Path.Combine(testRoot, "file-link"), fileTarget);
            Directory.CreateSymbolicLink(Path.Combine(testRoot, "directory-link"), directoryTarget);
            return null;
        }
        catch (Exception ex)
        {
            // Catching broadly on purpose: this runs from a lazy initializer, and an unexpected exception type
            // would otherwise surface as an opaque TypeInitializationException instead of a readable skip reason.
            return $"Symlink creation is not available here: {ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            TryDeleteTestRoot(testRoot);
        }
    }
}

/// <summary>
///     A <see cref="FactAttribute" /> that skips when the machine cannot create symbolic links.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class SymlinkFactAttribute : FactAttribute
{
    public SymlinkFactAttribute()
    {
        Skip = SymlinkTestSupport.SkipReason;
    }
}

/// <summary>
///     A <see cref="TheoryAttribute" /> that skips when the machine cannot create symbolic links.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class SymlinkTheoryAttribute : TheoryAttribute
{
    public SymlinkTheoryAttribute()
    {
        Skip = SymlinkTestSupport.SkipReason;
    }
}
