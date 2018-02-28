---
layout: post
title: "Worked example: Designing for correctness"
description: "How to make illegal states unrepresentable"
nav: why-use-fsharp
seriesId: "Why use F#?"
seriesOrder: 22
categories: [Correctness, Types, Worked Examples]
---

In this post, we'll see how you can design for correctness (or at least, for the requirements as you currently understand them), by which I mean that a client of a well designed model will not be able to put the system into an illegal state -- a state that doesn't meet the requirements. You literally cannot create incorrect code because the compiler will not let you.

For this to work, we do have to spend some time up front thinking about design and making an effort to encode the requirements into the types that you use.
If you just use strings or lists for all your data structures, you will not get any benefit from the type checking.

We'll use a simple example. Let's say that you are designing an e-commerce site which has a shopping cart and you are given the following requirements.

* You can only pay for a cart once.
* Once a cart is paid for, you cannot change the items in it.
* Empty carts cannot be paid for.

## A bad design in C# ##

In C#, we might think that this is simple enough and dive straight into coding. Here is a straightforward implementation in C# that seems OK at first glance. 

```csharp
public class NaiveShoppingCart<TItem>
{
   private List<TItem> items;
   private decimal paidAmount;

   public NaiveShoppingCart()
   {
      this.items = new List<TItem>();
      this.paidAmount = 0;
   }

   /// Is cart paid for?
   public bool IsPaidFor { get { return this.paidAmount > 0; } }

   /// Readonly list of items
   public IEnumerable<TItem> Items { get {return this.items; } }

   /// add item only if not paid for
   public void AddItem(TItem item)
   {
      if (!this.IsPaidFor)
      {
         this.items.Add(item);
      }
   }

   /// remove item only if not paid for
   public void RemoveItem(TItem item)
   {
      if (!this.IsPaidFor)
      {
         this.items.Remove(item);
      }
   }

   /// pay for the cart
   public void Pay(decimal amount)
   {
      if (!this.IsPaidFor)
      {
         this.paidAmount = amount;
      }
   }
}
```

Unfortunately, it's actually a pretty bad design:

* One of the requirements is not even met. Can you see which one?
* It has a major design flaw, and a number of minor ones. Can you see what they are?

So many problems in such a short piece of code! 

What would happen if we had even more complicated requirements and the code was thousands of lines long?  For example, the fragment that is repeated everywhere:

```csharp
if (!this.IsPaidFor) { do something }
```

looks like it will be quite brittle if requirements change in some methods but not others.

Before you read the next section, think for a minute how you might better implement the requirements above in C#, with these additional requirements:

* If you try to do something that is not allowed in the requirements, you will get a *compile time error*, not a run time error. For example, you must create a design such that you cannot even call the `RemoveItem` method from an empty cart.
* The contents of the cart in any state should be immutable. The benefit of this is that if I am in the middle of paying for a cart, the cart contents can't change even if some other process is adding or removing items at the same time.

## A correct design in F# ##

Let's step back and see if we can come up with a better design. Looking at these requirements, it's obvious that we have a simple state machine with three states and some state transitions:

* A Shopping Cart can be Empty, Active or PaidFor
* When you add an item to an Empty cart, it becomes Active
* When you remove the last item from an Active cart, it becomes Empty
* When you pay for an Active cart, it becomes PaidFor

And now we can add the business rules to this model:

* You can add an item only to carts that are Empty or Active 
* You can remove an item only from carts that are Active 
* You can only pay for carts that are Active 

Here is the state diagram:

![Shopping Cart](/assets/img/ShoppingCart.png)
 
It's worth noting that these kinds of state-oriented models are very common in business systems. Product development, customer relationship management, order processing, and other workflows can often be modeled this way.

Now we have the design, we can reproduce it in F#:

```fsharp
type CartItem = string    // placeholder for a more complicated type

type EmptyState = NoItems // don't use empty list! We want to
                          // force clients to handle this as a 
                          // separate case. E.g. "you have no 
                          // items in your cart"

type ActiveState = { UnpaidItems : CartItem list; }
type PaidForState = { PaidItems : CartItem list; 
                      Payment : decimal}

type Cart = 
    | Empty of EmptyState 
    | Active of ActiveState 
    | PaidFor of PaidForState 
```

