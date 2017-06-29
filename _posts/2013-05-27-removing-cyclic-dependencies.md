---
layout: post
title: "Refactoring to remove cyclic dependencies"
description: "Cyclic dependencies: Part 2"
categories: [Design]
seriesId: "Dependency cycles"
seriesOrder: 2

---

In the previous post, we looked at the concept of dependency cycles, and why they are bad.

In this post, we'll look at some techniques for eliminating them from your code.  Having to do this may seem annoying at first, but really, you'll come to appreciate that in the long run, "it's not a bug, it's a feature!"

## Classifying some common cyclic dependencies

Let's classify the kinds of dependencies you're likely to run into. I'll look at three common situations, and for each one, demonstrate some techniques for dealing with them.

First, there is what I will call a *"method dependency"*. 

* Type A stores a value of type B in a property
* Type B references type A in a method signature, but doesn't store a value of type A

Second, there is what I will call a *"structural dependency"*. 

* Type A stores a value of type B in a property
* Type B stores a value of type A in a property

Finally, there is what I will call an *"inheritance dependency"*. 

* Type A stores a value of type B in a property
* Type B inherits from type A

There are, of course, other variants. But if you know how to deal with these, you can use the same techniques to deal with the others as well.

## Three tips on dealing with dependencies in F# ##

Before we get started, here are three useful tips which apply generally when trying to untangle dependencies.

**Tip 1: Treat F# like F#**.  

Recognize that F# is not C#. If you are willing to work with F# using its native idioms, then it is normally very straightforward to avoid circular dependencies by using a different style of [code organization](/posts/recipe-part3/). 

**Tip 2: Separate types from behavior**. 

Since most types in F# are immutable, it is acceptable for them to be "exposed" and ["anemic"](http://www.martinfowler.com/bliki/AnemicDomainModel.html), even. So in a functional design it is common to separate the types themselves from the functions that act on them. This approach will often help to clean up dependencies, as we'll see below.

**Tip 3: Parameterize, parameterize, parameterize**. 

Dependencies can only happen when a specific type is referenced. If you use generic types, you cannot have a dependency! 

And rather than hard coding behavior for a type, why not parameterize it by passing in functions instead? The `List` module is a great example of this approach, and I'll show some examples below as well. 

## Dealing with a "method dependency"

We'll start with the simplest kind of dependency -- what I will call a "method dependency".

Here is an example.

```fsharp
module MethodDependencyExample = 

    type Customer(name, observer:CustomerObserver) = 
        let mutable name = name
        member this.Name 
            with get() = name
            and set(value) = 
                name <- value
                observer.OnNameChanged(this)

    and CustomerObserver() = 
        member this.OnNameChanged(c:Customer) =     
            printfn "Customer name changed to '%s' " c.Name

    // test
    let observer = new CustomerObserver()
    let customer = Customer("Alice",observer)
    customer.Name <- "Bob"
```

The `Customer` class has a property/field of type `CustomerObserver`, but the `CustomerObserver` class has a method which takes a `Customer` as a parameter, causing a mutual dependency.

### Using the "and" keyword

One straightforward way to get the types to compile is to use the `and` keyword, as I did above.

The `and` keyword is designed for just this situation -- it allows you to have two or more types that refer to each other.  

To use it, just replace the second `type` keyword with `and`. Note that using `and type`, as shown below, is incorrect. Just the single `and` is all you need.

```fsharp
type Something 
and type SomethingElse  // wrong

type Something 
and SomethingElse       // correct
```

But `and` has a number of problems, and using it is generally discouraged except as a last resort.

First, it only works for types declared in the same module. You can't use it across module boundaries.

Second, it should really only be used for tiny types. If you have 500 lines of code between the `type` and the `and`, then you are doing something very wrong.

```fsharp
type Something
   // 500 lines of code
and SomethingElse
   // 500 more lines of code
```

The code snippet shown above is an example of how *not* to do it.

In other words, don't treat `and` as a panacea. Overusing it is a symptom that you have not refactored your code properly. 

