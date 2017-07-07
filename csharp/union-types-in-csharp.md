---
layout: page
title: "Shopping cart example in C#"
permalink: /csharp/union-types-in-csharp.html
categories: [csharp]
---

This is an appendix to the [post on "designing for correctness"](/posts/designing-for-correctness/). 

In that post, I had some requirements for a simple shopping cart. I showed some bad C# code, and some F# code that implemented the requirements easily.
There are some responses that come up when a C# developer looks at the F# code, such as "why not just use an interface?" or "what about the visitor pattern?"

In this post, I'll demonstrate why the solution is not as straightforward as you might think.

## Background ##

The requirements again:

* A shopping cart might have states `Empty`, `Active` and `Paid`. 
  * You can call "Add" but not "Remove" on the `Empty` state
  * You can call "Add" and "Remove" and also "Pay" on the `Active` state
  * You can't do anything with the `Paid` state. Once paid, the cart is immutable.

In an object-oriented language, the standard approach is to use interfaces and inheritance. There is a class for each state, and each state inherits from a common base class or interface.

But where should the custom behavior live?

For example, should the "Remove" method be available at the interface level or not?

## Approach 1: Define all possible actions at the interface level

Let's say the "Remove" method *is* available at the interface level, then the interface would look like this:

```csharp
interface ICartState
{
    ICartState Add(Product product);
    ICartState Remove(Product product);
    ICartState Pay(decimal amount);
}
```

![Cart State - Approach 1](CartState-Approach1.png)

But what should the empty state implementation do with the methods that are not relevant?  It can either throw an exception or ignore them, as shown below.

```csharp
class CartStateEmpty : ICartState
{
    public ICartState Add(Product product)
    {
        // implementation
    }

    public ICartState Remove(Product product)
    {
        // throw not implemented exception or ignore
    }

    public ICartState Pay(decimal amount)
    {
        // throw not implemented exception or ignore
    }
}
```

Throwing an exception seems a bit drastic, and ignoring the call seems even worse. Is there another approach? 


## Approach 2: Define actions only at the appropriate level

The next approach is that the base interface should only have genuinely common code, and each subclass implements its own special behavior. 

![Cart State - Approach 2](CartState-Approach2.png)

So the code for the shopping cart would look something like this:

```csharp
interface ICartState
{
    // nothing in common between the states
}

class CartStateEmpty : ICartState
{
    public ICartState Add(Product product)
    {
        // implementation
    }
}

class CartStateActive : ICartState
{
    public ICartState Add(Product product) {} // implementation
    
    public ICartState Remove(Product product) {} // implementation
    
    public ICartState  Pay(decimal amount) {} // implementation 
}

class CartStatePaid : ICartState
{
    public decimal GetAmountPaid() {} // implementation 
}
```

This is much cleaner, but now the question is: how does the client know which state the cart is in?

Typically, the caller would have to try downcasting the object to each state in turn. The client code would look something like this:

```csharp
private class CientWithDowncasting
{
    public ICartState AddProduct(ICartState currentState, Product product)
    {
        var cartStateEmpty = currentState as CartStateEmpty; //CAST!
        if (cartStateEmpty != null)
        {
            return cartStateEmpty.Add(Product.ProductY);
        }

        var cartStateActive = currentState as CartStateActive; //CAST!
        if (cartStateActive != null)
        {
            return cartStateActive.Add(Product.ProductY);
        }

        // paid state -- do nothing    
        return currentState;
    }
}
```

But this approach is not only ugly, it's all error prone, as the client has to do all the work.  

For example, what happens if the client forgets to cast properly? And what happens if the requirements change and there are now four states to handle?  These kinds of errors will be hard to catch.

Or as Goering almost said: "Whenever I see the word 'as', I reach for my revolver."

## Approach 3: The double dispatch or visitor pattern

