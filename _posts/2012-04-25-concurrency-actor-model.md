---
layout: post
title: "Messages and Agents"
description: "Making it easier to think about concurrency"
nav: why-use-fsharp
seriesId: "Why use F#?"
seriesOrder: 25
categories: [Concurrency]
---

In this post, we'll look at the message-based (or actor-based) approach to concurrency.

In this approach, when one task wants to communicate with another, it sends it a message, rather than contacting it directly. The messages are put on a queue, and the receiving task (known as an "actor" or "agent") pulls the messages off the queue one at a time to process them.

This message-based approach has been applied to many situations, from low-level network sockets (built on TCP/IP) to enterprise wide application integration systems (for example MSMQ or IBM WebSphere MQ).

From a software design point of view, a message-based approach has a number of benefits:

* You can manage shared data and resources without locks.
* You can easily follow the "single responsibility principle", because each agent can be designed to do only one thing. 
* It encourages a "pipeline" model of programming with "producers" sending messages to decoupled "consumers", which has additional benefits:
  * The queue acts as a buffer, eliminating waiting on the client side.
  * It is straightforward to scale up one side or the other of the queue as needed in order to maximize throughput.
  * Errors can be handled gracefully, because the decoupling means that agents can be created and destroyed without affecting their clients. 

From a practical developer's point of view, what I find most appealing about the message-based approach is that when writing the code for any given actor, you don't have to hurt your brain by thinking about concurrency. The message queue forces a "serialization" of operations that otherwise might occur concurrently. And this in turn makes it much easier to think about (and write code for) the logic for processing a message, because you can be sure that your code will be isolated from other events that might interrupt your flow. 

With these advantages, it is not surprising that when a team inside Ericsson wanted to design a programming language for writing highly-concurrent telephony applications, they created one with a message-based approach, namely Erlang. Erlang has now become the poster child for the whole topic, and has created a lot of interest in implementing the same approach in other languages.

## How F# implements a message-based approach ##

F# has a built-in agent class called `MailboxProcessor`. These agents are very lightweight compared with threads - you can instantiate tens of thousands of them at the same time.

These are similar to the agents in Erlang, but unlike the Erlang ones, they do *not* work across process boundaries, only in the same process.
And unlike a heavyweight queueing system such as MSMQ, the messages are not persistent. If your app crashes, the messages are lost.

But these are minor issues, and can be worked around. In a future series, I will go into alternative implementations of message queues.  The fundamental approach is the same in all cases.

Let's see a simple agent implementation in F#:

```fsharp

let printerAgent = MailboxProcessor.Start(fun inbox-> 

    // the message processing function
    let rec messageLoop() = async{
        
        // read a message
        let! msg = inbox.Receive()
        
        // process a message
        printfn "message is: %s" msg

        // loop to top
        return! messageLoop()  
        }

    // start the loop 
    messageLoop() 
    )

```

The `MailboxProcessor.Start` function takes a simple function parameter. That function loops forever, reading messages from the queue (or "inbox") and processing them.


Here's the example in use:

```fsharp
// test it
printerAgent.Post "hello" 
printerAgent.Post "hello again" 
printerAgent.Post "hello a third time" 
```

In the rest of this post we'll look at two slightly more useful examples:

* Managing shared state without locks
* Serialized and buffered access to shared IO

In both of these cases, a message based approach to concurrency is elegant, efficient, and easy to program.

## Managing shared state ##

Let's look at the shared state problem first.

A common scenario is that you have some state that needs to be accessed and changed by multiple concurrent tasks or threads.
We'll use a very simple case, and say that the requirements are:

* A shared "counter" and "sum" that can be incremented by multiple tasks concurrently. 
* Changes to the counter and sum must be atomic -- we must guarantee that they will both be updated at the same time.

### The locking approach to shared state ###

Using locks or mutexes is a common solution for these requirements, so let's write some code using a lock, and see how it performs.

First let's write a static `LockedCounter` class that protects the state with locks.  

