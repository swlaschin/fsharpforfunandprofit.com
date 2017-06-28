---
layout: post
title: "Why I won't be writing a monad tutorial"
description: ""
categories: []
---

*"A 'newbie', in Haskell, is someone who hasn't yet implemented a compiler. They've only written a monad tutorial" - [Pseudonymn](http://sequence.complete.org/node?page=10)*

Let's start with a story...

### Alice learns to count

*Young Alice and her father (who happens to be a mathematician) are visiting a petting zoo...*

Alice: Look at those kitties.

![two kitties](/assets/img/two_kitties.jpg)

Daddy: Aren't they cute. There are *two* of them.

Alice: Look at those doggies.

![two kitties](/assets/img/two_puppies.jpg)

Daddy: That's right. Can you count? There are *two* doggies.

Alice: Look at those horsies.

![two kitties](/assets/img/two_horses.jpg)

Daddy: Yes darling. Do you know what the kitties and doggies and horsies all have in common?

Alice: No. Nothing in common! 

Daddy: Well, actually they *do* have something in common. Can you see what it is?

Alice: No! A doggy is not a kitty.  A horsie is not a kitty.  

Daddy: How about I explain for you?  First, let us consider [a set S which is strictly well-ordered with respect to set membership and where every element of S is also a subset of S](http://en.wikipedia.org/wiki/Ordinal_number#Von_Neumann_definition_of_ordinals). Does that give you a clue?

Alice: [Bursts into tears] 

### How not to win friends and influence people

No (sensible) parent would ever try to explain how to count by starting with a formal definition of ordinal numbers.

So why is it that many people feel compelled to explain a concept like monads by emphasizing their formal definition?

That might be fine for a college-level math class, but it plainly does not work for regular programmers, who just want to create something useful. 

As an unfortunate result of this approach, though, there is now a whole mystique around the concept of monads. It has become [a bridge you must cross](http://www.thefreedictionary.com/pons+asinorum) on the way to true enlightenment. And there are, of course, a [plethora of monad tutorials](http://www.haskell.org/haskellwiki/Monad_tutorials_timeline) to help you cross it.

Here's the truth: You *don't* need to understand monads to write useful functional code. This is especially true for F# compared to say, Haskell.

Monads are not a [golden hammer](http://en.wikipedia.org/wiki/Law_of_the_instrument). They won't make you any more productive. They won't make your code less buggy.

So really, don't worry about them. 

### Why I won't be writing a monad tutorial

So this is why I won't be writing a monad tutorial. I don't think it will help people learn about functional programming. If anything, it just creates confusion and anxiety.

Yes, I will use examples of monads in [many](/posts/recipe-part2/) different [posts](/posts/computation-expressions-wrapper-types/),
but, other than right here, I will try to avoid using the word "monad" anywhere on this site. In fact, it has pride of place on my [list of banned words](/about/#banned)!


### Why you should write a monad tutorial

On the other hand, I do think that *you* should write a monad tutorial.  When you try to explain something to somebody else, you end up understanding it better yourself. 

Here's the process I think you should follow:

1. First, write lots of practical code involving lists, sequences, options, async workflows, computation expressions, etc. 
1. As you become more experienced, you will start to use more abstractions, focusing on the shapes of things rather than the details.
1. At some point, you will have an aha! moment -- a sudden insight that all the abstractions have something in common. 
1. Bingo! Time to write your monad tutorial!

The key point is that [*you have to do it in this order*](http://byorgey.wordpress.com/2009/01/12/abstraction-intuition-and-the-monad-tutorial-fallacy/) -- you cannot jump straight to the last step and then work backwards. It is the very act of working your way through the details that enables you to understand the abstraction when you see it. 

Good luck with your tutorial -- I'm off to eat a burrito.