### Introducing parameterization

So, instead of using `and`, let's see what we can do using parameterization, as mentioned in the third tip.

If we think about the example code, do we *really* need a special `CustomerObserver` class? Why have we restricted it to `Customer` only?  Can't we have a more generic observer class? 

So why don't we create a `INameObserver<'T>` interface instead, with the same `OnNameChanged` method, but the method (and interface) parameterized to accept any class?

Here's what I mean:

```fsharp
module MethodDependency_ParameterizedInterface = 

    type INameObserver<'T> = 
        abstract OnNameChanged : 'T -> unit

    type Customer(name, observer:INameObserver<Customer>) = 
        let mutable name = name
        member this.Name 
            with get() = name
            and set(value) = 
                name <- value
                observer.OnNameChanged(this)

    type CustomerObserver() = 
        interface INameObserver<Customer> with 
            member this.OnNameChanged c =     
                printfn "Customer name changed to '%s' " c.Name

    // test
    let observer = new CustomerObserver()
    let customer = Customer("Alice", observer)
    customer.Name <- "Bob"
```

In this revised version, the dependency has been broken! No `and` is needed at all.  In fact, you could even put the types in different projects or assemblies now!

The code is almost identical to the first version, except that the `Customer` constructor accepts a interface, and `CustomerObserver` now implements the same interface.  In fact, I would argue that introducing the interface has actually made the code better than before. 

But we don't have to stop there.  Now that we have an interface, do we really need to create a whole class just to implement it?  F# has a great feature called [object expressions](http://msdn.microsoft.com/en-us/library/dd233237.aspx) which allows you to instantiate an interface directly.

Here is the same code again, but this time the `CustomerObserver` class has been eliminated completely and the `INameObserver` created directly.

```fsharp
module MethodDependency_ParameterizedInterface = 

    // code as above
    
    // test
    let observer2 = {
        new INameObserver<Customer> with 
            member this.OnNameChanged c =     
                printfn "Customer name changed to '%s' " c.Name
        }
    let customer2 = Customer("Alice", observer2)
    customer2.Name <- "Bob"
```

This technique will obviously work for more complex interfaces as well, such as that shown below, where there are two methods:

```fsharp
module MethodDependency_ParameterizedInterface2 = 

    type ICustomerObserver<'T> = 
        abstract OnNameChanged : 'T -> unit
        abstract OnEmailChanged : 'T -> unit

    type Customer(name, email, observer:ICustomerObserver<Customer>) = 
        
        let mutable name = name
        let mutable email = email

        member this.Name 
            with get() = name
            and set(value) = 
                name <- value
                observer.OnNameChanged(this)

        member this.Email
            with get() = email
            and set(value) = 
                email <- value
                observer.OnEmailChanged(this)

    // test
    let observer2 = {
        new ICustomerObserver<Customer> with 
            member this.OnNameChanged c =     
                printfn "Customer name changed to '%s' " c.Name
            member this.OnEmailChanged c =     
                printfn "Customer email changed to '%s' " c.Email
        }
    let customer2 = Customer("Alice", "x@example.com",observer2)
    customer2.Name <- "Bob"
    customer2.Email <- "y@example.com"
```

### Using functions instead of parameterization

In many cases, we can go even further and eliminate the interface class as well. Why not just pass in a simple function that is called when the name changes, like this:

```fsharp
module MethodDependency_ParameterizedClasses_HOF  = 

    type Customer(name, observer) = 
        
        let mutable name = name

        member this.Name 
            with get() = name
            and set(value) = 
                name <- value
                observer this

    // test
    let observer(c:Customer) = 
        printfn "Customer name changed to '%s' " c.Name
    let customer = Customer("Alice", observer)
    customer.Name <- "Bob"
```

I think you'll agree that this snippet is "lower ceremony" than either of the previous versions.  The observer is now defined inline as needed, very simply:

```fsharp
let observer(c:Customer) = 
    printfn "Customer name changed to '%s' " c.Name
