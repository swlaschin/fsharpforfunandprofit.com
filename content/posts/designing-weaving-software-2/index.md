---
layout: post
title: "Weaving design, part 2"
description: "Using generics to reduce duplication"
date: 2025-12-28
categories: []
seriesId: "Designing Weaving Software"
seriesOrder: 2
---


This is the second post in a series on designing software for weavers. These posts contain random thoughts and musings based on design challenges that I come across as I build a real-world project. In the [first post](../designing-weaving-software/) I give the context for this project and presented the code for a initial domain-driven design. In this post and the next one, we'll move to the next stage, converting between a text representation and the domain model. 


But before doing that, let's revisit the use of generics.

## The return of generics

In the previous posts, I stated that making the domain types generic (by adding type parameters) would make the domain types more confusing and less understandable.
Here are three of the non-generic domain types which, as you can see, are very structurally similar:

```fsharp
type ThreadingBlock =
  /// A single thread
  | Single of ThreadingEnd
  /// A collection of threads or subgroups
  | InlineGroup of ThreadingBlock list
  /// References a definition defined separately using a letter label
  | LabeledGroup of GroupLabel
  /// Repeats a block N times
  | Repeat of ThreadingBlock * RepeatCount

type LiftplanBlock =
  | Single of LiftplanPick
  | InlineGroup of LiftplanBlock list
  | LabeledGroup of GroupLabel
  | Repeat of LiftplanBlock * RepeatCount

type ColorPatternUnit =
  | Single of ColorIndex
  | InlineGroup of ColorPatternUnit list
  | LabeledGroup of GroupLabel
  | Repeat of ColorPatternUnit * RepeatCount
  | Mirrored of ColorPatternUnit 
```

And here's the generic version. Simpler, but it has lost the domain-centric terminology.


```fsharp
type Block<'single,'transform> =
  | Single of 'single
  | InlineGroup of Block<'single,'transform> list
  | LabeledGroup of GroupLabel
  | Transform of Block<'single,'transform> * 'transform
```

Sometimes, as in the case of a generic `List<>` type, there certainly is a higher level at which generics make sense. But in this specific domain there is no “higher semantic level” that is relevant. So I did not want to make the types generic by parameterizing them.

But...

When it comes to the *implementation* (as opposed to the domain model) I have no problem avoiding duplication in exchange for making code slightly more complicated.

For example, there are a number of operations that I want to do with these structures, such as

* "Flattening" a recursive block into a list of single values such as `ThreadingEnd`, `LiftplanPick` or `ColorIndex`
  * `ThreadingBlock -> ThreadingEnd list`
  * `LiftplanBlock -> LiftplanPick list`
  * `ColorPatternUnit -> ColorIndex list`
* Converting them into a text representation:
  * `ThreadingBlock -> string`
  * `LiftplanBlock -> string`
  * `ColorPatternUnit -> string`
* Constructing them from a string (using a parser):
  * `string -> ThreadingBlock`
  * `string -> LiftplanBlock`
  * `string -> ColorPatternUnit`

Each of these functions would have to be written separately for each domain type. That is a lot of duplication.


Here's the `flattenToSingles` implementation for `ThreadingBlock`. The other implementations for `LiftplanBlock` and `ColorPatternUnit` are very similar.

```fsharp
/// Alias a map for convenience
type LabeledThreadingGroups = Map<GroupLabel,ThreadingBlock list>

let flattenToSingles 
  (labeledGroups:LabeledThreadingGroups)
  (threadingBlock:ThreadingBlock) 
  :ThreadingEnd list =

  // define a function that will be used to recurse
  let rec recurse block =
    match block with
    | ThreadingBlock.Single threadingEnd ->
        [threadingEnd]
    | ThreadingBlock.InlineGroup blocks ->
        // flatten each subblock recursively
        blocks 
        |> List.collect recurse
    | ThreadingBlock.LabeledGroup label ->
        // if a reference, find the referred group 
        // from the `labeledGroups` parameter 			
        // and flatten that recursively
        labeledGroups 
        |> Map.tryFind label
        // if missing, return an empty list. 
        |> Option.defaultValue []
        |> List.collect recurse
    | ThreadingBlock.Repeat (block,RepeatCount repeatCount) ->
        let ends = recurse block
        List.replicate repeatCount ends |> List.collect id

  // and call it with the top level block
  recurse threadingBlock
```

