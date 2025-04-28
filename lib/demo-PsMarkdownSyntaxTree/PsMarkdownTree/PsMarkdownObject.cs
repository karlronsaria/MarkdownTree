using System.Management.Automation;

namespace PsMarkdownTree;

public interface IPsObjectConvertible
{
    public PSCustomObject Convert();
}

internal class PsMarkdownObject
{
}