```

True, it only works when the interface being replaced is simple, but even so, this approach can be used more often than you might think.


## A more functional approach: separating types from functions

As I mentioned above, a more "functional design" would be to separate the types themselves from the functions that act on those types. Let's see how this might be done in this case.

Here is a first pass:

```fsharp
module MethodDependencyExample_SeparateTypes = 

    module DomainTypes = 
        type Customer = { name:string; observer:NameChangedObserver }
        and  NameChangedObserver = Customer -> unit


    module Customer = 
        open DomainTypes

        let changeName customer newName = 
            let newCustomer = {customer with name=newName}
            customer.observer newCustomer
            newCustomer     // return the new customer

    module Observer = 
        open DomainTypes

        let printNameChanged customer = 
            printfn "Customer name changed to '%s' " customer.name

    // test
    module Test = 
        open DomainTypes

        let observer = Observer.printNameChanged 
        let customer = {name="Alice"; observer=observer}
        Customer.changeName customer "Bob"
```

In the example above, we now have *three* modules: one for the types, and one each for the functions. Obviously, in a real application, there will be a lot more Customer related functions in the `Customer` module than just this one!

In this code, though, we still have the mutual dependency between `Customer` and `CustomerObserver`. The type definitions are more compact, so it is not such a problem, but even so, can we eliminate the `and`?

Yes, of course. We can use the same trick as in the previous approach, eliminating the observer type and embedding a function directly in the `Customer` data structure, like this:

```fsharp
module MethodDependency_SeparateTypes2 = 

    module DomainTypes = 
        type Customer = { name:string; observer:Customer -> unit}

    module Customer = 
        open DomainTypes

        let changeName customer newName = 
            let newCustomer = {customer with name=newName}
            customer.observer newCustomer
            newCustomer     // return the new customer

    module Observer = 
        open DomainTypes

        let printNameChanged customer = 
            printfn "Customer name changed to '%s' " customer.name

    module Test = 
        open DomainTypes

        let observer = Observer.printNameChanged 
        let customer = {name="Alice"; observer=observer}
        Customer.changeName customer "Bob"
```

### Making types dumber

The `Customer` type still has some behavior embedded in it. In many cases, there is no need for this.  A more functional approach would be to pass a function only when you need it.

So let's remove the `observer` from the customer type, and pass it as an extra parameter to the `changeName` function, like this:

```fsharp
let changeName observer customer newName = 
    let newCustomer = {customer with name=newName}
    observer newCustomer    // call the observer with the new customer
    newCustomer             // return the new customer
```

Here's the complete code:

```fsharp
module MethodDependency_SeparateTypes3 = 

    module DomainTypes = 
        type Customer = {name:string}

    module Customer = 
        open DomainTypes

        let changeName observer customer newName = 
            let newCustomer = {customer with name=newName}
            observer newCustomer    // call the observer with the new customer
            newCustomer             // return the new customer

    module Observer = 
        open DomainTypes

        let printNameChanged customer = 
            printfn "Customer name changed to '%s' " customer.name

    module Test = 
        open DomainTypes

        let observer = Observer.printNameChanged 
        let customer = {name="Alice"}
        Customer.changeName observer customer "Bob"
```

You might be thinking that I have made things more complicated now -- I have to specify the `observer` function everywhere I call `changeName` in my code. Surely this is worse than before? At least in the OO version, the observer was part of the customer object and I didn't have to keep passing it in.

Ah, but, you're forgetting the magic of [partial application](/posts/partial-application/)!  You can set up a function with the observer "baked in", and then use *that* function everywhere, without needing to pass in an observer every time you use it. Clever!

```fsharp
module MethodDependency_SeparateTypes3 = 

    // code as above
    
    module TestWithPartialApplication = 
        open DomainTypes

        let observer = Observer.printNameChanged 

        // set up this partial application only once (at the top of your module, say)
        let changeName = Customer.changeName observer 

        // then call changeName without needing an observer
        let customer = {name="Alice"}
        changeName customer "Bob"
