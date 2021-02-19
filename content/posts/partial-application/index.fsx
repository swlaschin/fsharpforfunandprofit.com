// ---------------------------
// Some simple examples that demonstrate partial application
// ---------------------------

//>demo1
// create an "adder" by partial application of add
let add42 = (+) 42    // partial application
add42 1
add42 3

// create a new list by applying the add42 function
// to each element
[1;2;3] |> List.map add42

// create a "tester" by partial application of "less than"
let twoIsLessThan = (<) 2   // partial application
twoIsLessThan 1
twoIsLessThan 3

// filter each element with the twoIsLessThan function
[1;2;3] |> List.filter twoIsLessThan

// create a "printer" by partial application of printfn
let printer = printfn "printing param=%i"

// loop over each element and call the printer function
[1;2;3] |> List.iter printer
//<


//>listDemo
// an example using List.map
let add1 = (+) 1
let add1ToEach = List.map add1   // fix the "add1" function

// test
add1ToEach [1;2;3;4]

// an example using List.filter
let filterEvens =
   List.filter (fun i -> i%2 = 0) // fix the filter function

// test
filterEvens [1;2;3;4]
//<

// ---------------------------
// "plugin"
// ---------------------------


//>logger
// create an adder that supports a pluggable logging function
let adderWithPluggableLogger logger x y =
    logger "x" x
    logger "y" y
    let result = x + y
    logger "x+y"  result
    result

// create a logging function that writes to the console
let consoleLogger argName argValue =
    printfn "%s=%A" argName argValue

//create an adder with the console logger partially applied
let addWithConsoleLogger = adderWithPluggableLogger consoleLogger
addWithConsoleLogger 1 2
addWithConsoleLogger 42 99

// create a logging function that uses red text
let redLogger argName argValue =
    let message = sprintf "%s=%A" argName argValue
    System.Console.ForegroundColor <- System.ConsoleColor.Red
    System.Console.WriteLine("{0}",message)
    System.Console.ResetColor()

//create an adder with the popup logger partially applied
let addWithRedLogger = adderWithPluggableLogger redLogger
addWithRedLogger 1 2
addWithRedLogger 42 99
//<

//>add42WithConsoleLogger
// create a another adder with 42 baked in
let add42WithConsoleLogger = addWithConsoleLogger 42
[1;2;3] |> List.map add42WithConsoleLogger
[1;2;3] |> List.map add42               //compare without logger
//<

// ---------------------------
// list functions with and without partial application
// ---------------------------

//>listWithoutPa
List.map    (fun i -> i+1) [0;1;2;3]
List.filter (fun i -> i>1) [0;1;2;3]
List.sortBy (fun i -> -i ) [0;1;2;3]
//<

// And the same examples using partial application:

//>listWithPa
let eachAdd1 = List.map (fun i -> i+1)
eachAdd1 [0;1;2;3]

let excludeOneOrLess = List.filter (fun i -> i>1)
excludeOneOrLess [0;1;2;3]

let sortDesc = List.sortBy (fun i -> -i)
sortDesc [0;1;2;3]
//<


//>listPipe
// piping using list functions
let result =
   [1..10]
   |> List.map (fun i -> i+1)
   |> List.filter (fun i -> i>5)
// output => [6; 7; 8; 9; 10; 11]
//<

// partially applied list functions are easy to compose

//>listCompose
let f1 = List.map (fun i -> i+1)
let f2 = List.filter (fun i -> i>5)
let compositeOp = f1 >> f2 // compose
let result = compositeOp [1..10]
// output => [6; 7; 8; 9; 10; 11]
//<

// ----------------------------------------
// Wrapping BCL functions for partial application ###
// ----------------------------------------


//>wrapper
// create wrappers for .NET string functions
let replace oldStr newStr (s:string) =
    s.Replace(oldValue=oldStr, newValue=newStr)

let startsWith (lookFor:string) (s:string) =
    s.StartsWith(lookFor)
//<

// Once the string becomes the last parameter, we can then use them with pipes

//>wrapperPipes
let result =
     "hello"
     |> replace "h" "j"
     |> startsWith "j"

["the"; "quick"; "brown"; "fox"]
     |> List.filter (startsWith "f")
//<

// or with function composition:

//>wrapperCompose
let compositeOp = replace "h" "j" >> startsWith "j"
let result = compositeOp "hello"
//<

// ----------------------------------------
// Understanding the "pipe" function
// ----------------------------------------

// NOTE: put in a private module so as not to override built-in implementation!
module PipeOverride =
    // The pipe function is defined as:

    //>pipeDefinition
    let (|>) x f = f x
    //<

module PipeExample1 =
    //>pipe1
    let doSomething x y z = x+y+z
    doSomething 1 2 3       // all parameters after function
    //<

// Here's the same example rewritten to use partial application

module PipeExample2 =
    //>pipe2
    let doSomething x y  =
       let intermediateFn z = x+y+z
       intermediateFn        // return intermediateFn

    let doSomethingPartial = doSomething 1 2
    doSomethingPartial 3     // only one parameter after function now
    3 |> doSomethingPartial  // same as above - last parameter piped in
    //<

//>pipe3
"12" |> int               // parses string "12" to an int
1 |> (+) 2 |> (*) 3       // chain of arithmetic
//<

// ----------------------------------------
// The reverse pipe
// ----------------------------------------

//>reversePipe
let (<|) f x = f x
//<


//>reversePipeDemo1
printf "%i" 1+2          // error
printf "%i" (1+2)        // using parens
printf "%i" <| 1+2       // using reverse pipe
//<

//>reversePipeDemo2
let add x y = x + y
(1+2) add (3+4)          // error
1+2 |> add <| 3+4        // pseudo infix
//<
