function Test-MdCodeBlock {
    Param(
        [Parameter(ValueFromPipeline = $true)]
        [String]
        $InputObject,

        [Switch]
        $AsBranch
    )

    Process {
        return $InputObject -match "^\s*``````"
    }
}

function Select-MdCodeBlock {
    Param(
        [Parameter(ValueFromPipeline = $true)]
        [String]
        $InputObject
    )

    Process {
        $capture = [Regex]::Match( `
            $InputObject, `
            "^(?<indent>\s*)``````(?<lang>\S+)?"
        )

        return $([PsCustomObject]@{
            Success = $capture.Success
            Indent = $capture.Groups['indent']
            Language = $capture.Groups['lang']
        })
    }
}

function Add-MdCodeBlockRow {
    Param(
        [Parameter(ValueFromPipeline = $true)]
        [PsCustomObject]
        $CodeBlock,

        [String[]]
        $Row
    )

    foreach ($line in $Row) {
        $CodeBlock.Lines += @(
            $line -replace "^$($CodeBlock.Indent)", ""
        )
    }
}

function Get-MdCodeBlock {
    [CmdletBinding(DefaultParameterSetName = 'OnlyAllCodeBlocks')]
    Param(
        [Parameter(ValueFromPipeline = $true)]
        [String[]]
        $InputObject,

        [Parameter(ParameterSetName = 'NarrowByLanguage')]
        [String]
        $Language,

        [Parameter(ParameterSetName = 'GetMinus')]
        [Switch]
        $Minus,

        [Switch]
        $Numbered
    )

    Begin {
        $snippets = @()
        $snippet = $null
        $count = 0
        $minusLines = @()
    }

    Process {
        foreach ($line in $InputObject) {
            $count = $count + 1

            if ($null -eq $snippet) {
                $blockStart = Select-MdCodeBlock `
                    -InputObject $line

                $lang = $blockStart.Language.Value

                if (-not [String]::IsNullOrWhiteSpace($Language) `
                    -and $lang.ToLower() `
                    -ne $Language.ToLower()
                ) {
                    continue
                }

                if ($blockStart.Success) {
                    $snippet = [PsCustomObject]@{
                        LineNumber = $count
                        Lines = @()
                        Language = $lang
                        Indent = $blockStart.Indent
                    }
                }

                if ($Minus -and -not $blockStart.Success) {
                    $minusLines += @([PsCustomObject]@{
                        LineNumber = $count
                        Line = $line
                    })
                }

                continue
            }

            $blockEnd = $line | Test-MdCodeBlock `

            if ($blockEnd) {
                $snippets += @($snippet)
                $snippet = $null
                continue
            }

            Add-MdCodeBlockRow `
                -CodeBlock $snippet `
                -Row $line
        }
    }

    End {
        return $(if ($Minus) {
            [PsCustomObject]@{
                CodeBlocks =
                    $snippets | foreach {
                        [PsCustomObject]@{
                            LineNumber = $_.LineNumber
                            Lines = $_.Lines
                            Language = $_.Language
                        }
                    }
                Minus = $minusLines
            }
        }
        else {
            $snippets | foreach {
                [PsCustomObject]@{
                    LineNumber = $_.LineNumber
                    Lines = $_.Lines
                    Language = $_.Language
                }
            }
        })
    }
}
