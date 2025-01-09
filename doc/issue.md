# issue

- [ ] 2025_01_09_052828
  - howto
    - in powershell

      ```powershell
      $tree = dir C:\note\log\log_-_2024_12_21_pwsh_NotebookRecovery.md | cat | Get-MarkdownTree
      $tree
      ```

      ```powershell
      $tree[-1] | Get-NextTree | foreach { $_."2025_01_06".Lines }
      ```

    - in ``log...NotebookRecovery.md``

      ```markdown
      # pwsh: notebook recovery

      - pool all files into one
        - 2025_01_06

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

