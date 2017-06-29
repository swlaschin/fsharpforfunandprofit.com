---
layout: post
title: "Expressions vs. statements"
description: "Why expressions are safer and make better building blocks"
nav: thinking-functionally
seriesId: "Expressions and syntax"
seriesOrder: 2
---

In programming language terminology, an "expression" is a combination of values and functions that are combined and interpreted by the compiler to create a new value, as opposed to a "statement" which is just a standalone unit of execution and doesn't return anything.  One way to think of this is that the purpose of an expression is to create a value (with some possible side-effects), while the sole purpose of a statement is to have side-effects.

C# and most imperative languages make a distinction between expressions and statements and have rules about where each kind can be used.  But as should be apparent, a truly pure functional language cannot support statements at all, because in a truly pure language, there would be no side-effects.  

Even though F# is not pure, it does follow the same principle. In F# everything is an expression. Not just values and functions, but also control flows (such as if-then-else and loops), pattern matching, and so on.

There are some subtle benefits to using expressions over statements. First, unlike statements, smaller expressions can be combined (or "composed") into larger expressions. So if everything is an expression, then everything is also composable. 

Second, a series of statements always implies a specific order of evaluation, which means that a statement cannot be understood without looking at prior statements.  But with pure expressions, the subexpressions do not have any implied order of execution or dependencies. 

So in the expression `a+b`, if both the '`a`' and '`b`' parts are pure, then the '`a`' part can be isolated, understood, tested and evaluated on its own, as can the '`b`' part.   
This "isolatibility" of expressions is another beneficial aspect of functional programming.   

<div class="alert alert-info">
Note that the F# interactive window also relies on everything being an expression. It would be much harder to use a C# interactive window.
</div>

## Expressions are safer and more compact ##

Using expressions consistently leads to code that is both safer and more compact. Let's see what I mean by this.

First, let's look at a statement based approach.  Statements don't return values, so you have to use temporary variables that are assigned to from within statement bodies.  Here are some examples using a C-like language (OK, C#) rather than F#:

```csharp
public void IfThenElseStatement(bool aBool)
{
   int result;     //what is the value of result before it is used?
   if (aBool)
   {
      result = 42; //what is the result in the 'else' case?
   }
   Console.WriteLine("result={0}", result);
}
```

Because the "if-then" is a statement, the `result` variable must be defined *outside* the statement and but assigned to *inside* the statement, which leads to some issues:

* The `result` variable has to be set up outside the statement itself. What initial value should it be set to?
* What if I forget to assign to the `result` variable in the `if` statement?  The purpose of the "if "statement is purely to have side effects (the assignment to the variables).  This means that the statements are potentially buggy, because it would be easy to forget to do an assignment in one branch. And because the assignment was just a side effect, the compiler could not offer any warning.  Since the `result` variable has already been defined in scope, I could easily use it, unaware that it was invalid.
* What is the value of the `result` variable in the "else" case?  In this case, I haven't specified a value. Did I forget? Is this a potential bug? 
* Finally, the reliance on side-effects to get things done means that the statements are not easily usable in another context (for example, extracted for refactoring, or parallelizing) because they have a dependency on a variable that is not part of the statement itself.

Note: the code above will not compile in C# because the compiler will complain if you use an unassigned local variable like this. But having to define *some* default value for `result` before it is even used is still a problem.

For comparison, here is the same code, rewritten in an expression-oriented style:

```csharp
public void IfThenElseExpression(bool aBool)
{
    int result = aBool ? 42 : 0;
    Console.WriteLine("result={0}", result);
}
```

In the expression-oriented version, none of the earlier issues even apply!  

* The `result` variable is declared at the same time that it is assigned. No variables have to be set up "outside" the expression and there is no worry about what initial value they should be set to. 
* The "else" is explicitly handled. There is no chance of forgetting to do an assignment in one of the branches.
* And I cannot possibly forget to assign to `result`, because then the variable would not even exist!

In F#, the two examples would be written as:

```fsharp
let IfThenElseStatement aBool = 
   let mutable result = 0       // mutable keyword required
   if (aBool) then result <- 42 
   printfn "result=%i" result
```

The "`mutable`" keyword is considered a code smell in F#, and is discouraged except in certain special cases. It should be avoided at all cost while you are learning!

In the expression based version, the mutable variable has been eliminated and there is no reassignment anywhere.  

```fsharp
let IfThenElseExpression aBool = 
   let result = if aBool then 42 else 0   
                // note that the else case must be specified 
   printfn "result=%i" result
```

Once we have the `if` statement converted into an expression, it is now trivial to refactor it and move the entire subexpression to a different context without introducing errors.

Here's the refactored version in C#:

```csharp
public int StandaloneSubexpression(bool aBool)
{
    return aBool ? 42 : 0;
}

public void IfThenElseExpressionRefactored(bool aBool)
{
    int result = StandaloneSubexpression(aBool);
    Console.WriteLine("result={0}", result);
}
```

And in F#:

```fsharp
let StandaloneSubexpression aBool = 
   if aBool then 42 else 0   

let IfThenElseExpressionRefactored aBool = 
   let result = StandaloneSubexpression aBool 
   printfn "result=%i" result
```



### Statements vs. expressions for loops ###

Going back to C# again, here is a similar example of statements vs. expressions using a loop statement 

```csharp
public void LoopStatement()
{
    int i;    //what is the value of i before it is used? 
    int length;
    var array = new int[] { 1, 2, 3 };
    int sum;  //what is the value of sum if the array is empty?

    length = array.Length;   //what if I forget to assign to length?
    for (i = 0; i < length; i++)
    {
        sum += array[i];
    }

    Console.WriteLine("sum={0}", sum);
}
```

I've used an old-style "for" statement, where the index variables are declared outside the loop. Many of the issues discussed earlier apply to the loop index "`i`" and the max value "`length`", such as: can they be used outside the loop? And what happens if they are not assigned to?

A more modern version of a for-loop addresses these issues by declaring and assigning the loop variables in the "for" loop itself, and by requiring the "`sum`" variable to be initialized:

```csharp
public void LoopStatementBetter()
{
    var array = new int[] { 1, 2, 3 };
    int sum = 0;        // initialization is required

    for (var i = 0; i < array.Length; i++)
    {
        sum += array[i];
    }

    Console.WriteLine("sum={0}", sum);
}
```

This more modern version follows the general principle of combining the declaration of a local variable with its first assignment. 

But of course, we can keep improving by using a `foreach` loop instead of a `for` loop:

```csharp
public void LoopStatementForEach()
{
    var array = new int[] { 1, 2, 3 };
    int sum = 0;        // initialization is required

    foreach (var i in array)
    {
        sum += i;
    }

    Console.WriteLine("sum={0}", sum);
}
```

Each time, not only are we condensing the code, but we are reducing the likelihood of errors.

But taking that principle to its logical conclusion leads to a completely expression based approach! Here's how it might be done using LINQ:

```csharp
public void LoopExpression()
{
    var array = new int[] { 1, 2, 3 };

    var sum = array.Aggregate(0, (sumSoFar, i) => sumSoFar + i);

    Console.WriteLine("sum={0}", sum);
}
```

Note that I could have used LINQ's built-in "sum" function, but I used `Aggregate` in order to show how the sum logic embedded in a statement can be converted into a lambda and used as part of an expression.

In the next post, we'll look at the various kinds of expressions in F#.