```

### But wait... there's more!

Let's look at the `changeName` function again:

```fsharp
let changeName observer customer newName = 
    let newCustomer = {customer with name=newName}
    observer newCustomer    // call the observer with the new customer
    newCustomer             // return the new customer
```

It has the following steps:

1. do something to make a result value
1. call the observer with the result value
1. return the result value

This is completely generic logic -- it has nothing to do with customers at all. So we can rewrite it as a completely generic library function. Our new function will allow *any* observer function to "hook into" into the result of *any* other function, so let's call it `hook` for now. 

```fsharp
let hook2 observer f param1 param2 = 
    let y = f param1 param2 // do something to make a result value
    observer y              // call the observer with the result value
    y                       // return the result value
```

Actually, I called it `hook2` because the function `f` being "hooked into" has two parameters. I could make another version for functions that have one parameter, like this:

```fsharp
let hook observer f param1 = 
    let y = f param1 // do something to make a result value 
    observer y       // call the observer with the result value
    y                // return the result value
```

If you have read the [railway oriented programming post](/posts/recipe-part2/), you might notice that this is quite similar to what I called a "dead-end" function.  I won't go into more details here, but this is indeed a common pattern.

Ok, back to the code -- how do we use this generic `hook` function?  

* `Customer.changeName` is the function being hooked into, and it has two parameters, so we use `hook2`.
* The observer function is just as before

So, again, we create a partially applied `changeName` function, but this time we create it by passing the observer and the hooked function to `hook2`, like this:

```fsharp
let observer = Observer.printNameChanged 
let changeName = hook2 observer Customer.changeName 
```

Note that the resulting `changeName` has *exactly the same signature* as the original `Customer.changeName` function, so it can be used interchangably with it anywhere.

```fsharp
let customer = {name="Alice"}
changeName customer "Bob"
```

Here's the complete code:

```fsharp
module MethodDependency_SeparateTypes_WithHookFunction = 

    [<AutoOpen>]
    module MyFunctionLibrary = 

        let hook observer f param1 = 
            let y = f param1 // do something to make a result value 
            observer y       // call the observer with the result value
            y                // return the result value

        let hook2 observer f param1 param2 = 
            let y = f param1 param2 // do something to make a result value
            observer y              // call the observer with the result value
            y                       // return the result value

    module DomainTypes = 
        type Customer = { name:string}

    module Customer = 
        open DomainTypes

        let changeName customer newName = 
            {customer with name=newName}

    module Observer = 
        open DomainTypes

        let printNameChanged customer = 
            printfn "Customer name changed to '%s' " customer.name

    module TestWithPartialApplication = 
        open DomainTypes

        // set up this partial application only once (at the top of your module, say)
        let observer = Observer.printNameChanged 
        let changeName = hook2 observer Customer.changeName 

        // then call changeName without needing an observer
        let customer = {name="Alice"}
        changeName customer "Bob"
```

Creating a `hook` function like this might seem to add extra complication initially, but it has eliminated yet more code from the main application, and once you have built up a library of functions like this, you will find uses for them everywhere.

By the way, if it helps you to use OO design terminology, you can think of this approach as a "Decorator" or "Proxy" pattern.


## Dealing with a "structural dependency"

The second of our classifications is what I am calling a "structural dependency", where each type stores a value of the other type.

* Type A stores a value of type B in a property
* Type B stores a value of type A in a property

For this set of examples, consider an `Employee` who works at a `Location`. The `Employee` contains the `Location` they work at, and the `Location` stores a list of `Employees` who work there.

Voila -- mutual dependency!

Here is the example in code:

```fsharp
module StructuralDependencyExample = 

    type Employee(name, location:Location) = 
        member this.Name = name
        member this.Location = location

    and Location(name, employees: Employee list) = 
        member this.Name = name
        member this.Employees  = employees 
