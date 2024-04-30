[TestFixture]
public class ProgramTests
{
    [Test]
    public async Task Help()
    {
        await using var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(writer);
        await Program.Main(["--help"]);
        await Verify(writer.GetStringBuilder());
    }
}