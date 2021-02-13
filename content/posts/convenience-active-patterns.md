---
layout: post
title: "Active patterns"
description: "Dynamic patterns for powerful matching"
date: 2012-04-17
nav: why-use-fsharp
seriesId: "Why use F#?"
seriesOrder: 17
categories: [Convenience, Patterns]
---

F# has a special type of pattern matching called "active patterns" where the pattern can be parsed or detected dynamically. As with normal patterns, the matching and output are combined into a single step from the caller's point of view.

Here is an example of using active patterns to parse a string into an int or bool.

```fsharp
// create an active pattern
let (|Int|_|) str =
   match System.Int32.TryParse(str:string) with
   | (true,int) -> Some(int)
   | _ -> None

// create an active pattern
let (|Bool|_|) str =
   match System.Boolean.TryParse(str:string) with
   | (true,bool) -> Some(bool)
   | _ -> None
```

{{<alertinfo>}}
You don't need to worry about the complex syntax used to define the active pattern right now -- this is just an example so that you can see how they are used.
{{</alertinfo>}}

Once these patterns have been set up, they can be used as part of a normal "`match..with`" expression.

```fsharp
// create a function to call the patterns
let testParse str =
    match str with
    | Int i -> printfn "The value is an int '%i'" i
    | Bool b -> printfn "The value is a bool '%b'" b
    | _ -> printfn "The value '%s' is something else" str

// test
testParse "12"
testParse "true"
testParse "abc"
```

You can see that from the caller's point of view, the matching with an `Int` or `Bool` is transparent, even though there is parsing going on behind the scenes.

A similar example is to use active patterns with regular expressions in order to both match on a regex pattern and return the matched value in a single step.

```fsharp
// create an active pattern
open System.Text.RegularExpressions
let (|FirstRegexGroup|_|) pattern input =
   let m = Regex.Match(input,pattern)
   if (m.Success) then Some m.Groups.[1].Value else None
```

Again, once this pattern has been set up, it can be used transparently as part of a normal match expression.

```fsharp
// create a function to call the pattern
let testRegex str =
    match str with
    | FirstRegexGroup "http://(.*?)/(.*)" host ->
           printfn "The value is a url and the host is %s" host
    | FirstRegexGroup ".*?@(.*)" host ->
           printfn "The value is an email and the host is %s" host
    | _ -> printfn "The value '%s' is something else" str

// test
testRegex "http://google.com/test"
testRegex "alice@hotmail.com"
```

And for fun, here's one more: the well-known [FizzBuzz challenge](http://www.codinghorror.com/blog/2007/02/why-cant-programmers-program.html) written using active patterns.

```fsharp
// setup the active patterns
let (|MultOf3|_|) i = if i % 3 = 0 then Some MultOf3 else None
let (|MultOf5|_|) i = if i % 5 = 0 then Some MultOf5 else None

// the main function
let fizzBuzz i =
  match i with
  | MultOf3 & MultOf5 -> printf "FizzBuzz, "
  | MultOf3 -> printf "Fizz, "
  | MultOf5 -> printf "Buzz, "
  | _ -> printf "%i, " i

// test
[1..20] |> List.iter fizzBuzz
```