```

Before we get on to refactoring, let's consider how awkward this design is. How can we initialize an `Employee` value without having a `Location` value, and vice versa.

Here's one attempt. We create a location with an empty list of employees, and then create other employees using that location:

```fsharp
module StructuralDependencyExample = 

    // code as above
    
    module Test = 
        let location = new Location("CA",[])       
        let alice = new Employee("Alice",location)       
        let bob = new Employee("Bob",location)      

        location.Employees  // empty!
        |> List.iter (fun employee -> 
            printfn "employee %s works at %s" employee.Name employee.Location.Name) 
```

But this code doesn't work as we want. We have to set the list of employees for `location` as empty because we can't forward reference the `alice` and `bob` values..

F# will sometimes allow you to use the `and` keyword in these situation too, for recursive "lets". Just as with "type", the "and" keyword replaces the "let" keyword. Unlike "type", the first "let" has to be marked as recursive with `let rec`.

Let's try it. We will give `location` a list of `alice` and `bob` even though they are not declared yet. 

```fsharp
module UncompilableTest = 
    let rec location = new Location("NY",[alice;bob])       
    and alice = new Employee("Alice",location  )       
    and bob = new Employee("Bob",location )      
```

But no, the compiler is not happy about the infinite recursion that we have created.  In some cases, `and` does indeed work for `let` definitions, but this is not one of them! 
And anyway, just as for types, having to use `and` for "let" definitions is a clue that you might need to refactor.

So, really, the only sensible solution is to use mutable structures, and to fix up the location object *after* the individual employees have been created, like this:

```fsharp
module StructuralDependencyExample_Mutable = 

    type Employee(name, location:Location) = 
        member this.Name = name
        member this.Location = location

    and Location(name, employees: Employee list) = 
        let mutable employees = employees

        member this.Name = name
        member this.Employees  = employees 
        member this.SetEmployees es = 
            employees <- es

    module TestWithMutableData = 
        let location = new Location("CA",[])       
        let alice = new Employee("Alice",location)       
        let bob = new Employee("Bob",location)      
        // fixup after creation
        location.SetEmployees [alice;bob]  

        location.Employees  
        |> List.iter (fun employee -> 
            printfn "employee %s works at %s" employee.Name employee.Location.Name) 
```

So, a lot of trouble just to create some values. This is another reason why mutual dependencies are a bad idea!

### Parameterizing again

To break the dependency, we can use the parameterization trick again. We can just create a parameterized vesion of `Employee`.

```fsharp
module StructuralDependencyExample_ParameterizedClasses = 

    type ParameterizedEmployee<'Location>(name, location:'Location) = 
        member this.Name = name
        member this.Location = location

    type Location(name, employees: ParameterizedEmployee<Location> list) = 
        let mutable employees = employees
        member this.Name = name
        member this.Employees  = employees 
        member this.SetEmployees es = 
            employees <- es

    type Employee = ParameterizedEmployee<Location> 

    module Test = 
        let location = new Location("CA",[])       
        let alice = new Employee("Alice",location)       
        let bob = new Employee("Bob",location)      
        location.SetEmployees [alice;bob]

        location.Employees  // non-empty!
        |> List.iter (fun employee -> 
            printfn "employee %s works at %s" employee.Name employee.Location.Name) 
```

Note that we create a type alias for `Employee`, like this:

```fsharp
type Employee = ParameterizedEmployee<Location> 
```

One nice thing about creating an alias like that is that the original code for creating employees will continue to work unchanged.

```fsharp
let alice = new Employee("Alice",location)       
```

### Parameterizing with behavior dependencies

The code above assumes that the particular class being parameterized over is not important. But what if there are dependencies on particular properties of the type? 

For example, let's say that the `Employee` class expects a `Name` property, and the `Location` class expects an `Age` property, like this:

```fsharp
module StructuralDependency_WithAge = 

    type Employee(name, age:float, location:Location) = 
        member this.Name = name
        member this.Age = age
        member this.Location = location
        
        // expects Name property
        member this.LocationName = location.Name  

    and Location(name, employees: Employee list) = 
        let mutable employees = employees
        member this.Name = name
        member this.Employees  = employees 
        member this.SetEmployees es = 
            employees <- es
        
        // expects Age property            
        member this.AverageAge = 
            employees |> List.averageBy (fun e -> e.Age)

    module Test = 
        let location = new Location("CA",[])       
        let alice = new Employee("Alice",20.0,location)       
        let bob = new Employee("Bob",30.0,location)      
        location.SetEmployees [alice;bob]
        printfn "Average age is %g" location.AverageAge 