In OO design, a reliance on this kind of [branching](http://sourcemaking.com/refactoring/replace-conditional-with-polymorphism) and [casting](http://www.daedtech.com/casting-is-a-polymorphism-fail) for control flow is always a sign that polymorphism is not being used properly.
And indeed there is a way to avoid downcasting by using so-called "double dispatch" or its big brother, the visitor pattern.

The idea behind it is that the caller does not know the type of the state, but the state knows its own type, and can call back a type-specific method on the caller. For example, the empty state can call a "VisitEmpty" method, while the active state can call a "VisitActive" method, and so on.  In this way, no casting is used. Furthermore, if the number of states increase, the code will break until a handler for the new state is implemented.

To use this approach, first we implement a visitor interface as follows:

```csharp
interface ICartStateVisitor
{
    ICartState VisitEmpty(CartStateEmpty empty);
    ICartState VisitActive(CartStateActive active);
    ICartState VisitPaid(CartStatePaid paid);
}
```

Next we change the base interface to allow the visitor to call on the state:

```csharp
interface ICartState
{
    ICartState Accept(ICartStateVisitor visitor);
}
```

Finally, for each state, we implement the appropriate `VisitXXX` method:

```csharp
class CartStateEmpty : ICartState
{
    public ICartState Accept(ICartStateVisitor visitor)
    {
        return visitor.VisitEmpty(this);
    }
}

class CartStateActive : ICartState
{
    public ICartState Accept(ICartStateVisitor visitor)
    {
        return visitor.VisitActive(this);
    }

class CartStatePaid : ICartState
{
    public ICartState Accept(ICartStateVisitor visitor)
    {
        return visitor.VisitPaid(this);
    }
}
```

Now we are ready to use it -- and here is where our problems start! Because, for each different set of visitor behavior, we have to implement a custom class.

For example, when we want to add an item, we have to create an instance of `AddProductVisitor` and when we want to pay for something, we need to create a `PayForCartVisitor`.

Here's some code to show what I mean:

```csharp
class CientWithVisitor
{
    class AddProductVisitor: ICartStateVisitor
    {
        public Product productToAdd;
        public ICartState VisitEmpty(CartStateEmpty empty) { empty.Add(productToAdd); return empty; }
        public ICartState VisitActive(CartStateActive active) { active.Add(productToAdd); return active; }
        public ICartState VisitPaid(CartStatePaid paid) { return paid; }
    }

    class PayForCartVisitor : ICartStateVisitor
    {
        public decimal amountToPay;
        public ICartState VisitEmpty(CartStateEmpty empty) { return empty; }
        public ICartState VisitActive(CartStateActive active) { active.Pay(amountToPay); return active; }
        public ICartState VisitPaid(CartStatePaid paid) { return paid; }
    }

    public ICartState AddProduct(ICartState currentState, Product product)
    {
        var visitor = new AddProductVisitor() { productToAdd = product };
        return currentState.Accept(visitor);
    }

    public ICartState Pay(ICartState currentState, decimal amountToPay)
    {
        var visitor = new PayForCartVisitor() { amountToPay = amountToPay };
        return currentState.Accept(visitor);
    }
}
```

Phew! It does work, and it is polymorphic and type safe.  But it seems like an awful lot of scaffolding is needed just to do something straightforward. An interface, two new classes, and nine new methods!

Here's are all the players visualized as a diagram:

![Cart State - Approach 3](CartState-Approach3.png)

Compared with the second approach, you can see that it is a lot more complicated. And this is a simple example! It reminds me of the "[Kingdom of Nouns](http://steve-yegge.blogspot.co.uk/2006/03/execution-in-kingdom-of-nouns.html)".

Surely there must be another way -- combining the simplicity of the second approach with the safeness of the visitor approach?

## Implementing choices in C# ##

The F# code gives us a clue on how we might solve the problem in C#. 

The idea is that for each possible state, we pass in a lambda specifically designed for that state. We do this through a single method call. In other words we provide a *list* of functions but we don't know which one will actually get evaluated -- we let the cart decide, based on its state.

Here is some example code:

```csharp
partial interface ICartState 
{
	ICartState Transition(
        Func<CartStateEmpty, ICartState> cartStateEmpty, 
        Func<CartStateActive, ICartState> cartStateActive, 
        Func<CartStatePaid, ICartState> cartStatePaid
        );
}
	
class CartStateEmpty : ICartState 
{
	ICartState ICartState.Transition(
        Func<CartStateEmpty, ICartState> cartStateEmpty, 
        Func<CartStateActive, ICartState> cartStateActive, 
        Func<CartStatePaid, ICartState> cartStatePaid
        )
	{
        // I'm the empty state, so invoke cartStateEmpty 
		return cartStateEmpty(this);
	}
}
	
class CartStateActive : ICartState 
{
	ICartState ICartState.Transition(
        Func<CartStateEmpty, ICartState> cartStateEmpty, 
        Func<CartStateActive, ICartState> cartStateActive, 
        Func<CartStatePaid, ICartState> cartStatePaid
        )
	{
        // I'm the active state, so invoke cartStateActive
		return cartStateActive(this);
	}
}
	
class CartStatePaid : ICartState 
{
	ICartState ICartState.Transition(
        Func<CartStateEmpty, ICartState> cartStateEmpty, 
        Func<CartStateActive, ICartState> cartStateActive, 
        Func<CartStatePaid, ICartState> cartStatePaid
        )
	{
        // I'm the paid state, so invoke cartStatePaid
		return cartStatePaid(this);
	}
}
```

And here is an example of how a client might call it in practice:

```csharp
public ICartState AddProduct(ICartState currentState, Product product)
{
    return currentState.Transition(
        cartStateEmpty => cartStateEmpty.Add(product),
        cartStateActive => cartStateActive.Add(product),
        cartStatePaid => cartStatePaid
        );
            
}

public void Example()
{
    var currentState = new CartStateEmpty() as ICartState;

    //add some products 
    currentState = AddProduct(currentState, Product.ProductX);
    currentState = AddProduct(currentState, Product.ProductY);

    //pay 
    const decimal paidAmount = 12.34m;
    currentState = currentState.Transition(
        cartStateEmpty => cartStateEmpty,
        cartStateActive => cartStateActive.Pay(paidAmount),
        cartStatePaid => cartStatePaid
        );
}
```

As you can see, this approach solves all the problems discussed earlier:

* It is completely idiot proof. A client of the interface always gets a valid value back. 
  The client cannot mess things up by forgetting to cast or forgetting to check for null.
* Illegal states are not even *representable*. A client cannot call the "wrong" method, such as calling "Pay" on an empty cart. The compiler will not allow it.
  Because of this, there is no need for runtime errors or "not implemented" exceptions. 
* If the number of states ever changes to four say, just add a new parameter to the `Transition` method.  The `Transition` method will now take four lambdas, so all your existing code will fail to compile.  This a good thing! You cannot accidentally forget to handle a state.
* The code is simple and low complexity. Just as in the second approach above, each state only implements the methods it needs to, and there are no `if` statements or special case handlers anywhere.  

In a way, this is what the visitor pattern was trying to get at in its complicated way, but the use of inline lambdas rather than whole visitor classes reduces the complexity immensely.


## Summary

Well, I hope you found this interesting and useful. It's definitely a different way of thinking if you are not familiar with functional programming.

