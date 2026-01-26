using Microsoft.PowerShell.Commands;
using System.Management.Automation;

using System.Text;
using System.Text.RegularExpressions;

namespace PsMarkdownTree;

[Cmdlet(
    VerbsCommon.Select, "MarkdownString",
    DefaultParameterSetName = "File",
    HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097119"
)]
public class SelectMarkdownStringCommand : PSCmdlet
{
    [Parameter()]
    [ValidateNotNull()]
    public string Culture = "Current";

    [Parameter(ParameterSetName = "Object", Mandatory = true, ValueFromPipeline = true)]
    [Parameter(ParameterSetName = "ObjectRaw", Mandatory = true, ValueFromPipeline = true)]
    [AllowNull()]
    [AllowEmptyString()]
    public PSObject[] InputObject;

    [Parameter(Mandatory = true, Position = 0)]
    public string[] Pattern = [];

    [Parameter(ParameterSetName = "File", Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true)]
    [Parameter(ParameterSetName = "FileRaw", Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true)]
    public string[] Path;

    [Parameter(ParameterSetName = "LiteralFile", Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [Parameter(ParameterSetName = "LiteralFileRaw", Mandatory = true, ValueFromPipelineByPropertyName = true)]
    [Alias("PSPath", "LP")]
    public string[] LiteralPath;

    [Parameter(ParameterSetName = "ObjectRaw", Mandatory = true)]
    [Parameter(ParameterSetName = "FileRaw", Mandatory = true)]
    [Parameter(ParameterSetName = "LiteralFileRaw", Mandatory = true)]
    public SwitchParameter Raw;

    public SwitchParameter SimpleMatch;

    public SwitchParameter CaseSensitive;

    [Parameter(ParameterSetName = "Object")]
    [Parameter(ParameterSetName = "File")]
    [Parameter(ParameterSetName = "LiteralFile")]
    public SwitchParameter Quiet;

    public SwitchParameter List;

    public SwitchParameter NoEmphasis;

    [ValidateNotNullOrEmpty()]
    public string[] Include = [];

    [ValidateNotNullOrEmpty()]
    public string[] Exclude = [];

    public SwitchParameter NotMatch;

    public SwitchParameter AllMatches;

    [ValidateNotNullOrEmpty()]
    public System.Text.Encoding? Encoding;

    [ValidateNotNullOrEmpty()]
    [ValidateCount(1, 2)]
    [ValidateRange(0, 2147483647)]
    public int[] Context;
}




