using System.Management.Automation;

namespace PsMarkdownTree;

[Cmdlet(VerbsCommon.Get, "HelloWorld")]
public class GetHelloWorldCommand : Cmdlet
{
    protected override void ProcessRecord()
    {
        base.ProcessRecord();
        WriteObject("Hello, world!");
    }
}

[Cmdlet(VerbsCommon.Get, "Greeting")]
public class GetGreetingCommand : Cmdlet
{
    [Parameter(
        ValueFromPipeline = true,
        Position = 0
    )]
    public string[] Name { get; set; } = [];

    protected override void ProcessRecord()
    {
        base.ProcessRecord();

        foreach (var item in Name)
            if (item is not null)
                WriteObject($"Hello, {item}!");
    }
}

[Cmdlet(VerbsCommon.Get, "Expletive")]
public class GetExpletiveCommand : Cmdlet
{
    private readonly string[] _adjectives = [
        "non-standard",
        "gormless",
        "crusted",
        "rotten",
    ];

    protected override void ProcessRecord()
    {
        base.ProcessRecord();
        WriteObject($"What in the {string.Join(", ", _adjectives)} heck is this?");
    }
}










