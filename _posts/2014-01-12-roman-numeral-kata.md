---
layout: post
title: "Commentary on 'Roman Numerals Kata with Commentary'"
description: "My approach to the Roman Numerals Kata"
categories: ["Worked Examples"]
---

I recently watched a video called ["Roman Numerals Kata with Commentary"](http://blog.coreyhaines.com/2012/12/roman-numerals-kata-with-commentary.html). 
In it, Corey Haines demonstrates how to implement the [Arabic to Roman Numerals Kata](http://codingdojo.org/kata/RomanNumerals/) in Ruby using a TDD approach.  

*This video annoyed me intensely.*

I mean no disrespect to Corey Haines' programming skills, and many people seem to have found the video useful, but I just found it exasperating.

I thought that in this post I'd try to explain why I got annoyed, and to present my alternative approach to problems like this. 

## Where are the requirements?

> *"Few programmers write even a rough sketch of what their programs will do before they start coding. Most programmers regard anything that doesn't generate code to be a waste of time."*
>
> *Leslie Lamport, ["Why We Should Build Software Like We Build Houses"](http://www.wired.com/opinion/2013/01/code-bugs-programming-why-we-need-specs/)*

In standard TDD fashion, the video starts with implementing a initial failing case (handling zero), then making that work, then adding a test case for handling "1", then making that work, and so on.

This was the first thing that irritated me -- diving into code without really understanding the requirements. 

A [programming kata](http://en.wikipedia.org/wiki/Kata_(programming)) is so called because the goal is to practice your skills as a developer. 
But for me, coding skills are just one aspect of a being a software developer, and not always the most important. 

If there is anything that needs practicing by most developers, it is listening to and understanding the needs of the customer (a.k.a. requirements).
We should never forget that our goal is to deliver value, not just to write code.

In this case, even though there is a [page](http://codingdojo.org/kata/RomanNumerals/) for the kata, the requirements are still somewhat fuzzy,
and so I view this as an excellent opportunity to drill down into them, and maybe learn something new.

## Becoming a domain expert

In fact, I believe that going as deep as possible into the requirements has some important benefits. 

**It's fun**. It's fun to really understand a new domain. I like to learn new things -- it is one of the perks of being a developer. 


It's not just me. Dan North tells of how much fun he had working very closely with domain experts in his ["accelerating agile"](http://vimeo.com/68215534) presentation. 
Part of that team's success was that the developers studied the domain (trading) right along with the traders themselves, so that communication was easy and confusion minimized.

**Good design**. I do believe that in order to produce good software you have to become reasonably expert in the domain you are attempting to model. 
This is the thesis behind Domain Driven Design, of course, but also is a key component of an Agile process: the "on site customer" who works very closely with the developers at all stages.

And almost always, understanding the requirements properly will lead you into the right* way to implement a solution.
No amount of shallow iterations will make up for a lack of deep insight.

<sub>* Of course there is not really a "right" way, but there are plenty of wrong ways. So here I just mean not horribly complicated and unmaintainable.<sub>

**Good tests**. You can't create good tests without understanding the requirements. A process like BDD makes this explicit;
the requirements are written in such a way that they actually *become* the tests.


## Understanding Roman numerals 

> *"During an inception, when we are most ignorant about most aspects of the project, the best use we can possibly make of the time available is to attempt to identify and reduce our ignorance across all the axes we can think of."*
> -- *Dan North, ["Deliberate Discovery"](http://dannorth.net/2010/08/30/introducing-deliberate-discovery/)*

So, how does this apply to the Roman Numerals Kata? Should we seriously become domain experts before we write a line of code?

I would say yes!

I know it is a trivial problem, and it seems like overkill, but then again, this is a kata, so you should be practising all the steps carefully and mindfully.

So, what can we find out about Roman numerals?

First, a little [background reading from a reliable source](http://en.wikipedia.org/wiki/Roman_numerals) shows that they probably originated from something similar to [tally marks](http://en.wikipedia.org/wiki/Tally_marks).

![Tally marks](/assets/img/200px-Tally_marks.svg.png)

This explains the simple strokes for "I" to "IIII" and then the different symbol for "V".

As it evolved, symbols were added for ten and fifty, one hundred and five hundred, and so on.  
This system of counting with ones and fives can be seen in the design of the [abacus](http://en.wikipedia.org/wiki/Roman_abacus), old and new.

![Roman Abacus](/assets/img/RomanAbacusRecon.jpg)
![Modern abacus](/assets/img/320px-Sharp-abacus-japan.jpg)

In fact, this system even has a name which I'd never heard of -- ["bi-quinary coded decimal"](http://en.wikipedia.org/wiki/Bi-quinary_coded_decimal). 
Isn't that fun? I shall now attempt to drop that phrase into casual conversation wherever possible.
(And by the way, the little stones used as counters are called "calculi", whence the name for the bane of high school students everywhere.)

Much later, in the 13th century, certain abbreviations were added -- substituting "IV" for "IIII" and "IX" for "VIIII". This [subtractive notation](http://en.wikipedia.org/wiki/Subtractive_notation)
means that the order of the symbols becomes important, something that is not required for a pure tally-based system.

These new requirements show us that nothing has changed in the development biz...

*Pope: "We need to add subtractive notation ASAP -- the Arabs are beating us on features."<br>
You: "But it's not backwards compatible, sir. It's a breaking change!" <br>
Pope: "Tough. I need it by next week."*

So now that we know all about Roman numerals, do we have enough information to create the requirements?

Alas, no. As we investigate further, it becomes clear that there is a lot of inconsistency. There is no ISO or ANSI standard for Roman numerals! 

This is not unusual of course. A fuzziness around requirements affects most software projects.
Indeed, part of our job as a developer is to help clarify things and eliminate ambiguity. So, let's create some requirements based on what we know so far.

## The requirements for this kata

> *"Programmers are not to be measured by their ingenuity and their logic but by the completeness of their case analysis."*
> -- *Alan Perlis, [Epigrams](http://cpsc.yale.edu/epigrams-programming)*

I think we would all agree that having unambiguous and testable requirements is a critical step towards having a successful project. 

Now when I talk about "requirements", I'm not talking about a 200 page document that takes six months to write.
I'm just talking about a few bullet points that take 5 or 10 minutes to write down.

But... it is important to have them. Thinking carefully before coding is an essential skill that needs to be practiced,
and so I would recommend doing this step as part of the discipline for any code kata. 

So here are the requirements as I see them:

* The output will be generated by tallying 1, 5, 10, 50, 100, 500, and 1000, using the symbols "I", "V", "X", "L", "C", "D" and "M" respectively. 
* The symbols must be written in descending order: "M" before "D" before "C" before "L", etc.
* Using the tallying logic, it's clear that we can only have up to four repetitions of "I", "X", "C" and "M". And only one "V", "L" or "D".
  Any more than that and the multiple tally marks are abbreviated to the next "higher" tally mark.
* Finally, we have the six (optional) substitution rules: "IIII"=>"IV", "VIIII"=>"IX", "XXXX"=>"XL", "LXXXX"=>"XC", "CCCC"=>"CD", "DCCCC"=>"CM". These are exceptions to the descending order rule.

There's one other very important requirement that isn't on this list. 

* What is the range of valid inputs?

If we don't explicitly document this, we could easily assume that all integers are valid, including zero and negative numbers. 

And what about large numbers, in the millions or billions? Are they allowed? Probably not.

So let's be explicit and say that the valid input ranges from 0 to 4000.  Then what should happen if the input is not valid? Return an empty string? Throw an exception?

In a functional programming language like F#, the most common approach is to return an `Option` type, or to return a Success/Failure `Choice` type.
Let's just use an `Option`, so to finish off the requirements, we have:

* The Arabic number 0 is mapped to the empty string.
* If the input is < 0 or > 4000 return `None` otherwise return `Some(roman)`, where `roman` is the Arabic number converted to Roman numerals as described above.

So to sum up this step, we have read about Roman numerals, learned a few fun things, and come up with some clear requirements for the next stage.
The whole thing took only 5-10 mins. In my opinion, that was time well spent.

## Writing the tests 

> *"Unit tests have been compared with shining a flashlight into a dark room in search of a monster. 
> Shine the light into the room and then into all the scary corners. 
> It doesn't mean the room is monster free --- just that the monster isn't standing where you've shined your flashlight."*

Now that we have the requirements, we can start writing the tests.

In the original video, the tests were developed incrementally, starting with 0, then 1, and so on.

Personally, I think that there are a number of problems with that approach.

First, as we should know, a major goal of TDD is not testing but *design*. 

But this micro, incremental approach to design does not seem to me to lead to a particularly good end result.

For example, in the video, there is a big jump in the implementation complexity from testing the "I" case to testing the "II" case. But the rationale is a bit hard to understand,
and to me it smacks a little of sleight-of-hand, of someone who already knows the answer, rather than naturally evolving from the previous case.

Unfortunately, I have seen this happen a lot with a strict TDD approach.
You might be cruising along nicely and then bump into a huge road block which forces a huge rethink and refactoring. 

A strict TDD'er using Uncle Bob's ["Transformation Priority Premise"](http://blog.8thlight.com/uncle-bob/2013/05/27/TheTransformationPriorityPremise.html) approach would say that that is fine and good, and part of the process.

Personally, I'd rather start with the trickiest requirements first, and front-load the risk rather than leaving it till the end.

Second, I don't like testing individual cases. I'd prefer that my tests cover *all* inputs. This is not always feasible, but when you can do it, as in this case, I think you should.

### Two tests compared

To demonstrate what I mean, let's compare the test suite developed in the video with a more general requirements-based test.

The test suite developed in the video checks only the obvious inputs, plus the case 3497 "for good measure". Here's the Ruby code ported to F#:

```fsharp
[<Test>]
let ``For certain inputs, expect certain outputs``() = 
    let testpairs = [ 
      (1,"I")
      (2,"II")
      (4,"IV")
      (5,"V")
      (9,"IX")
      (10,"X")
      // etc
      (900,"CM")
      (1000,"M")
      (3497,"MMMCDXCVII")
      ]
    for (arabic,expectedRoman) in testpairs do
       let roman = arabicToRoman arabic
       Assert.AreEqual(expectedRoman, roman)
```

With this set of inputs, how confident are we that the code meets the requirements?  

In a simple case like this, I might be reasonably confident,
but this approach to testing worries me because of the use of "magic" test inputs that are undocumented.

For example, why was 3497 plucked out of nowhere? Because (a) it is bigger than a thousand and (b) it has some 4's and 9's in it.
But the reason it was picked is not documented in the test code.

Furthermore, if we compare this test suite with the requirements, we can see that the second and third requirements are not explicitly tested for at all.
True, the test with 3497 implicitly checks the ordering requirement ("M" before "C" before "X"), but that is never made explicit.

Now compare that test with this one:

```fsharp
[<Test>]
let ``For all valid inputs, there must be a max of four "I"s in a row``() = 
    for i in [1..4000] do
       let roman = arabicToRoman i
       roman |> assertMaxRepetition "I" 4
```

This test checks the requirement that you can only have four repetitions of "I".  

Unlike the one in the TDD video, this test case covers *all possible inputs*, not just one.
If it passes, I will have complete confidence that the code meets this particular requirement.

### Property-based testing

If you are not familiar with this approach to testing, it is called *"property-based testing"*. You define a "property" that must be true in general, and then you generate as many inputs
as possible in order to find cases where the property is not true. 

In this case, we can test all 4000 inputs. In general though, our problems have a much larger range of possible inputs,
so we generally just test on some representative sample of the inputs.

Most property-based testing tools are modelled after [Haskell's QuickCheck](http://en.wikipedia.org/wiki/QuickCheck),
which is a tool that automatically generates "interesting" inputs for you, in order to find edge cases as quickly as possible.
These inputs would include things like nulls, negative numbers, empty lists, strings with non-ascii characters in them, and so on.

An equivalent to QuickCheck is available for most languages now, including [FsCheck](https://fscheck.github.io/FsCheck/) for F#.

The advantage of property-based testing is that it forces you to think about the requirements in general terms, rather than as lots of special cases.

That is, rather than a test that says `the input "4" maps to "IV"`, we have a more general test that says `any input with 4 in the units place has "IV" as the last two characters`.

### Implementing a property-based test

To switch to property-based testing for the requirement above, I would refactor the code so that
(a) I create a function that defines a property and then (b) I check that property against a range of inputs.

Here's the refactored code:

```fsharp
// Define a property that should be true for all inputs
let ``has max rep of four Is`` arabic = 
   let roman = arabicToRoman arabic
   roman |> assertMaxRepetition "I" 4

// Explicitly enumerate all inputs...
[<Test>]
let ``For all valid inputs, there must be a max of four "I"s``() = 
    for i in [1..4000] do
       //check that the property holds
       ``has max rep of four Is`` i

// ...Or use FsCheck to generate inputs for you
let isInRange i = (i >= 1) && (i <= 4000)
// input is in range implies has max of four Is
let prop i = isInRange i ==> ``has max rep of four Is`` i
// check all inputs for this property
Check.Quick prop
       
```


Or for example, let's say that I want to test the substitution rule for 40 => "XL".

```fsharp
// Define a property that should be true for all inputs
let ``if arabic has 4 tens then roman has one XL otherwise none`` arabic = 
   let roman = arabicToRoman arabic
   let has4Tens = (arabic % 100 / 10) = 4 
   if has4Tens then
       assertMaxOccurs "XL" 1 roman
   else 
       assertMaxOccurs "XL" 0 roman

// Explicitly enumerate all inputs...
[<Test>]
let ``For all valid inputs, check the XL substitution``() = 
    for i in [1..4000] do
       ``if arabic has 4 tens then roman has one XL otherwise none`` i

// ...Or again use FsCheck to generate inputs for you
let isInRange i = (i >= 1) && (i <= 4000)
let prop i = isInRange i ==> ``if arabic has 4 tens then roman has one XL otherwise none`` i
Check.Quick prop
```

I'm not going to go into property-based testing any more here, but I think you can see the benefits over hand-crafted cases with magic inputs.

*The [code for this post](https://gist.github.com/swlaschin/8409306) has a full property-based test suite.*

## Requirements Driven Design™

At this point, we can start on the implementation.

Unlike the TDD video, I'd rather build the implementation by iterating on the *requirements*, not on the *test cases*.
I need a catchy phrase for this, so I'll call it Requirements Driven Design™. Watch out for a Requirements Driven Design Manifesto coming soon.

And rather than implementing code that handles individual inputs one by one, I prefer my implementations to cover as many input cases as possible -- preferably all of them.
As each new requirement is added the implementation is modified or refined, using the tests to ensure that it still meets the requirements.

But isn't this exactly TDD as demonstrated in the video? 

No, I don't think so. The TDD demonstration was *test-driven*, but not *requirements driven*. Mapping 1 to "I" and 2 to "II" are tests, but are not true requirements in my view.
A good requirement is based on insight into the domain. Just testing that 2 maps to "II" does not provide that insight.

### A very simple implementation

After criticizing someone else's implementation, time for me to put up or shut up.

So, what is the simplest implementation I can think of that would work?

How about just converting our arabic number to tally marks?  1 becomes "I", 2 becomes "II", and so on.

```fsharp
let arabicToRoman arabic = 
   String.replicate arabic "I"
```

Here it is in action:

```fsharp
arabicToRoman 1    // "I"
arabicToRoman 5    // "IIIII"
arabicToRoman 10   // "IIIIIIIIII" 
```

This code actually meets the first and second requirements already, and for all inputs!

Of course, having 4000 tally marks is not very helpful, which is no doubt why the Romans started abbreviating them.  

This is where insight into the domain comes in. If we understand that the tally marks are being abbreviated, we can emulate that in our code.

So let's convert all runs of five tally marks into a "V".
 
```fsharp
let arabicToRoman arabic = 
   (String.replicate arabic "I")
    .Replace("IIIII","V")

// test
arabicToRoman 1    // "I"
arabicToRoman 5    // "V"
arabicToRoman 6    // "VI"
arabicToRoman 10   // "VV" 
```

But now we can have runs of "V"s. Two "V"s need to be collapsed into an "X".

```fsharp
let arabicToRoman arabic = 
   (String.replicate arabic "I")
    .Replace("IIIII","V")
    .Replace("VV","X")

// test
arabicToRoman 1    // "I"
arabicToRoman 5    // "V"
arabicToRoman 6    // "VI"
arabicToRoman 10   // "X" 
arabicToRoman 12   // "XII" 
arabicToRoman 16   // "XVI" 
```

I think you get the idea. We can go on adding abbreviations... 

```fsharp
let arabicToRoman arabic = 
   (String.replicate arabic "I")
    .Replace("IIIII","V")
    .Replace("VV","X")
    .Replace("XXXXX","L")
    .Replace("LL","C")
    .Replace("CCCCC","D")
    .Replace("DD","M")

// test
arabicToRoman 1    // "I"
arabicToRoman 5    // "V"
arabicToRoman 6    // "VI"
arabicToRoman 10   // "X" 
arabicToRoman 12   // "XII" 
arabicToRoman 16   // "XVI" 
arabicToRoman 3497 // "MMMCCCCLXXXXVII" 
```

And now we're done. We've met the first three requirements.

If we want to add the optional abbreviations for the fours and nines, we can do that at the end, after all the tally marks have been accumulated.

```fsharp
let arabicToRoman arabic = 
   (String.replicate arabic "I")
    .Replace("IIIII","V")
    .Replace("VV","X")
    .Replace("XXXXX","L")
    .Replace("LL","C")
    .Replace("CCCCC","D")
    .Replace("DD","M")
    // optional substitutions
    .Replace("IIII","IV")
    .Replace("VIV","IX")
    .Replace("XXXX","XL")
    .Replace("LXL","XC")
    .Replace("CCCC","CD")
    .Replace("DCD","CM")
    

// test
arabicToRoman 1    // "I"
arabicToRoman 4    // "IV"
arabicToRoman 5    // "V"
arabicToRoman 6    // "VI"
arabicToRoman 10   // "X" 
arabicToRoman 12   // "XII" 
arabicToRoman 16   // "XVI" 
arabicToRoman 40   // "XL" 
arabicToRoman 946  // "CMXLVI" 
arabicToRoman 3497 // "MMMCDXCVII"
```

Here is what I like about this approach:

* It is derived from understanding the domain model (tally marks) rather than jumping right into a recursive design.
* As a result, the implementation follows the requirements very closely. In fact it basically writes itself.
* By following this step-by-step approach, someone else would have high confidence in the code being correct just by examining the code.
  There is no recursion or special tricks that would confuse anyone.
* The implementation generates output for all inputs at all times. In the intermediate stages, when it doesn't meet all the requirements,
  it at least generates output (e.g. 10 mapped to "VV") that tells us what we need to do next.

Yes, this might not be the most efficient code, creating strings with 4000 "I"s in them! And of course, a more efficient approach would
subtract the large tallies ("M", then "D", then "C") straight from the input, leading to the recursive solution demonstrated in the TDD video.

But on the other hand, this implementation might well be efficient enough.
The requirements don't say anything about performance constraints -- YAGNI anyone? -- so I'm tempted to leave it at this.

### A bi-quinary coded decimal implementation

I can't resist another implementation, just so that I can use the word "bi-quinary" again.

The implementation will again be based on our understanding of the domain, in this case, the Roman abacus.

In the abacus, each row or wire represents a decimal place, just as our common Arabic notation does.
But the number in that place can be encoded by two different symbols, depending on the number.

Some examples:

* 1 in the tens place is encoded by "X"
* 2 in the tens place is encoded by "XX"
* 5 in the tens place is encoded by "L"
* 6 in the tens place is encoded by "LX"

and so on.

This leads directly to an algorithm based on converting the beads on the abacus into a string representation.

* Split the input number into units, tens, hundreds and thousands. These represent each row or wire on the abacus.
* Encode the digit for each place into a string using the "bi-quinary" representation and the appropriate symbols for that place.
* Concat the representations for each place together to make single output string.

Here's an implementation that is a direct translation of that algorithm:

```fsharp
let biQuinaryDigits place (unit,five) arabic =
    let digit =  arabic % (10*place) / place
    match digit with
    | 0 -> ""
    | 1 -> unit
    | 2 -> unit + unit
    | 3 -> unit + unit + unit
    | 4 -> unit + unit + unit + unit
    | 5 -> five
    | 6 -> five + unit
    | 7 -> five + unit + unit
    | 8 -> five + unit + unit + unit
    | 9 -> five + unit + unit + unit + unit
    | _ -> failwith "Expected 0-9 only"

let arabicToRoman arabic = 
    let units = biQuinaryDigits 1 ("I","V") arabic
    let tens = biQuinaryDigits 10 ("X","L") arabic
    let hundreds = biQuinaryDigits 100 ("C","D") arabic
    let thousands = biQuinaryDigits 1000 ("M","?") arabic
    thousands + hundreds + tens + units

```

Note that the above code does not produce the abbreviations for the four and nine cases.
We can easily modify it to do this though. We just need to pass in the symbol for ten, and tweak the mapping for the 4 and 9 case, as follows:

```fsharp
let biQuinaryDigits place (unit,five,ten) arabic =
  let digit =  arabic % (10*place) / place
  match digit with
  | 0 -> ""
  | 1 -> unit
  | 2 -> unit + unit
  | 3 -> unit + unit + unit
  | 4 -> unit + five // changed to be one less than five 
  | 5 -> five
  | 6 -> five + unit
  | 7 -> five + unit + unit
  | 8 -> five + unit + unit + unit
  | 9 -> unit + ten  // changed to be one less than ten
  | _ -> failwith "Expected 0-9 only"

let arabicToRoman arabic = 
  let units = biQuinaryDigits 1 ("I","V","X") arabic
  let tens = biQuinaryDigits 10 ("X","L","C") arabic
  let hundreds = biQuinaryDigits 100 ("C","D","M") arabic
  let thousands = biQuinaryDigits 1000 ("M","?","?") arabic
  thousands + hundreds + tens + units
```

Again, both these implementations are very straightforward and easy to verify. There are no subtle edge cases lurking in the code.

## Review

I started off this post being annoyed at a TDD demonstration. Let me review the reasons why I was annoyed, and how my approach differs.

**Requirements**

The TDD demonstration video did not make any attempt to document the requirements at all.
I would say that this a dangerous thing to do, especially if you are learning.

I would prefer that before you start coding you *always* make an effort to be explicit about what you are trying to do.

With only a tiny bit of effort I came up with some explicit requirements that I could use for verification later.

I also explicitly documented the range of valid inputs -- something that was unfortunately lacking in the TDD demonstration.

**Understanding the domain**

Even if the requirements have been made explicit for you, I think that it is always worthwhile spending time to *really* understand the domain you are working in. 

In this case, understanding that Roman numerals were a tally-based system helped with the design later. (Plus I learned what "bi-quinary" means and got to use it in this post!)

**Unit tests**

The unit tests in the TDD demonstration were built one single case at a time. First zero, then one, and so on. 

As I note above, I feel very uncomfortable with this approach because (a) I don't think it leads to a good design and (b) the single cases don't cover all possible inputs.

I would strongly recommend that you write tests that map *directly* to the requirements. If the requirements are any good, this will mean that the tests cover many inputs at once,
so you can then test as many inputs as you can.

Ideally, you would use a property-based testing tool like QuickCheck. Not only does it make this approach much easier to implement, but it forces you to identify what
the properties of your design should be, which in turn helps you clarify any fuzzy requirements.

**Implementation**

Finally, I described two implementations, both completely different from the recursive one demonstrated in the TDD video.

Both designs were derived directly from an understanding of the domain. The first from using tally marks, and the second from using an abacus.

To my mind, both of these designs were also easier to understand -- no recursion! -- and thus easier to have confidence in.

## Summary

*(Added based on comments I made below.)*

Let me be clear that I have absolutely no problem with TDD. And I don't have a problem with katas either.

But here's my concern about these kinds of "dive-in" demos, namely that novices and learners might unintentionally learn the following (implicit) lessons:

* It is OK to accept requirements as given without asking questions.
* It is OK to work without a clear idea of the goal.
* It is OK to start coding immediately.
* It is OK to create tests that are extremely specific (e.g. with magic numbers).
* It is OK to consider only the happy path.
* It is OK to do micro refactoring without looking at the bigger picture.

Personally, I think that if you are *practicing* to be a *professional* developer, you should:

* Practice asking for as much information as possible before you start coding.
* Practice writing requirements (from unclear input) in such a way that they can be tested.
* Practice thinking (analyzing and designing) rather than immediately coding.
* Practice creating general tests rather than specific ones.
* Practice thinking about and handling bad inputs, corner cases, and errors.
* Practice major refactoring (rather than micro refactoring) so as to develop an intuition about where the [shearing layers](http://jonjagger.blogspot.co.uk/2009/10/how-buildings-learn-chapter-2-shearing.html) should be.

These principles are all completely compatible with TDD (or at least [the "London" school of TDD](http://codemanship.co.uk/parlezuml/blog/?postid=987)) and programming katas. There is no conflict, and I cannot see why they would be controversial.

## What do you think?

I'm sure many of you will disagree with this post. I'm up for a (civilized) debate. Please leave comments below or on Reddit.

If you'd like to see the complete code for this post, it is available as a [gist here](https://gist.github.com/swlaschin/8409306).
The gist also includes full property-based tests for both implementations.

