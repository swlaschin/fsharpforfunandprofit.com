---
layout: post
title: "Enterprise Tic-Tac-Toe"
description: "A walkthrough of the design decisions in a purely functional implementation"
categories: ["Worked Examples"]
seriesId: "Annotated walkthroughs"
seriesOrder: 5
---

*UPDATE: [Slides and video from my talk on this topic](/ettt/)*

*This post is one of series in I which I hope to close the gap between theory and practice in functional programming.
I pick a small project and show you my thought processes as I go about designing and implementing it from beginning to end.*
 
For the next project in this series of posts, I'm going to do a walkthrough of a Tic-Tac-Toe (aka Noughts and Crosses) implementation, written in a functional style.

![tic-tac-toe](/assets/img/tic-tac-toe.png)

Now, to be clear, I'm not a games developer in any shape or form, so I won't be focused on performance or UX at all, just on the design process
-- taking some requirements that we all know (I hope) and translating them to functional code.   

In fact, to be very clear, I'll deliberately be going a bit overboard on the design just to demonstrate what you can do. There will be no objects. Everything will be immutable, Everything will be [typed](/series/designing-with-types.html).
There will be [capability based security](/posts/capability-based-security/), and more.
Performance will *definitely* be taking a back seat. Luckily, Tic-Tac-Toe does not need to support a high frame rate!

In fact, I'm going to call this version "Enterprise Tic-Tac-Toe"!  

Why? Well let's look at what you need for "Enterprise":

* We need **separation of concerns** so that specialist teams can work on different parts of the code at the same time.
* We need **a documented API** so that the different teams can work effectively in parallel.
* We need a **security  model** to prevent unauthorized actions from occurring.
* We need **well-documented code** so that the architect can ensure that the implementation matches the UML diagrams.
* We need **auditing and logging** to ensure that the system is SOX compliant.
* We need **scalability** to ensure that the system is ready for the challenges of rapid customer acquisition.

Actually, those are the *stated* reasons, but we all know that this is not the whole story.
The *real* reasons for an "enterprise design" become apparent when you talk to the people involved:

* *Development Manager:* "We need separation of concerns because the front-end team and back-end team hate each other and refuse to work in the same room."
* *Front-end team:* "We need a documented API so that those dummies building the back-end won't keep breaking our code on every commit."
* *Back-end team:* "We need a security model because those idiots building the front-end will always find a way to do something stupid unless we constrain them."
* *Maintenance team:* "We need well-documented code because we're fed up of having to reverse engineer the hacked-up spaghetti being thrown at us."
* *Testers and Operations:* "We need auditing and logging so that we can see what the effing system is doing inside."
* *Everyone:* "We don't really need scalability at all, but the CTO wants to us to be buzzword compliant."

