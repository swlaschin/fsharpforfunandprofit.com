---
layout: post
title: "Enterprise Tic-Tac-Toe, part 2"
description: "In which I throw away the previous design, and switch to a capability-centric approach"
categories: ["Worked Examples"]
seriesId: "Annotated walkthroughs"
seriesOrder: 6
---

*UPDATE: [Slides and video from my talk on this topic](/ettt/)*

*This post is one of series in I which I hope to close the gap between theory and practice in functional programming.
I pick a small project and show you my thought processes as I go about designing and implementing it from beginning to end.*
 
In the [previous post](/posts/enterprise-tic-tac-toe/), I did a design for a Tic-Tac-Toe (aka Noughts and Crosses) game.

It wasn't bad for a direct-to-code brain dump, but there were a couple of things that I wasn't happy with.

Unfortunately, the more I thought about it, the more those little niggles became full-fledged annoyances, and I got unhappier and unhappier.

In this post, I'll explain why I was so unhappy, and how I arrived at a design that I am much more satisfied with.

## The old design

To recap the previous post briefly, here is the old design:

* There is a hidden `GameState` known only to the implementation.
* There are some functions that allow the players to move (`PlayerXMoves` and `PlayerOMoves`).
* The UI (or other client) passes the game state into each move, and gets a updated game state back.
* Each move also returns a `MoveResult` which contains the game status (in process, won, tied), and if the game is still in process, whose turn it is, and what the available moves are.

Here's the code:

```fsharp
module TicTacToeDomain =

    type HorizPosition = Left | HCenter | Right
    type VertPosition = Top | VCenter | Bottom
    type CellPosition = HorizPosition * VertPosition 

    type Player = PlayerO | PlayerX

    type CellState = 
        | Played of Player 
        | Empty

    type Cell = {
        pos : CellPosition 
        state : CellState 
        }

    type PlayerXPos = PlayerXPos of CellPosition 
    type PlayerOPos = PlayerOPos of CellPosition 

    type ValidMovesForPlayerX = PlayerXPos list
    type ValidMovesForPlayerO = PlayerOPos list
        
    type MoveResult = 
        | PlayerXToMove of ValidMovesForPlayerX 
        | PlayerOToMove of ValidMovesForPlayerO 
        | GameWon of Player 
        | GameTied 

    // the "use-cases"        
    type NewGame<'GameState> = 
        'GameState * MoveResult      
    type PlayerXMoves<'GameState> = 
        'GameState -> PlayerXPos -> 'GameState * MoveResult
    type PlayerOMoves<'GameState> = 
        'GameState -> PlayerOPos -> 'GameState * MoveResult
```

## What's wrong with the old design?

So what's wrong with this design? Why was I so unhappy?

First, I was unhappy about the use of the `PlayerXPos` and `PlayerOPos` types. The idea was to wrap a `CellPosition` in a type
so that it would be "owned" by a particular player.
By doing this, and then having the valid moves be one of these types, I could prevent player X from playing twice, say.
That is, after player X has moved, the valid moves for the next run would be wrapped in a `PlayerOPos` type so that only player O could use them.

The problem was that the `PlayerXPos` and `PlayerOPos` types are public, so that a malicious user could have forged one and played twice anyway!

Yes, these types could have been made private by parameterizing them like the game state, but the design would have become very ugly very quickly.

Second, even if the moves *were* made unforgeable, there's that game state floating about. 

It's true that the game state internals are private, but a malicious user could have still caused problems by reusing a game state.
For example, they could attempt to play one of the valid moves with a game state from a previous turn, or vice versa.  

In this particular case, it would not be dangerous, but in general it might be a problem. 

So, as you can see, this design was becoming a bit smelly to me, which was why I was becoming unhappy.

## What's up with this malicious user?

Why I am assuming that the user of the API will be so malicious -- forging fake moves and all that?

The reason is that I use this as a design guideline. If a malicious user can do something I don't want, then the design is probably not good enough.