With [three or more such similar implementations](https://en.wikipedia.org/wiki/Rule_of_three_(computer_programming)), it makes sense to avoid duplication.
I don't believe that refactoring to generics is a problem in this case, because it is a different context to the domain model, 

* The implementation code is not critical to understanding the domain. 
* As long as the domain model itself is understandable, the implementation can be be more complicated.
* We are unlikely to fall into the trap of what Sandi Metz calls the ["The Wrong Abstraction"](https://sandimetz.com/blog/2016/1/20/the-wrong-abstraction) (highly recommended talk btw) because if one of the domain models changes sufficiently, it will be easy to reject the generic implementation and hand-code a more custom implementation.

In fact, using a generic implementation can also have benefits:

* It can make it easier to add features (only one implementation to evolve) 
* It can reduce bugs (only one implementation to fix) 
* It can reduce testing (only one implementation to test)


![](looks_like_generics_are_back_on_the_menu.jpg)

## Implementing generic types

So how will we go about this? First, we start with the same generic `Block` structure as we defined above:

```fsharp
type Block<'single,'transform> =
  | Single of 'single
  | InlineGroup of Block<'single,'transform> list
  | LabeledGroup of GroupLabel
  | Transform of Block<'single,'transform> * 'transform
```

For later use, we'll also need the generic "labeled group" and the overall structure, which I'll call a "plan":

```fsharp
type LabeledGroup<'single,'transform> = {
  Label: GroupLabel
  Blocks: Block<'single,'transform> list
  }

type Plan<'single,'transform> = {
  Blocks: Block<'single,'transform> list
  LabeledGroups: LabeledGroup<'single,'transform> list
  }
```

And then we create mapping functions between the domain type and the generic type. These are tedious to write, but only need to be done once.
Here are the functions for `ThreadingBlock`. We will create similar ones for `LiftplanBlock` and `ColorPatternUnit`.

```fsharp
type WeaveTransform =
  | Repeat of RepeatCount

module ThreadingBlock =
  open GenericStructure

  let rec toGeneric block :Block<_,_> =
    match block with
    | ThreadingBlock.Single s ->
        GenericStructure.Block.Single s
    | ThreadingBlock.LabeledGroup g ->
        GenericStructure.Block.LabeledGroup g
    | ThreadingBlock.InlineGroup gs ->
        gs 
        |> List.map toGeneric
        |> GenericStructure.Block.InlineGroup 
    | ThreadingBlock.Repeat(b, rc) ->
        let transform = WeaveTransform.Repeat rc
        GenericStructure.Block.Transform (toGeneric b, transform)

  let rec fromGeneric block :ThreadingBlock =
    match block with
    | GenericStructure.Single s ->
        ThreadingBlock.Single s
    | GenericStructure.LabeledGroup g ->
        ThreadingBlock.LabeledGroup g
    | GenericStructure.InlineGroup gs ->
        ThreadingBlock.InlineGroup (gs |> List.map fromGeneric)
    | GenericStructure.Transform(b, t) ->
        match t with
       | WeaveTransform.Repeat rc -> 
           ThreadingBlock.Repeat (fromGeneric b, rc)
```


## Implementing 'flattenToSingles'

Next, we need to write a generic version of `flattenToSingles`. To do this, we now need an extra parameter `applyTransform` that tells how to transform a list of singles (e.g. by replication). 

```fsharp
let flattenToSingles 
  (labeledGroups:Map<GroupLabel,Block<_,_> list>)
  (applyTransform: 'transform -> 'single list -> 'single list)
  (block:Block<'single,'transform>)
  :'single list =
  
  // define a function that will be used to recurse
  let rec recurse block =
    match block with
    | Single single ->
        [single]
    | InlineGroup blocks ->
        blocks 
        |> List.collect recurse
    | LabeledGroup label ->
        // if a reference, find the referred group 
        // from the `labeledGroups` parameter 			
        // and flatten that recursively
        labeledGroups 
        |> Map.tryFind label
        // if missing, return an empty list. 
        |> Option.defaultValue []
        |> List.collect recurse
    | Transform (block,transform) ->
        block
        |> recurse 
        |> applyTransform transform 
		
  // and call it with the top level block
  recurse block
```

Notice that the type annotations for the parameters are made much more complicated by the use of generics.
Thankfully, with F#'s type inference, we do not need to specify the types, and a typeless parameter list works just as well!.

```fsharp
let flattenToSingles labeledGroups applyTransform block =
  
  // define a function that will be used to recurse
  let rec recurse block =
    ... etc ...
```

The generic implementation is not any more complicated, and in fact, by making the transform logic into a parameter, rather than hard-coding the transform inline, we would be justified in thinking that the generic implementation is simpler.

We do need to implement that `transform` function now:

```fsharp
module WeaveTransform =

  let applyTransform transform singles =
    match transform with
    | Repeat (RepeatCount rc) ->
        singles  
        |> List.replicate rc 
        |> List.collect id
```


And finally we can put it all together. We start with the non-generic domain type, convert it into the generic type, and then use the generic `flattenToSingles` function, like so:

```fsharp
module ThreadingBlock =

  let toGeneric =  ...

  let flattenToSingles labeledGroups block =
    let transform = WeaveTransform.applyTransform
    block
    |> toGeneric
    |> GenericImplementation.flattenToSingles labeledGroups transform
```

From the caller's point of view, there is no knowledge of the generic code. That is, there is no coupling, no dependency on the generic design, 
and so we are free to safely change the implementation if it ever becomes awkward.

That was a lot of work for just one function, but we can now write the other implementations for `LiftplanBlock` and `ColorPatternUnit` quickly.

Furthermore, we can reuse this exact approach when we implement the functions to make text representations of the domain types.


## Using a even more generic fold

In my [post on recursive types](https://fsharpforfunandprofit.com/posts/recursive-types-and-folds), I mentioned creating a complete generic `fold` where each case in a union has a corresponding function parameter that acts on that case. Here's how that approach would look for our generic block:

```fsharp
let rec fold
  (foldSingle: 'single -> 'r)
  (foldInlineGroup: 'r list -> 'r)
  (foldLabeledGroup: GroupLabel -> 'r)
  (foldTransform: 'r * 'transform -> 'r)
  (block: Block<'single,'transform>) =
  
  let recurse = 
    fold foldSingle foldInlineGroup foldLabeledGroup foldTransform
  match block with
  | Single single -> 
      foldSingle single
  | InlineGroup blocks ->  
      foldInlineGroup (List.map recurse blocks)
  | LabeledGroup label -> 
      foldLabeledGroup label
  | Transform (block,transform) -> 
      foldTransform (recurse block, transform)
```

With this `fold` function, we could rewrite the generic `flattenToSingles` like this:

```fsharp
let flattenToSingles labeledGroups applyTransform block =

  // use "rec" because the functions can refer to each other
  let rec fSingle single = 
    [single]
  and fInlineGroup blocks = 
    blocks |> List.collect id
  and fLabeledGroup label =
    labeledGroups
    |> Map.tryFind label
    |> Option.defaultValue List.Empty
    |> List.collect recurse
  and fTransform (singles,transform) =
    applyTransform transform singles
  and recurse = 
    fold fSingle fInlineGroup fLabeledGroup fTransform
  
  recurse block
```

Which is better? Should `flattenToSingles` do explicit pattern matching on the union cases, as in the first implementation, or should it call a generic `fold`, as in the implementation above? I personally would lean towards the explicit pattern matching version, because it's easier to understand -- anyone maintaining it does not have to know about catamorphisms!


## Converting to a text representation

I mentioned in the first post that I wanted a text representation of these domain types. The idea is that the user can enter some text, and it would get parsed into the domain types, which in turn get rendered visually as a weaving draft. At some point, there might also be a more interactive version a la [Scratch](https://en.wikipedia.org/wiki/Scratch_(programming_language)), but I wanted to start with a text-based approach because it is the easiest to prototype.

The cases in a block would represented like this

```text
1                    // single
A                    // labeled group reference
[1 2 3]              // group
1x2  Ax2  [1 2 3]x3  // transform
```

And the overall plan, with labeled groups, would be represented like this:

```text
A = 1 3 2 3    // labeled group
B = 1 4 2 4    // labeled group
C = 1 5 2 5    // labeled group
D = 1 6 2 6    // labeled group
Ax2 B Cx2 D    // overall threading
```

The parsing of this text can be complicated, and we'll save that for the next post. But converting *to* a text representation is easy.
The code is very similar to the `flattenToSingles` code. We need to pass in two function parameters:

* `singleToRepr`: how to convert a `'single` value to a text representation
* `transformToRepr`: how to convert a `'transform` to a text representation

And then we can implement the rest using the `fold` function described above.

```fsharp
module Block =

  let rec toRepr singleToRepr transformToRepr block =
    let recurse =
      toRepr singleToRepr transformToRepr
    let fSingle =
      singleToRepr
    let fInlineGroup blockTexts =
      blockTexts
      |> String.concat " "
      |> sprintf "[ %s ]"
    let fLabeledGroup label =
      GroupLabel.toRepr label
    let fTransform (blockText,transform) =
      sprintf "%s%s" blockText (transformToRepr transform)
    fold fSingle fInlineGroup fLabeledGroup fTransform block
```

For the specific case of representing a `ThreadingBlock` as text. we can write:

```fsharp
module ThreadingBlock =

  let rec toRepr block =
    let singleToRepr = ThreadingEnd.toRepr
    let transformToRepr = WeaveTransform.toRepr
    block
    |> ThreadingBlock.toGeneric
    |> GenericStructure.Block.toRepr singleToRepr transformToRepr
```

For a different type, like `LiftplanBlock`, we would just replace `ThreadingEnd` with `LiftplanPick`. 

```fsharp
module LiftplanBlock =

  let rec toRepr block =
    let singleToRepr = LiftplanPick.toRepr
    let transformToRepr = WeaveTransform.toRepr
    ...
```

And similarly with `ColorPatternUnit` etc.

We should also implement `toRepr` for the generic `LabeledGroup` and `Plan` types:

```fsharp
module LabeledGroup =

  let toRepr singleToRepr transformToRepr (group:LabeledGroup<_,_>) =
    let lhs =
      GroupLabel.toRepr group.Label
    let rhs =
      group.Blocks
      |> List.map (Block.toRepr singleToRepr transformToRepr)
      |> String.concat " "
    $"{lhs}={rhs}"

module Plan  =

  let toRepr singleToRepr transformToRepr (plan:Plan<_,_>) =
    // helper functions
    let labeledGroupToRepr = 
      LabeledGroup.toRepr singleToRepr transformToRepr
    let blockToRepr = 
      Block.toRepr singleToRepr transformToRepr

    // generate the text for each labeled group,
    // and then the final line
    seq {
      // labeled groups
      yield! plan.LabeledGroups 
        |> List.map labeledGroupToRepr
      // last line
      yield plan.Blocks 
        |> List.map blockToRepr 
        |> String.concat " "
    }
    |> String.concat "\n"
```

And then use them to implement text representations of the concrete `ThreadingGroup` and `Threading` types.


```fsharp
module ThreadingGroup =

  let toRepr group =
    let singleToRepr = ThreadingEnd.toRepr
    let transformToRepr = WeaveTransform.toRepr
    group
      |> ThreadingGroup.toGeneric
      |> GenericStructure.LabeledGroup.toRepr singleToRepr transformToRepr

module ThreadingPlan =

  let toRepr group =
    let singleToRepr = ThreadingEnd.toRepr
    let transformToRepr = WeaveTransform.toRepr
    group
    |> ThreadingPlan.toGeneric
    |> GenericStructure.Plan.toRepr singleToRepr transformToRepr
```

So, as you can see, once we have coded the generic versions of these functions, it is quick and easy to create functions for each specific type.

## Conclusion

What can we learn from this stage?

* It's ok to use generics in the implementation, even if the domain itself is not generic.
* We do need some extra effort to create the core helper functions such as `toGeneric`, `fromGeneric` and `fold`, but once this is done, most of the rest of the code can be built quickly.
* But, there are downsides. This approach (a) involves extra work and (b) makes things more complicated. It is only worth it if the benefits of reuse outweigh the downsides. In this case, I think it does.



## Next time

Next time, we'll look at going in the other direction -- parsing text into the domain types. 