We create a type for each state, and `Cart` type that is a choice of any one of the states. I have given everything a distinct name (e.g. `PaidItems` and `UnpaidItems` rather than just `Items`) because this helps the inference engine and makes the code more self documenting.

<div class="alert alert-info">
<p>This is a much longer example than the earlier ones! Don't worry too much about the F# syntax right now, but I hope that you can get the gist of the code, and see how it fits into the overall design. </p>
<p>Also, do paste the snippets into a script file and evaluate them for yourself as they come up.</p>
</div>

Next we can create the operations for each state. The main thing to note is each operation will always take one of the States as input and return a new Cart. That is, you start off with a particular known state, but you return a `Cart` which is a wrapper for a choice of three possible states.

```fsharp
// =============================
// operations on empty state
// =============================

let addToEmptyState item = 
   // returns a new Active Cart
   Cart.Active {UnpaidItems=[item]}

// =============================
// operations on active state
// =============================

let addToActiveState state itemToAdd = 
   let newList = itemToAdd :: state.UnpaidItems
   Cart.Active {state with UnpaidItems=newList }

let removeFromActiveState state itemToRemove = 
   let newList = state.UnpaidItems 
                 |> List.filter (fun i -> i<>itemToRemove)
                
   match newList with
   | [] -> Cart.Empty NoItems
   | _ -> Cart.Active {state with UnpaidItems=newList} 

let payForActiveState state amount = 
   // returns a new PaidFor Cart
   Cart.PaidFor {PaidItems=state.UnpaidItems; Payment=amount}
```

Next, we attach the operations to the states as methods

```fsharp
type EmptyState with
   member this.Add = addToEmptyState 

type ActiveState with
   member this.Add = addToActiveState this 
   member this.Remove = removeFromActiveState this 
   member this.Pay = payForActiveState this 
```   

And we can create some cart level helper methods as well. At the cart level, we have to explicitly handle each possibility for the internal state with a `match..with` expression.

```fsharp
let addItemToCart cart item =  
   match cart with
   | Empty state -> state.Add item
   | Active state -> state.Add item
   | PaidFor state ->  
       printfn "ERROR: The cart is paid for"
       cart   

let removeItemFromCart cart item =  
   match cart with
   | Empty state -> 
      printfn "ERROR: The cart is empty"
      cart   // return the cart 
   | Active state -> 
      state.Remove item
   | PaidFor state ->  
      printfn "ERROR: The cart is paid for"
      cart   // return the cart

let displayCart cart  =  
   match cart with
   | Empty state -> 
      printfn "The cart is empty"   // can't do state.Items
   | Active state -> 
      printfn "The cart contains %A unpaid items"
                                                state.UnpaidItems
   | PaidFor state ->  
      printfn "The cart contains %A paid items. Amount paid: %f"
                                    state.PaidItems state.Payment

type Cart with
   static member NewCart = Cart.Empty NoItems
   member this.Add = addItemToCart this 
   member this.Remove = removeItemFromCart this 
   member this.Display = displayCart this 
```

{% include book_page_ddd.inc %}


## Testing the design ##

Let's exercise this code now:

```fsharp
let emptyCart = Cart.NewCart
printf "emptyCart="; emptyCart.Display

let cartA = emptyCart.Add "A"
printf "cartA="; cartA.Display
```

We now have an active cart with one item in it. Note that "`cartA`" is a completely different object from "`emptyCart`" and is in a different state.

Let's keep going:

```fsharp
let cartAB = cartA.Add "B"
printf "cartAB="; cartAB.Display

let cartB = cartAB.Remove "A"
printf "cartB="; cartB.Display

let emptyCart2 = cartB.Remove "B"
printf "emptyCart2="; emptyCart2.Display
```

So far, so good. Again, all these are distinct objects in different states,

