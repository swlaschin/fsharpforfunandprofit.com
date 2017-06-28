---
layout: post
title: "Functional approaches to dependency injection"
description: "Part one of a series, starting with partial application."
categories: [Dependency Injection ]
---

> This post is part of the [F# Advent Calendar in English 2016](https://sergeytihon.wordpress.com/2016/10/23/f-advent-calendar-in-english-2016/) project.
> Check out all the other great posts there! And special thanks to Sergey Tihon for organizing this.

For people who are learning functional programming techniques, one of the more common questions is: "how do you do dependency injection?"
This series of posts addresses that topic.

As [I have said elsewhere](https://www.youtube.com/watch?v=E8I19uA-wGY&t=224), it is not very helpful to try to translate OO concepts directly to FP. Better, instead,
to focus on the *goals* (which are the same) rather than the implementation techniques.

In this series then:

* We'll start off with the reasons for doing dependency injection.
* Then we'll implement a classic OO-style dependency injection.
* Then we'll look at three different FP-oriented approaches: partial application, the Reader monad, and the Interpreter pattern (aka Free monad).

Partial application will be covered in this post, and the Reader monad and Interpreter pattern in future posts.

## The reasons for dependency injection

*Before I start talking about dependency injection, I have to point out that I'm indebted to Mark Seemann, who [literally wrote the book on the subject](https://www.amazon.com/gp/product/1935182501). If I misrepresent some of these ideas, the fault is all mine!*

So, here are three common reasons for using dependency injection:

* First, to promote *information hiding* and *loose coupling*. These concepts go back to 1971 and two classic papers by David Parnas on [decomposing systems](https://blog.acolyer.org/2016/09/05/on-the-criteria-to-be-used-in-decomposing-systems-into-modules/) and [design methodology](https://blog.acolyer.org/2016/10/17/information-distribution-aspects-of-design-methodology/).
  * The first paper can be summed up as "look at design decisions which are likely to change and create modules which are designed to hide these decisions from the rest of the system." This is of course, a key principle behind the OO principle of encapsulating implementation details in objects ("program to the interface not to the implementation"), but as we will see shortly, it applies equally well to FP design.
  * The second paper can be summed up as "if you make information available, programmers can't help but make use of it, so don't expose it unless you really need to!"
* Second, to support fast unit testing. It is common to isolate I/O-related operations (database. filesystem, network) into dependencies that can be mocked easily. This is
useful even if the implementation is unlikely to change.
* Thirdly, to allow independent development. If two components are connected only through a documented interface, then development can safely proceed in parallel.

Note that the second reason ("isolation") is very important for functional programmers. To reason about our code, we like
to separate "effects" from pure code. "Effects" can mean I/O of course, but also randomness, global state (such as DateTime.Now), and mutability.
The ability to test and mock is just a convenient consequence of this approach.

F# doesn't strictly enforce this separation of effects from pure code in the same way as Haskell, say, but it is a worthwhile goal nevertheless.

## A use-case for dependency injection 

Let's look at a concrete use-case that we can use as a basis to experiment with different implementations.

Say that we have some kind of web app with users, and each user has a "profile" with their name, email, preferences, etc.
A use-case for updating their profile might be something like this:

* define "Update a User profile"
* input: a JSON object representing the request
* steps:
  1. Parse the JSON request into a domain object 
     representing the request
  1. Read the user's current email address from the
     database (needed for step 4)
  1. Update the user's profile in the database
  1. If the email has changed, send a courtesy email message
     to the user's old email notifying them that it has changed

Of course, we will need to implement other stuff as well, such as:

* Logging
* Auditing
* Profiling

and so on. For now, we'll just include logging.


## OO implementations with and without dependency injection

Let's take this use-case and do two different OO implementations, one with and one without dependency injection.

First, let's define the domain types that will be shared across all the implementations (OO and functional):

```fsharp
type UserId = int
type UserName = string
type EmailAddress = string

type UpdateProfileRequest = {
    UserId : UserId 
    Name : UserName 
    EmailAddress : EmailAddress 
}
```

For now, I'm just going to use type aliases for `UserId`, `UserName`. For a more sophisticated approach, we would use [constrained types](/posts/designing-with-types-more-semantic-types/) instead of primitives such as `int` and `string`. And also, I won't be doing
any kind of validation either. That's a big topic for another time!

So, here's the first OO version, without dependency injection:

```fsharp
type UserProfileUpdater() = 

  member this.UpdateCustomerProfile(json: string) =
    try
      let request = this.ParseRequest(json) 
      let currentEmail = DbService.GetEmail(request.UserId)
      DbService.UpdateProfile(request.UserId, request.Name, request.EmailAddress)
      Logger.Info("Updated Profile")

      if currentEmail <> request.EmailAddress then
        Logger.Info("Sending Email Changed Notification")
        EmailService.SendEmailChangedNotification(currentEmail,request.EmailAddress)
    with
    | ex -> 
      Logger.Error(sprintf "UpdateCustomerProfile failed: '%s'" ex.Message)
```

You can see that we have hard-coded the services such as `DbService` and `EmailService` and `Logger` right into the method. This is not good!

The standard OO way to fix this is to define interfaces for each service, and then inject them in the class constructor.

Here are the interfaces we will use:

```fsharp
type ILogger =
  abstract Info : string -> unit
  abstract Error : string -> unit

type IDbService = 
  abstract GetEmail : UserId -> EmailAddress
  abstract UpdateProfile : UserId * UserName * EmailAddress -> unit

type IEmailService = 
  abstract SendEmailChangedNotification : EmailAddress * EmailAddress -> unit
```

You can read these interfaces like this: 

* `Info` takes a `string` as input and returns nothing.
* `GetEmail` takes a `UserId` as input and returns a `EmailAddress`
* `UpdateProfile` takes a 3-tuple of `UserId`, `UserName` and `EmailAddress`, and returns nothing.
* and so on.

Here's the updated implementation, now with dependency injection:

```fsharp
type UserProfileUpdater 
  ( dbService:IDbService, 
    emailService:IEmailService, 
    logger:ILogger ) = 

  member this.UpdateCustomerProfile(json: string) =
    try
      let request = this.ParseRequest(json) 
      let currentEmail = dbService.GetEmail(request.UserId)
      dbService.UpdateProfile(request.UserId, request.Name, request.EmailAddress)
      logger.Info("Updated Profile")

      if currentEmail <> request.EmailAddress then
        logger.Info("Sending Email Changed Notification")
        emailService.SendEmailChangedNotification(currentEmail,request.EmailAddress)
    with
    | ex -> 
      logger.Error(sprintf "UpdateCustomerProfile failed: '%s'" ex.Message)

  member this.ParseRequest(json:string) : UpdateProfileRequest =
    ...            
```

This is better. All the services are injected in the class constructor, and there are no direct dependencies on the service implementations. 
So we have gained the benefits of loose coupling, mockability and parallel development, as promised.

But this approach is not perfect either. Let's look at some of the issues that still remain:

* **Hidden dependencies on local methods.** The `UpdateCustomerProfile` method has a hidden dependency on another method in the same class: `ParseRequest`. 
  One of the issues with OO is that you can accidentally take a dependency on other methods in the same scope, and this can often make refactoring
  awkward. In particular, you can accidentally depend on methods in a parent class, causing the ["fragile base class" problem](http://wiki.c2.com/?FragileBaseClassProblem).
* **Unintentional dependencies.**  The use of dependency injection at the class level means that *any* method in the class can use *any* of the injected dependencies. For example, what's to stop the `ParseRequest` method from using the `emailService` dependency? Unlikely, you say? But remember the maxim "if you make information available, programmers can't help but make use of it." In my experience, this is true and can cause maintenance nightmares.
* **Unneeded interface methods** Worse, interfaces naturally tend to accumulate new methods over time and become [more and more general purpose](https://softwareengineering.stackexchange.com/questions/245350/split-up-large-interfaces). I wouldn't be surprised if `IDbService` soon starting gaining other methods such as `DeleteCustomer` and `ResetPassword`. And then what's to stop `UpdateCustomerProfile` from calling `DeleteCustomer` by mistake?
  In other words, we have failed at information hiding -- one of our core design principles!
  The [Interface Segregation Principle](https://en.wikipedia.org/wiki/Interface_segregation_principle) is a reminder not to do this, but it is often an uphill battle.
* Finally, the name of the class `UserProfileUpdater` is awkward and a bit of a code smell. It's a sign that this business logic doesn't fit into an obvious domain class.
  
Oh, but I do know the fix for that last one -- we just rename it to `UserProfileUpdateService` -- problem solved! :)

One more thing. In order to use this class, we need to create the concrete implementations and pass them in. There are various ways of doing this,
but typically, there is some application-level component (the ["Composition Root"](http://blog.ploeh.dk/2011/07/28/CompositionRoot/)) that is responsible for creating and wiring up the components and services. 

For example, in our implementation, we might have something like this:

```fsharp
module CompositionRoot = 

  // read from config file for example
  let dbConnectionString = "server=dbserver; database=mydatabase"
  let smtpConnectionString = "server=emailserver"

  // construct the services
  let dbService = Services.DbService(dbConnectionString)
  let emailService = Services.EmailService(smtpConnectionString)
  let logger = Services.Logger()

  // construct the class, injecting the services
  let customerUpdater = UserProfileUpdater(dbService,emailService,logger)
```

Ok. That should all be quite familiar. Notice that the `DbService` needs a `dbConnectionString`, which is passed in to its constructor. Similarly, the `EmailService`
needs a `smtpConnectionString`.

Now let's look at the FP approach.

## Implementing the use case as a function

In FP, we don't have classes, only functions. So rather than a class constructor, each function *must* have all of its dependencies passed in explicitly.

Now, we could copy the OO approach, and pass interfaces in to the function as parameters like this:

```fsharp
let updateCustomerProfile 
  (dbService:IDbService) 
  (emailService:IEmailService) 
  (logger:ILogger)  
  (json: string) =
  
  try
    let request = parseRequest(json) 
    let currentEmail = dbService.GetEmail(request.UserId)
    dbService.UpdateProfile(request.UserId, request.Name, request.EmailAddress)
    // etc
```

But using interfaces still has the same problems with accidental dependencies that we mentioned above.

A better approach is to break out each separate dependency into a standalone function.

For example, if each interface had only *one* method, then they would look like this:

```fsharp
type ILogInfo  = 
    abstract LogInfo : string -> unit

type ILogError = 
    abstract LogError : string -> unit

type IDbGetEmail  = 
    abstract GetEmail : UserId -> EmailAddress

type IDbUpdateProfile = 
    abstract UpdateProfile : UserId * UserName * EmailAddress -> unit

type ISendEmailChangedNotification = 
    abstract Notify : EmailAddress * EmailAddress -> unit
```

But of course, an interface with one method is just a function type, so we could rewrite these "interfaces" as:

```fsharp
type LogInfo = string -> unit
type LogError = string -> unit

type DbGetEmail = UserId -> EmailAddress
type DbUpdateProfile = UserId * UserName * EmailAddress -> unit
type Notify = EmailAddress * EmailAddress -> unit
```

Notice that in the functional approach, `DbGetEmail` and `DbUpdateProfile` do *not* have any `dbConnectionString` parameter. These functions define only what is needed from
the caller's point of view, and the caller shouldn't need to know anything about connection strings.
But as a result, there's nothing in these functions that mentions a database at all. Having a `Db` in the name is now misleading!

Ok, let's change our function to use these function types rather than interfaces. It now looks like this:

```fsharp
module CustomerUpdater = 

  let updateCustomerProfile 
    (logInfo:LogInfo) 
    (logError:LogError) 
    (getEmail:DbGetEmail) 
    (updateProfile:DbUpdateProfile) 
    (notify:Notify)  
    (json: string) =
    try
      let request = parseRequest(json) 
      let currentEmail = getEmail(request.UserId)
      updateProfile(request.UserId, request.Name, request.EmailAddress)
      logInfo("Updated Profile")
    // etc    
```

We actually don't need the type annotations for the parameters at all, and we could equally well write it like this:

```fsharp
let updateCustomerProfile logInfo logError getEmail updateProfile notify json =
    try
      let request = parseRequest(json) 
      let currentEmail = getEmail(request.UserId)
      updateProfile(request.UserId, request.Name, request.EmailAddress)
      logInfo("Updated Profile")
    // etc    
```

However, the type annotations may be useful if you are working top-down -- you know what the services are and you want to ensure that compiler errors occur within `updateCustomerProfile` if you get it wrong.

Ok, let's pause and analyze this version.

First, the good thing is that *all* dependencies are now explicit. And there are no dependencies that it doesn't need. This function can't accidentally delete a customer, for example. 

And if you want to test it, it is very easy to mock all the function parameters, as we'll see soon.

The downside of course, is that there are now five extra parameters for the function, which looks painful. (Of course, the equivalent method in the OO version also had these five dependencies, but they were implicit). 

In my opinion though, this pain is actually helpful! With OO style interfaces, there is a natural tendency
for them to accrete crud over time. But with explicit parameters like this, there is a natural disincentive to have too many dependencies! The need
for a guideline such as the Interface Segregation Principle is much diminished.

## Building functions with partial application

Let's look at how this function would be created. Just as with the OO design, we need some component that is responsible for setting everything up.
I'll steal the OO vocabulary for now and call it `CompositionRoot` again.

```fsharp
module CompositionRoot = 

  let dbConnectionString = "server=dbserver; database=mydatabase"
  let smtpConnectionString = "server=emailserver"
 
  let getEmail = 
      // partial application
      DbService.getEmail dbConnectionString 
      
  let updateProfile = 
      // partial application
      DbService.updateProfile dbConnectionString 
  
  let notify = 
      // partial application
      EmailService.sendEmailChangedNotification smtpConnectionString
 
  let logInfo = Logger.info
  let logError = Logger.error
 
  let parser = CustomerUpdater.parseRequest
 
  let updateCustomerProfile = 
      // partial application
      CustomerUpdater.updateCustomerProfile logInfo logError getEmail updateProfile notify
```

What we're doing here is using *partial application* to provide all the dependencies that each function needs.
(If you are not familiar with partial application, see [this discussion](https://www.youtube.com/watch?v=E8I19uA-wGY&t=1548))

![](/assets/img/partial_appl.png)

For example, the database functions might be implemented like this, with an explicit `connectionString` parameter in addition to the main parameters:

```fsharp
module DbService = 

    let getEmail connectionString (userId:UserId) :EmailAddress =
        // ...

    let updateProfile connectionString ((userId:UserId),(name:UserName),(emailAddress:EmailAddress)) =
        // ...
```

In our composition root, we are passing in just the `dbConnectionString`, leaving the other parameters open:

```fsharp
let getEmail = 
  // partial application
  DbService.getEmail dbConnectionString 

let updateProfile = 
  // partial application
  DbService.updateProfile dbConnectionString 
```

The resulting functions match the types that we need to pass into the main `updateCustomerProfile` function:

```fsharp
type DbGetEmail = UserId -> EmailAddress
type DbUpdateProfile = UserId * UserName * EmailAddress -> unit
```

And at the end of the module we take the same approach with `updateCustomerProfile` itself. We pass in the five dependencies, leaving the `json` string parameter open, to be supplied later.

```fsharp
let updateCustomerProfile = 
  // partial application
  CustomerUpdater.updateCustomerProfile logInfo logError getEmail updateProfile notify
```

That means that when we actually use the function, we only need to pass in the json string, like this:

```fsharp
let json = """{"UserId" : "1","Name" : "Alice","EmailAddress" : "new@example.com"}"""
CompositionRoot.updateCustomerProfile json 
```

In this way, the caller does not need to know exactly what the dependencies of `updateCustomerProfile` are. We have the decoupling that we wanted.

And of course, it is easy to test because all the dependencies can be mocked. Here's an example of a test that checks that the email
notification is not sent if the database update fails. You can see that we can quickly mock every single dependency.

```fsharp
// test
let ``when email changes but db update fails, expect notification email not sent``() =

  // --------------------
  // arrange
  // --------------------
  let getEmail _ = 
      "old@example.com"
  
  let updateProfile _ = 
      // deliberately fail
      failwith "update failed"
  
  let mutable notificationWasSent = false
  let notify _ = 
      // just set flag
      notificationWasSent <- true
  
  let logInfo msg = printfn "INFO: %s" msg
  let logError msg = printfn "ERROR: %s" msg
  
  let updateCustomerProfile = 
      CustomerUpdater.updateCustomerProfile logInfo logError getEmail updateProfile notify

  // --------------------      
  // act
  // --------------------
  
  let json = """{"UserId" : "1","Name" : "Alice","EmailAddress" : "new@example.com"}"""
  updateCustomerProfile json 
  
  // --------------------
  // assert
  // --------------------
  
  if notificationWasSent then failwith "test failed"
```

## Passing dependencies to inner functions

What happens if the inner dependencies share parameters with the main function?

For example, let's say that the `DbService` functions need logging functions as well, like this?

```fsharp
module DbService = 

  let getEmail connectionString logInfo logError userId  =
                // logging functions ^
    ...

  let updateProfile connectionString logInfo logError (userId,name,emailAddress) =
                   // logging functions ^
    ...
```

How should we deal with this? Should we take the `logInfo` parameter and pass it to the dependency like this:

```fsharp
let updateCustomerProfile logInfo logError getEmail updateProfile notify json =
    try
        let request = parseRequest json 
        let currentEmail = getEmail logInfo logError request.UserId 
                            // Added ^logInfo ^logError 
        updateProfile logInfo logError (request.UserId, request.Name, request.EmailAddress)
               Added ^logInfo ^logError 
        ...
```

No, this is (generally) the wrong approach. What dependencies `getEmail` has is no concern of `updateCustomerProfile`.

Instead, passing in these new dependencies should be the responsibility of the top-level composition root:

```fsharp
module CompositionRoot = 

    let logInfo = Logger.info
    let logError = Logger.error

    let dbConnectionString = "server=dbserver; database=mydatabase"
    let smtpConnectionString = "server=emailserver"

    let getEmail = 
        DbService.getEmail dbConnectionString logInfo logError 
                                  // Pass in ^logInfo ^logError 
    let updateProfile = 
        DbService.updateProfile dbConnectionString logInfo logError 
                                       // Pass in ^logInfo ^logError 
```

The end result is that `getEmail` and `updateProfile` have exactly the same "interface" as before, and we haven't broken any code that depends on them.

## Refactoring 

The `updateCustomerProfile` function feels ugly to me though. Let's see if we can do some refactoring to make it nicer!

### Refactoring step 1: Logging at the epicenter

The first refactoring we'll do is to move the logging around.  

Who should be responsible for logging the result of an action: the caller or the callee? In general, I think, the callee. Inside the callee,
there is generally more information available to log. It also means that the called functions are more self-contained and can be more easily composed.

So, as before, assume that the logging functions are passed in to the services, and they do their own logging:

```fsharp
module DbService =
  let getEmail connectionString logInfo logError userId  =
    ...

  let updateProfile connectionString logInfo logError (userId,name,emailAddress) =
    logInfo (sprintf "profile updated to %s; %s" name emailAddress)

module EmailService =

  let sendEmailChangedNotification smtpConnectionString logInfo (oldEmailAddress,newEmailAddress) =
    logInfo (sprintf "email sent to old %s and new %s" oldEmailAddress newEmailAddress) 
```

With these changes, the main function doesn't really need any logging at all now:

```fsharp
let updateCustomerProfile logError getEmail updateProfile notify json =
  try
    let request = parseRequest json 
    let currentEmail = getEmail request.UserId
    updateProfile (request.UserId, request.Name, request.EmailAddress)
 
    if currentEmail <> request.EmailAddress then
      notify(currentEmail,request.EmailAddress)
  with
  | ex -> logError (sprintf "UpdateCustomerProfile failed: '%s'" ex.Message)
```

Um, except for `logError` in the exception handling!  We'll get rid of that in the next section.

But first a short digression. What happens if you *do* need context from a higher level? Or let's say that the database interface is fixed, and you can't change it to add logging.

The answer is simple -- just wrap the dependency in your own logging logic when everything is set up in the composition root!

For example, let's say that `updateProfile` and `notify` are just given to you, and do *not* have any logging, like this:

```fsharp
let updateProfile = 
    DbService.updateProfile dbConnectionString 
                                // No logging ^
let notify = 
    EmailService.sendEmailChangedNotification smtpConnectionString   
                                                     // No logging ^
```

All you need to do is create a little logging helper function and "decorate" the functions with it:

```fsharp
// helper function
let withLogInfo msg f x = 
    logInfo msg
    f x

let updateProfileWithLog = 
    updateProfile |> withLogInfo "Updated Profile"

let notifyWithLog = 
    notify |> withLogInfo "Sending Email Changed Notification"
```

The new functions `updateProfileWithLog` and `notifyWithLog` have exactly the same signature as the originals, and so can be passed into `updateCustomerProfile`
just as the originals were.

```fsharp
let updateCustomerProfile = 
    updateCustomerProfile logError getEmail updateProfileWithLog notifyWithLog
                                  // With logging ^    With logging ^
```

This is a very simple example, but of course you can extend the idea to handle more complex scenarios.

### Refactoring step 2: Replacing exceptions with Results

As promised, let's get rid of the exception handling logic now.

Here's the exception handling code we are using:

```fsharp
let updateCustomerProfile ... = 
    try
       ...
    with
    | ex -> 
      logError(sprintf "UpdateCustomerProfile failed: '%s'" ex.Message)
```

It's not very well designed, because it catches *all* exceptions, even ones we probably want to fail-fast due to a programming error (such as `NullReferenceException`). 

Let's replace the exception handling logic with the choice type `Result` (see my [functional error handling talk](/rop/) for more on this concept).

First we can define the `Result` type itself (and this is built-in to F# 4.1, hooray!).

```fsharp
type Result<'a> = 
    | Ok of 'a
    | Error of string
```
    
And then we need some common functions such as `map` and `bind`:

```fsharp
module Result =

    let map f xResult = 
        match xResult with
        | Ok x -> Ok (f x)
        | Error err -> Error err

    let bind f xResult = 
        match xResult with
        | Ok x -> f x
        | Error err -> Error err
```

And finally, a minimal "result" computation expression

```fsharp
type ResultBuilder() =
    member this.Return x = Ok x
    member this.Zero() = Ok ()
    member this.Bind(xResult,f) = Result.bind f xResult
    
let result = ResultBuilder()
```

Assuming that our services now return `Result` rather than throw exceptions, we can rewrite the `updateCustomerProfile` inside the `result` computation expression like this:

```fsharp
let parseRequest json :Result<UpdateProfileRequest> =
    ...
  
let updateCustomerProfile getEmail updateProfile notify json :Result<unit> =
    result {
        let! request = parseRequest json 
        let! currentEmail = getEmail request.UserId
        do! updateProfile(request.UserId,request.Name,request.EmailAddress)

        if currentEmail <> request.EmailAddress then
            do! notify (currentEmail,request.EmailAddress)
    }
```

It's cleaner now and we've removed the dependency on `logError`, but we can do even better!

### Refactoring step 3: Replacing multiple function parameters with one

In all the code so far, we have been passing two logging functions `logInfo` and `logError`.  That's kind of annoying, since they are both so similar.
And I can see that we might also need to pass in even more functions, such `logDebug`, `logWarn`, etc.

So, is there a way to avoid having to pass all these functions around all the time?

One way is to go back to the object oriented approach of using an `ILogger` interface which contains all the methods we need.

An alternative approach is to use *data* to represent the choices. That is, rather than having three or four different functions being
passed in as parameters, we pass in *one* function parameter, and
then we pass that one function a value with three or four different choices.

Let's see how this might work in practice.

Here's an example of the original approach, using three different functions for logging:

```fsharp
let example1 logDebug logInfo logError =
    //       ^ three  ^ dependencies ^
    logDebug "Testing"
    if 1 = 1 then
        logInfo "OK"
    else
        logError "Unexpected"
```

But we could instead define a discriminated union with a case associated with each function:

```fsharp
type LogMessage =
    | Debug of string
    | Info of string
    | Error of string
```

The code that uses the logging now only needs *one* dependency:

```fsharp
let example2 logger =
    //       ^ one dependency
    logger (Debug "Testing")
    if 1 = 1 then
        logger (Info "OK")
    else
        logger (Error "Unexpected")
```

The implementation of the logging function then just matches on the case to determine how to handle the message:

```fsharp
let logger logMessage =
    match logMessage with
    | Debug msg -> printfn "DEBUG %s" msg
    | Info msg -> printfn "INFO %s" msg
    | Error msg -> printfn "ERROR %s" msg
```

Can we use this approach everywhere?  For example, we have two separate database functions `getEmail` and `updateProfile`. Could we merge them into
one dependency using the same trick?

Alas, no. Not in this case. The reason is that they return *different* types. `getEmail` returns an `EmailAddress` and `updateProfile` returns `unit`.
And so, we can't easily create a function that encapsulates both.

However, there is a way to extend this "data" oriented approach to *all* the dependencies, although it makes things a bit more complex. That will be the topic of the upcoming post on the "Interpreter"/"Free Monad."

### Refactoring step 4: Replacing the two database functions with one

However, we shouldn't give up on replacing those two database functions.

If we look at the code, we can see that we are making some assumptions, in particular, that we need two calls to the database: one to fetch the old email,
and one to do the update.

```fsharp
...
let! currentEmail = getEmail request.UserId
do! updateProfile(request.UserId,request.Name,request.EmailAddres
if currentEmail <> request.EmailAddress then
    ...
...
```

Perhaps that's wrong? Perhaps our SQL expert can write a stored procedure that can do it in one call?

But how can we merge them into one? One way would be to have `updateProfile` return the original email that was updated:

```fsharp
...
let! oldEmail = updateProfile(request.UserId,request.Name,request.EmailAddress)
if oldEmail <> request.EmailAddress then
    do! notify (oldEmail,request.EmailAddress)
...    
```

This is better. We no longer care how many SQL calls are needed, we are just focusing on what is needed by the caller.

But now we have introduced a subtle coupling. Why does `updateProfile` return the email? Only because the next step in the business logic requires it. If you just looked at `updateProfile` in isolation, it wouldn't be obvious why it was designed that way.

If we're going to introduce coupling, I think we should make it explicit. I think that `updateProfile` should return a choice: either a simple update happened
with no need of notification, or the email changed and a notification is needed.

These two cases can be captured in a type:

```fsharp
type ProfileUpdated =
    | NoNotificationNeeded
    | NotificationNeeded of oldEmail:EmailAddress * newEmail:EmailAddress
```

When the `updateProfile` function returns a value of this type, at least the business logic is more explicit now.

We've now separated the logic from the implementation. This type tells us when something of interest happened, but *doesn't* say how it should be handled.

We could put the pattern matching for the two cases directly in the `updateCustomerProfile` like this:

```fsharp
let updateCustomerProfile updateProfile notify json =
    result {
        let! request = parseRequest json 
        let! updateResult = updateProfile(request.UserId,request.Name,request.EmailAddress)
        match updateResult with
        | NoNotificationNeeded -> ()
        | NotificationNeeded (oldEmail,newEmail) -> 
            do! notify oldEmail newEmail
    }
```

But personally, I'd prefer to create a helper function to hide it, and then the main function is just a linear series of calls with no branching:

```fsharp
let updateCustomerProfile updateProfile handleProfileUpdateResult json =
    //                                  ^replaces "notify"
    result {
        let! request = parseRequest json 
        let! updateResult = updateProfile(request.UserId,request.Name,request.EmailAddress)
        do! handleProfileUpdateResult updateResult 
    }

// implementation of helper function
let handleProfileUpdateResult notify updateResult =
    result {
        match updateResult with
        | NoNotificationNeeded -> ()
        | NotificationNeeded (oldEmail,newEmail) -> 
            do! notify oldEmail newEmail
    }
```

Note that the `handleProfileUpdateResult` is passed into `updateCustomerProfile` as a parameter, replacing the `notify` parameter. We haven't hard-coded it.

### Refactoring step 5: Passing in the `parseRequest`

One final thing I'm tempted to do is pass in the `parseRequest` function as a parameter as well, like this:

```fsharp
let updateCustomerProfile parseRequest updateProfile handleProfileUpdateResult json =
    //                    ^new parameter
    result {
        let! request = parseRequest json 
        let! updateResult = updateProfile(request.UserId,request.Name,request.EmailAddress)
        do! handleProfileUpdateResult updateResult 
    }
```

Why on earth would I want to do that? Why add an *extra* parameter -- we are trying to get rid of them!

Well, one reason is so that we can define `parseRequest` in a completely different module. In particular, we could define it *after* `updateCustomerProfile` is defined.

This is one way to get around the linear order that F# imposes on files. If we explicitly reference the implementation of `parseRequest`, then `parseRequest` *must* be defined in
a module earlier than (or the same as) the module that `updateCustomerProfile` is defined in. By passing it in as a parameter, we break the connection and eliminate the need for any special file ordering.

Another reason for doing this is that `updateCustomerProfile` really shouldn't care about the particular implementation of `parseRequest` or where it lives. By making it a parameter, we can enforce that.

A final reason that I want to do this is that we are now very close to being able to chain the three functions (`parseRequest`, `updateProfile`, `handleProfileUpdateResult`) together directly like this:

```fsharp
let updateCustomerProfile parseRequest updateProfile handleProfileUpdateResult =
    parseRequest 
    >=> updateProfile 
    >=> handleProfileUpdateResult 
```

The `>=>` is the [Kleisli composition](http://fsharpforfunandprofit.com/posts/elevated-world-3/#kleisli) operator -- it composes two `Result`-returning functions into a new `Result`-returning function.
That is, if function `f` has signature `'a -> Result<'b>` and function `g` has signature `'b -> Result<'c>`, then `f >=> g` is a new function with signature `'a -> Result<'c>`

Using the code we have written so far, we can define it like this:

```fsharp
let (>=>) f g = f >> Result.bind g
```

But here's a thought. Once we have the ability to chain the functions directly, do we even need a separate `updateCustomerProfile` function at all?

No! We can do everything in the composition root and get rid of `updateCustomerProfile` completely!

Here's the final version of the composition root module, where the various components are assembled:

```fsharp
module CompositionRoot = 

    // -------------
    // Get the configuration
    // -------------
    let dbConnectionString = "server=dbserver; database=mydatabase"
    let smtpConnectionString = "server=emailserver"

    // -------------
    // Line up the components and "inject the dependencies" using partial application
    // -------------
    let logInfo = Logger.info
    let logError = Logger.error

    let getEmail = DbService.getEmail dbConnectionString logInfo logError 
    let updateProfile = DbService.updateProfile dbConnectionString logInfo logError 

    let notify = EmailService.sendEmailChangedNotification smtpConnectionString logInfo logError 

    let parseRequest = JsonParsers.parseRequest

    // -------------
    // Create some helper functions to make the components fit together smoothly
    // -------------

    // helper 1: a variant of updateProfile to use in the pipeline
    let updateProfile' request = updateProfile(request.UserId,request.Name,request.EmailAddress)

    // helper 2: handle the updateResult 
    let handleProfileUpdateResult updateResult =
        result {
            match updateResult with
            | NoNotificationNeeded -> ()
            | NotificationNeeded (oldEmail,newEmail) -> 
                do! notify(oldEmail,newEmail)
        }

    // -------------
    // Assemble the pipeline
    // -------------

    let updateCustomerProfile = 
        parseRequest  
        >=> updateProfile'
        >=> handleProfileUpdateResult 
```

In this version of `CompositionRoot`, it is no longer merely a place where dependencies are injected, but a place where entire pipelines are assembled for use in the
rest of the application. And if the pieces of the pipeline don't quite fit together properly, some little helper functions are created to even out the rough spots.

You can see this approach at work with the [Suave (web framework) combinators](https://suave.io/composing.html).
For example, we might see code for a Suave web application that looks like this:

```fsharp
// define the routes and the pipelines for each route
let app = 
    choose [
      GET  >=> something >=> somethingElse >=> OK "Hello"
      POST >=> something >=> somethingElse >=> OK "Thanks for posting"
      ]

// start the app      
startWebServer defaultConfig app      
```

There is no need for a special `UserProfileUpdater` class or module any longer. Instead, the various pipelines are assembled right there in the application (or controller). 

## Conclusion

In this post, we did a little exploration of some approaches to decoupling the various components that make up an application.

We started off with a `UpdateCustomerProfile` method which glued together some components with a bit of business logic (the if-then-else branch).
We then implemented the OO approach to working with dependencies (injecting interfaces) and the FP-sort-of-equivalent (partial application).

But by the end, with some refactoring, we eliminated the need for any special "gluing together" function at all!
That is, we evolved the code from an "injection" oriented approach to a composition-oriented approach (using Kleisli composition).
And that is the ultimate in decoupling: standalone functions that are implemented independently and can be glued together in various ways as needed.

That's it for now. Until next time, Happy Holidays!