```

How can we possibly parameterize this?

Well, let's try using the same approach as before:

```fsharp
module StructuralDependencyWithAge_ParameterizedError = 

    type ParameterizedEmployee<'Location>(name, age:float, location:'Location) = 
        member this.Name = name
        member this.Age = age
        member this.Location = location
        member this.LocationName = location.Name  // error

    type Location(name, employees: ParameterizedEmployee<Location> list) = 
        let mutable employees = employees
        member this.Name = name
        member this.Employees  = employees 
        member this.SetEmployees es = 
            employees <- es
        member this.AverageAge = 
            employees |> List.averageBy (fun e -> e.Age)
```

The `Location` is happy with `ParameterizedEmployee.Age`, but `location.Name` fails to compile. obviously, because the type parameter is too generic.

One way would be to fix this by creating interfaces such as `ILocation` and `IEmployee`, and that might often be the most sensible approach.

But another way is to let the Location parameter be generic and pass in an *additional function* that knows how to handle it. In this case a `getLocationName` function.

```fsharp
module StructuralDependencyWithAge_ParameterizedCorrect = 

    type ParameterizedEmployee<'Location>(name, age:float, location:'Location, getLocationName) = 
        member this.Name = name
        member this.Age = age
        member this.Location = location
        member this.LocationName = getLocationName location  // ok

    type Location(name, employees: ParameterizedEmployee<Location> list) = 
        let mutable employees = employees
        member this.Name = name
        member this.Employees  = employees 
        member this.SetEmployees es = 
            employees <- es
        member this.AverageAge = 
            employees |> List.averageBy (fun e -> e.Age)


```

One way of thinking about this is that we are providing the behavior externally, rather than as part of the type.

To use this then, we need to pass in a function along with the type parameter. This would be annoying to do all the time, so naturally we will wrap it in a function, like this:

```fsharp
module StructuralDependencyWithAge_ParameterizedCorrect = 

    // same code as above

    // create a helper function to construct Employees
    let Employee(name, age, location) = 
        let getLocationName (l:Location) = l.Name
        new ParameterizedEmployee<Location>(name, age, location, getLocationName)
```

With this in place, the original test code continues to work, almost unchanged (we have to change `new Employee` to just `Employee`).

```fsharp
module StructuralDependencyWithAge_ParameterizedCorrect = 

    // same code as above

    module Test = 
        let location = new Location("CA",[])       
        let alice = Employee("Alice",20.0,location)       
        let bob = Employee("Bob",30.0,location)      
        location.SetEmployees [alice;bob]

        location.Employees  // non-empty!
        |> List.iter (fun employee -> 
            printfn "employee %s works at %s" employee.Name employee.LocationName) 
```

## The functional approach: separating types from functions again

Now let's apply the functional design approach to this problem, just as we did before.

Again, we'll separate the types themselves from the functions that act on those types. 

```fsharp
module StructuralDependencyExample_SeparateTypes = 

    module DomainTypes = 
        type Employee = {name:string; age:float; location:Location}
        and Location = {name:string; mutable employees: Employee list}

    module Employee = 
        open DomainTypes 

        let Name (employee:Employee) = employee.name
        let Age (employee:Employee) = employee.age
        let Location (employee:Employee) = employee.location
        let LocationName (employee:Employee) = employee.location.name

    module Location = 
        open DomainTypes 

        let Name (location:Location) = location.name
        let Employees (location:Location) = location.employees
        let AverageAge (location:Location) =
            location.employees |> List.averageBy (fun e -> e.age)
    
    module Test = 
        open DomainTypes 

        let location = { name="NY"; employees= [] }
        let alice = {name="Alice"; age=20.0; location=location  }
        let bob = {name="Bob"; age=30.0; location=location }
        location.employees <- [alice;bob]
         
        Location.Employees location
        |> List.iter (fun e -> 
            printfn "employee %s works at %s" (Employee.Name e) (Employee.LocationName e) ) 