Let's test the requirement that you cannot remove items from an empty cart:

```fsharp
let emptyCart3 = emptyCart2.Remove "B"    //error
printf "emptyCart3="; emptyCart3.Display
```

An error -- just what we want!

Now say that we want to pay for a cart. We didn't create this method at the Cart level, because we didn't want to tell the client how to handle all the cases. This method only exists for the Active state, so the client will have to explicitly handle each case and only call the `Pay` method when an Active state is matched.

First we'll try to pay for cartA.

```fsharp
//  try to pay for cartA
let cartAPaid = 
    match cartA with
    | Empty _ | PaidFor _ -> cartA 
    | Active state -> state.Pay 100m
printf "cartAPaid="; cartAPaid.Display
```

The result was a paid cart.

Now we'll try to pay for the emptyCart.

```fsharp
//  try to pay for emptyCart
let emptyCartPaid = 
    match emptyCart with
    | Empty _ | PaidFor _ -> emptyCart
    | Active state -> state.Pay 100m
printf "emptyCartPaid="; emptyCartPaid.Display
```

Nothing happens. The cart is empty, so the Active branch is not called. We might want to raise an error or log a message in the other branches, but no matter what we do we cannot accidentally call the `Pay` method on an empty cart, because that state does not have a method to call!

The same thing happens if we accidentally try to pay for a cart that is already paid.

```fsharp
//  try to pay for cartAB 
let cartABPaid = 
    match cartAB with
    | Empty _ | PaidFor _ -> cartAB // return the same cart
    | Active state -> state.Pay 100m

//  try to pay for cartAB again
let cartABPaidAgain = 
    match cartABPaid with
    | Empty _ | PaidFor _ -> cartABPaid  // return the same cart
    | Active state -> state.Pay 100m
```

You might argue that the client code above might not be representative of code in the real world -- it is well-behaved and already dealing with the requirements. 

So what happens if we have badly written or malicious client code that tries to force payment:

```fsharp
match cartABPaid with
| Empty state -> state.Pay 100m
| PaidFor state -> state.Pay 100m
| Active state -> state.Pay 100m
```

If we try to force it like this, we will get compile errors. There is no way the client can create code that does not meet the requirements.

## Summary ##

We have designed a simple shopping cart model which has many benefits over the C# design.

* It maps to the requirements quite clearly. It is impossible for a client of this API to call code that doesn't meet the requirements.
* Using states means that the number of possible code paths is much smaller than the C# version, so there will be many fewer unit tests to write. 
* Each function is simple enough to probably work the first time, as, unlike the C# version, there are no conditionals anywhere. 

<div class="well">
<h3>Analysis of the original C# code</h3>

<p>
Now that you have seen the F# code, we can revisit the original C# code with fresh eyes. In case you were wondering, here are my thoughts as to what is wrong with the C# shopping cart example as designed.
</p>

<p>
<i>Requirement not met</i>: An empty cart can still be paid for. 
</p>

<p>
<i>Major design flaw</i>: Overloading the payment amount to be a signal for IsPaidFor means that a zero paid amount can never lock down the cart. Are you sure it would never be possible to have a cart which is paid for but free of charge? The requirements are not clear, but what if this did become a requirement later? How much code would have to be changed?
</p>

<p>
<i>Minor design flaws</i>: What should happen when trying to remove an item from an empty cart? And what should happen when attempting to pay for a cart that is already paid for? Should we throw exceptions in these cases, or just silently ignore them? And does it make sense that a client should be able to enumerate the items in an empty cart? And this is not thread safe as designed; so what happens if a secondary thread adds an item to the cart while a payment is being made on the main thread?
</p>

<p>
That's quite a lot of things to worry about. 
</p>

<p>
The nice thing about the F# design is none of these problems can even exist. So designing this way not only ensures correct code, but it also really reduces the cognitive effort to ensure that the design is bullet proof in the first place.
</p>

