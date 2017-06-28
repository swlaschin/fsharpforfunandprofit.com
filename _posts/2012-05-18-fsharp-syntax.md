---
layout: post
title: "F# syntax: indentation and verbosity"
description: "Understanding the offside rule"
nav: thinking-functionally
seriesId: "Expressions and syntax"
seriesOrder: 5
---

The syntax for F# is mostly straightforward. But there are a few rules that you should understand if you want to avoid common indentation errors.  If you are familiar with a language like Python that also is whitespace sensitive, be aware that that the rules for indentation in F# are subtly different.

## Indentation and the "offside" rule ##

In soccer, the offside rule says that in some situations, a player cannot be "ahead" of the ball when they should be behind or level with it. The "offside line" is the line the player must not cross. F# uses the same term to describe the line at which indentation must start. As with soccer, the trick to avoiding a penalty is to know where the line is and not get ahead of it.

Generally, once an offside line has been set, all the expressions must align with the line.

```fsharp
//character columns
//3456789
let f = 
  let x=1     // offside line is at column 3
  let y=1     // this line must start at column 3
  x+y         // this line must start at column 3 

let f = 
  let x=1     // offside line is at column 3 
   x+1        // oops! don't start at column 4
              // error FS0010: Unexpected identifier in binding

let f = 
  let x=1    // offside line is at column 3 
 x+1         // offside! You are ahead of the ball!
             // error FS0588: Block following this 
             // 'let' is unfinished
```

Various tokens can trigger new offside lines to be created. For example, when the F# sees the "`=`" used in a let expression, a new offside line is created at the position of the very next symbol or word encountered.

```fsharp
//character columns
//34567890123456789
let f =   let x=1  // line is now at column 11 (start of "let x=")
          x+1      // must start at column 11 from now on

//        |        // offside line at col 11 
let f =   let x=1  // line is now at column 11 (start of "let x=")
         x+1       // offside!


// |        // offside line at col 4
let f =  
   let x=1  // first word after = sign defines the line 
            // offside line is now at column 4
   x+1      // must start at column 4 from now on
```

Other tokens have the same behavior, including parentheses, "`then`", "`else`", "`try`", "`finally`" and "`do`", and "`->`" in match clauses.

```fsharp
//character columns
//34567890123456789
let f = 
   let g = (         
    1+2)             // first char after "(" defines 
                     // a new line at col 5
   g 

let f = 
   if true then
    1+2             // first char after "then" defines 
                    // a new line at col 5

let f = 
   match 1 with 
   | 1 ->
       1+2          // first char after match "->" defines 
                    // a new line at col 8
```

The offside lines can be nested, and are pushed and popped as you would expect:

```fsharp
//character columns
//34567890123456789
let f = 
   let g = let x = 1 // first word after "let g =" 
                     // defines a new offside line at col 12
           x + 1     // "x" must align at col 12
                     // pop the offside line stack now
   g + 1             // back to previous line. "g" must align
                     // at col 4
```

New offside lines can never go forward further than the previous line on the stack:

```fsharp
let f = 
   let g = (         // let defines a new line at col 4
  1+2)               // oops! Cant define new line less than 4
   g 
```

## Special cases ##

There are number of special cases which have been created to make code formatting more flexible.  Many of them will seem natural, such as aligning the start of each part of an `if-then-else` expression or a `try-catch` expression. There are some non-obvious ones, however.

Infix operators such as "+", "|>" and ">>" are allowed to be outside the line by their length plus one space:

```fsharp
//character columns
//34567890123456789
let x =  1   // defines a new line at col 10
       + 2   // "+" allowed to be outside the line
       + 3

let f g h =   g   // defines a new line at col 15
           >> h   // ">>" allowed to be outside the line
```

If an infix operator starts a line, that line does not have to be strict about the alignment:

```fsharp
let x =  1   // defines a new line at col 10
        + 2   // infix operators that start a line don't count
             * 3  // starts with "*" so doesn't need to align
         - 4  // starts with "-" so doesn't need to align
```

If a "`fun`" keyword starts an expression, the "fun" does *not* start a new offside line:

```fsharp
//character columns
//34567890123456789
let f = fun x ->  // "fun" should define a new line at col 9
   let y = 1      // but doesn't. The real line starts here.
   x + y          
```

### Finding out more 

There are many more details as to how indentation works, but the examples above should cover most of the common cases. If you want to know more, the complete language spec for F# is available from Microsoft as a [downloadable PDF](http://research.microsoft.com/en-us/um/cambridge/projects/fsharp/manual/spec.pdf), and is well worth reading.

## "Verbose" syntax

By default, F# uses indentation to indicate block structure -- this is called "light" syntax. There is an alternative syntax that does not use indentation; it is called "verbose" syntax.  With verbose syntax, you are not required to use indentation, and whitespace is not significant, but the downside is that you are required to use many more keywords, including things like:

* "`in`" keywords after every "let" and "do" binding
* "`begin`"/"`end`" keywords for code blocks such as if-then-else
* "`done`" keywords at the end of loops
* keywords at the beginning and end of type definitions

Here is an example of verbose syntax with wacky indentation that would not otherwise be acceptable:

```fsharp
#indent "off"

      let f = 
    let x = 1 in
  if x=2 then 
begin "a" end else begin
"b" 
end

#indent "on"
```

Verbose syntax is always available, even in "light" mode, and is occasionally useful. For example, when you want to embed "let" into a one line expression:

```fsharp
let x = let y = 1 in let z = 2 in y + z
```

Other cases when you might want to use verbose syntax are:

* when outputting generated code
* to be compatible with OCaml
* if you are visually impaired or blind and use a screen reader
* or just to gain some insight into the abstract syntax tree used by the F# parser

Other than these cases, verbose syntax is rarely used in practice.
