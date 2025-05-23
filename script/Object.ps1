<#
.SYNOPSIS
f: tree -> markdown
#>
function Write-MarkdownTree {
    Param(
        [Parameter(ValueFromPipeline = $true)]
        $InputObject,

        [Int]
        $Level = 0,

        [Switch]
        $AsTree,

        [Switch]
        $BranchTables,

        [Switch]
        $NoTables
    )

    Process {
        if ($null -eq $InputObject) {
            return
        }

        switch ($InputObject) {
            { $InputObject -is [Array] } {
                foreach ($subitem in $InputObject) {
                    if ($NoTables) {
                        Write-MarkdownTree " " $Level -AsTree:$AsTree -NoTables:$NoTables
                        Write-MarkdownTree $subitem ($Level + 1) -AsTree:$AsTree -NoTables:$NoTables
                    }
                    else {
                        $table = $subitem | Write-MdTable
                        Write-Output ""

                        if ($BranchTables) {
                            $lead = '- '

                            foreach ($row in $table) {
                                Write-Output "$('  ' * $Level)$lead$row"
                                $lead = '  '
                            }
                        }
                        else {
                            $table
                        }

                        Write-Output ""
                    }
                }
            }

            { $InputObject -is [PsCustomObject] } {
                $properties = $InputObject.
                    PsObject.
                    Properties |
                    where {
                        'NoteProperty' -eq $_.MemberType
                    }

                foreach ($property in $properties) {
                    if (-not $AsTree `
                        -and $property.Name -eq 'complete' `
                        -and $property.Value -is [Boolean])
                    {
                        continue
                    }

                    if ($property.Name -eq 'list_subitem') {
                        Write-MarkdownTree `
                            -InputObject $property.Value `
                            -Level $Level `
                            -AsTree:$AsTree `
                            -NoTables:$NoTables

                        continue
                    }

                    $list = Write-MarkdownTree `
                        -InputObject $property.Value `
                        -Level ($Level + 1) `
                        -AsTree:$AsTree `
                        -NoTables:$NoTables

                    $inline =
                        [String]::IsNullOrWhiteSpace($property.Name) `
                        -and @($list).Count -gt 0

                    if ($inline) {
                        Write-Output "- $($list[0].Trim())"
                        Write-Output $list[1 .. ($list.Count - 1)]
                        continue
                    }

                    $actionItemCapture = [PsCustomObject]@{
                        Success = $false
                    }

                    $token = ''

                    if (-not $AsTree) {
                        $actionItemCapture = $property.Value `
                            | Get-NoteProperty `
                                -PropertyName 'complete'

                        $token =
                            if ($actionItemCapture.Value) { 'x' } else { ' ' }
                    }

                    $content = if ($actionItemCapture.Success) {
                        "[$token] $($property.Name)"
                    } else {
                        $property.Name
                    }

                    Write-Output "$('  ' * $Level)- $content"
                    Write-Output $list
                }
            }

            default {
                Write-Output "$('  ' * $Level)- $InputObject"
            }
        }
    }
}

<#
.SYNOPSIS
f: tree -> str -> tree
f: tree -> str -> str -> tree
f: tree -> (tree -> bool) -> tree
f: tree -> (tree -> bool) -> str -> tree
#>
function Find-Subtree {
    [CmdletBinding(DefaultParameterSetName = 'ByPropertyName')]
    Param(
        [Parameter(ValueFromPipeline = $true)]
        $InputObject,

        [Parameter(ParameterSetName = 'ByPropertyName')]
        [String]
        $PropertyName,

        [Parameter(ParameterSetName = 'ByScriptBlock')]
        [ScriptBlock]
        $Where,

        [String]
        $Parent
    )

    Process {
        if ($null -eq $InputObject) {
            return @()
        }

        switch ($InputObject) {
            { $InputObject -is [Array] } {
                $params = switch ($PsCmdlet.ParameterSetName) {
                    'ByPropertyName' {
                        @{
                            InputObject = $null
                            PropertyName = $PropertyName
                            Parent = $Parent
                        }
                    }

                    'ByScriptBlock' {
                        @{
                            InputObject = $null
                            'Where' = $Where
                            Parent = $Parent
                        }
                    }
                }

                @($InputObject) |
                    where { $_ } |
                    foreach {
                        $params.InputObject = $_
                        Find-Subtree @params
                    }

                break
            }

            { $InputObject -is [PsCustomObject] } {
                $properties = $InputObject.PsObject.Properties |
                    where {
                        'NoteProperty' -eq $_.MemberType
                    }

                if ($null -eq $properties) {
                    @()
                    break
                }

                switch ($PsCmdlet.ParameterSetName) {
                    'ByScriptBlock' {
                        $result = $InputObject | where $Where

                        if ($result) {
                            if ($Parent) {
                                [PsCustomObject]@{
                                    parent = $Parent
                                    child = $result
                                }
                            }
                            else {
                                $result
                            }
                        }

                        foreach ($property in $properties) {
                            $params = @{
                                InputObject = $property.Value
                                'Where' = $Where
                            }

                            Find-Subtree @params
                        }

                        break
                    }

                    'ByPropertyName' {
                        if ($PropertyName -in $properties.Name) {
                            if ($Parent) {
                                [PsCustomObject]@{
                                    parent = $Parent
                                    child = $InputObject
                                }
                            }
                            else {
                                $InputObject
                            }
                        }
                        else {
                            foreach ($property in $properties) {
                                $params = @{
                                    InputObject = $property.Value
                                    PropertyName = $PropertyName
                                }

                                if ($Parent) {
                                    $params | Add-Member `
                                        -MemberType NoteProperty `
                                        -Name Parent `
                                        -Value $property.Name
                                }

                                Find-Subtree @params
                            }
                        }

                        break
                    }
                }

                break
            }
        }
    }
}

