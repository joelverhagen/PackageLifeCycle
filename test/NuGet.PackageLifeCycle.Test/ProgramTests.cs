using System.Text;

namespace NuGet.PackageLifeCycle;

public class ProgramTests
{
    [Fact]
    public async Task Help()
    {
        await Verify(RunProgram(["--help"]));
    }

    [Fact]
    public async Task Help_Deprecate()
    {
        await Verify(RunProgram(["--help", "deprecate"]));
    }

    private static async Task<StringBuilder> RunProgram(string[] args)
    {
        await using var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        await Program.Main(args);
        return writer.GetStringBuilder();
    }
}