<p>
<i>Compile time checking:</i>  The original C# design mixes up all the states and transitions in a single class, which makes it very error prone. A better approach would be to create separate state classes (with a common base class say) which reduces complexity, but still, the lack of a built in "union" type means that you cannot statically verify that the code is correct.  There are ways of doing "union" types in C#, but it is not idiomatic at all, while in F# it is commonplace.
</p>


</div>

## Appendix: C# code for a correct solution

When faced with these requirements in C#, you might immediately think -- just create an interface!

But it is not as easy as you might think. I have written a follow up post on this to explain why: [The shopping cart example in C#](/csharp/union-types-in-csharp.html).

If you are interested to see what the C# code for a solution looks like, here it is below. This code meets the requirements above and guarantees correctness at *compile time*, as desired.

The key thing to note is that, because C# doesn't have union types, the implementation uses a ["fold" function](/posts/match-expression/#folds),
a function that has three function parameters, one for each state. To use the cart, the caller passes a set of three lambdas in, and the (hidden) state determines what happens.

```csharp
var paidCart = cartA.Do(
    // lambda for Empty state
    state => cartA,  
    // lambda for Active state
    state => state.Pay(100),
    // lambda for Paid state
    state => cartA);
```

This approach means that the caller can never call the "wrong" function, such as "Pay" for the Empty state, because the parameter to the lambda will not support it. Try it and see!

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace WhyUseFsharp
{

    public class ShoppingCart<TItem>
    {

        #region ShoppingCart State classes

        /// <summary>
        /// Represents the Empty state
        /// </summary>
        public class EmptyState
        {
            public ShoppingCart<TItem> Add(TItem item)
            {
                var newItems = new[] { item };
                var newState = new ActiveState(newItems);
                return FromState(newState);
            }
        }

        /// <summary>
        /// Represents the Active state
        /// </summary>
        public class ActiveState
        {
            public ActiveState(IEnumerable<TItem> items)
            {
                Items = items;
            }

            public IEnumerable<TItem> Items { get; private set; }

            public ShoppingCart<TItem> Add(TItem item)
            {
                var newItems = new List<TItem>(Items) {item};
                var newState = new ActiveState(newItems);
                return FromState(newState);
            }

            public ShoppingCart<TItem> Remove(TItem item)
            {
                var newItems = new List<TItem>(Items);
                newItems.Remove(item);
                if (newItems.Count > 0)
                {
                    var newState = new ActiveState(newItems);
                    return FromState(newState);
                }
                else
                {
                    var newState = new EmptyState();
                    return FromState(newState);
                }
            }

            public ShoppingCart<TItem> Pay(decimal amount)
            {
                var newState = new PaidForState(Items, amount);
                return FromState(newState);
            }


        }

        /// <summary>
        /// Represents the Paid state
        /// </summary>
        public class PaidForState
        {
            public PaidForState(IEnumerable<TItem> items, decimal amount)
            {
                Items = items.ToList();
                Amount = amount;
            }

            public IEnumerable<TItem> Items { get; private set; }
            public decimal Amount { get; private set; }
        }

        #endregion ShoppingCart State classes

        //====================================
        // Execute of shopping cart proper
        //====================================

        private enum Tag { Empty, Active, PaidFor }
        private readonly Tag _tag = Tag.Empty;
        private readonly object _state;       //has to be a generic object

        /// <summary>
        /// Private ctor. Use FromState instead
        /// </summary>
        private ShoppingCart(Tag tagValue, object state)
        {
            _state = state;
            _tag = tagValue;
        }

        public static ShoppingCart<TItem> FromState(EmptyState state)
        {
            return new ShoppingCart<TItem>(Tag.Empty, state);
        }

        public static ShoppingCart<TItem> FromState(ActiveState state)
        {
            return new ShoppingCart<TItem>(Tag.Active, state);
        }

        public static ShoppingCart<TItem> FromState(PaidForState state)
        {
            return new ShoppingCart<TItem>(Tag.PaidFor, state);
        }

        /// <summary>
        /// Create a new empty cart
        /// </summary>
        public static ShoppingCart<TItem> NewCart()
        {
            var newState = new EmptyState();
            return FromState(newState);
        }

        /// <summary>
        /// Call a function for each case of the state
        /// </summary>
        /// <remarks>
        /// Forcing the caller to pass a function for each possible case means that all cases are handled at all times.
        /// </remarks>
        public TResult Do<TResult>(
            Func<EmptyState, TResult> emptyFn,
            Func<ActiveState, TResult> activeFn,
            Func<PaidForState, TResult> paidForyFn
            )
        {
            switch (_tag)
            {
                case Tag.Empty:
                    return emptyFn(_state as EmptyState);
                case Tag.Active:
                    return activeFn(_state as ActiveState);
                case Tag.PaidFor:
                    return paidForyFn(_state as PaidForState);
                default:
                    throw new InvalidOperationException(string.Format("Tag {0} not recognized", _tag));
            }
        }

        /// <summary>
        /// Do an action without a return value
        /// </summary>
        public void Do(
            Action<EmptyState> emptyFn,
            Action<ActiveState> activeFn,
            Action<PaidForState> paidForyFn
            )
        {
            //convert the Actions into Funcs by returning a dummy value
            Do(
                state => { emptyFn(state); return 0; },
                state => { activeFn(state); return 0; },
                state => { paidForyFn(state); return 0; }
                );
        }



    }

    /// <summary>
    /// Extension methods for my own personal library
    /// </summary>
    public static class ShoppingCartExtension
    {
        /// <summary>
        /// Helper method to Add
        /// </summary>
        public static ShoppingCart<TItem> Add<TItem>(this ShoppingCart<TItem> cart, TItem item)
        {
            return cart.Do(
                state => state.Add(item), //empty case
                state => state.Add(item), //active case
                state => { Console.WriteLine("ERROR: The cart is paid for and items cannot be added"); return cart; } //paid for case
            );
        }

        /// <summary>
        /// Helper method to Remove
        /// </summary>
        public static ShoppingCart<TItem> Remove<TItem>(this ShoppingCart<TItem> cart, TItem item)
        {
            return cart.Do(
                state => { Console.WriteLine("ERROR: The cart is empty and items cannot be removed"); return cart; }, //empty case
                state => state.Remove(item), //active case
                state => { Console.WriteLine("ERROR: The cart is paid for and items cannot be removed"); return cart; } //paid for case
            );
        }

        /// <summary>
        /// Helper method to Display
        /// </summary>
        public static void Display<TItem>(this ShoppingCart<TItem> cart)
        {
            cart.Do(
                state => Console.WriteLine("The cart is empty"),
                state => Console.WriteLine("The active cart contains {0} items", state.Items.Count()),
                state => Console.WriteLine("The paid cart contains {0} items. Amount paid {1}", state.Items.Count(), state.Amount)
            );
        }
    }

    [NUnit.Framework.TestFixture]
    public class CorrectShoppingCartTest
    {
        [NUnit.Framework.Test]
        public void TestCart()
        {
            var emptyCart = ShoppingCart<string>.NewCart();
            emptyCart.Display();

            var cartA = emptyCart.Add("A");  //one item
            cartA.Display();

            var cartAb = cartA.Add("B");  //two items
            cartAb.Display();

            var cartB = cartAb.Remove("A"); //one item
            cartB.Display();

            var emptyCart2 = cartB.Remove("B"); //empty
            emptyCart2.Display();

            Console.WriteLine("Removing from emptyCart");
            emptyCart.Remove("B"); //error


            //  try to pay for cartA
            Console.WriteLine("paying for cartA");
            var paidCart = cartA.Do(
                state => cartA,
                state => state.Pay(100),
                state => cartA);
            paidCart.Display();

            Console.WriteLine("Adding to paidCart");
            paidCart.Add("C");

            //  try to pay for emptyCart
            Console.WriteLine("paying for emptyCart");
            var emptyCartPaid = emptyCart.Do(
                state => emptyCart,
                state => state.Pay(100),
                state => emptyCart);
            emptyCartPaid.Display();
        }
    }
}

```