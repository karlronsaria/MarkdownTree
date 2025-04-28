#Requires -Module PsMarkdownTree

Describe 'Get-HelloWorld' {
    It 'Called' {
        Get-HelloWorld | Should be "Hello, world!"
    }
}

Describe 'Get-Greeting' {
    It 'Called without arguments' {
        Get-Greeting | Should be $null
    }

    It 'Called with one argument' {
        Get-Greeting -Name 'est' | Should be "Hello, est!"
        Get-Greeting 'est' | Should be "Hello, est!"
    }

    It 'Called with an argument list' {
        Get-Greeting -Name 'est', 'uan', 'sin' | Should be @("Hello, est!", "Hello, uan!", "Hello, sin!")
        Get-Greeting 'est', 'uan', 'sin' | Should be @("Hello, est!", "Hello, uan!", "Hello, sin!")
    }

    It 'Called with pipeline' {
        'est' | Get-Greeting | Should be "Hello, est!"
        'est', 'uan', 'sin' | Get-Greeting | Should be @("Hello, est!", "Hello, uan!", "Hello, sin!")
    }
}

