using System.Text;

[TestFixture]
public class ProgramTests
{
    [Test]
    public Task Help() =>
        Verify(RunProgram(["--help"]));

    [Test]
    public Task Help_deprecate() =>
        Verify(RunProgram(["--help", "deprecate"]));

    static async Task<StringBuilder> RunProgram(string[] args)
    {
        await using var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        await Program.Main(args);
        return writer.GetStringBuilder();
    }
}