<#
.SYNOPSIS
f: tree -> tree
#>
function Get-SubtreeRotation {
    Param(
        [Parameter(ValueFromPipeline = $true)]
        $InputObject,

        [String[]]
        $RotateProperty
    )

    Begin {
        $tree = [PsCustomObject]@{}
    }

    Process {
        if ([String]::IsNullOrEmpty($RotateProperty)) {
            return $InputObject
        }

        $properties = $InputObject.PsObject.Properties
        $subtree = [PsCustomObject]@{}

        $rotate = $properties | where {
            $_.Name -in $RotateProperty
        }

        if ($null -eq $rotate) {
            return $InputObject
        }

        # For every property name submitted, make an attempt to create a
        # new object with rotated properties, but return on first successful
        # occurrence
        foreach ($attempt in $rotate) {
            # Create an object identical to the input object sans the
            # properties to be rotated
            $properties | where {
                $_.Name -notin $RotateProperty
            } | foreach {
                $subtree | Add-Member `
                    -MemberType NoteProperty `
                    -Name $_.Name `
                    -Value $_.Value
            }

            switch ($attempt.Value) {
                { $_ -is [String] } {
                    # Graft the newly created object as subtree to the new
                    # tree, with the rotated property as the parent
                    $tree | Add-Member `
                        -MemberType NoteProperty `
                        -Name $_ `
                        -Value $subtree
                }

                { $_ -is [PsCustomObject] } {
                    $subproperties = $_.PsObject.Properties | where {
                        $_.MemberType -eq 'NoteProperty'
                    }

                    foreach ($subproperty in $subproperties) {
                        $subtreeCopy = $subtree.PsObject.Copy()

                        $subtreeCopy | Add-Member `
                            -MemberType NoteProperty `
                            -Name $attempt.Name `
                            -Value $subproperty.Value

                        $tree | Add-Member `
                            -MemberType NoteProperty `
                            -Name $subproperty.Name `
                            -Value $subtreeCopy
                    }
                }
            }

            # return on first successful occurrence
            break
        }
    }

    End {
        if ([String]::IsNullOrEmpty($RotateProperty)) {
            return
        }

        return $tree
    }
}