```fsharp
open System
open System.Threading
open System.Diagnostics

// a utility function
type Utility() = 
    static let rand = new Random()
    
    static member RandomSleep() = 
        let ms = rand.Next(1,10)
        Thread.Sleep ms

// an implementation of a shared counter using locks
type LockedCounter () = 

    static let _lock = new Object()

    static let mutable count = 0
    static let mutable sum = 0

    static let updateState i = 
        // increment the counters and...
        sum <- sum + i
        count <- count + 1
        printfn "Count is: %i. Sum is: %i" count sum 

        // ...emulate a short delay
        Utility.RandomSleep()


    // public interface to hide the state
    static member Add i = 
        // see how long a client has to wait
        let stopwatch = new Stopwatch()
        stopwatch.Start() 

        // start lock. Same as C# lock{...}
        lock _lock (fun () ->
        
            // see how long the wait was
            stopwatch.Stop()
            printfn "Client waited %i" stopwatch.ElapsedMilliseconds

            // do the core logic
            updateState i 
            )
        // release lock
```

Some notes on this code:

* This code is written using a very imperative approach, with mutable variables and locks
* The public `Add` method has explicit `Monitor.Enter` and `Monitor.Exit` expressions to get and release the lock. This is the same as the `lock{...}` statement in C#.
* We've also added a stopwatch to measure how long a client has to wait to get the lock.
* The core "business logic" is the `updateState` method, which not only updates the state, but adds a small random wait as well to emulate the time taken to do the processing.

Let's test it in isolation:

```fsharp
// test in isolation
LockedCounter.Add 4
LockedCounter.Add 5
```

Next, we'll create a task that will try to access the counter:

```fsharp
let makeCountingTask addFunction taskId  = async {
    let name = sprintf "Task%i" taskId
    for i in [1..3] do 
        addFunction i
    }

// test in isolation
let task = makeCountingTask LockedCounter.Add 1
Async.RunSynchronously task
```

In this case, when there is no contention at all, the wait times are all 0.

But what happens when we create 10 child tasks that all try to access the counter at once:

```fsharp
let lockedExample5 = 
    [1..10]
        |> List.map (fun i -> makeCountingTask LockedCounter.Add i)
        |> Async.Parallel
        |> Async.RunSynchronously
        |> ignore
```

Oh dear! Most tasks are now waiting quite a while. If two tasks want to update the state at the same time, one must wait for the other's work to complete before it can do its own work, which affects performance. 

And if we add more and more tasks, the contention will increase, and the tasks will spend more and more time waiting rather than working. 

### The message-based approach to shared state ###

Let's see how a message queue might help us. Here's the message based version:
        
```fsharp
type MessageBasedCounter () = 

    static let updateState (count,sum) msg = 

        // increment the counters and...
        let newSum = sum + msg
        let newCount = count + 1
        printfn "Count is: %i. Sum is: %i" newCount newSum 

        // ...emulate a short delay
        Utility.RandomSleep()

        // return the new state
        (newCount,newSum)

    // create the agent
    static let agent = MailboxProcessor.Start(fun inbox -> 

        // the message processing function
        let rec messageLoop oldState = async{

            // read a message
            let! msg = inbox.Receive()

            // do the core logic
            let newState = updateState oldState msg

            // loop to top
            return! messageLoop newState 
            }

        // start the loop 
        messageLoop (0,0)
        )

    // public interface to hide the implementation
    static member Add i = agent.Post i
```

Some notes on this code:

* The core "business logic" is again in the `updateState` method, which has almost the same implementation as the earlier example, except the state is immutable, so that a new state is created and returned to the main loop.
* The agent reads messages (simple ints in this case) and then calls `updateState` method
* The public method `Add` posts a message to the agent, rather than calling the `updateState` method directly
* This code is written in a more functional way; there are no mutable variables and no locks anywhere. In fact, there is no code dealing with concurrency at all!
The code only has to focus on the business logic, and is consequently much easier to understand.

Let's test it in isolation:

```fsharp
// test in isolation
MessageBasedCounter.Add 4
MessageBasedCounter.Add 5
```        

Next, we'll reuse a task we defined earlier, but calling `MessageBasedCounter.Add` instead:

```fsharp
let task = makeCountingTask MessageBasedCounter.Add 1
Async.RunSynchronously task
```