In my series on [capability based security](/posts/capability-based-security/) I point out that by designing for the
[Principle Of Least Authority](https://en.wikipedia.org/wiki/Principle_of_least_privilege) ("POLA"), you end up with a good design as a side-effect.

That is, if you design the most minimal interface that the caller needs, then you will both avoid accidental complexity (good design) and increase security (POLA).

I had a little tip in that post: **design for malicious callers and you will probably end up with more modular code**.

I think I will follow my own advice and see where I end up!

## Designing for POLA

So, let's design for POLA -- let's give the user the minimal "capability" to do something and no more.

In this case, I want to give the user the capability to mark a specific position with an "X" or "O".

Here's what I had before:

```fsharp
type PlayerXMoves = 
    GameState * PlayerXPos -> // input
        GameState * MoveResult // output
```

The user is passing in the location (`PlayerXPos`) that they want to play.

But let's now take away the user's ability to choose the position. Why don't I give the user a function, a `MoveCapability` say, that has the position baked in?

```fsharp
type MoveCapability = 
    GameState -> // input
        GameState * MoveResult // output
```

In fact, why not bake the game state into the function too? That way a malicious user can't pass the wrong game state to me.

This means that there is no "input" at all now -- everything is baked in!

```fsharp
type MoveCapability = 
    unit -> // no input
        GameState * MoveResult // output
```

But now we have to give the user a whole set of capabilities, one for each possible move they can make.
Where do these capabilities come from?  

Answer, the `MoveResult` of course! We'll change the `MoveResult` to return a list of capabilities rather than a list of positions.

```fsharp
type MoveResult = 
    | PlayerXToMove of MoveCapability list 
    | PlayerOToMove of MoveCapability list 
    | GameWon of Player 
    | GameTied 
```

Excellent! I'm much happier with this approach.

And now that the `MoveCapability` contains the game state baked in, we don't need the game state to be in the output either!

So our move function has simplified dramatically and now looks like this:

```fsharp
type MoveCapability = 
    unit -> MoveResult 
```

Look ma! No `'GameState` parameter! It's gone!

## A quick walkthrough from the UI's point of view

So now let's pretend we are the UI, and let's attempt to use the new design.

* First, assume that we have a list of available capabilities from the previous move.
* Next, the user must pick one of the capabilities (e.g. squares) to play -- they can't just create any old cell position and play it, which is good.
  But how will the user know which capability corresponds to which square?  The capabilities are completely opaque. We can't tell from the outside what they do!
* Then, given that the user has picked a capability somehow, we run it (with no parameters).
* Next we update the display to show the result of the move.
  But again, how are we going to know what to display? There is no game state to extract the cells from any longer.
  
Here's some pseudo-code for the UI game loop:  
  
```fsharp
// loop while game not over
let rec playMove moveResult = 

    let availableCapabilities = // from moveResult
    
    // get capability from user input somehow
    let capability = ??
    
    // use the capability
    let newMoveResult = capability()
    
    // display updated grid
    let cells = ??  // from where
    
    // play again
    match newMoveResult with
    | PlayerXToMove capabilities -> 
        // play another move
        playMove newMoveResult
    | etc            
```

Let's deal with the first issue: how does the user know which capability is associated with which square?

The answer is just to create a new structure that "labels" the capability. In this case, with the cell position.
```fsharp
type NextMoveInfo = {
    posToPlay : CellPosition 
    capability : MoveCapability }
```

And now we must change the `MoveResult` to return a list of these labelled capabilities, rather than the unlabelled ones:

```fsharp
type MoveResult = 
    | PlayerXToMove of NextMoveInfo list 
    | PlayerOToMove of NextMoveInfo list 
    | GameWon of Player 
    | GameTied 
```

Note that the cell position is for the user's information only -- the actual position is still baked into the capability and cannot be forged.

Now for the second issue: how does the UI know what to display as a result of the move?  Let's just return that information to it directly in a new structure:

```fsharp
/// Everything the UI needs to know to display the board
type DisplayInfo = {
    cells : Cell list
    }
```

And once again, the `MoveResult` must be changed, this time to return the `DisplayInfo` for each case:

```fsharp
type MoveResult = 
    | PlayerXToMove of DisplayInfo * NextMoveInfo list 
    | PlayerOToMove of DisplayInfo * NextMoveInfo list 
    | GameWon of DisplayInfo * Player 
    | GameTied of DisplayInfo 
```

## Dealing with circular dependencies

Here's our final design:

```fsharp
/// The capability to make a move at a particular location.
/// The gamestate, player and position are already "baked" into the function.
type MoveCapability = 
    unit -> MoveResult 

/// A capability along with the position the capability is associated with.
/// This allows the UI to show information so that the user
/// can pick a particular capability to exercise.
type NextMoveInfo = {
    // the pos is for UI information only
    // the actual pos is baked into the cap.
    posToPlay : CellPosition 
    capability : MoveCapability }

/// The result of a move. It includes: 
/// * The information on the current board state.
/// * The capabilities for the next move, if any.
type MoveResult = 
    | PlayerXToMove of DisplayInfo * NextMoveInfo list 
    | PlayerOToMove of DisplayInfo * NextMoveInfo list 
    | GameWon of DisplayInfo * Player 
    | GameTied of DisplayInfo 
```

But oops! This won't compile!

`MoveCapability` depends on `MoveResult` which depends on `NextMoveInfo` which in turn depends on `MoveCapability` again. But the F# compiler does not allow forward references in general.

Circular dependencies like this are generally frowned upon (I even have a post called ["cyclic dependencies are evil"](/posts/cyclic-dependencies/)!)
and there are [normally work-arounds which you can use](/posts/removing-cyclic-dependencies/) to remove them.

In this case though, I will link them together using the `and` keyword, which replaces the `type` keyword and is useful for just these kinds of cases.

```fsharp
type MoveCapability = 
    // etc
and NextMoveInfo = {
    // etc
and MoveResult = 
    // etc
```

## Revisiting the API

What does the API look like now?

Originally, we had an API with slots for the three use-cases and also a helper function `getCells`:

```fsharp
type TicTacToeAPI<'GameState>  = 
    {
    newGame : NewGame<'GameState>
    playerXMoves : PlayerXMoves<'GameState> 
    playerOMoves : PlayerOMoves<'GameState> 
    getCells : GetCells<'GameState>
    }
```

But now, we don't need the `playerXMoves` or `playerOMoves`, because they are returned to us in the `MoveResult` of a previous move.

And `getCells` is no longer needed either, because we are returning the `DisplayInfo` directly now.

So after all these changes, the new API just has a single slot in it and looks like this:

```fsharp
type NewGame = unit -> MoveResult

type TicTacToeAPI = 
    {
    newGame : NewGame 
    }
```

I've changed `NewGame` from a constant to a parameterless function, which is in fact, just a `MoveCapability` in disguise.

## The new design in full

Here's the new design in full:

```fsharp
module TicTacToeDomain =

    type HorizPosition = Left | HCenter | Right
    type VertPosition = Top | VCenter | Bottom
    type CellPosition = HorizPosition * VertPosition 

    type Player = PlayerO | PlayerX

    type CellState = 
        | Played of Player 
        | Empty

    type Cell = {
        pos : CellPosition 
        state : CellState 
        }

    /// Everything the UI needs to know to display the board
    type DisplayInfo = {
        cells : Cell list
        }
    
    /// The capability to make a move at a particular location.
    /// The gamestate, player and position are already "baked" into the function.
    type MoveCapability = 
        unit -> MoveResult 

    /// A capability along with the position the capability is associated with.
    /// This allows the UI to show information so that the user
    /// can pick a particular capability to exercise.
    and NextMoveInfo = {
        // the pos is for UI information only
        // the actual pos is baked into the cap.
        posToPlay : CellPosition 
        capability : MoveCapability }

    /// The result of a move. It includes: 
    /// * The information on the current board state.
    /// * The capabilities for the next move, if any.
    and MoveResult = 
        | PlayerXToMove of DisplayInfo * NextMoveInfo list 
        | PlayerOToMove of DisplayInfo * NextMoveInfo list 
        | GameWon of DisplayInfo * Player 
        | GameTied of DisplayInfo 

    // Only the newGame function is exported from the implementation
    // all other functions come from the results of the previous move
    type TicTacToeAPI  = 
        {
        newGame : MoveCapability
        }
```

I'm much happier with this design than with the previous one:

* There is no game state for the UI to worry about.
* There are no type parameters to make it look ugly.
* The api is even more encapsulated -- a malicious UI can do very little now.
* It's shorter -- always a good sign!

## The complete application

I have updated the implementation and console application to use this new design.

The complete application is available on GitHub in [this gist](https://gist.github.com/swlaschin/7a5233a91912e66ac1e4) if you want to play with it.

Surprisingly, the implementation also has become slightly simpler, because all the state is now hidden and there is no need to deal with types like `PlayerXPos` any more.

## Logging revisited

In the previous post, I demonstrated how logging could be injected into the API.

But in this design, the capabilities are opaque and have no parameters, so how are we supposed to log that a particular player chose a particular location?

Well, we can't log the capabilities, but we *can* log their context, which we have via the `NextMoveInfo`. Let's see how this works in practice.

First, given a `MoveCapability`, we want to transform it into another `MoveCapability` that also logs the player and cell position used.

Here's the code for that:

```fsharp
/// Transform a MoveCapability into a logged version
let transformCapability transformMR player cellPos (cap:MoveCapability) :MoveCapability =
    
    // create a new capability that logs the player & cellPos when run
    let newCap() =
        printfn "LOGINFO: %A played %A" player cellPos
        let moveResult = cap() 
        transformMR moveResult 
    newCap
```    

This code works as follows:

* Create a new capability `newCap` function that is parameterless and returns a `MoveResult` just like the original one.
* When it is called, log the player and cell position. These are not available from the `MoveCapability` that was passed in, so we have to pass them in explicitly.
* Next, call the original capability and get the result.
* The result itself contains the capabilities for the next move, so we need to recursively transform each capability in the `MoveResult` and return
  a new `MoveResult`. This is done by the `transformMR` function that is passed in.

Now that we can transform a `MoveCapability`, we can go up a level and transform a `NextMoveInfo`.
  
```fsharp
/// Transform a NextMove into a logged version
let transformNextMove transformMR player (move:NextMoveInfo) :NextMoveInfo = 
    let cellPos = move.posToPlay 
    let cap = move.capability
    {move with capability = transformCapability transformMR player cellPos cap} 
```

This code works as follows:

* Given a `NextMoveInfo`, replace its capability with a transformed one. The output of `transformNextMove` is a new `NextMoveInfo`.
* The cellPos comes from the original move.
* The player and `transformMR` function are not available from the move, so must be passed in explicitly again.
   
Finally, we need to implement the function that will transform a `MoveResult`:
   
```fsharp
/// Transform a MoveResult into a logged version
let rec transformMoveResult (moveResult:MoveResult) :MoveResult =
    
    let tmr = transformMoveResult // abbreviate!

    match moveResult with
    | PlayerXToMove (display,nextMoves) ->
        let nextMoves' = nextMoves |> List.map (transformNextMove tmr PlayerX) 
        PlayerXToMove (display,nextMoves') 
    | PlayerOToMove (display,nextMoves) ->
        let nextMoves' = nextMoves |> List.map (transformNextMove tmr PlayerO)
        PlayerOToMove (display,nextMoves') 
    | GameWon (display,player) ->
        printfn "LOGINFO: Game won by %A" player 
        moveResult
    | GameTied display ->
        printfn "LOGINFO: Game tied" 
        moveResult
```

This code works as follows:

* Given a `MoveResult`, handle each case. The output is a new `MoveResult`.
* For the `GameWon` and `GameTied` cases, log the result and return the original moveResult.
* For the `PlayerXToMove` case, take each of the `NextMoveInfo`s and transform them, passing in the required player (`PlayerX`) and `transformMR` function.
  Note that the `transformMR` function is a reference to this very function! This means that `transformMoveResult` must be marked with `rec` to allow this self-reference.
* For the `PlayerOToMove` case, do the same as the `PlayerXToMove` case, except change the player to `PlayerO`.

Finally, we can inject logging into the API as a whole by transforming the `MoveResult` returned by `newGame`:

```fsharp
/// inject logging into the API
let injectLogging api =
   
    // create a new API with the functions 
    // replaced with logged versions
    { api with
        newGame = fun () -> api.newGame() |> transformMoveResult
        }
```

So there you go. Logging is a bit trickier than before, but still possible.

## A warning on recursion

In this code, I've been passing around functions that call each other recursively.
When you do this, you have to be careful that you don't unwittingly cause a stack overflow.

In a game like this, when the number of nested calls is guaranteed to be small, then there is no issue.
But if you are doing tens of thousands of nested calls, then you should worry about potential problems.

In some cases, the F# compiler will do tail-call optimization, but I suggest that you stress test your code to be sure!

## Data-centric vs capability-centric designs

There is an interesting difference between the original design and the new design.

The original design was *data-centric*. Yes, we gave each player a function to use, but it was the *same* function used over and over, with different data passed in each time.

The new design is *function-centric* (or as I prefer, *capability-centric*). There is very little data now.
Instead, the result of each function call is *another* set of functions than can be used for the next step, and so on, ad infinitum. 

In fact, it reminds me somewhat of a [continuation-based](/posts/computation-expressions-continuations/) approach, except that rather than passing in a continuation, 
the function itself returns a list of continuations, and then you pick one to use.

## Capabilities and RESTful designs -- a match made in heaven

If for some crazy reason you wanted to turn this design into a web service, how would you go about doing that?

In a *data-centric* design, we have a function to call (an endpoint URI in the web API) and then we pass data to it (as JSON or XML). The result of the call
is more data that we use to update the display (e.g. the DOM).

But in a *capability-centric* design, where's the data? And how do we pass functions around? It seems like this approach would not work for web services at all.

It might surprise you to know that there *is* a way to do this, and what's more it is exactly the same approach used by a RESTful design using [HATEOAS](https://en.wikipedia.org/wiki/HATEOAS).

What happens is that each capability is mapped to a URI by the server, and then visiting that URI is the same as exercising that capability (e.g. calling the function).

For example, in a web-based application based on this Tic-Tac-Toe design, the server would initially return nine URIs, one for each square.
Then, when one of those squares was clicked, and the associated URI visited, the server would return eight new URIs, one for each remaining unplayed square.
The URI for the just-played square would not be in this list, which means that it could not be clicked on again.

And of course when you click on one of the eight unplayed squares, the server would now return seven new URIs, and so on.

This model is exactly what REST is supposed to be; you decide what to do next based on the contents of the returned page rather than hard-code the endpoints into your app.

One possible downside of this approach is that it is does not appear to be stateless. 

* In the data-centric version, all the data needed for a move was passed in each time, which means that scaling the backend services would be trivial.
* In capability-centric approach though, the state has to be stored somewhere. If the complete game state can be encoded into the URI, then this approach will allow stateless servers as well,
but otherwise, some sort of state-storage will be needed. 

Some web frameworks have made this function-centred approach a key part of their design, most notably [Seaside](https://en.wikipedia.org/wiki/Seaside_%28software%29).

The excellent [WebSharper framework for F#](http://websharper.com) also uses [something similar](http://websharper.com/blog-entry/3965), I think
(I don't know WebSharper as well as I want to, alas, so correct me if I'm wrong).

## Summary

In this post, I tore up my original design and replaced it with an even more function-centric one, which I like better.

But of course it still has all those qualities we love: separation of concerns, an API, a watertight security model, self-documenting code, and logging.

I'm going to stop with Tic-Tac-Toe now -- I think I've wrung it dry! I hope you found these two walkthoughs interesting; I learned a lot myself.

*NOTE: The code for this post is available on GitHub in [this gist](https://gist.github.com/swlaschin/7a5233a91912e66ac1e4).*