@echo off
pwsh -NoExit -Command "get-item ""%~dp0.\bin\Release\net9.0\PsMarkdownTree.dll"" | foreach { ipmo $_.FullName }"