It's true that there are already some wonderful "enterprise" projects out there, such as
[Easy Plus in PHP](https://github.com/Herzult/SimplePHPEasyPlus) and
[Fizz Buzz Enterprise Edition in Java](https://github.com/EnterpriseQualityCoding/FizzBuzzEnterpriseEdition),
but I hope that my own small contribution to this genre will be considered worthy.

Seriously, I hope that the code won't be quite as ~~bad~~ amusing as those other enterprise projects. In fact, I hope to demonstrate that you can have "enterprise" ready
functional code which is still readable!

## Designing the domain

> "Write the game such that someone unfamiliar with it could learn the rules by looking at the source code" -- [Raganwald](http://raganwald.com/)

As always, let's use a type-first design.  If you recall, this approach means that:

* We start with types only -- no implementation code.
* Every use-case or scenario corresponds to a function type, with one input and one output (which means I'll use tuples when multiple parameters are needed).
* We work mostly top-down and outside-in, but occasionally bottom up as well.
* We ignore the UI for now. And there will be no events or observables in the design of the core domain. It will be purely functional.

In fact, an alternative title for this post might be **growing functional software, guided by types.**

As I have said before, I like to drive the design by working from the events that can happen, rather than the objects involved.
I'm old school, so I call them use-cases, but I also like the [event-storming approach](http://ziobrando.blogspot.co.uk/2013/11/introducing-event-storming.html). 

Either way, for the Tic-Tac-Toe "domain", we have three different "event-driven use-cases" (in this case, just various mouse clicks!) to think about:

* Initialize a game
* Player X moves
* Player O moves

Let's start with the first: initialization. This is equivalent to a `new`-style constructor in an OO program.

For Tic-Tac-Toe, there are no configuration parameters needed, so the input would be "null" (aka `unit`) and the output would be a game ready to play, like this:

```fsharp
type InitGame = unit -> Game
```

Now, what is this `Game`?  Since everything is immutable, the other scenarios are going to have to take an existing game as input, and return a slightly changed version of the
game. So `Game` is not quite appropriate. How about `GameState` instead? A "player X moves" function will thus look something like this:

```fsharp
type PlayerXMoves = GameState * SomeOtherStuff -> GameState
```

You'll see that I added `SomeOtherStuff` to the input parameters because there's *always* some other stuff! We'll worry about what the "other stuff" is later. 

Ok, What should we do next?  Should we look more deeply into the internals of `GameState`? 

No. Let's stay high-level and do more "outside-in" style design. I like this approach in general because it allows me to focus on what's important and not
get side-tracked by implementation details.

## Designing the move functions

I said originally that we should have a function for each scenario. Which means we would have functions like this:

```fsharp
type PlayerXMoves = GameState * SomeOtherStuff -> GameState 
type PlayerOMoves = GameState * SomeOtherStuff -> GameState 
```

For each player's move, we start with the current game state, plus some other input created by the player, and end up with a *new* game state.

The problem is that both functions look exactly the same and could be easily substituted for each other. 
To be honest, I don't trust the user interface to always call the right one -- or at least, it could be a potential issue.

One approach is to have only *one* function, rather than *two*. That way there's nothing to go wrong.

But now we need to handle the two different input cases. How to do that?  Easy! A discriminated union type:

```fsharp
type UserAction = 
    | PlayerXMoves of SomeStuff
    | PlayerOMoves of SomeStuff
```

And now, to process a move, we just pass the user action along with the state, like this:

```fsharp
type Move = UserAction * GameState -> GameState 
```

So now there is only *one* function for the UI to call rather than two, and less to get wrong.

This approach is great where there is one user, because it documents all the things that they can do. For example, in other games, you might have a type like this:

```fsharp
type UserAction = 
    | MoveLeft 
    | MoveRight 
    | Jump
    | Fire
```

However in this situation, this way doesn't feel quite right. Since there are *two* players, what I want to do is give each player their own distinct function to call
and not allow them to use the other player's function. This not only stops the user interface component from messing up, but also gives me my capability-based security!

But now we are back to the original problem: how can we tell the two functions apart?

What I'll do is to use types to distinguish them. We'll make the `SomeOtherStuff`  be *owned* by each player, like this:

```fsharp
type PlayerXMoves = GameState * PlayerX's Stuff -> GameState 
type PlayerOMoves = GameState * PlayerO's Stuff -> GameState 
```

This way the two functions are distinct, and also PlayerO cannot call PlayerX's function without having some of PlayerX's `Stuff` as well.
If this sound's complicated, stay tuned -- it's easier than it looks!

## What is SomeOtherStuff?

What is this mysterious `SomeOtherStuff`?  In other words, what information do we need to make a move?

For most domains, there might quite a lot of stuff that needs to be passed in, and the stuff might vary based on the context and the state of the system.

But for Tic-Tac-Toe, it's easy, it's just the location on the grid where the player makes their mark. "Top Left", "Bottom Center", and so on. 

How should we define this position using a type?

The most obvious approach would be to use a 2-dimensional grid indexed by integers: `(1,1) (1,2) (1,3)`, etc.
But I have to admit that I'm too lazy to write unit tests that deal with bounds-checking,
nor can I ever remember which integer in the pair is the row and which the column. I want to write code that I don't have to test! 

Instead, let's define a type explicitly listing each position of horizontally and vertically:

```fsharp
type HorizPosition = Left | HCenter | Right
type VertPosition = Top | VCenter | Bottom
```

And then the position of a square in the grid (which I'm going to call a "cell") is just a pair of these:

```fsharp
type CellPosition = HorizPosition * VertPosition 
```

If we go back to the "move function" definitions, we now have:

```fsharp
type PlayerXMoves = GameState * CellPosition -> GameState 
type PlayerOMoves = GameState * CellPosition -> GameState 
```

which means: "to play a move, the input is a game state and a selected cell position, and the output is an updated game state".

Both player X and player O can play the *same* cell position, so, as we said earlier, we need to make them distinct.

I'm going to do that by wrapping them in a [single case union](/posts/designing-with-types-single-case-dus/):

```fsharp
type PlayerXPos = PlayerXPos of CellPosition 
type PlayerOPos = PlayerOPos of CellPosition 
```

And with that, our move functions now have different types and can't be mixed up:

```fsharp
type PlayerXMoves = GameState * PlayerXPos -> GameState 
type PlayerOMoves = GameState * PlayerOPos -> GameState 
```

## What is the GameState?

Now let's focus on the game state.  What information do we need to represent the game completely between moves?

I think it is obvious that the only thing we need is a list of the cells, so we can define a game state like this:

```fsharp
type GameState = {
    cells : Cell list
    }
```

But now, what do we need to define a `Cell`?

First, the cell's position. Second, whether the cell has an "X" or an "O" on it. We can therefore define a cell like this:

```fsharp
type CellState = 
    | X
    | O
    | Empty

type Cell = {
    pos : CellPosition 
    state : CellState 
    }
```

## Designing the output

What about the output? What does the UI need to know in order to update itself?  

One approach is just to pass the entire game state to the UI and let the UI redisplay the whole thing from scratch. Or perhaps, to be more efficient,
the UI could cache the previous state and do a diff to decide what needs to be updated.

In more complicated applications, with thousands of cells, we can be more efficient and make the UI's life easier
by explicitly returning the cells that changed with each move, like this:

```fsharp
// added "ChangedCells"
type PlayerXMoves = GameState * PlayerXPos -> GameState * ChangedCells
type PlayerOMoves = GameState * PlayerOPos -> GameState * ChangedCells
```

Since Tic-Tac-Toe is a tiny game, I'm going to keep it simple and just return the game state and *not* anything like `ChangedCells` as well.

But as I said at the beginning, I want the UI to be as dumb as possible!
The UI should not have to "think" -- it should be given everything it needs to know by the backend, and to just follow instructions.

As it stands, the cells can be fetched directly from the `GameState`, but I'd rather that the UI did *not* know how `GameState` is defined.
So let's give the UI a function (`GetCells`, say) that can extract the cells from the `GameState`:

```fsharp
type GetCells = GameState -> Cell list
```

Another approach would be for `GetCells` to return all the cells pre-organized into a 2D grid -- that would make life even easier for the UI.

```fsharp
type GetCells = GameState -> Cell[,] 
```

But now the game engine is assuming the UI is using a indexed grid. Just as the UI shouldn't know about the internals of the backend, the backend shouldn't make assumptions about how the UI works.

It's fair enough to allow the UI to share the same definition of `Cell` as the backend, so we can just give the UI a list of `Cell`s and let it display them in its own way.

Ok, the UI should have everything it needs to display the game now. 

## Review of the first version of the design 

Great! Let's look at what we've got so far:

```fsharp
module TicTacToeDomain =

    type HorizPosition = Left | HCenter | Right
    type VertPosition = Top | VCenter | Bottom
    type CellPosition = HorizPosition * VertPosition 

    type CellState = 
        | X
        | O
        | Empty

    type Cell = {
        pos : CellPosition 
        state : CellState 
        }
        
    type PlayerXPos = PlayerXPos of CellPosition 
    type PlayerOPos = PlayerOPos of CellPosition 

    // the private game state
    type GameState = exn  // use a placeholder
    
    // the "use-cases"        
    type InitGame = unit -> GameState       
    type PlayerXMoves = GameState * PlayerXPos -> GameState 
    type PlayerOMoves = GameState * PlayerOPos -> GameState 
    
    // helper function
    type GetCells = GameState -> Cell list
```

Note that in order to make this code compile while hiding the implementation of `GameState`, I've used a generic exception class (`exn`) as a placeholder for the actual implementation of `GameState`.
I could also have used `unit` or `string` instead, but `exn` is not likely to get mixed up with anything else, and
will prevent it being accidentally overlooked later! 

## A note on tuples

Just a reminder that in this design phase, I'm going to combine all the input parameters into a single tuple rather than treat them as separate parameters.

This means that I'll write:

```fsharp
InputParam1 * InputParam2 * InputParam3 -> Result 
```

rather than the more standard:

```fsharp
InputParam1 -> InputParam2 -> InputParam3 -> Result
```

I'm doing this just to make the input and output obvious.  When it comes to the implementation, it's more than likely that we'll switch to the standard way, so that
we can take advantage of the techniques in our functional toolbox such as partial application.

## Doing a design walkthrough

At this point, with a rough design in place, I like to do a walkthrough as if it were being used for real.
In a larger design, I might develop a small throwaway prototype, but in this case,
the design is small enough that I can do it in my head.

So, let's pretend that we are the UI and we are given the design above. We start by calling the initialization function to get a new game:

```fsharp
type InitGame = unit -> GameState 
```

Ok, so now we have a `GameState` and we are ready to display the initial grid. 

At this point, the UI would create, say, a grid of empty buttons, associate a cell to each button, and then draw the cell in the "empty" state. 

This is fine, because the UI doesn't have to think. We are explicitly giving the UI a list of all cells, and also making the initial cell state `Empty`,
so the UI doesn't have to know which is the default state -- it just displays what it is given.

One thing though. Since there is no input needed to set up the game, *and* the game state is immutable, we will have exactly the same initial state for every game. 

Therefore we don't need a function to create the initial game state, just a "constant" that gets reused for each game.

```fsharp
type InitialGameState = GameState 
```

## When does the game stop?

Next in our walkthrough, let's play a move. 

* A player, "X" or "O", clicks on a cell
* We combine the player and `CellPosition` into the appropriate type, such as a `PlayerXPos`
* We then pass that and the `GameState` into the appropriate `Move` function

```fsharp
type PlayerXMoves = 
    GameState * PlayerXPos -> GameState 
```

The output is a new `GameState`. The UI then calls `GetCells` to get the new cells. We loop through this list, update the display, and now we're ready to try again.

Excellent! 

Umm... except for the bit about knowing when to stop.

As designed, This game will go on forever.  We need to include something in the output of the move to let us know whether the game is over!

So let's create a `GameStatus` type to keep track of that. 

```fsharp
type GameStatus = 
    | InProcess 
    | PlayerXWon 
    | PlayerOWon 
    | Tie
```

And we need to add it to the output of the move as well, so now we have:

```fsharp
type PlayerXMoves = 
    GameState * PlayerXPos -> GameState * GameStatus 
```

So now we can keep playing moves repeatedly while `GameStatus` is `InProcess` and then stop.

The pseudocode for the UI would look like

```fsharp
// loop while game not over
let rec playMove gameState = 
    let pos = // get position from user input
    let newGameState,status = 
        playerXMoves (gameState,pos) // process move
    match status with
    | InProcess -> 
        // play another move
        playMove newGameState
    | PlayerXWon -> 
        // show that player X won
    | etc            

// start the game with the initial state
let startGame() = 
    playMove initialGameState
```

I think we've got everything we need to play a game now, so let's move on to error handling. 

## What kind of errors can happen?

Before we starting thinking about the internals of the game, let's think about what kinds of errors the UI team could make when using this design:  

**Could the UI create an invalid `GameState` and corrupt the game?**

No, because we are going to keep the internals of the game state hidden from the UI.

**Could the UI pass in an invalid `CellPosition`?**

No, because the horizontal and vertical components of `CellPosition` are restricted and therefore it cannot be created in an invalid state.
No validation is needed.

**Could the UI pass in a *valid* `CellPosition` but at the *wrong* time?**

Ah, now you're talking! Yes -- that is totally possible. In the design we have so far, there is nothing stopping a player playing the same square twice!

**Could the UI allow player X to play twice in a row?**

Again, yes. Nothing in our design prevents this.

**What about when the game has ended but the dumb UI forgets to check the `GameStatus` and doesn't notice. Should the game logic still accept moves?**

Of course not, but yet again our design fails to do this.

The big question is: can we fix these three issues *in our design* without having to rely on special validation code in the implementation?
That is, can we encode these rules into *types*.

At this point you might be thinking "why bother with all these types?"

The advantage of using types over validation code is that the types are part of the design, which means that business rules like these are self-documenting.
On the other hand, validation code tends to be scattered around and buried in obscure classes, so it is hard to get a big picture of all the constraints.

In general then, I prefer to use types rather than code if I can.

## Enforcing the rules through types

So, can we encode these rules using types? The answer is yes!  

To stop someone playing the same square twice we can change the game engine so that it outputs a list of valid moves.
And then we can require that *only* items in this list are allowed to be played in the next turn.

If we do this, our move type will look like this:

```fsharp
type ValidPositionsForNextMove = CellPosition list

// a move returns the list of available positions for the next move
type PlayerXMoves = 
    GameState * PlayerXPos -> // input
        GameState * GameStatus * ValidPositionsForNextMove // output
```

And we can extend this approach to stop player X playing twice in a row too. Simply make the `ValidPositionsForNextMove` be a list of `PlayerOPos` rather than generic positions.
Player X will not be able to play them!

```fsharp
type ValidMovesForPlayerX = PlayerXPos list
type ValidMovesForPlayerO = PlayerOPos list

type PlayerXMoves = 
    GameState * PlayerXPos -> // input
        GameState * GameStatus * ValidMovesForPlayerO // output
        
type PlayerOMoves = 
    GameState * PlayerOPos -> // input
        GameState * GameStatus * ValidMovesForPlayerX // output
```

This approach also means that when the game is over, there are *no valid moves* available. So the UI cannot just loop forever, it will be forced to stop and deal with the situation.

So now we have encoded all three rules into the type system -- no manual validation needed.


## Some refactoring

Let's do some refactoring now. 

First we have a couple of choice types with a case for Player X and another similar case for Player O.

```fsharp
type CellState = 
    | X
    | O
    | Empty

type GameStatus = 
    | InProcess 
    | PlayerXWon 
    | PlayerOWon 
    | Tie
```

Let's extract the players into their own type, and then we can parameterize the cases to make them look nicer:

```fsharp
type Player = PlayerO | PlayerX

type CellState = 
    | Played of Player 
    | Empty

type GameStatus = 
    | InProcess 
    | Won of Player
    | Tie
```

The second thing we can do is to note that we only need the valid moves in the `InProcess` case, not the `Won` or `Tie` cases, so let's merge `GameStatus`
and `ValidMovesForPlayer` into a single type called `MoveResult`, say:

```fsharp
type ValidMovesForPlayerX = PlayerXPos list
type ValidMovesForPlayerO = PlayerOPos list

type MoveResult = 
    | PlayerXToMove of GameState * ValidMovesForPlayerX
    | PlayerOToMove of GameState * ValidMovesForPlayerO
    | GameWon of GameState * Player 
    | GameTied of GameState 
```

We've replaced the `InProcess` case with two new cases `PlayerXToMove` and `PlayerOToMove`, which I think is actually clearer.

The move functions now look like:

```fsharp
type PlayerXMoves = 
    GameState * PlayerXPos -> 
        GameState * MoveResult
        
type PlayerOMoves = 
    GameState * PlayerOPos -> 
        GameState * MoveResult
```

I could have had the new `GameState` returned as part of `MoveResult` as well, but I left it "outside" to make it clear that is not to be used by the UI.

Also, leaving it outside will give us the option of writing helper code that will thread a game state through a series of calls for us.
This is a more advanced technique, so I'm not going to discuss it in this post.

Finally, the `InitialGameState` should also take advantage of the `MoveResult` to return the available moves for the first player.
Since it has both a game state and a initial set of moves, let's just call it `NewGame` instead.

```fsharp
type NewGame = GameState * MoveResult      
```

If the initial `MoveResult` is the `PlayerXToMove` case, then we have also constrained the UI so that only player X can move first.
Again, this allows the UI to be ignorant of the rules.

## Second recap 

So now here's the tweaked design we've got after doing the walkthrough. 

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

    // the private game state
    type GameState = exn  // use a placeholder

    type ValidMovesForPlayerX = PlayerXPos list
    type ValidMovesForPlayerO = PlayerOPos list
    
    // the move result
    type MoveResult = 
        | PlayerXToMove of ValidMovesForPlayerX
        | PlayerOToMove of ValidMovesForPlayerO
        | GameWon of Player 
        | GameTied 

    // the "use-cases"        
    type NewGame = 
        GameState * MoveResult      
    type PlayerXMoves = 
        GameState * PlayerXPos -> GameState * MoveResult
    type PlayerOMoves = 
        GameState * PlayerOPos -> GameState * MoveResult
    
    // helper function
    type GetCells = GameState -> Cell list
```

We're not quite done with the outside-in design yet. One question is yet to be resolved: how can we hide the implementation of `GameState` from the UI?

## Decoupling shared and private types

In any design, we want to decouple the "interface" from the "implementation". In this case, we have:

* A set of shared data structures and functions that are used by both the UI and the game engine. (`CellState`, `MoveResult`, `PlayerXPos`, etc.)
* A set of private data structures and functions that should only be accessed by the game logic. (just `GameState` so far)

It's obviously a good idea to keep these types separate. How should we do this?

In F#, the easiest way is to put them into separate modules, like this:

```fsharp
/// Types shared by the UI and the game logic
module TicTacToeDomain = 
    
    type HorizPosition = Left | HCenter | Right
    type VertPosition = Top | VCenter | Bottom
    type CellPosition = HorizPosition * VertPosition 

    type Player = PlayerO | PlayerX

    type CellState = 
        | Played of Player 
        | Empty
    
    type PlayerXMoves = 
        GameState * PlayerXPos -> GameState * MoveResult    
    // etc

/// Private types used by the internal game logic
module TicTacToeImplementation = 
    open TicTacToeDomain 

    // private implementation detail
    type GameState = {
        cells : Cell list
        }

    // etc
```

But if we want to keep the internals of the game logic private, what do we do with `GameState`?  It's used by public functions such as `PlayerXMoves`, but we want to keep
its structure secret. How can we do that?

### Option 1 - put the public and private types in the same module

The first choice might be to put the public and private types in the same module, and have this module be the "core" domain module that all other modules depend on.

Here's some code that demonstrates what this approach would look like:

```fsharp
module TicTacToeImplementation = 
     
    // public types  
    type HorizPosition = Left | HCenter | Right
    type VertPosition = Top | VCenter | Bottom
    type CellPosition = HorizPosition * VertPosition 
    
    type CellState = 
        | Played of Player 
        | Empty
    
    type PlayerXMoves = 
        GameState * PlayerXPos -> GameState * MoveResult    
    // etc
    
    // --------------------    
    // private types  

    type private InternalType = // to do

    // --------------------
    // public types with private constructor

    type GameState = private {
        cells : Cell list
        }

    // etc
```

All the types are in one module.

Many of the types, such as `CellState`, will be public by default. That's fine.

But you can see that some of the types, such as `InternalType`, have been marked private. That means that they cannot be used outside the module at all.

Finally, `GameState` is not private, but its constructor is, which means that it *can* be used outside the module, but because its constructor is private,
new ones can't be created, which sounds like what we need. 

We might have appeared to solve the issue, but this approach often causes problems of its own. For starters, trying to keep the `public` and `private` qualifiers straight
can cause annoying compiler errors, such as this one:

```text
The type 'XXX' is less accessible than the value, member or type 'YYY' it is used in
```

And even if this weren't a problem, putting the "interface" and the "implementation" in the same file will generally
end up creating extra complexity as the implementation gets larger.

### Option 2 - representing `GameState` with an abstract base class

The object-oriented way of approaching this would be to represent `GameState` as an abstract base class or interface, and then have a particular implementation
inherit from the abstract class.

This allows all the shared types to reference the abstract class or interface safely, while any particular implementation is hidden.

Here's how you might do this in F#:

```fsharp
/// Types shared by the UI and the game logic
module TicTacToeDomain = 
    
    // abstract base class    
    type GameState() = class end

/// Private types used by the internal game logic
module TicTacToeImplementation = 
    open TicTacToeDomain 

    type GameStateImpl() =
        inherit GameState()
        
    // etc        
```        

But alas, there are problems with this approach too.

First, it's not very functional, is it?  F# does support classes and interfaces for those situations when we need them,
but we should really be able to find a more idiomatic functional solution than this!

Second, it's potentially not safe. The actual implementation would have to downcast `GameState` into the type it expects in order to get at the internal data.
But if I had *two* implementations that inherited `GameState`, what's to stop me passing
a game state from implementation B into a function that is expecting a game state from implementation A?  Nothing! Chaos would ensue!

Note that in a pure OO model this situation could not happen because the `GameState` itself would have stateful methods instead of the pure functional API that we have here.

### Option 3 - parameterize the implementation

Let's think about the requirements again: "The `GameState` is public but we don't know what the implementation will be."

When you rephrase it like this, the functional way of modeling this becomes clear, which is to use *generic parameters* (aka "parametric polymorphism"). 

In other words, we make `GameState` a *generic type* which represents a particular implementation.

This means that the UI can work with the `GameState` type, but because the actual implementation type used is not known, the UI cannot accidentally
"look inside" and extract any information, *even if the implementation type is public*.

This last point is important, so I'm going to say it again with another example. If I give you a object of type `List<T>` in C#, you can work with the list in many ways,
but you cannot know what the `T` is, and so you can never accidentally write code that assumes that `T` is an `int` or a `string` or a `bool`.
And this "hidden-ness" has got nothing to do with whether `T` is a public type or not.

If we do take this approach then we can allow the internals of the game state to be completely public,
safe in the knowledge that the UI cannot use that information even if it wanted to!

So here's some code demonstrating this approach. 

First the shared types, with `GameState<'T>` being the parameterized version.

```fsharp
/// Types shared by the UI and the game logic
module TicTacToeDomain = 

    // unparameterized types
    type PlayerXPos = PlayerXPos of CellPosition 
    type PlayerOPos = PlayerOPos of CellPosition 

    // parameterized types
    type PlayerXMoves<'GameState> = 
        'GameState * PlayerXPos -> 'GameState * MoveResult
    type PlayerOMoves<'GameState> = 
        'GameState * PlayerOPos -> 'GameState * MoveResult
    
    // etc
```        

The types that don't use the game state are unchanged, but you can see that `PlayerXMoves<'T>` has been parameterized with the game state.

Adding generics like this can often cause cascading changes to many types, forcing them all to be parameterized.
Dealing with all these generics is one reason why type inference is so helpful in practice!

Now for the types internal to the game logic. They can all be public now, because the UI won't be able to know about them.

```fsharp
module TicTacToeImplementation =
    open TicTacToeDomain

    // can be public
    type GameState = {
        cells : Cell list
        }
```        

Finally, here's what the implementation of a `playerXMoves` function might look like:

```fsharp
let playerXMoves : PlayerXMoves<GameState> = 
    fun (gameState,move) ->
        // logic
```        

This function references a particular implementation, but can be passed into the UI code because it conforms to the `PlayerXMoves<'T>` type.

Furthermore, by using generic parameters, we naturally enforce that the same implementation, say "GameStateA", is used throughout.

In other words, the game state created by `InitGame<GameStateA>` can *only* be passed to a `PlayerXMoves<GameStateA>` function which is parameterized on the *same* implementation type.

## Glueing it all together with "dependency injection"

Finally, let's talk about how everything can be glued together. 

The UI code will be designed to work with a *generic* implementation of `GameState`, and thus generic versions of the `newGame` and `move` functions.

But of course, at some point we need to get access to the `newGame` and `move` functions for a *specific* implementation. What's the best way to glue all this together?

The answer is the functional equivalent of dependency injection. We will have an "application" or "program" component as a top-level layer that will construct an implementation
and pass it to the UI.

Here's an example of what such code would look like:

* The `GameImplementation` module exports specific implementations of `newGame` and the `move` functions.
* The `UserInterface` module exports a `TicTacToeForm` class that accepts these implementations in its constructor.
* The `Application` module glues everything together. It creates a `TicTacToeForm` and passes it the implementations exported from the `GameImplementation` module.

Here's some code to demonstrate this approach:

```fsharp
module TicTacToeImplementation = 
    open TicTacToeDomain 
   
    /// create the state of a new game
    let newGame : NewGame<GameState> = 
        // return new game and current available moves
        let validMoves = // to do
        gameState, PlayerXToMove validMoves
    
    let playerXMoves : PlayerXMoves<GameState> = 
        fun (gameState,move) ->
            // implementation

module WinFormUI = 
    open TicTacToeDomain
    open System.Windows.Forms

    type TicTacToeForm<'T>
        (
        // pass in the required functions 
        // as parameters to the constructor
        newGame:NewGame<'T>, 
        playerXMoves:PlayerXMoves<'T>,
        playerOMoves:PlayerOMoves<'T>,
        getCells:GetCells<'T>
        ) = 
        inherit Form()
     // implementation to do

module WinFormApplication = 
    open WinFormUI

    // get functions from implementation
    let newGame = TicTacToeImplementation.newGame
    let playerXMoves = TicTacToeImplementation.playerXMoves
    let playerOMoves = TicTacToeImplementation.playerOMoves
    let getCells = TicTacToeImplementation.getCells

    // create form and start game
    let form = 
        new TicTacToeForm<_>(newGame,playerXMoves,playerOMoves,getCells)
    form.Show()
```

A few notes on this code:

First, I'm using WinForms rather than WPF because it has Mono support and because it works without NuGet dependencies. If you want to use something better, check out [ETO.Forms](http://picoe.ca/2012/09/11/introducing-eto-forms-a-cross-platform-ui-for-net/).

Next, you can see that I've explicitly added the type parameters to `TicTacToeForm<'T>` like this.  

```fsharp
TicTacToeForm<'T>(newGame:NewGame<'T>, playerXMoves:PlayerXMoves<'T>, etc)
```

I could have eliminated the type parameter for the form by doing something like this instead:

```fsharp
TicTacToeForm(newGame:NewGame<_>, playerXMoves:PlayerXMoves<_>, etc)
```

or even:

```fsharp
TicTacToeForm(newGame, playerXMoves, etc)
```

and let the compiler infer the types, but this often causes a "less generic" warning like this:

```text
warning FS0064: This construct causes code to be less generic than indicated by the type annotations. 
The type variable 'T has been constrained to be type 'XXX'.
```

By explicitly writing `TicTacToeForm<'T>`, this can be avoided, although it is ugly for sure.

## Some more refactoring

We've got four different functions to export. That's getting a bit much so let's create a record to store them in:

```fsharp
// the functions exported from the implementation
// for the UI to use.
type TicTacToeAPI<'GameState>  = 
    {
    newGame : NewGame<'GameState>
    playerXMoves : PlayerXMoves<'GameState> 
    playerOMoves : PlayerOMoves<'GameState> 
    getCells : GetCells<'GameState>
    }
```

This acts both as a container to pass around functions, *and* as nice documentation of what functions are available in the API.

The implementation now has to create an "api" object:

```fsharp
module TicTacToeImplementation = 
    open TicTacToeDomain 
   
    /// create the functions to export
    let newGame : NewGame<GameState> = // etc
    let playerXMoves : PlayerXMoves<GameState> = // etc
    // etc

    // export the functions
    let api = {
        newGame = newGame 
        playerOMoves = playerOMoves 
        playerXMoves = playerXMoves 
        getCells = getCells
        }
```

But the UI code simplifies as a result:

```fsharp
module WinFormUI = 
    open TicTacToeDomain
    open System.Windows.Forms

    type TicTacToeForm<'T>(api:TicTacToeAPI<'T>) = 
        inherit Form()
     // implementation to do

module WinFormApplication = 
    open WinFormUI

    // get functions from implementation
    let api = TicTacToeImplementation.api

    // create form and start game
    let form = new TicTacToeForm<_>(api)
    form.Show()
```

## Prototyping a minimal implementation

It seems like we're getting close to a final version. But let's do one more walkthrough to exercise the "dependency injection" design,
this time writing some minimal code to test the interactions.

For example, here is some minimal code to implement the `newGame` and `playerXMoves` functions. 

* The `newGame` is just an game with no cells and no available moves
* The minimal implementation of `move` is easy -- just return game over!

```fsharp
let newGame : NewGame<GameState> = 
    // create initial game state with empty everything
    let gameState = { cells=[]}            
    let validMoves = []
    gameState, PlayerXToMove validMoves

let playerXMoves : PlayerXMoves<GameState> = 
    // dummy implementation
    fun gameState move ->  gameState,GameTied

let playerOMoves : PlayerOMoves<GameState> = 
    // dummy implementation
    fun gameState move ->  gameState,GameTied

let getCells gameState = 
    gameState.cells 

let api = {
    newGame = newGame 
    playerOMoves = playerOMoves 
    playerXMoves = playerXMoves 
    getCells = getCells
    }
```

Now let's create a minimal implementation of the UI. We won't draw anything or respond to clicks, just mock up some functions so that we can test the logic. 

Here's my first attempt:

```fsharp
type TicTacToeForm<'GameState>(api:TicTacToeAPI<'GameState>) = 
    inherit Form()

    let mutable gameState : 'GameState = ???
    let mutable lastMoveResult : MoveResult = ???

    let displayCells gameState = 
        let cells = api.getCells gameState 
        for cell in cells do
            // update display

    let startGame()= 
        let initialGameState,initialResult = api.newGame
        gameState <- initialGameState
        lastMoveResult <- initialResult 
        // create cell grid from gameState 

    let handleMoveResult moveResult =
        match moveResult with
        | PlayerXToMove availableMoves -> 
            // show available moves
        | PlayerOToMove availableMoves -> 
            // show available moves
        | GameWon player -> 
            let msg = sprintf "%A Won" player 
            MessageBox.Show(msg) |> ignore
        | GameTied -> 
            MessageBox.Show("Tied") |> ignore

    // handle a click
    let handleClick() =
        let gridIndex = 0,0  // dummy for now
        let cellPos = createCellPosition gridIndex
        match lastMoveResult with
        | PlayerXToMove availableMoves -> 
            let playerXmove = PlayerXPos cellPos
            // if move is in available moves then send it
            // to the api
            let newGameState,newResult = 
                api.playerXMoves gameState playerXmove 
            handleMoveResult newResult 

            //update the globals
            gameState <- newGameState
            lastMoveResult <- newResult 
        | PlayerOToMove availableMoves -> 
            let playerOmove = PlayerOPos cellPos
            // if move is in available moves then send it
            // to the api
            // etc
        | GameWon player -> 
            ?? // we aleady showed after the last move
```

As you can see, I'm planning to use the standard Form event handling approach -- each cell will have a "clicked" event handler associated with it. 
How the control or pixel location is converted to a `CellPosition` is something I'm not going to worry about right now, so I've just hard-coded some dummy data.

I'm *not* going to be pure here and have a recursive loop. Instead, I'll keep the current `gameState` as a mutable which gets updated after each move.

But now we have got a tricky situation... What is the `gameState` when the game hasn't started yet? What should we initialize it to?  Similarly,
when the game is over, what should it be set to?

```fsharp
let mutable gameState : 'GameState = ???
```

One choice might be to use a `GameState option` but that seems like a hack, and it makes me think that we are failing to think of something.

Similarly, we have a field to hold the result of the last move (`lastMoveResult`) so we can keep track of whose turn it is, or whether the game is over.

But again, what should it be set to when the game hasn't started?

Let's take a step back and look at all the states the user interface can be in -- not the state of the *game* itself, but the state of the *user interface*.

* We start off in an "idle" state, with no game being played.
* Then the user starts the game, and we are "playing".
* While each move is played, we stay in the "playing" state.
* When the game is over, we show the win or lose message.
* We wait for the user to acknowledge the end-of-game message, then go back to idle again.

Again, this is for the UI only, it has nothing to do with the internal game state. 

So -- our solution to all problems! -- let's create a type to represent these states.

```fsharp
type UiState = 
    | Idle
    | Playing
    | Won
    | Lost
```

But do we really need the `Won` and `Lost` states? Why don't we just go straight back to `Idle` when the game is over?

So now the type looks like this:

```fsharp
type UiState = 
    | Idle
    | Playing
```

The nice thing about using a type like this is that we can easily store the data that we need for each state. 

* What data do we need to store in the `Idle` state? Nothing that I can think of.
* What data do we need to store in the `Playing` state? Well, this would be a perfect place to keep track of
  the `gameState` and `lastMoveResult` that we were having problems with earlier.
  They're only needed when the game is being played, but not otherwise. 

So our final version looks like this. We've had to add the `<'GameState>` to `UiState` because we don't know what the actual game state is.
  
```fsharp
type UiState<'GameState> = 
    | Idle
    | Playing of 'GameState * MoveResult 
```

With this type now available, we no longer need to store the game state directly as a field in the class. Instead we store a mutable `UiState`, which is initialized to `Idle`.

```fsharp
type TicTacToeForm<'GameState>(api:TicTacToeAPI<'GameState>) = 
    inherit Form()

    let mutable uiState = Idle
```

When we start the game, we change the UI state to be `Playing`:

```fsharp
let startGame()= 
    uiState <- Playing api.newGame
    // create cell grid from gameState 
```

And when we handle a click, we only do something if the uiState is in `Playing` mode,
and then we have no trouble getting the `gameState` and `lastMoveResult` that we need, because it is stored as part of the data for that case.

```fsharp
let handleClick() =
    match uiState with
    | Idle -> ()
        // do nothing
        
    | Playing (gameState,lastMoveResult) ->
        let gridIndex = 0,0  // dummy for now
        let cellPos = createCellPosition gridIndex
        match lastMoveResult with
        | PlayerXToMove availableMoves -> 
            let playerXmove = PlayerXPos cellPos
            // if move is in available moves then send it
            // to the api
            let newGameState,newResult = 
                api.playerXMoves gameState playerXmove 
                
            // handle the result
            // e.g. if the game is over
            handleMoveResult newResult 

            // update the uiState with newGameState
            uiState <- Playing (newGameState,newResult)

        | PlayerOToMove availableMoves -> 
            // etc
        | _ -> 
            // ignore other states
```

If you look at the last line of the `PlayerXToMove` case, you can see the global `uiState` field being updated with the new game state:

```fsharp
| PlayerXToMove availableMoves -> 
    // snipped
    
    let newGameState,newResult = // get new state

    // update the uiState with newGameState
    uiState <- Playing (newGameState,newResult)
```


So where have we got to with this bit of prototyping?

It's pretty ugly, but it has served its purpose. 

The goal was to quickly implement the UI to see if the design held up in use, and I think we can say that it did,
because the design of the domain types and api has remained unchanged.

We also understand the UI requirements a bit better, which is a bonus. I think we can stop now!

## The complete game, part 1: The design

To finish off, I'll show the code for the complete game, including implementation and user interface.

If you don't want to read this code, you can skip to the [questions and summary](#questions) below.

*All the code shown is available on GitHub in [this gist](https://gist.github.com/swlaschin/3418b549bd222396da82).*

We'll start with our final domain design:

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

    // helper function
    type GetCells<'GameState> = 
        'GameState -> Cell list

    // the functions exported from the implementation
    // for the UI to use.
    type TicTacToeAPI<'GameState>  = 
        {
        newGame : NewGame<'GameState>
        playerXMoves : PlayerXMoves<'GameState> 
        playerOMoves : PlayerOMoves<'GameState> 
        getCells : GetCells<'GameState>
        }
```

## The complete game, part 2: The game logic implementation

Next, here's a complete implementation of the design which I will not discuss in detail. I hope that the comments are self-explanatory. 

```fsharp
module TicTacToeImplementation =
    open TicTacToeDomain

    /// private implementation of game state
    type GameState = {
        cells : Cell list
        }

    /// the list of all horizontal positions
    let allHorizPositions = [Left; HCenter; Right]
    
    /// the list of all horizontal positions
    let allVertPositions = [Top; VCenter; Bottom]

    /// A type to store the list of cell positions in a line
    type Line = Line of CellPosition list

    /// a list of the eight lines to check for 3 in a row
    let linesToCheck = 
        let makeHLine v = Line [for h in allHorizPositions do yield (h,v)]
        let hLines= [for v in allVertPositions do yield makeHLine v] 

        let makeVLine h = Line [for v in allVertPositions do yield (h,v)]
        let vLines = [for h in allHorizPositions do yield makeVLine h] 

        let diagonalLine1 = Line [Left,Top; HCenter,VCenter; Right,Bottom]
        let diagonalLine2 = Line [Left,Bottom; HCenter,VCenter; Right,Top]

        // return all the lines to check
        [
        yield! hLines
        yield! vLines
        yield diagonalLine1 
        yield diagonalLine2 
        ]

    /// get the cells from the gameState
    let getCells gameState = 
        gameState.cells 

    /// get the cell corresponding to the cell position
    let getCell gameState posToFind = 
        gameState.cells 
        |> List.find (fun cell -> cell.pos = posToFind)

    /// update a particular cell in the GameState 
    /// and return a new GameState
    let private updateCell newCell gameState =

        // create a helper function
        let substituteNewCell oldCell =
            if oldCell.pos = newCell.pos then
                newCell
            else 
                oldCell                 

        // get a copy of the cells, with the new cell swapped in
        let newCells = gameState.cells |> List.map substituteNewCell 
        
        // return a new game state with the new cells
        {gameState with cells = newCells }

    /// Return true if the game was won by the specified player
    let private isGameWonBy player gameState = 
        
        // helper to check if a cell was played by a particular player
        let cellWasPlayedBy playerToCompare cell = 
            match cell.state with
            | Played player -> player = playerToCompare
            | Empty -> false

        // helper to see if every cell in the Line has been played by the same player
        let lineIsAllSamePlayer player (Line cellPosList) = 
            cellPosList 
            |> List.map (getCell gameState)
            |> List.forall (cellWasPlayedBy player)

        linesToCheck
        |> List.exists (lineIsAllSamePlayer player)


    /// Return true if all cells have been played
    let private isGameTied gameState = 
        // helper to check if a cell was played by any player
        let cellWasPlayed cell = 
            match cell.state with
            | Played _ -> true
            | Empty -> false

        gameState.cells
        |> List.forall cellWasPlayed 

    /// determine the remaining moves for a player
    let private remainingMovesForPlayer playerMove gameState = 

        // helper to return Some if a cell is playable
        let playableCell cell = 
            match cell.state with
            | Played player -> None
            | Empty -> Some (playerMove cell.pos)

        gameState.cells
        |> List.choose playableCell


    /// create the state of a new game
    let newGame = 

        // allPositions is the cross-product of the positions
        let allPositions = [
            for h in allHorizPositions do 
            for v in allVertPositions do 
                yield (h,v)
            ]

        // all cells are empty initially
        let emptyCells = 
            allPositions 
            |> List.map (fun pos -> {pos = pos; state = Empty})
        
        // create initial game state
        let gameState = { cells=emptyCells }            

        // initial set of valid moves for player X is all positions
        let validMoves = 
            allPositions 
            |> List.map PlayerXPos

        // return new game
        gameState, PlayerXToMove validMoves

    // player X makes a move
    let playerXMoves gameState (PlayerXPos cellPos) = 
        let newCell = {pos = cellPos; state = Played PlayerX}
        let newGameState = gameState |> updateCell newCell 
        
        if newGameState |> isGameWonBy PlayerX then
            // return the new state and the move result
            newGameState, GameWon PlayerX
        elif newGameState |> isGameTied then
            // return the new state and the move result
            newGameState, GameTied  
        else
            let remainingMoves = 
                newGameState |> remainingMovesForPlayer PlayerOPos 
            newGameState, PlayerOToMove remainingMoves

    // player O makes a move
    let playerOMoves gameState (PlayerOPos cellPos) = 
        let newCell = {pos = cellPos; state = Played PlayerO}
        let newGameState = gameState |> updateCell newCell 
        
        if newGameState |> isGameWonBy PlayerO then
            // return the new state and the move result
            newGameState, GameWon PlayerO
        elif newGameState |> isGameTied then
            // return the new state and the move result
            newGameState, GameTied 
        else
            let remainingMoves = 
                newGameState |> remainingMovesForPlayer PlayerXPos 
            newGameState, PlayerXToMove remainingMoves

        // Exercise - refactor to remove the duplicate code from                 
        // playerXMoves  and playerOMoves 


    /// export the API to the application
    let api = {
        newGame = newGame 
        playerOMoves = playerOMoves 
        playerXMoves = playerXMoves 
        getCells = getCells
        }
```

## The complete game, part 3: A console based user interface

And to complete the implementation, here's the code for a console based user interface. 

Obviously this part of the implementation is not pure! I'm writing to and reading from the console, duh.
If you want to be extra good, it would be easy enough to convert this to a pure implementation using `IO` or similar.  

Personally, I like to focus on the core domain logic being pure and I generally don't bother about the UI too much, but that's just me. 

```fsharp
/// Console based user interface
module ConsoleUi =
    open TicTacToeDomain
    
    /// Track the UI state
    type UserAction<'a> =
        | ContinuePlay of 'a
        | ExitGame

    /// Print each available move on the console
    let displayAvailableMoves moves = 
        moves
        |> List.iteri (fun i move -> 
            printfn "%i) %A" i move )

    /// Get the move corresponding to the 
    /// index selected by the user
    let getMove moveIndex moves = 
        if moveIndex < List.length moves then
            let move = List.nth moves moveIndex 
            Some move
        else
            None

    /// Given that the user has not quit, attempt to parse
    /// the input text into a index and then find the move
    /// corresponding to that index
    let processMoveIndex inputStr gameState availableMoves makeMove processInputAgain = 
        match Int32.TryParse inputStr with
        // TryParse will output a tuple (parsed?,int)
        | true,inputIndex ->
            // parsed ok, now try to find the corresponding move
            match getMove inputIndex availableMoves with
            | Some move -> 
                // corresponding move found, so make a move
                let moveResult = makeMove gameState move 
                ContinuePlay moveResult // return it
            | None ->
                // no corresponding move found
                printfn "...No move found for inputIndex %i. Try again" inputIndex 
                // try again
                processInputAgain()
        | false, _ -> 
            // int was not parsed
            printfn "...Please enter an int corresponding to a displayed move."             
            // try again
            processInputAgain()

    /// Ask the user for input. Process the string entered as 
    /// a move index or a "quit" command
    let rec processInput gameState availableMoves makeMove = 

        // helper that calls this function again with exactly
        // the same parameters
        let processInputAgain() = 
            processInput gameState availableMoves makeMove 

        printfn "Enter an int corresponding to a displayed move or q to quit:" 
        let inputStr = Console.ReadLine()
        if inputStr = "q" then
            ExitGame
        else
            processMoveIndex inputStr gameState availableMoves makeMove processInputAgain
            
    /// Display the cells on the console in a grid
    let displayCells cells = 
        let cellToStr cell = 
            match cell.state with
            | Empty -> "-"            
            | Played player ->
                match player with
                | PlayerO -> "O"
                | PlayerX -> "X"

        let printCells cells  = 
            cells
            |> List.map cellToStr
            |> List.reduce (fun s1 s2 -> s1 + "|" + s2) 
            |> printfn "|%s|"

        let topCells = 
            cells |> List.filter (fun cell -> snd cell.pos = Top) 
        let centerCells = 
            cells |> List.filter (fun cell -> snd cell.pos = VCenter) 
        let bottomCells = 
            cells |> List.filter (fun cell -> snd cell.pos = Bottom) 
        
        printCells topCells
        printCells centerCells 
        printCells bottomCells 
        printfn ""   // add some space
        
    /// After each game is finished,
    /// ask whether to play again.
    let rec askToPlayAgain api  = 
        printfn "Would you like to play again (y/n)?"             
        match Console.ReadLine() with
        | "y" -> 
            ContinuePlay api.newGame
        | "n" -> 
            ExitGame
        | _ -> askToPlayAgain api 

    /// The main game loop, repeated
    /// for each user input
    let rec gameLoop api userAction = 
        printfn "\n------------------------------\n"  // a separator between moves
        
        match userAction with
        | ExitGame -> 
            printfn "Exiting game."             
        | ContinuePlay (state,moveResult) -> 
            // first, update the display
            state |> api.getCells |> displayCells

            // then handle each case of the result
            match moveResult with
            | GameTied -> 
                printfn "GAME OVER - Tie"             
                printfn ""             
                let nextUserAction = askToPlayAgain api 
                gameLoop api nextUserAction
            | GameWon player -> 
                printfn "GAME WON by %A" player            
                printfn ""             
                let nextUserAction = askToPlayAgain api 
                gameLoop api nextUserAction
            | PlayerOToMove availableMoves -> 
                printfn "Player O to move" 
                displayAvailableMoves availableMoves
                let newResult = processInput state availableMoves api.playerOMoves
                gameLoop api newResult 
            | PlayerXToMove availableMoves -> 
                printfn "Player X to move" 
                displayAvailableMoves availableMoves
                let newResult = processInput state availableMoves api.playerXMoves
                gameLoop api newResult 

    /// start the game with the given API
    let startGame api =
        let userAction = ContinuePlay api.newGame
        gameLoop api userAction 
```

And finally, the application code that connects all the components together and launches the UI:

```fsharp
module ConsoleApplication = 

    let startGame() =
        let api = TicTacToeImplementation.api
        ConsoleUi.startGame api
```

## Example game

Here's what the output of this game looks like:

```text
|-|X|-|
|X|-|-|
|O|-|-|

Player O to move
0) PlayerOPos (Left, Top)
1) PlayerOPos (HCenter, VCenter)
2) PlayerOPos (HCenter, Bottom)
3) PlayerOPos (Right, Top)
4) PlayerOPos (Right, VCenter)
5) PlayerOPos (Right, Bottom)
Enter an int corresponding to a displayed move or q to quit:
1

------------------------------

|-|X|-|
|X|O|-|
|O|-|-|

Player X to move
0) PlayerXPos (Left, Top)
1) PlayerXPos (HCenter, Bottom)
2) PlayerXPos (Right, Top)
3) PlayerXPos (Right, VCenter)
4) PlayerXPos (Right, Bottom)
Enter an int corresponding to a displayed move or q to quit:
1

------------------------------

|-|X|-|
|X|O|-|
|O|X|-|

Player O to move
0) PlayerOPos (Left, Top)
1) PlayerOPos (Right, Top)
2) PlayerOPos (Right, VCenter)
3) PlayerOPos (Right, Bottom)
Enter an int corresponding to a displayed move or q to quit:
1

------------------------------

|-|X|O|
|X|O|-|
|O|X|-|

GAME WON by PlayerO

Would you like to play again (y/n)?
```

## Logging

Oops! We promised we would add logging to make it enterprise-ready!

That's easy -- all we have to do is replace the api functions with equivalent functions that log the data we're interested in

```fsharp
module Logger = 
    open TicTacToeDomain
     
    let logXMove (PlayerXPos cellPos)= 
        printfn "X played %A" cellPos

    let logOMove (PlayerOPos cellPos)= 
        printfn "O played %A" cellPos

    /// inject logging into the API
    let injectLogging api =

        // make a logged version of the game function 
        let playerXMoves state move = 
            logXMove move 
            api.playerXMoves state move 

        // make a logged version of the game function 
        let playerOMoves state move = 
            logOMove move 
            api.playerOMoves state move 
                                     
        // create a new API with                             
        // the move functions replaced
        // with logged versions
        { api with
            playerXMoves = playerXMoves
            playerOMoves = playerOMoves
            }
```

Obviously, in a real system you'd replace it with a proper logging tool such as `log4net` and generate better output, but I think this demonstrates the idea.

Now to use this, all we have to do is change the top level application to transform the original api to a logged version of the api:

```fsharp
module ConsoleApplication = 

    let startGame() =
        let api = TicTacToeImplementation.api
        let loggedApi = Logger.injectLogging api
        ConsoleUi.startGame loggedApi 
```

And that's it. Logging done!  

Oh, and remember that I originally had the initial state created as a function rather than as a constant?

```fsharp
type InitGame = unit -> GameState
```

I changed to a constant early on in the design. But I'm regretting that now, because it means that I can't hook into the "init game" event and log it.
If I do want to log the start of each game, I should really change it back to a function again.

<a id="questions"></a>

## Questions

**Question: You went to the trouble of hiding the internal structure of `GameState`, yet the `PlayerXPos` and `PlayerOPos` types are public. Why?**

I forgot! And then laziness kept me from updating the code, since this is really just an exercise in design.

It's true that in the current design, a malicious user interface could construct a `PlayerXPos`
and then play X when it is not player X's turn, or to play a position that has already been played.

You could prevent this by hiding the implementation of `PlayerXPos` in the same way as we did for game state, using a type parameter.
And of course you'd have to tweak all the related classes too.

Here's a snippet of what that would look like:

```fsharp
type MoveResult<'PlayerXPos,'PlayerOPos> = 
    | PlayerXToMove of 'PlayerXPos list
    | PlayerOToMove of 'PlayerOPos list
    | GameWon of Player 
    | GameTied 

type NewGame<'GameState,'PlayerXPos,'PlayerOPos> = 
    'GameState * MoveResult<'PlayerXPos,'PlayerOPos>      

type PlayerXMoves<'GameState,'PlayerXPos,'PlayerOPos> = 
    'GameState -> 'PlayerXPos -> 
        'GameState * MoveResult<'PlayerXPos,'PlayerOPos>      
type PlayerOMoves<'GameState,'PlayerXPos,'PlayerOPos> = 
    'GameState -> 'PlayerOPos -> 
        'GameState * MoveResult<'PlayerXPos,'PlayerOPos>      
```

We'd also need a way for the UI to see if the `CellPosition` a user selected was valid. That is, given a `MoveResult` and the desired `CellPosition`, 
if the position *is* valid, return `Some` move, otherwise return `None`.

```fsharp
type GetValidXPos<'PlayerXPos,'PlayerOPos> = 
    CellPosition * MoveResult<'PlayerXPos,'PlayerOPos> -> 'PlayerXPos option
```

It's getting kind of ugly now, though. That's one problem with type-first design: the type parameters can get complicated! 

So it's a trade-off. How much do you use types to prevent accidental bugs without overwhelming the design?

In this case, I do think the `GameState` should be secret,
as it is likely to change in the future and we want to ensure that the UI is not accidentally coupled to implementation details.

For the move types though, (a) I don't see the implementation changing and (b) the consequence of a malicious UI action is not very high, so overall I don't mind having the
implementation be public.

*UPDATE 2015-02-16: In the [next post](/posts/enterprise-tic-tac-toe-2/) I solve this problem in a more elegant way, and get rid of `GameState` as well!*

**Question: Why are you using that strange syntax for defining the `initGame` and `move` functions?**

You mean, why I am defining the functions like this:

```fsharp
/// create the state of a new game
let newGame : NewGame<GameState> = 
    // implementation

let playerXMoves : PlayerXMoves<GameState> = 
    fun (gameState,move) ->
        // implementation
```

rather than in the "normal" way like this:

```fsharp
/// create the state of a new game
let newGame  = 
    // implementation

let playerXMoves (gameState,move) = 
    // implementation

```

I'm doing this when I want to treat functions as values. Just as we might say "*x* is a value of type *int*" like this `x :int = ...`,
I'm saying that "*playerXMoves* is a value of type *PlayerXMoves*" like this:
`playerXMoves : PlayerXMoves = ...`. It's just that in this case, the value is a function rather than a simple value.

Doing it this way follows from the type-first approach: create a type, then implement things that conform to that type.

Would I recommend doing this for normal code? Probably not! 

I'm only doing this as part of an exploratory design process. Once the design stabilizes, I would tend to switch back to the normal
way of doing things.  


**Question: This seems like a lot of work. Isn't this just [BDUF](https://en.wikipedia.org/wiki/Big_Design_Up_Front) under another guise?**

This might seem like quite a long winded way of doing design, but in practice, it would probably not take very long.
Certainly no longer than mocking up an exploratory prototype in another language.

We've gone through a number of quick iterations, using types to document the design,
and using the REPL as a "executable spec checker" to make sure that it all works together properly.

And at the end of all this, we now have a decent design with some nice properties:

* There is a "API" that separates the UI from the core logic, so that work on each part can proceed in parallel if needed.
* The types act as documentation and will constrain the implementation in a way that UML diagrams could never do!
* The design is encoded in types, so that ~~any~~ the inevitable changes that occur during development can be made with confidence.

I think this whole process is actually pretty agile, once you get used to working this way.

**Question: Come on, would you *really* write Tic-Tac-Toe this way?**

It depends. If it was just me, maybe not. :-)  

But if it was a more complex system with different teams for the front-end and back-end, then I would certainly use a design-first approach like this.
In cases like that, things like data-hiding and abstract interfaces are critical, and I think this approach delivers that.

**Question: Why is the design so specific? It seems like none of it will be reusable at all. Why not?**

Yes, this code is full of very specific types: `Cell`, `GameState`, etc. And it's true that none of it will be reusable.  

There is always a tension between a very domain-specific and non-reusable design, like this one,
and an [abstract and reusable library](https://msdn.microsoft.com/en-us/library/ee353738.aspx) of things like lists and trees.

Ideally, you would start with low-level, reusable components and then compose them into larger more-specific ones (e.g. a DSL),
from which you can build a application. (Tomas has a good post on [exactly this](http://tomasp.net/blog/2015/library-layers/index.html)).

The reasons why I did not do that here is that, first, I always like to start with very *concrete* designs.
You can't even know what a good abstraction looks like until you have built something a few times.

We have separated the UI from the core logic, but going any further than that does not make sense to me right now.
If I was going to build lots of other kinds of games that were similar to Tic-Tac-Toe, then some useful abstractions might become apparent.

Second, designs with concrete types are easier for non-experts to understand.
I'd like to think that I could show these domain types to a non-programmer (e.g. a domain expert) and have them understand and comment sensibly on them.
If they were more abstract, that would not be possible.

## Exercises

If you want a challenge, here are some exercises for you:

* The `playerXMoves` and `playerOMoves` functions have very similar code. How would you refactor them to reduce that? 
* Do a security audit and think of all the ways that a malicious user or UI could corrupt the game using the current design. Then fix them! 

## Summary

In this post, we've seen how to design a system using mostly types, with the occasional code fragments to help us clarify issues.

It was definitely an exercise in design overkill but I hope that there are some ideas in there that might be applicable to real non-toy projects.

At the start, I claimed that this design would be "enterprise" ready. Is it?

* We *do* have separation of concerns via the functions that are exported to the UI.
* We *do* have a well documented API. There are no magic numbers, the names of the types are self-documenting, and the list of functions exported is in one place.
* We *do* have a security  model to prevent unauthorized actions from occurring. As it stands, it would be hard to accidentally mess up.
  And if we go the extra distance by parameterizing the move types as well, then it becomes really quite hard for the game to be corrupted.
* We *do* have well-documented code, I think. Even though this is "enterprise", the code is quite explicit in what it does. There are no wasted abstractions -- no `AbstractSingletonProxyFactoryBean` to make fun of.
* We *did* add auditing and logging easily, and in an elegant way after the fact, without interfering with the core design.
* We get *scalability* for free because there is no global session data. All we have to do is persist the game state in the browser (Or we could use MongoDb and be web scale).

This is not a perfect design -- I can think of a number of ways to improve it -- but overall I'm quite happy with it, considering it was a straight brain-to-code dump. 

What do you think?  Let me know in the comments.

**UPDATE 2015-02-16: I ended up being unhappy with this design after all. In the [next post](/posts/enterprise-tic-tac-toe-2/) I tell you why, and present a better design.**

*NOTE: The code for this post is available on GitHub in [this gist](https://gist.github.com/swlaschin/3418b549bd222396da82).*