```

Before we go any further, let's remove some unneeded code.  One nice thing about using a record type is that you don't need to define "getters", so the only functions you need in the modules
are functions that manipulate the data, such as `AverageAge`.

```fsharp
module StructuralDependencyExample_SeparateTypes2 = 

    module DomainTypes = 
        type Employee = {name:string; age:float; location:Location}
        and Location = {name:string; mutable employees: Employee list}

    module Employee = 
        open DomainTypes 

        let LocationName employee = employee.location.name

    module Location = 
        open DomainTypes 

        let AverageAge location =
            location.employees |> List.averageBy (fun e -> e.age)
```

### Parameterizing again

Once again, we can remove the dependency by creating a parameterized version of the types.

Let's step back and think about the "location" concept. Why does a location have to only contain Employees? If we make it a bit more generic, we could consider a location as being a "place"
plus "a list of things at that place".

For example, if the things are products, then a place full of products might be a warehouse. If the things are books, then a place full of books might be a library.

Here are these concepts expressed in code:

```fsharp
module LocationOfThings =

    type Location<'Thing> = {name:string; mutable things: 'Thing list}

    type Employee = {name:string; age:float; location:Location<Employee> }
    type WorkLocation = Location<Employee>

    type Product = {SKU:string; price:float }
    type Warehouse = Location<Product>

    type Book = {title:string; author:string}
    type Library = Location<Book>
```

Of course, these locations are not exactly the same, but there might be something in common that you can extract into a generic design, especially as there is no behavior requirement attached to
the things they contain.

So, using the "location of things" design, here is our dependency rewritten to use parameterized types.

```fsharp
module StructuralDependencyExample_SeparateTypes_Parameterized = 

    module DomainTypes = 
        type Location<'Thing> = {name:string; mutable things: 'Thing list}
        type Employee = {name:string; age:float; location:Location<Employee> }

    module Employee = 
        open DomainTypes 

        let LocationName employee = employee.location.name

    module Test = 
        open DomainTypes 

        let location = { name="NY"; things = [] }
        let alice = {name="Alice"; age=20.0; location=location  }
        let bob = {name="Bob"; age=30.0; location=location }
        location.things <- [alice;bob]

        let employees = location.things
        employees 
        |> List.iter (fun e -> 
            printfn "employee %s works at %s" (e.name) (Employee.LocationName e) ) 

        let averageAge = 
            employees 
            |> List.averageBy (fun e -> e.age) 
```

In this revised design you will see that the `AverageAge` function has been completely removed from the `Location` module. There is really no need for it, because we can do these
kinds of calculations quite well "inline" without needing the overhead of special functions.

And if you think about it, if we *did* need to have such a function pre-defined, it would probably be more appropriate to put in the `Employee` module rather than the `Location` module.
After all, the functionality is much more related to how employees work than how locations work.

Here's what I mean:

```fsharp
module Employee = 

    let AverageAgeAtLocation location = 
        location.things |> List.averageBy (fun e -> e.age) 
