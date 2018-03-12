---
layout: post
title: "Ten reasons not to use a statically typed functional programming language"
description: "A rant against something I don't get"
nav: fsharp-types
#seriesId: ""
seriesOrder: 1
categories: []
---


Are you fed up with all the hype about functional programming?  Me too! I thought I'd rant about some reasons why sensible people like us should stay away from it.

<sub>Just to be clear, when I say "statically typed functional programming language", I mean languages that also include things such as type inference, immutability by default, and so on. In practice, this means Haskell and the ML-family (including OCaml and F#).
</sub>


## Reason 1: I don't want to follow the latest fad

Like most programmers, I'm naturally conservative and I dislike learning new things. That's why I picked a career in IT.

I don't jump on the latest bandwagon just because all the "cool kids" are doing it -- I wait until things have matured and I can get some perspective.

To me, functional programming just hasn't been around long enough to convince me that it is here to stay.

Yes, I suppose some pedants will claim that [ML (from 1973)](http://en.wikipedia.org/wiki/ML_(programming_language)) and [Haskell (from 1990)](http://en.wikipedia.org/wiki/Haskell_(programming_language)) have been around almost as long as old favorites like Java and PHP, but I only heard of Haskell recently, so that argument doesn't wash with me.

And look at the baby of the bunch, [F#](http://fsharp.org/). It's only seven years old, for Pete's sake!  Sure, that may be a long time to a geologist, but in internet time, seven years is just the blink of an eye. 

So, all told, I would definitely take the cautious approach and wait a few decades to see if this functional programming thing sticks around or whether it is just a flash in the pan.
  
## Reason 2: I get paid by the line

I don't know about you, but the more lines of code I write, the more productive I feel. If I can churn out 500 lines of code in a day, that's a job well done. 
My commits are big, and my boss can see that I've been busy.

But when I [compare code](/posts/fvsc-sum-of-squares/) written in a functional language with a good old C-like language, there's so much less code that it scares me.

I mean, just look at this code written in a familiar language:

```csharp
public static class SumOfSquaresHelper
{
   public static int Square(int i)
   {
      return i * i;
   }

   public static int SumOfSquares(int n)
   {
      int sum = 0;
      for (int i = 1; i <= n; i++)
      {
         sum += Square(i);
      }
      return sum;
   }
}
```

and compare it with this:

```fsharp
let square x = x * x
let sumOfSquares n = [1..n] |> List.map square |> List.sum
```

That's 17 lines vs. only 2 lines.  [Imagine that difference multiplied over a whole project!](http://fpbridge.co.uk/why-fsharp.html#conciseness)  

If I did use this approach, my productivity would drop drastically. I'm sorry -- I just can't afford it.

## Reason 3: I love me some curly braces

And that's another thing. What's up with all these languages that get rid of curly braces. How can they call themselves real programming languages?

I'll show you what I mean. Here's a code sample with familiar curly braces.

```csharp
public class Squarer
{
    public int Square(int input)
    {
        var result = input * input;
        return result;
    }

    public void PrintSquare(int input)
    {
        var result = this.Square(input);
        Console.WriteLine("Input={0}. Result={1}", input, result);
    }
}
```
    
And here's some similar code, but without curly braces. 

```fsharp
module Squarer =  

    let square input = 
        let result = input * input
        result

    let printSquare input = 
        let result = square input
        printfn "Input=%i. Result=%i" input result
```

Look at the difference! I don't know about you, but I find the second example a bit disturbing, as if something important is missing. 

To be honest, I feel a bit lost without the guidance that curly braces give me.  

## Reason 4: I like to see explicit types

Proponents of functional languages claim that type inference makes the code cleaner because you don't have to clutter your code with type declarations all the time.

Well, as it happens, I *like* to see type declarations. I feel uncomfortable if I don't know the exact type of every parameter. That's why [Java](http://steve-yegge.blogspot.co.uk/2006/03/execution-in-kingdom-of-nouns.html) is my favorite language.

Here's a function signature for some ML-ish code. There are no type declarations needed and all types are inferred automatically.

```fsharp
let groupBy source keySelector = 
    ... 
```

And here's the function signature for similar code in C#, with explicit type declarations.

```csharp
public IEnumerable<IGrouping<TKey, TSource>> GroupBy<TSource, TKey>(
    IEnumerable<TSource> source,
    Func<TSource, TKey> keySelector
    )
    ...
```

I may be in the minority here, but I like the second version much better. It's important to me to know that the return is of type `IEnumerable<IGrouping<TKey, TSource>>`.

Sure, the compiler will type check this for you and warn you if there is a type mismatch. But why let the compiler do the work when your brain can do it instead?

Ok, I admit that if you do use generics, and lambdas, and functions that return functions, and all the other newfangled stuff, then yes, your type declarations can get really hairy and complex. And it gets really hard to type them properly.

But I have an easy fix for that -- don't use generics and don't pass around functions. Your signatures will be much simpler.

## Reason 5: I like to fix bugs

To me, there's nothing quite like the thrill of the hunt -- finding and killing a nasty bug. And if the bug is in a production system, even better, because I'll be a hero as well.

But [I've read](https://web.archive.org/web/20130918053426/http://www.simontylercousins.net/journal/2013/3/7/why-bugs-dont-like-f.html) that in statically typed functional languages, it is much harder to introduce bugs.

That's a bummer. 

## Reason 6: I live in the debugger
 
And talking of bug fixing, I spend most of my day in the debugger, stepping through code. Yes, I know I should be using unit tests, but easier said than done, OK?

Anyway, apparently with these statically typed functional languages, [if your code compiles, it usually works](http://www.haskell.org/haskellwiki/Why_Haskell_just_works).

I'm told that you do have to spend a lot of time up front getting the types to match up, but once that is done and it compiles successfully, there is nothing to debug. Where's the fun in that?

Which brings me to...

## Reason 7: I don't want to think about every little detail

All this matching up types and making sure everything is perfect sounds tiring to me. 

In fact, I hear that you are forced to think about all the possible edge cases, and all the possible error conditions, and every other thing that could go wrong.
And you have to do this at the beginning -- you can't be lazy and postpone it till later.

I'd much rather get everything (mostly) working for the happy path, and then fix bugs as they come up. 

## Reason 8: I like to check for nulls

I'm very conscientious about [checking for nulls](http://stackoverflow.com/questions/7585493/null-parameter-checking-in-c-sharp) on every method. It gives me great satisfaction to know that my code is completely bulletproof as a result.

```csharp
void someMethod(SomeClass x)
{
    if (x == null) { throw new NullArgumentException(); }

    x.doSomething();
}
```

Haha! Just kidding! Of course I can't be bothered to put null-checking code everywhere. I'd never get any real work done.  

I've never had any serious problems caused by a null. Well, OK, one. But the business didn't lose too much money during the outage. And I know that most of the staff appreciated the unexpected day off. So I'm not sure why this is such a [big deal](http://www.infoq.com/presentations/Null-References-The-Billion-Dollar-Mistake-Tony-Hoare). 

## Reason 9: I like to use design patterns everywhere

I first read about design patterns in the [Design Patterns book](http://www.amazon.com/First-Design-Patterns-Elisabeth-Freeman/dp/0596007124) (for some reason it's referred to as the Gang of Four book, but I'm not sure why, since it has a girl on the front), and since then I have been diligent in using them at all times for all problems. It certainly makes my code look serious and "enterprise-y", and it impresses my boss.

But I don't see any mention of patterns in functional design. How can you get useful stuff done without Strategy, AbstractFactory, Decorator, Proxy, and so on? 

Perhaps the functional programmers are not aware of them?

## Reason 10: It's too mathematical

Here's some more code for calculating the sum of squares. This is *way* too hard to understand because of all the weird symbols in it.

```text
ss=: +/ @: *:
```

Oops, sorry! My mistake. That was [J code](http://en.wikipedia.org/wiki/J_(programming_language)).

But I do hear that functional programs use strange symbols like `<*>` and `>>=` and obscure concepts called "monads" and "functors". 

I don't know why the functional people couldn't stick with things I already know -- obvious symbols like `++` and `!=` and easy concepts such as "inheritance" and "polymorphism".

## Summary: I don't get it

You know what. I don't get it. I don't get why functional programming is useful. 

What I'd really like is for someone to just show me some [real benefits on a single page](/why-use-fsharp/), instead of giving me too much information.

UPDATE: So now I've read the "everything you need to know on one page" page. But it's too short and simplistic for me.

I'm really looking for something with a bit more depth -- [something](/posts/designing-for-correctness/) I can [get](/series/designing-with-types.html) my teeth [into](/posts/computation-expressions-intro/).  

And no, don't say that I should read [tutorials](http://learnyouahaskell.com/), and [play with examples](http://www.tryfsharp.org/Learn), and write my own code. I just want to grok it without doing all of that work.  

I don't want to have to [change the way I think](https://web.archive.org/web/20140118170751/http://dave.fayr.am/posts/2011-08-19-lets-go-shopping.html) just to learn a new paradigm.





 
