---
layout: post
title: "Worked example: Parsing command line arguments"
description: "Pattern matching in practice"
nav: thinking-functionally
seriesId: "Expressions and syntax"
seriesOrder: 11
categories: [Patterns, Worked Examples]
---

Now that we've seen how the match expression works, let's look at some examples in practice. But first, a word about the design approach.

## Application design in F# ##

We've seen that a generic function takes input and emits output.  But in a sense, that approach applies at *any* level of functional code, even at the top level.

In fact, we can say that a functional *application* takes input, transforms it, and emits output:

![](/assets/img/function_transform1.png)

Now ideally, the transformations work within the pure type-safe world that we create to model the domain, but unfortunately, the real world is untyped!
That is, the input is likely to be simple strings or bytes, and the output also. 

How can we work with this? The obvious solution is to have a separate stage to convert the input to our pure internal model, and then another separate stage to convert from the internal model to the output.

![](/assets/img/function_transform2.png)

In this way, we can hide the messiness of the real world from the core of the application. This "keep your model pure" approach is similar to the ["Hexagonal Architecture"](http://alistair.cockburn.us/Hexagonal+architecture) concept in the large, or the MVC pattern in the small.

In this post and the [next](/posts/roman-numerals), we'll see some simple examples of this.

## Example: parsing a command line

We talked about the match expression in general in the [previous post](/posts/match-expression), so let's look at a real example where it is useful, namely parsing a command line.

We'll design and implement two slightly different versions, one with a basic internal model, and second one with some improvements.

### Requirements

Let's say that we have three commandline options: "verbose", "subdirectories", and "orderby".
"Verbose" and "subdirectories" are flags, while "orderby" has two choices: "by size" and "by name".

So the command line params would look like

	MYAPP [/V] [/S] [/O order]
	/V    verbose
	/S    include subdirectories
	/O    order by. Parameter is one of 
			N - order by name. 
			S - order by size

## First version

Following the design rule above, we can see that:

* the input will be an array (or list) of strings, one for each argument.
* the internal model will be a set of types that model the (tiny) domain.
* the output is out of scope in this example.

So we'll start by first creating the internal model of the parameters, and then look at how we can parse the input into types used in the internal model. 

Here's a first stab at the model:

```fsharp
// constants used later
let OrderByName = "N"
let OrderBySize = "S"

// set up a type to represent the options
type CommandLineOptions = {
    verbose: bool;
    subdirectories: bool;
    orderby: string; 
    }
```

Ok, that looks alright. Now let's parse the arguments.

The parsing logic is very similar to the `loopAndSum` example in the previous post. 

* We create a recursive loop on the list of arguments.
* Each time through the loop, we parse one argument.
* The options parsed so far are passed into each loop as a parameter (the "accumulator" pattern).

```fsharp
let rec parseCommandLine args optionsSoFar = 
    match args with 
    // empty list means we're done.
    | [] -> 
        optionsSoFar  

    // match verbose flag
    | "/v"::xs -> 
        let newOptionsSoFar = { optionsSoFar with verbose=true}
        parseCommandLine xs newOptionsSoFar 

    // match subdirectories flag
    | "/s"::xs -> 
        let newOptionsSoFar = { optionsSoFar with subdirectories=true}
        parseCommandLine xs newOptionsSoFar 

    // match orderBy by flag
    | "/o"::xs -> 
        //start a submatch on the next arg
        match xs with
        | "S"::xss -> 
            let newOptionsSoFar = { optionsSoFar with orderby=OrderBySize}
            parseCommandLine xss newOptionsSoFar 
            
        | "N"::xss -> 
            let newOptionsSoFar = { optionsSoFar with orderby=OrderByName}
            parseCommandLine xss newOptionsSoFar 
            
        // handle unrecognized option and keep looping
        | _ -> 
            eprintfn "OrderBy needs a second argument"
            parseCommandLine xs optionsSoFar 

    // handle unrecognized option and keep looping
    | x::xs -> 
        eprintfn "Option '%s' is unrecognized" x
        parseCommandLine xs optionsSoFar 
```

This code is straightforward, I hope. 

Each match consist of a `option::restOfList` pattern.
If the option is matched, a new `optionsSoFar` value is created and the loop repeats with the remaining list, until the list becomes empty,
at which point we can exit the loop and return the `optionsSoFar` value as the final result.

There are two special cases:

* Matching the "orderBy" option creates a submatch pattern that looks at the first item in the rest of the list and if not found, complains about a missing second parameter.
* The very last match on the main `match..with` is not a wildcard, but a "bind to value". Just like a wildcard, this will always succeed, but because we havd bound to the value, it allows us to print the offending unmatched argument.
* Note that for printing errors, we use `eprintf` rather than `printf`. This will write to STDERR rather than STDOUT.

So now let's test this:

```fsharp
parseCommandLine ["/v"; "/s"] 
```
            
Oops! That didn't work -- we need to pass in an initial `optionsSoFar` argument! Lets try again:

```fsharp
// define the defaults to pass in
let defaultOptions = {
    verbose = false;
    subdirectories = false;
    orderby = ByName
    }

// test it
parseCommandLine ["/v"] defaultOptions
parseCommandLine ["/v"; "/s"] defaultOptions
parseCommandLine ["/o"; "S"] defaultOptions
```

Check that the output is what you would expect.

And we should also check the error cases:

```fsharp
parseCommandLine ["/v"; "xyz"] defaultOptions
parseCommandLine ["/o"; "xyz"] defaultOptions
```

You should see the error messages in these cases now.

Before we finish this implementation, let's fix something annoying.
We are passing in these default options every time -- can we get rid of them?

This is a very common situation: you have a recursive function that takes a "accumulator" parameter, but you don't want to be passing initial values all the time.

The answer is simple: just create another function that calls the recursive function with the defaults.

Normally, this second one is the "public" one and the recursive one is hidden, so we will rewrite the code as follows:

* Rename `parseCommandLine` to `parseCommandLineRec`. There are other naming conventions you could use as well, such as `parseCommandLine'` with a tick mark, or `innerParseCommandLine`.
* Create a new `parseCommandLine` that calls `parseCommandLineRec` with the defaults

```fsharp
// create the "helper" recursive function
let rec parseCommandLineRec args optionsSoFar = 
	// implementation as above

// create the "public" parse function
let parseCommandLine args = 
    // create the defaults
    let defaultOptions = {
        verbose = false;
        subdirectories = false;
        orderby = OrderByName
        }

    // call the recursive one with the initial options
    parseCommandLineRec args defaultOptions 
```

In this case the helper function can stand on its own. But if you really want to hide it, you can put it as a nested subfunction in the defintion of `parseCommandLine` itself.

```fsharp
// create the "public" parse function
let parseCommandLine args = 
    // create the defaults
    let defaultOptions = 
		// implementation as above

	// inner recursive function
	let rec parseCommandLineRec args optionsSoFar = 
		// implementation as above

    // call the recursive one with the initial options
    parseCommandLineRec args defaultOptions 
```

In this case, I think it would just make things more complicated, so I have kept them separate.

So, here is all the code at once, wrapped in a module:

```fsharp
module CommandLineV1 =

    // constants used later
    let OrderByName = "N"
    let OrderBySize = "S"

    // set up a type to represent the options
    type CommandLineOptions = {
        verbose: bool;
        subdirectories: bool;
        orderby: string; 
        }

    // create the "helper" recursive function
    let rec parseCommandLineRec args optionsSoFar = 
        match args with 
        // empty list means we're done.
        | [] -> 
            optionsSoFar  

        // match verbose flag
        | "/v"::xs -> 
            let newOptionsSoFar = { optionsSoFar with verbose=true}
            parseCommandLineRec xs newOptionsSoFar 

        // match subdirectories flag
        | "/s"::xs -> 
            let newOptionsSoFar = { optionsSoFar with subdirectories=true}
            parseCommandLineRec xs newOptionsSoFar 

        // match orderBy by flag
        | "/o"::xs -> 
            //start a submatch on the next arg
            match xs with
            | "S"::xss -> 
                let newOptionsSoFar = { optionsSoFar with orderby=OrderBySize}
                parseCommandLineRec xss newOptionsSoFar 
            
            | "N"::xss -> 
                let newOptionsSoFar = { optionsSoFar with orderby=OrderByName}
                parseCommandLineRec xss newOptionsSoFar 
            
            // handle unrecognized option and keep looping
            | _ -> 
                eprintfn "OrderBy needs a second argument"
                parseCommandLineRec xs optionsSoFar 

        // handle unrecognized option and keep looping
        | x::xs -> 
            eprintfn "Option '%s' is unrecognized" x
            parseCommandLineRec xs optionsSoFar 

    // create the "public" parse function
    let parseCommandLine args = 
        // create the defaults
        let defaultOptions = {
            verbose = false;
            subdirectories = false;
            orderby = OrderByName
            }

        // call the recursive one with the initial options
        parseCommandLineRec args defaultOptions 


// happy path
CommandLineV1.parseCommandLine ["/v"] 
CommandLineV1.parseCommandLine  ["/v"; "/s"] 
CommandLineV1.parseCommandLine  ["/o"; "S"] 

// error handling
CommandLineV1.parseCommandLine ["/v"; "xyz"] 
CommandLineV1.parseCommandLine ["/o"; "xyz"] 
```

## Second version

In our initial model we used bool and string to represent the possible values. 

```fsharp
type CommandLineOptions = {
    verbose: bool;
    subdirectories: bool;
    orderby: string; 
    }
```

There are two problems with this:

* **It doesn't *really* represent the domain.** For example, can `orderby` really be *any* string? Would my code break if I set it to "ABC"?

* **The values are not self documenting.** For example, the verbose value is a bool. We only know that the bool represents the "verbose" option because of the *context* (the field named `verbose`) it is found in.
If we passed that bool around, and took it out of context, we would not know what it represented. I'm sure we have all seen C# functions with many boolean parameters like this:

```csharp
myObject.SetUpComplicatedOptions(true,false,true,false,false);
```

Because the bool doesn't represent anything at the domain level, it is very easy to make mistakes.

The solution to both these problems is to be as specific as possible when defining the domain, typically by creating lots of very specific types.

So here's a new version of `CommandLineOptions`:

```fsharp
type OrderByOption = OrderBySize | OrderByName
type SubdirectoryOption = IncludeSubdirectories | ExcludeSubdirectories
type VerboseOption = VerboseOutput | TerseOutput

type CommandLineOptions = {
    verbose: VerboseOption;
    subdirectories: SubdirectoryOption;
    orderby: OrderByOption
    }
```

A couple of things to notice:

* There are no bools or strings anywhere.
* The names are quite explicit. This acts as documentation when a value is taken in isolation,
but also means that the name is unique, which helps type inference, which in turn helps you avoid explicit type annotations.

Once we have made the changes to the domain, it is easy to fix up the parsing logic.

So, here is all the revised code, wrapped in a "v2" module:

```fsharp
module CommandLineV2 =

    type OrderByOption = OrderBySize | OrderByName
    type SubdirectoryOption = IncludeSubdirectories | ExcludeSubdirectories
    type VerboseOption = VerboseOutput | TerseOutput

    type CommandLineOptions = {
        verbose: VerboseOption;
        subdirectories: SubdirectoryOption;
        orderby: OrderByOption
        }

    // create the "helper" recursive function
    let rec parseCommandLineRec args optionsSoFar = 
        match args with 
        // empty list means we're done.
        | [] -> 
            optionsSoFar  

        // match verbose flag
        | "/v"::xs -> 
            let newOptionsSoFar = { optionsSoFar with verbose=VerboseOutput}
            parseCommandLineRec xs newOptionsSoFar 

        // match subdirectories flag
        | "/s"::xs -> 
            let newOptionsSoFar = { optionsSoFar with subdirectories=IncludeSubdirectories}
            parseCommandLineRec xs newOptionsSoFar 

        // match sort order flag
        | "/o"::xs -> 
            //start a submatch on the next arg
            match xs with
            | "S"::xss -> 
                let newOptionsSoFar = { optionsSoFar with orderby=OrderBySize}
                parseCommandLineRec xss newOptionsSoFar 
            | "N"::xss -> 
                let newOptionsSoFar = { optionsSoFar with orderby=OrderByName}
                parseCommandLineRec xss newOptionsSoFar 
            // handle unrecognized option and keep looping
            | _ -> 
                printfn "OrderBy needs a second argument"
                parseCommandLineRec xs optionsSoFar 

        // handle unrecognized option and keep looping
        | x::xs -> 
            printfn "Option '%s' is unrecognized" x
            parseCommandLineRec xs optionsSoFar 

    // create the "public" parse function
    let parseCommandLine args = 
        // create the defaults
        let defaultOptions = {
            verbose = TerseOutput;
            subdirectories = ExcludeSubdirectories;
            orderby = OrderByName
            }

        // call the recursive one with the initial options
        parseCommandLineRec args defaultOptions 
            
// ==============================
// tests    

// happy path
CommandLineV2.parseCommandLine ["/v"] 
CommandLineV2.parseCommandLine ["/v"; "/s"] 
CommandLineV2.parseCommandLine ["/o"; "S"] 

// error handling
CommandLineV2.parseCommandLine ["/v"; "xyz"] 
CommandLineV2.parseCommandLine ["/o"; "xyz"] 
```

## Using fold instead of recursion?

We said in the previous post that it is good to avoid recursion where possible and use the built in functions in the `List` module like `map` and `fold`.

So can we take this advice here and fix up this code to do this?

Unfortunately, not easily. The problem is that the list functions generally work on one element at a time, while the "orderby" option requires a "lookahead" argument as well.

To make this work with something like `fold`, we need to create a "parse mode" flag to indicate whether we are in lookahead mode or not.
This is possible, but I think it just adds extra complexity compared to the straightforward recursive version above.  

And in a real-world situation, anything more complicated than this would be a signal that you need to switch to a proper parsing system such as [FParsec](http://www.quanttec.com/fparsec/).

However, just to show you it can be done with `fold`:

```fsharp
module CommandLineV3 =

    type OrderByOption = OrderBySize | OrderByName
    type SubdirectoryOption = IncludeSubdirectories | ExcludeSubdirectories
    type VerboseOption = VerboseOutput | TerseOutput

    type CommandLineOptions = {
        verbose: VerboseOption;
        subdirectories: SubdirectoryOption;
        orderby: OrderByOption
        }

    type ParseMode = TopLevel | OrderBy

    type FoldState = {
        options: CommandLineOptions ;
        parseMode: ParseMode;
        }

    // parse the top-level arguments
    // return a new FoldState
    let parseTopLevel arg optionsSoFar = 
        match arg with 

        // match verbose flag
        | "/v" -> 
            let newOptionsSoFar = {optionsSoFar with verbose=VerboseOutput}
            {options=newOptionsSoFar; parseMode=TopLevel}

        // match subdirectories flag
        | "/s"-> 
            let newOptionsSoFar = { optionsSoFar with subdirectories=IncludeSubdirectories}
            {options=newOptionsSoFar; parseMode=TopLevel}

        // match sort order flag
        | "/o" -> 
            {options=optionsSoFar; parseMode=OrderBy}

        // handle unrecognized option and keep looping
        | x -> 
            printfn "Option '%s' is unrecognized" x
            {options=optionsSoFar; parseMode=TopLevel}

    // parse the orderBy arguments
    // return a new FoldState
    let parseOrderBy arg optionsSoFar = 
        match arg with
        | "S" -> 
            let newOptionsSoFar = { optionsSoFar with orderby=OrderBySize}
            {options=newOptionsSoFar; parseMode=TopLevel}
        | "N" -> 
            let newOptionsSoFar = { optionsSoFar with orderby=OrderByName}
            {options=newOptionsSoFar; parseMode=TopLevel}
        // handle unrecognized option and keep looping
        | _ -> 
            printfn "OrderBy needs a second argument"
            {options=optionsSoFar; parseMode=TopLevel}

    // create a helper fold function
    let foldFunction state element  = 
        match state with
        | {options=optionsSoFar; parseMode=TopLevel} ->
            // return new state
            parseTopLevel element optionsSoFar

        | {options=optionsSoFar; parseMode=OrderBy} ->
            // return new state
            parseOrderBy element optionsSoFar
           
    // create the "public" parse function
    let parseCommandLine args = 

        let defaultOptions = {
            verbose = TerseOutput;
            subdirectories = ExcludeSubdirectories;
            orderby = OrderByName
            }
      
        let initialFoldState = 
            {options=defaultOptions; parseMode=TopLevel}

        // call fold with the initial state
        args |> List.fold foldFunction initialFoldState 

// ==============================
// tests    

// happy path
CommandLineV3.parseCommandLine ["/v"] 
CommandLineV3.parseCommandLine ["/v"; "/s"] 
CommandLineV3.parseCommandLine ["/o"; "S"] 

// error handling
CommandLineV3.parseCommandLine ["/v"; "xyz"] 
CommandLineV3.parseCommandLine ["/o"; "xyz"] 
```

By the way, can you see a subtle change of behavior in this version? 

In the previous versions, if there was no parameter to the "orderBy" option, the recursive loop would still parse it next time.
But in the 'fold' version, this token is swallowed and lost.

To see this, compare the two implementations:

```fsharp
// verbose set
CommandLineV2.parseCommandLine ["/o"; "/v"] 

// verbose not set! 
CommandLineV3.parseCommandLine ["/o"; "/v"] 
```

To fix this would be even more work. Again this argues for the second implementation as the easiest to debug and maintain.

## Summary

In this post we've seen how to apply pattern matching to a real-world example.

More importantly, we've seen how easy it is to create a properly designed internal model for even the smallest domain. And that this internal model provides more type safety and documentation than using primitive types such as string and bool.

In the next example, we'll do even more pattern matching!