Finally let's create 5 child tasks that try to access the counter at once.

```fsharp
let messageExample5 = 
    [1..5]
        |> List.map (fun i -> makeCountingTask MessageBasedCounter.Add i)
        |> Async.Parallel
        |> Async.RunSynchronously
        |> ignore
```

We can't measure the waiting time for the clients, because there is none!

## Shared IO ##

A similar concurrency problem occurs when accessing a shared IO resource such as a file:

* If the IO is slow, the clients can spend a lot of time waiting, even without locks. 
* If multiple threads write to the resource at the same time, you can get corrupted data.

Both problems can be solved by using asynchronous calls combined with buffering -- exactly what a message queue does.

In this next example, we'll consider the example of a logging service that many clients will write to concurrently.
(In this trivial case, we'll just write directly to the Console.)

We'll first look at an implementation without concurrency control, and then at an implementation that uses message queues to serialize all requests.

### IO without serialization ###

In order to make the corruption very obvious and repeatable, let's first create a "slow" console that writes each individual character in the log message
and pauses for a millisecond between each character. During that millisecond, another thread could be writing as well, causing an undesirable
interleaving of messages.

```fsharp
let slowConsoleWrite msg = 
    msg |> String.iter (fun ch->
        System.Threading.Thread.Sleep(1)
        System.Console.Write ch
        )

// test in isolation
slowConsoleWrite "abc"
```

Next, we will create a simple task that loops a few times, writing its name each time to the logger:

```fsharp
let makeTask logger taskId = async {
    let name = sprintf "Task%i" taskId
    for i in [1..3] do 
        let msg = sprintf "-%s:Loop%i-" name i
        logger msg 
    }

// test in isolation
let task = makeTask slowConsoleWrite 1
Async.RunSynchronously task
```


Next, we write a logging class that encapsulates access to the slow console. It has no locking or serialization, and is basically not thread-safe:

```fsharp
type UnserializedLogger() = 
    // interface
    member this.Log msg = slowConsoleWrite msg

// test in isolation
let unserializedLogger = UnserializedLogger()
unserializedLogger.Log "hello"
```

Now let's combine all these into a real example. We will create five child tasks and run them in parallel, all trying to write to the slow console.

```fsharp
let unserializedExample = 
    let logger = new UnserializedLogger()
    [1..5]
        |> List.map (fun i -> makeTask logger.Log i)
        |> Async.Parallel
        |> Async.RunSynchronously
        |> ignore
```

Ouch! The output is very garbled!

### Serialized IO with messages ###

So what happens when we replace `UnserializedLogger` with a `SerializedLogger` class that encapsulates a message queue. 

The agent inside `SerializedLogger` simply reads a message from its input queue and writes it to the slow console.  Again there is no code dealing with concurrency and no locks are used.

```fsharp
type SerializedLogger() = 

    // create the mailbox processor
    let agent = MailboxProcessor.Start(fun inbox -> 

        // the message processing function
        let rec messageLoop () = async{

            // read a message
            let! msg = inbox.Receive()

            // write it to the log
            slowConsoleWrite msg

            // loop to top
            return! messageLoop ()
            }

        // start the loop
        messageLoop ()
        )

    // public interface
    member this.Log msg = agent.Post msg

// test in isolation
let serializedLogger = SerializedLogger()
serializedLogger.Log "hello"
```

So now we can repeat the earlier unserialized example but using the `SerializedLogger` instead. Again, we create five child tasks and run them in parallel:

```fsharp
let serializedExample = 
    let logger = new SerializedLogger()
    [1..5]
        |> List.map (fun i -> makeTask logger.Log i)
        |> Async.Parallel
        |> Async.RunSynchronously
        |> ignore
```		

What a difference! This time the output is perfect.  


## Summary ##

There is much more to say about this message based approach. In a future series, I hope to go into much more detail, including discussion of topics such as:

* alternative implementations of message queues with MSMQ and TPL Dataflow.
* cancellation and out of band messages.
* error handling and retries, and handling exceptions in general.
* how to scale up and down by creating or removing child agents.
* avoiding buffer overruns and detecting starvation or inactivity.
