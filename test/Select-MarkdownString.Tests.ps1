Import-Module -Name Pester

Describe 'Select-MarkdownString' {
    BeforeAll {
        iex "$PsScriptRoot\..\Get-Scripts.ps1" | foreach { . $_ }
    }

    Context "From Pipeline" {
        It "Given valid input '<Directory>' with Pattern '<Pattern>'" -TestCases @(
            @{
                Directory = "doc_-_2023_03_30_ChatGPT_Cpp11MonadTemplate.md"
                Pattern = "result_of|result_type"
                ExpectedCount = 13
                ExpectedLineNumbers = @(
                    97, 102, 104, 114, 120, 122, 134, 140, 142, 148, 150, 160, 180
                )
            }
        ) {
            Param(
                $Directory,
                $Pattern,
                $ExpectedCount,
                $ExpectedLineNumbers
            )

            $path = "$PsScriptRoot\sample\$Directory"

            $byFile = dir $path |
                Select-MarkdownString `
                    -Pattern $Pattern

            $byCat = dir $path |
                dir |
                cat |
                Select-MarkdownString `
                    -Pattern $Pattern

            $byFile.Count | Should Be $ExpectedCount
            $byFile.LineNumber | Should Be $ExpectedLineNumbers
            $byCat.Count | Should Be $ExpectedCount
            $byCat.LineNumber | Should Be $ExpectedLineNumbers
            diff ($byFile.Line) ($byCat.Line) | Should Be $null
        }
    }
}