<#
.SYNOPSIS
f: str -> tree
f: str -> int -> tree
.PARAMETER DepthLimit
Note: Inline or folded trees can escape the depth limit
#>
function Get-MarkdownTree {
    Param(
        [Parameter(ValueFromPipeline = $true)]
        [String]
        $Line,

        [Int]
        $DepthLimit = -1,

        [String[]]
        $MuteProperty
    )

    Begin {
        function Test-EmptyObject {
            Param(
                [PsCustomObject]
                $InputObject
            )

            return 0 -eq @($InputObject.PsObject.Properties).Count
        }

        function Add-Property {
            Param(
                $InputObject,

                [String]
                $Name,

                $Value,

                [Switch]
                $Overwrite,

                [Switch]
                $Table
            )

            $property = $InputObject.PsObject.Properties | where {
                $_.Name -eq $Name
            }

            if ($null -eq $property) {
                $InputObject | Add-Member `
                    -MemberType NoteProperty `
                    -Name $Name `
                    -Value $Value

                return
            }

            if (@($property.Value).Count -eq 1) {
                $property.Value = @($property.Value)
            }

            if ((Test-EmptyObject $property.Value[-1])) {
                $property.Value[-1] = @($Value)
            }
            else {
                $property.Value += @($Value)
            }
        }

        function Add-Table {
            Param(
                [PsCustomObject[]]
                $Stack,

                [PsCustomObject[]]
                $Table,

                [PsCustomObject]
                $TableStart,

                [Int]
                $PrevLevel,

                [String]
                $PrevName
            )

            $Table = ConvertTo-MdTable `
                -TableBuild $Table

            $tempLevel = if ('ListItem' -in $TableStart.Type) {
                $TableStart.Level - 2
            }
            else {
                $PrevLevel - 1
            }

            Add-Property `
                -InputObject $Stack[$tempLevel] `
                -Name $PrevName `
                -Value $Table
        }

        <#
        .SYNOPSIS
        f: str -> (int, enum, str)
        #>
        function Get-LexInfo {
            Param(
                [Parameter(ValueFromPipeline = $true)]
                [String]
                $Line
            )

            Begin {
                $prevType = 'None'
                $level = 0
                $indentLength = 0
                $pattern =
                    '^(?<indent>\s*)((?<header>#+)|(?<branch_start>\-|\*|\d+\.)\s)?\s*(?<content>.+)?$'
                $inCodeBlock = $false
                $codeBlockLevel = -1
            }

            Process {
                $capture = [Regex]::Match($Line, $pattern)
                $header = $capture.Groups['header']
                $indent = $capture.Groups['indent']
                $content = $capture.Groups['content']
                $type = @()

                if ($inCodeBlock) {
                    if (($content.Value | Test-MdCodeBlock)) {
                        $type += @('CodeBlock')
                        $inCodeBlock = $false
                    }
                    else {
                        $type += @('UngraftedRow')
                    }
                }
                else {
                    $type += if ($capture.Groups['branch_start'].Success) {
                        if ($content.Success) {
                            @('ListItem')
                        }
                        else {
                            @('UngraftedRow')
                        }
                    }
                    elseif ($header.Success) {
                        @('Header')
                    }
                    else {
                        @()
                    }

                    if ('Header' -notin $type `
                        -and $content.Success
                    ) {
                        if (($content.Value | Test-MdTable)) {
                            $type += @('TableRow')
                        }
                        elseif (($content.Value | Test-MdCodeBlock)) {
                            $type += @('CodeBlock')
                            $inCodeBlock = -not $inCodeBlock
                        }
                        else {
                            $type += @('UngraftedRow')
                        }
                    }

                    if ($type.Count -eq 0) {
                        return
                    }
                }

                $nextLevel = if ('Header' -in $type) {
                    $header.Length
                }
                elseif ('Header' -in $prevType) {
                    $level + 1
                }
                elseif ($indent.Length -ne $indentLength) {
                    $level + ($indent.Length - $indentLength) / 2
                }
                else {
                    $level
                }

                if (-not $inCodeBlock -or 'CodeBlock' -in $type) {
                    $level = $nextLevel
                }

                $indentLength = $indent.Length
                $prevType = $type

                return [PsCustomObject]@{
                    Level =
                        if ($inCodeBlock) {
                            $level
                        }
                        else {
                            $nextLevel
                        }
                    Type = $type
                    Content = $content.Value
                    IndentLength = $indent.Length
                }
            }
        }

        <#
        .SYNOPSIS
        f: (int, str) -> tree
        #>
        function Get-Parse {
            Param(
                [Parameter(ValueFromPipeline = $true)]
                [PsCustomObject[]]
                $TableRow,

                [Int]
                $HighestLevel,

                [String[]]
                $MuteProperty
            )

            Begin {
                $stack = @($null) * ($HighestLevel + 1)
                # $keys = @($null) * ($HighestLevel + 1)
                $stack[0] = [PsCustomObject]@{}
                $level = 0
                $tableBuild = $null
                $prevLevel = 0
                $prevName = ""
                $tableStart = $null
                $snippet = $null

                function Convert-StackLeafToFoldedBranch {
                    Param(
                        [Object[]]
                        $Stack,

                        [Int]
                        $Level,

                        [String]
                        $PrevPropertyName
                    )

                    if (-not $Stack[$Level] -is [String]) {
                        return $Stack
                    }

                    $props = $Stack[$Level - 1].PsObject.Properties `
                        | where {
                            $_.Name -eq $PrevPropertyName
                        }

                    foreach ($prop in $props) {
                        $Stack[$Level - 1] | Add-Member `
                            -MemberType NoteProperty `
                            -Name "$($prop.Name): $($prop.Value)" `
                            -Value $Stack[$Level]

                        $Stack[$Level - 1].
                            PsObject.
                            Properties.
                            Remove($prop.Name)
                    }

                    $Stack[$Level] = [PsCustomObject]@{}
                    return $Stack
                }

                function Test-IsLeaf {
                    Param(
                        [PsCustomObject]
                        $InputObject
                    )

                    $props = Get-NoteProperty $InputObject

                    return @($props).Count -eq 1 -and $(
                            $value = @($props)[0].Value;
                            $value -is [PsCustomObject]
                        ) -and
                        $null -eq (Get-NoteProperty $value)
                }

                function Convert-LeafToString {
                    Param(
                        [PsCustomObject]
                        $InputObject
                    )

                    foreach ($prop in (Get-NoteProperty $InputObject)) {
                        $value = $prop.Value

                        if (Test-IsLeaf $value) {
                            $InputObject.($prop.Name) =
                                @(Get-NoteProperty $value)[0].Name
                        }
                        else {
                            $value | foreach {
                                Convert-LeafToString $_
                            }
                        }
                    }
                }
            }

            Process {
                $level = $TableRow.Level
                $content = $TableRow.Content

                if ($null -eq $stack[$level - 1] -and $null -eq $snippet) {
                    return 'Error'
                }

                $capture = [Regex]::Match( `
                    $content, `
                    "^\s*(\[(?<check>x| )\] )?((?<key>[^:`"]+)\s*:\s+)?(?<value>.*)?\s*$" `
                )

                # if ($level -lt $prevLevel) {
                #     # todo
                #     Convert-LeafToString $stack[$prevLevel]
                # }

                $stack[$level] = [PsCustomObject]@{}

                if ('TableRow' -in $TableRow.Type) {
                    if ($null -eq $tableBuild) {
                        $tableBuild = New-MdTableBuild
                        $tableStart = $TableRow
                    }

                    $tableBuild | Add-MdTableRow `
                        -RowList $content `
                        -RemoveStyling

                    return
                }

                if ($null -ne $tableBuild) {
                    Add-Table `
                        -Stack $stack `
                        -Table $tableBuild `
                        -TableStart $tableStart `
                        -PrevLevel $prevLevel `
                        -PrevName $prevName

                    $tableBuild = $null
                }

                if ('CodeBlock' -in $TableRow.Type) {
                    if ($null -eq $snippet) {
                        $blockCapture = Select-MdCodeBlock `
                            -InputObject $content

                        $snippet = [PsCustomObject]@{
                            Lines = @()
                            Language = $blockCapture.Language.Value
                            Indent = $TableRow.IndentLength
                        }
                    }
                    else {
                        $value = [PsCustomObject]@{
                            Lines = $snippet.Lines
                            Language = $snippet.Language
                        }

                        if (-not $prevName) {
                            $stack[$prevLevel - 1] = $value
                            return
                        }

                        Add-Property `
                            -InputObject $stack[$prevLevel - 1] `
                            -Name $prevName `
                            -Value $value

                        $snippet = $null
                    }

                    return
                }
                elseif ($null -ne $snippet) {
                    $snippet.Lines += @(
                        if ($content -match "^\s*$") {
                            $content
                        }
                        else {
                            "$(' ' * ($TableRow.IndentLength - $snippet.Indent))$content"
                        }
                    )

                    return
                }

                $checkGroup = $capture.Groups['check']

                if ($checkGroup.Success) {
                    Add-Property `
                        -InputObject $stack[$level] `
                        -Name 'complete' `
                        -Value ($checkGroup.Value -eq 'x')
                }

                $prevLevel = $level
                $keyCapture = $capture.Groups['key']

                if ($keyCapture.Success) {
                    $key = $keyCapture.Value
                    $value = $capture.Groups['value'].Value

                    if ([String]::IsNullOrWhiteSpace($key)) {
                        $key = 'list_subitem'
                    }

                    if ($key -notin $MuteProperty) {
                        Add-Property `
                            -InputObject $stack[$level - 1] `
                            -Name $key `
                            -Value ([PsCustomObject]@{
                                $value =
                                    if (@($stack).Count -gt 1) {
                                        $stack[$level]
                                    }
                                    else {
                                        $null
                                    }
                            })

                        # $stack[$level - 1] = $stack[$level]
                        $prevName = $key
                    }

                    return
                }

                $content = $capture.Groups['value']

                if ([String]::IsNullOrWhiteSpace($content)) {
                    $content = 'list_subitem'
                }

                # # Reprocess key-value expression as a single property
                # # name. This only happens when a string is left behind on
                # # the stack.
                # $stack = Convert-StackLeafToFoldedBranch `
                #     -Stack $stack `
                #     -Level ($level - 1) `
                #     -PrevPropertyName $prevName

                if ($content -notin $MuteProperty) {
                    Add-Property `
                        -InputObject $stack[$level - 1] `
                        -Name $content `
                        -Value $(
                            if (@($stack).Count -gt 1) {
                                $stack[$level]
                            }
                            else {
                                $null
                            }
                        )

                    $prevName = $content
                }
            }

            End {
                if ($null -ne $tableBuild) {
                    Add-Table `
                        -Stack $stack `
                        -Table $tableBuild `
                        -TableStart $tableStart `
                        -PrevLevel $prevLevel `
                        -PrevName $prevName
                }

                Convert-LeafToString $stack[0]
                return $stack[0]
            }
        }

        $content = @()
        $startLevel = $null
        $highestLevel = $null
    }

    Process {
        $content += @($Line)
    }

    End {
        $content = $content `
            | Get-LexInfo `
            | foreach {
                if ($null -eq $startLevel) {
                    $highestLevel = $startLevel = $_.Level
                }

                if ($_.Level -gt $highestLevel) {
                    $highestLevel = $_.Level
                }

                $_
            }

        return $content `
            | where {
                $_.Type.Count -gt 0
            } `
            | where {
                $DepthLimit -eq -1 -or $_.Level -le $DepthLimit
            } `
            | Get-Parse `
                -HighestLevel $highestLevel `
                -MuteProperty:$MuteProperty `
            | where {
                -not (Test-EmptyObject $_)
            }
    }
}