```

This is one advantage of modules over classes; you can mix and match functions with different types, as long as they are all related to the underlying use cases.

### Moving relationships into distinct types

In the examples so far, the "list of things" field in location has had to be mutable.  How can we work with immutable types and still support relationships?

Well one way *not* to do it is to have the kind of mutual dependency we have seen.  In that design, synchronization (or lack of) is a terrible problem

For example, I could change Alice's location without telling the location she points to, resulting in an inconsistency. But if I tried to change the contents of the location as well, then I would also need to update the value of Bob as well. And so on, ad infinitum. A nightmare, basically.

The correct way to do this with immutable data is steal a leaf from database design, and extract the relationship into a separate "table" or type in our case.
The current relationships are held in a single master list, and so when changes are made, no synchronization is needed.

Here is a very crude example, using a simple list of `Relationship`s. 

```fsharp
module StructuralDependencyExample_Normalized = 

    module DomainTypes = 
        type Relationship<'Left,'Right> = 'Left * 'Right

        type Location= {name:string}
        type Employee = {name:string; age:float }

    module Employee = 
        open DomainTypes 

        let EmployeesAtLocation location relations = 
            relations
            |> List.filter (fun (loc,empl) -> loc = location) 
            |> List.map (fun (loc,empl) -> empl) 

        let AverageAgeAtLocation location relations = 
            EmployeesAtLocation location relations 
            |> List.averageBy (fun e -> e.age) 

    module Test = 
        open DomainTypes 

        let location = { Location.name="NY"}
        let alice = {name="Alice"; age=20.0; }
        let bob = {name="Bob"; age=30.0; }
        let relations = [ 
            (location,alice)
            (location,bob) 
            ]

        relations 
        |> List.iter (fun (loc,empl) -> 
            printfn "employee %s works at %s" (empl.name) (loc.name) ) 
```

Or course, a more efficient design would use dictionaries/maps, or special in-memory structures designed for this kind of thing.

## Inheritance dependencies

Finally, let's look at an "inheritance dependency". 

* Type A stores a value of type B in a property
* Type B inherits from type A

We'll consider a UI control hierarchy, where every control belongs to a top-level "Form", and the Form itself is a Control.

Here's a first pass at an implementation:

```fsharp
module InheritanceDependencyExample = 

    type Control(name, form:Form) = 
        member this.Name = name

        abstract Form : Form
        default this.Form = form

    and Form(name) as self = 
        inherit Control(name, self)

    // test
    let form = new Form("form")       // NullReferenceException!
    let button = new Control("button",form)
```

The thing to note here is that the Form passes itself in as the `form` value for the Control constructor.

This code will compile, but will cause a `NullReferenceException` error at runtime. This kind of technique will work in C#, but not in F#, because the class initialization logic is done differently.

Anyway, this is a terrible design. The form shouldn't have to pass itself in to a constructor.

A better design, which also fixes the constructor error, is to make `Control` an abstract class instead, and distinguish between non-form child classes (which do take a form in their constructor)
and the `Form` class itself, which doesn't.  

Here's some sample code:

```fsharp
module InheritanceDependencyExample2 = 

    [<AbstractClass>]
    type Control(name) = 
        member this.Name = name

        abstract Form : Form

    and Form(name) = 
        inherit Control(name)

        override this.Form = this

    and Button(name,form) = 
        inherit Control(name)

        override this.Form = form

    // test
    let form = new Form("form")       
    let button = new Button("button",form)
```

### Our old friend parameterization again

To remove the circular dependency, we can parameterize the classes in the usual way, as shown below.

```fsharp
module InheritanceDependencyExample_ParameterizedClasses = 

    [<AbstractClass>]
    type Control<'Form>(name) = 
        member this.Name = name

        abstract Form : 'Form

    type Form(name) = 
        inherit Control<Form>(name)

        override this.Form = this

    type Button(name,form) = 
        inherit Control<Form>(name)

        override this.Form = form


    // test
    let form = new Form("form")       
    let button = new Button("button",form)
```

### A functional version

I will leave a functional design as an exercise for you to do yourself.

If we were going for truly functional design, we probably would not be using inheritance at all. Instead, we would use composition in conjunction with parameterization.

But that's a big topic, so I'll save it for another day.

## Summary

I hope that this post has given you some useful tips on removing dependency cycles. With these various approaches in hand, any problems with [module organization](/posts/recipe-part3/) should be able to be resolved easily.

In the next post in this series, I'll look at dependency cycles "in the wild", by comparing some real world C# and F# projects. 

As we have seen, F# is a very opinionated language! It wants us to use modules instead of classes and it prohibits dependency cycles. Are these just annoyances, or do they really make a difference to the way that code is organized?
[Read on and find out!](/posts/cycles-and-modularity-in-the-wild/)



