# dev: MarkdownTree Backus-Naur Form

- tag: #markdown #item #syntax #tree #bnf #backus-naur-form

## 2025-04-15

On the contrary, Raziel
- _every line_ in a markdown file can be immediately classified based on _how it starts_

### Classes

| type | pattern | actionable | next indent size |
|------|---------|------------|------------------|
| heading        | ``^#+\s``               | false | 2 |
| vinculum       | ``^\s*(-\|=\|\*){3}``   | false | 2 |
| unordered list | ``^\s*(-\|\+\|\*)\s``   | true  | 2 |
| ordered list   | ``^\s*[1-9][0-9]*\.\s`` | true  | 3 |
| table row      | ``^\s*\|``              | false | 2 |
| define         | ``^\s+:\s``             | false | 2 |
| local          | ``^\s*!\[``             | false | 2 |
| code block     | ``^\s*` ` ` ``          | false | 2 |
| white space    | ``^\s*$``               | false | 2 |
| paragraph      | _                       | false | 2 |

## 2025-03-02

```text
<markdown-file> ::= <first-heading> <newline> <document>
<document> ::= nil | <subdocument> | <content>
<subdocument> ::= <next-heading> <newline> <document>
<content> ::= <item> | <item> <newline> <content> | <item> <newline> <subitem>
<item> ::= <paragraph> | <current-indent> <ordered-list> | <current-indent> <unordered-list> | <image> | <table> | <define>
<paragraph> ::= <current-indent> any
<subitem> ::= <next-indent> <ordered-list> | <next-indent> <unorderd-list>
<ordered-list> ::= <number-item> | <number-item> <newline> <next-indent> <item>
<number-item> ::= <number-bullet> <action-item>
<unordered-list> ::= <bullet-item> | <bullet-item> <newline> <next-indent> <item>
<bullet-item> ::= <bullet> <action-item>
<table> ::= <newline> <current-indent> <headers> <newline> <current-indent> <vinculum> <newline> <rows> <newline>
<rows> ::= nil | <current-indent> <bar-separated-cells> <newline> <rows>
<image> ::= <newline> <current-indent> '![' any '](<' any '>)' <newline>
<define> ::= <next-indent> ': ' any
any ::= non-newline characters
```

```markdown
- est
  1. uan
     - sin
       1. ter
          - ius
```

```text
  ^  ^ ^  ^
```