<#
.SYNOPSIS
f: P(key, value) -> key -> value
#>
function Get-NoteProperty {
    Param(
        [Parameter(
            ValueFromPipeline = $true,
            Position = 0
        )]
        [PsCustomObject]
        $InputObject,

        [String]
        $PropertyName,

        $Default
    )

    $properties = $InputObject.PsObject.Properties `
        | where { 'NoteProperty' -eq $_.MemberType }

    if ([String]::IsNullOrEmpty($PropertyName)) {
        return $properties
    }

    try {
        return $(if ($null -eq $properties -or @($properties).Count -eq 0) {
            [PsCustomObject]@{
                Success = $false
                Name = $PropertyName
                Value = $null
            }
        } elseif ($PropertyName -in $properties.Name) {
            [PsCustomObject]@{
                Success = $true
                Name = $PropertyName
                Value = $InputObject.$PropertyName
            }
        } elseif ($null -ne $Default) {
            [PsCustomObject]@{
                Success = $false
                Name = $PropertyName
                Value = $Default.$PropertyName
            }
        } else {
            [PsCustomObject]@{
                Success = $false
                Name = $PropertyName
                Value = $null
            }
        })
    }
    catch {
        return [PsCustomObject]@{
            Success = $false
            Name = $PropertyName
            Value = $null
        }
    }

    return [PsCustomObject]@{
        Success = $false
        Name = $PropertyName
        Value = $null
    }
}

function Convert-MdTreeToHtml {
    Param(
        [Parameter(ValueFromPipeline = $true)]
        [PsCustomObject]
        $InputObject
    )

    Write-Output "<ul class=""contains-task-list"">"

    $properties = $InputObject.PsObject.Properties `
        | where { $_.MemberType -eq 'NoteProperty' } `
        | where { $_.Name.ToLower() -ne 'complete' }

    foreach ($prop in $properties) {
        $value = $prop.Value
        $actionItem = 'complete' -in $value.PsObject.Properties.Name

        Write-Output @(if ($actionItem) {
            "<li class=""task-list-item""><input type=""checkbox"">$($prop.Name)"
        }
        else {
            "<li>$($prop.Name)"
        })

        Convert-MdTreeToHtml $value
        Write-Output "</li>"
    }

    Write-Output "</ul>"
}

