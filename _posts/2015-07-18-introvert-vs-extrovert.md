---
layout: post
title: "Introvert and extrovert programming languages"
description: "Late night thoughts on language personality types"
categories: []
---

> What's the difference between an introvert and extrovert mathematician?
> <br>
> An introvert mathematician looks at his shoes while talking to you; an extrovert mathematician looks at *your* shoes.  

For a long time, I've been aware of [differences](https://plus.google.com/110981030061712822816/posts/KaSKeg4vQtz) in how programming languages present themselves to the outside world.

If programming languages had personalities, I would be tempted to call some of them "introvert", and some of them "extrovert".

An extrovert programming language is all about the outside world, never happier than when partying with IO and external data sources.

On the other hand, an introvert programming language is quite happy to be alone and would prefer not to deal with the outside world, if possible.
Sure, it can be social and work with IO when it needs to, but it finds the activity quite tiring, and is relieved when IO has gone home and it can go back to reading a good book.

What's interesting is that you can tell a lot about the personality type of a language by looking at what is considered important in a user guide or tutorial.

For example, the classic "C Programming Language" book [famously has](https://books.google.co.uk/books?id=va1QAAAAMAAJ&focus=searchwithinvolume&q=hello%2C+world)
`printf("hello, world\n")` at the very beginning, and most other C books [follow the same pattern](https://en.wikibooks.org/wiki/C_Programming/A_taste_of_C).

And indeed, C *is* a very extrovert language. Coding examples are littered with files and console IO.
Similarly, you can tell that PHP, Python and Perl are equally extrovert with just one glance at their manuals.

In fact, I would say that *all* of the most popular languages are extrovert, and the reasons are obvious.
They ooze confidence, they make friends easily, and they get things done.

On the other hand, I would say that Haskell is a great example of an introvert language.

For example, in the book "Learn You A Haskell" the "hello world" example doesn't appear until [chapter 9](http://learnyouahaskell.com/input-and-output#hello-world)!
And in "Real World Haskell", IO is not invited to dinner until [chapter 7](http://book.realworldhaskell.org/read/io.html).

If you don't have the full manual handy, another telling clue that a language is introverted
is if it early on introduces you to its close friend, the Fibonacci function. Introvert languages love recursion!

Now, just as in the real world, introvert languages are misunderstood by extroverts. They are accused of being too arrogant, too serious, too risk-averse.

But that's not fair -- introvert languages are really just more reflective and thoughtful, and thus more likely to have deep insights than the shallow, yapping extroverts.


### But...

> "All generalisations are false including this one"

You might think that imperative and OO languages would be extrovert, while languages with more declarative paradigms (functional, logic) would be introvert,
but that is not always the case.

For example, SQL is a declarative language, but its whole purpose in life is data-processing, which makes it extrovert in my book.

And nearer to home, F# is a functional-first language, but is very happy to do IO, and in fact
has excellent support for real-world data processing via [type providers](http://blogs.msdn.com/b/dsyme/archive/2013/01/30/twelve-type-providers-in-pictures.aspx) and
[Deedle](https://bluemountaincapital.github.io/Deedle/).

Just as people are not all one or the other, so with programming languages.
There is a range. Some languages are extremely extrovert, some extremely introvert, and some in-between.

### A lot of people say I am egocentric -- but enough about them

> "Reality is merely an illusion, albeit a very persistent one."

Some languages do not fall so neatly into this personality type spectrum.

Take Smalltalk for example. 

In many ways, Smalltalk is *extremely* extrovert. It has lots of support for user interface interaction, and itself was one of the first
[graphical development environments](http://arstechnica.com/features/2005/05/gui/3/). 

But there's a problem. Sure, it's friendly and chatty and great at intense one-to-one conversations, but it has a dark side -- it doesn't play well with others.
It only reluctantly acknowledges the operating system, and rather than dealing with messiness of external libraries, prefers to implement things in its own perfect way.

Most Lisps have the same failing as well. Idealistic and elegant, but rather isolated.
I'm going to call this personality type [solipsistic](http://www.merriam-webster.com/dictionary/solipsism).

In their defence, many important programming concepts were first developed in solipsistic languages. But alas, despite their ardent followers,
they never gain the widespread recognition they deserve. 

### Where Do We Come From? What Are We? Where Are We Going?

And that brings me, inevitably, to the all-devouring black hole that is JavaScript. Where does that fit in this silly scheme?

Obviously, its original purpose was to to aid communication with the user (and also, for animating monkeys), so at first glance it would seem to be extrovert.

But the fact that it runs in a sandbox (and until somewhat recently didn't do many kinds of IO) makes me think otherwise.
The clincher for me is node.js. Need a server language? Let's create one in our own image! Let's write all the libraries from scratch! 

But it seems to have worked, for now at least. So, solipsism for the win!

### Concluding Unscientific Postscript

> "Nature has made us frivolous to console us for our miseries"

At this point, I should wrap up with some profound observations about different programming communities and what their language preference reveals about them.

But really, this is a just a bit of whimsy, so I won't.





