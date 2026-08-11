# issue

## resolved

- [x] issue 2026-08-08-141404
  - howto

    ```powershell
    cat .\test\mock_-_2026-08-02_HeadingHeadingTable.md | Get-MarkdownTree | Write-MarkdownTree
    ```

  - actual

    ```text
    - heading 1
      - heading 2
    
    | id       | descriptor | retrieved  | model |
    | -------- | ---------- | ---------- | ----- |
    | 00000000 | est        | 0000-01-01 | est   |
    
    
    | id       | descriptor | retrieved  | model |
    | -------- | ---------- | ---------- | ----- |
    | 00000001 | uan        | 0000-01-01 | uan   |
    
    
    | id       | descriptor | retrieved  | model |
    | -------- | ---------- | ---------- | ----- |
    | 00000002 | sin        | 0000-01-01 | sin   |
    
    
    | id       | descriptor | retrieved  | model |
    | -------- | ---------- | ---------- | ----- |
    | 00000000 | est        | 0000-01-01 | est   |
    
    
    | id       | descriptor | retrieved  | model |
    | -------- | ---------- | ---------- | ----- |
    | 00000001 | uan        | 0000-01-01 | uan   |
    
    
    | id       | descriptor | retrieved  | model |
    | -------- | ---------- | ---------- | ----- |
    | 00000002 | sin        | 0000-01-01 | sin   |
    
    
    | id       | descriptor | retrieved  | model |
    | -------- | ---------- | ---------- | ----- |
    | 00000000 | est        | 0000-01-01 | est   |
    
    
    | id       | descriptor | retrieved  | model |
    | -------- | ---------- | ---------- | ----- |
    | 00000001 | uan        | 0000-01-01 | uan   |
    
    
    | id       | descriptor | retrieved  | model |
    | -------- | ---------- | ---------- | ----- |
    | 00000002 | sin        | 0000-01-01 | sin   |
    ```

  - expected

    ```text
    - heading 1
      - heading 2
    
        | id | descriptor | retrieved | model |
        |----|------------|-----------|-------|
        | 00000000 | est | 0000-01-01 | est |
        | 00000001 | uan | 0000-01-01 | uan |
        | 00000002 | sin | 0000-01-01 | sin |
    ```

- [x] issue 2026-08-02-200045
  - where: ``Get-MarkdownTree``
  - howto

    ```powershell
    "sched: est", "sched: uan", "sched: sin", "sched: ter" | get-markdowntree
    ```

  - actual

    ```text
    sched
    -----
    {@{est=}, @{uan=}}
    ```

  - expected

    ```text
    sched
    -----
    {@{est=}, @{uan=}, @{sin=}, @{ter=}}
    ```

- [x] issue 2025-03-01-173847
  - where: ``Get-MarkdownTree``, ``Object.ps1``
  - howto
    - in markdown file ``howto-vim.md``

      ```markdown
      # howto: vim

      - change encoding

        ` ` ` text
        :e ++enc=utf16

        :e ++enc=utf16le
        :w!
        :e ++ff=mac
        :setlocal ff=dos
        :wq
        ` ` `
      ```

    - in powershell

      ```powershell
      Get-Item howto-vim.md | Get-Content | Get-MarkdownTree
      ```

    - actual

      ```text
      Line |
       702 |  …           "$(' ' * ($TableRow.IndentLength - $snippet.Indent))$conten …
           |                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
           | times ('-2') must be a non-negative value. (Parameter 'times') Actual value was -2.

      howto
      -----
      @{vim=}
      ```

    - expected

      ```text
      howto
      -----
      @{vim=}
      ```

- [x] issue 2025-01-09-052828
  - howto
    - in powershell

      ```powershell
      $tree = dir C:\note\log\log_-_2024-12-21_pwsh_NotebookRecovery.md | cat | Get-MarkdownTree
      $tree
      ```

      ```powershell
      $tree[-1] | Get-NextTree | foreach { $_."2025-01-06".Lines }
      ```

    - in ``log...NotebookRecovery.md``

      ```markdown
      # pwsh: notebook recovery

      - pool all files into one
        - 2025-01-06

          ` ` ` powershell
          dir pool*.md -Exclude pool*Master.md |
              sort -Descending |
              foreach {
                  $dtpat = "\d{4}_\d{2}_\d{2}(_\d+)?"
                  $dt = [regex]::Match($_.BaseName, $dtpat)
                  $subtitle = [regex]::Match($_.BaseName, "(?<=$($dtpat)`_).+$")
                  $title = "## $($dt.Value)"

                  if ($subtitle.Success) {
                      $title = "$title`: $($subtitle.Value -replace "_", " ")"
                  }

                  $title, ""

                  $cat = cat $_ | foreach {
                      $_ -replace "^#", "###"
                  }

                  $cat, $(if ($cat[-1]) { "" })
              }
          ` ` `
      ```

  - actual

    ```text
    Error
    Error
    Error
    Error
    Error
    Error
    Error
    Error
    Error
    Error
    Error
    Error
    Error
    Error
    Error

    pwsh
    ----
    @{notebook recovery=}
    ```

    ```text
    dir pool*.md -Exclude pool*Master.md |
    ```

  - expected

    ```text
    pwsh
    ----
    @{notebook recovery=}
    ```

    ```text
    dir pool*.md -Exclude pool*Master.md |
        sort -Descending |
        foreach {
            $dtpat = "\d{4}_\d{2}_\d{2}(_\d+)?"
            $dt = [regex]::Match($_.BaseName, $dtpat)
            $subtitle = [regex]::Match($_.BaseName, "(?<=$($dtpat)`_).+$")
            $title = "## $($dt.Value)"

            if ($subtitle.Success) {
                $title = "$title`: $($subtitle.Value -replace "_", " ")"
            }

            $title, ""

            $cat = cat $_ | foreach {
                $_ -replace "^#", "###"
            }

            $cat, $(if ($cat[-1]) { "" })
        }
    ```

---
[← Go Back](../readme.md)

