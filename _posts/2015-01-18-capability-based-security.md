---
layout: post
title: "A functional approach to authorization"
description: "Capability based security and more"
seriesId: "A functional approach to authorization"
seriesOrder: 1
categories: []
---

*UPDATE: [Slides and video from my talk on this topic](/cap/)*

In this series of posts, I'll look at how you might handle the common security challenge of authorization.
That is, how can you ensure that clients of your code can only do what you want them to do?

This series will sketch out two different approaches, first using an approach called *capability based security*, and second using statically checked types to emulate access tokens.

Interestingly, both approaches tend to produce a cleaner, more modular design as a side effect, which is why I like them! 

Before I start, I must mention a major caveat. In a .NET environment, you can generally use reflection to bypass compile-time checking,
so the approaches shown here are not about preventing truly malicious attacks so much as 
helping you create designs that reduce *unintentional* security vulnerabilities.

Finally, I'm no expert on security -- I'm just putting down some of my own thoughts and suggestions.
This post is certainly not meant to substitute for a proper full-fledged security design, nor is it a serious study of security practices.
If you want to know more, there are links to further reading at the bottom of the post.

## Part 1: A configuration example

First, let's start with a simple scenario: 

* You have a configuration option that can be set by one part of the the code. Let's say it is a boolean called `DontShowThisMessageAgain`.
* We have a component of the application (the UI say) that wants to set this option.
* In addition, we're also going to assume that the component was written by a malicious developer and is going to try to cause trouble if possible.

So, how should we expose this configuration setting to a potentially malicious caller?  

**Attempt 1: Give the caller the name of the configuration file**

Let's start with a really bad idea. We'll just provide the name of the config file to the caller, and let them change the file themselves.

Here's how this might be written in C# pseudocode:

```csharp
interface IConfiguration
{   
    string GetConfigFilename();
}
```     

and the caller code would be 

```csharp
var filename = config.GetConfigFilename();
// open file
// write new config
// close file
```     

Obviously, this is not good!  In order for this to work, we have to give the caller the ability to write to any file on the filesystem, and
then a malicious caller could delete or corrupt all sorts of things. 

You could avoid this to some extent by having strict permissions on the file system, but we're still giving way too much control to the caller.

**Attempt 2: Give the caller a TextWriter**

Ok, so let's open the file ourselves and just give the caller the opened file stream as a `TextWriter`. That way the caller doesn't need permission to access the file system at all.

But of course, a malicious caller could still corrupt the config file by writing garbage to the file. Again, we're giving way too much control to the caller.

**Attempt 3: Give the caller a key/value interface**

Let's lock this down by providing the caller an interface that forces them to treat the config file as a key/value store, like this:

```csharp
interface IConfiguration
{   
    void SetConfig(string key, string value);
}
```     

The caller code is then something like this:

```csharp
config.SetConfig("DontShowThisMessageAgain", "True");
```     

That's much better, but because it is a stringly-typed interface, a malicious caller could still corrupt the configuration by setting the value to a non-boolean which would not parse.
They could also corrupt all the other configuration keys if they wanted to.

**Attempt 4: Give the caller a domain-centric interface**

Ok, so rather than having a generic config interface, let's provide an interface that provides specific methods for each configuration setting. 

```csharp
enum MessageFlag {
   ShowThisMessageAgain,
   DontShowThisMessageAgain
   }

interface IConfiguration
{   
    void SetMessageFlag(MessageFlag value);
    void SetConnectionString(ConnectionString value);
    void SetBackgroundColor(Color value);
}
```     

Now the caller can't possibly corrupt the config, because each option is statically typed.  

But we still have a problem! What's to stop a malicious caller changing the connection string when they were only supposed to change the message flag?

**Attempt 5: Give the caller only the interface they need**

Ok, so let's define a new interface that contains *only* the methods the caller should have access to, with all the other methods hidden. 

```csharp
interface IWarningMessageConfiguration
{   
    void SetMessageFlag(MessageFlag value);
}
```     

That's about as locked down as we can get!  The caller can *only* do the thing we allow them to do.

In other words, we have just created a design using the [Principle Of Least Authority](https://en.wikipedia.org/wiki/Principle_of_least_privilege), normally abbreviated to "POLA".

## Security as good design

What's interesting about this approach is that it exactly parallels what you would do for good design *anyway*, regardless of a malicious caller.

Here's how I might think about designing this, basing my decisions only on core design principles such information hiding and decoupling.

* If we give the caller a filename, we would be limiting ourselves to file-based config files.
  By giving the caller a TextWriter, we can make the design more mockable.
* But if we give the caller a TextWriter, we are exposing a specific storage format (XML, JSON, etc) and are also limiting ourselves to text-based storage.
  By giving the caller a generic KeyValue store, we hide the format and make the implementation choices more flexible.
* But if we give the caller a generic KeyValue store using strings, we are still exposing ourselves to bugs where the value is not a boolean, and we'd have to write validation and tests for that.
  If we use a statically typed interface instead, we don't have to write any corruption checking code.
* But if we give the caller an interface with too many methods, we are not following the *[Interface Segregation Principle](https://en.wikipedia.org/wiki/Interface_segregation_principle)*.
  Hence, we should reduce the number of available methods to the absolute minimum needed by the caller.
  
Working through a thought process like this, using good design practices only, we end up with exactly the same result as if we had been worried about security!

That is: designing the most minimal interface that the caller needs will both avoid accidental complexity (good design) and increase security (POLA).

Of course, we don't normally have to deal with malicious callers, but we should treat ourselves, as developers, as unintentionally malicious.
For example, if there is a extra method in the interface, it might well be used in a different context, which then increases
coupling between the two contexts and makes refactoring harder.

So, here's a tip: **design for malicious callers and you will probably end up with more modular code!**

## Introducing capability-based security

What we have done above is gradually reduce the surface area to the caller so that by the final design, the caller can only do exactly one thing.

That "one thing" is a "capability". The caller has a capability to set the message flag, and that's all.

["Capability-based" security](https://en.wikipedia.org/wiki/Capability-based_security) is a security model that is based on this idea:

* The system provides "capabilities" to clients (in our case, via an implementation of an interface).
* These capabilities encapsulate any access rights that are needed. For example, the very fact that I have access to an implementation of the interface means that I can set that flag.
  If I did not have permission to set that flag, I would have not been given the capability (interface) in the first place. (I'll talk more about authorization in the next post).
* Finally, the capabilities can be passed around. For example, I can acquire the capability at startup and then later pass it to the UI layer which
  can use it as needed.

In other words, we have a "just-in-time" rather than a "just-in-case" model; we pass in the minimal amount of authority as and when needed,
rather than having excess "ambient" authority available globally to everyone.
  
The capability-based model is often focused on operating systems, but it can be mapped to programming languages very nicely,
where it is called [the object-capability model](https://en.wikipedia.org/wiki/Object-capability_model).
  
I hope to demonstrate in this post that by using a capability-based approach in your code, you can create better designed and more robust code.
In addition, potential security errors will be detectable at *compile-time* rather than at run-time.
  
As I mentioned above, if your app is trusted, you can always use .NET reflection to "forge" capabilities that you are not entitled to.
So, again, the approach shown here is not about preventing truly malicious attacks so much as it about
creating a more robust design that reduces *unintentional* security vulnerabilities.

<a id="authority"></a>

## Authority vs. permission

A capability-based security model tends to use the term "authority" rather than "permission". There is a distinction between the two:

* In an *authority* based system, once I have been granted authority to do something, I can pass some or all of that authority to others, add additional constraints of my own, and so on.
* In a *permission* based system, I can ask for permission to do something, but I cannot pass that around to others. 

It might seem that an authority based system is more open and "dangerous" than a permission based system. But in a permission based system, if others have access to me and I cooperate with them,
I can act as proxy for anything they want to do so, so third-parties can *still* get authority indirectly.
Permissions don't really make things more secure -- an attacker just has to use a more convoluted approach.

Here's a concrete example. Let's say Alice trusts me to drive her car, and she is willing to let me borrow it, but she doesn't trust Bob.
If I'm friends with Bob, I can let Bob drive the car anyway when Alice is not looking. So if Alice trusts me, she also implicitly trusts anyone that I trust.
An authority-based system just makes this explicit.  Alice giving me her car keys is giving me the "capability" to drive her car, with full knowledge that I might give the car keys to someone else.

Of course, when I act as a proxy in a permission based system, I can stop cooperating with the third-party if I want to,
at which point the third-party loses their access. 

The equivalent of that in an authority based system is "revokable authority", which we will see an example of later.
In the car key analogy, this might be like having car keys that self-destruct on demand!

## Modelling capabilities as functions

An interface with one method can be better realized as a function. So this interface:

```csharp
interface IWarningMessageConfiguration
{   
    void SetMessageFlag(MessageFlag value);
}
```     

becomes just this function:

```csharp
Action<MessageFlag> messageFlagCapability = // get function;
```     

or in F#:

```fsharp
let messageFlagCapability = // get function;
```     

In a functional approach to capability-based security, each capability is represented by a function rather than an interface. 

The nice thing about using functions to represent capabilities is that we can use all the standard functional programming techniques: we can compose them, combine them with combinators, and so on.

## The object-capability model vs. the functional programming model

Many of the other requirements of the object-capability model fit well within a functional programming framework. Here is a comparison table: 

<table class="table table-bordered table-striped">
<tr>
<th>Object-capability model
</th>
<th>Functional programming 
</th>
</tr>
<tr>
<td>No global mutable state is allowed.
</td>
<td>No global mutable state is allowed. 
</td>
</tr>
<tr>
<td>Capabilities are always passed around explicitly from parent to child, or from a sender to a receiver. 
</td>
<td>Functions are values that can be passed as parameters. 
</td>
</tr>
<tr>
<td>Capabilities are never extracted out of the environment ("ambient authority").
</td>
<td>Pure functions have all "dependencies" passed in explicitly. 
</td>
</tr>
<tr>
<td>Capabilities cannot be tampered with.
</td>
<td>Data is immutable.
</td>
</tr>
<tr>
<td>Capabilities cannot be forged or cast to other capabilities.
</td>
<td>In a uncompromising FP language, there is no reflection or casting available (of course, F# is not strict in this way).
</td>
</tr>
<tr>
<td>Capabilities should "fail safe". If a capability cannot be obtained, or doesn't work, we must not allow any progress
on paths that assumed that it was successful.
</td>
<td>In a statically typed language such as F#, we can embed these kinds of control-flow rules into the type system. The use of <code>Option</code> is an example of this.
</td>
</tr>
</table>

You can see that there is quite a lot of overlap.

One of the *unofficial* goals of the object-capability model is **make security user-friendly by making the security invisible**. I think that this is a great idea, 
and by passing capabilities as functions, is quite easily achievable.

It's important to note there is one important aspect in which a capability-based model does *not* overlap with a true functional model.

Capabilities are mostly all about (side) effects -- reading or writing the file system, the network, etc.
A true functional model would try to wrap them somehow (e.g. in a monad).
Personally, using F#, I would generally just allow the side-effects rather than constructing [a more complex framework](http://hackage.haskell.org/package/Capabilities).

But again, as I noted above, the goal of this post is to not to force you into a 100% strict object-capability model, but to borrow some of the same ideas in order to create better designs.

## Getting capabilities

A natural question at this point is: where do these capability functions come from?  

The answer is, some sort of service that can authorize you to have that capability.  In the configuration example, we generally don't do serious authorization, so the configuration
service itself will normally provide the capabilities without checking your identity, role or other claims.

But now I need a capability to access the configuration service. Where does that come from? The buck has to stop somewhere!

In OO designs, there is typically a bootstrap/startup stage where all the dependencies are constructed and an IoC container is configured. In a capability based system,
a [so-called Powerbox](http://c2.com/cgi/wiki?PowerBox) plays a similar role of being the starting point for all authority.

Here's the code for a service that provides configuration capabilities:

```csharp
interface IConfigurationCapabilities
{   
    Action<MessageFlag> SetMessageFlag();
    Action<ConnectionString> SetConnectionString();
    Action<Color> SetBackgroundColor();
}
```     

This code might look very similar to the interface defined earlier, but the difference is that this one will be initialized at startup to return capabilities that are then passed around. 

The actual users of the capabilities will not have access to the configuration system at all, just the capabilities they have been given.
That is, the capability will be injected into the clients in the same way as a one method interface would be injected in an OO model.

Here's some sample C# pseudocode to demonstrate:

* The capability is obtained at startup.
* The capability is injected into the main window (`ApplicationWindow`) via the constructor.
* The `ApplicationWindow` creates a checkbox.
* The event handler for the checkbox calls the capability.

```csharp
// at startup
var messageFlagCapability = 
    configurationCapabilities.SetMessageFlag()
var appWindow = new ApplicationWindow(messageFlagCapability)

// and in the UI class
class ApplicationWindow
{
    // pass in capability in the constructor 
    // just as you would an interface
    ApplicationWindow(Action<MessageFlag> messageFlagCapability)
    {  
        // set fields
    }
    
    // setup the check box and register the "OnCheckboxChecked" handler
    
    // use the capability when the event happens
    void OnCheckboxChecked(CheckBox sender)
    {
        messageFlagCapability(sender.IsChecked)
    }
}
```

## A complete example in F# ##

Here's the code to a complete example in F# (also available as a [gist here](https://gist.github.com/swlaschin/909c5b24bf921e5baa8c#file-capabilitybasedsecurity_configexample-fsx)).

This example consists of a simple window with a main region and some extra buttons.

* If you click in the main area, an annoying dialog pops up with a "don't show this message again" option.
* One of the buttons allows you to change the background color using the system color picker, and store it in the config.
* The other button allows you to reset the "don't show this message again" option back to false.

It's very crude and very ugly -- no UI designers were hurt in the making of it -- but it should demonstrate the main points so far.

![Example application](/assets/img/auth_annoying_popup.png)

### The configuration system

We start with the configuration system. Here's an overview:

* The custom types `MessageFlag`, `ConnectionString`, and `Color` are defined.
* The record type `ConfigurationCapabilities` is defined to hold all the capabilities.
* An in-memory store (`ConfigStore`) is created for the purposes of the demo
* Finally, the `configurationCapabilities` are created using functions that read and write to the `ConfigStore`

```fsharp
module Config =

    type MessageFlag  = ShowThisMessageAgain | DontShowThisMessageAgain
    type ConnectionString = ConnectionString of string
    type Color = System.Drawing.Color

    type ConfigurationCapabilities = {
        GetMessageFlag : unit -> MessageFlag 
        SetMessageFlag : MessageFlag -> unit
        GetBackgroundColor : unit -> Color 
        SetBackgroundColor : Color -> unit
        GetConnectionString  : unit -> ConnectionString 
        SetConnectionString : ConnectionString -> unit
        }

    // a private store for demo purposes
    module private ConfigStore =
        let mutable MessageFlag = ShowThisMessageAgain 
        let mutable BackgroundColor = Color.White
        let mutable ConnectionString = ConnectionString ""

    // public capabilities
    let configurationCapabilities = {
        GetMessageFlag = fun () -> ConfigStore.MessageFlag 
        SetMessageFlag = fun flag -> ConfigStore.MessageFlag <- flag
        GetBackgroundColor = fun () -> ConfigStore.BackgroundColor
        SetBackgroundColor = fun color -> ConfigStore.BackgroundColor <- color
        SetConnectionString = fun _ -> () // ignore
        GetConnectionString = fun () -> ConfigStore.ConnectionString 
        SetConnectionString = fun connStr -> ConfigStore.ConnectionString <- connStr
        }
```

### The annoying popup dialog

Next, we'll create the annoying popup dialog.  This will be triggered whenever you click on the main window,
*unless* the "Don't show this message again" option is checked.

The dialog consists of a label control, the message flag checkbox, and the OK button.

Notice that the `createMessageFlagCheckBox` function, which creates the checkbox control, is passed only the two capabilities it needs -- to get and set the flag.

This requires in turn that the main form creation function (`createForm`) is also passed the capabilities.
These capabilities, and these capabilities *only* are passed in to the form. The capabilities for setting the background color or connection string are *not* passed in,
and thus not available to be (mis)used.

```fsharp
module AnnoyingPopupMessage = 
    open System.Windows.Forms
   
    let createLabel() = 
        new Label(Text="You clicked the main window", Dock=DockStyle.Top)

    let createMessageFlagCheckBox capabilities  = 
        let getFlag,setFlag = capabilities 
        let ctrl= new CheckBox(Text="Don't show this annoying message again", Dock=DockStyle.Bottom)
        ctrl.Checked <- getFlag()
        ctrl.CheckedChanged.Add (fun _ -> ctrl.Checked |> setFlag)
        ctrl   // return new control

    let createOkButton (dialog:Form) = 
        let ctrl= new Button(Text="OK",Dock=DockStyle.Bottom)
        ctrl.Click.Add (fun _ -> dialog.Close())
        ctrl

    let createForm capabilities = 
        let form = new Form(Text="Annoying Popup Message", Width=300, Height=150)
        form.FormBorderStyle <- FormBorderStyle.FixedDialog
        form.StartPosition <- FormStartPosition.CenterParent

        let label = createLabel()
        let messageFlag = createMessageFlagCheckBox capabilities
        let okButton = createOkButton form
        form.Controls.Add label 
        form.Controls.Add messageFlag 
        form.Controls.Add okButton 
        form
```

### The main application window

We can now create a main window for our rather silly "application". It consists of:

* A label control that can be clicked to produce the annoying popup (`createClickMeLabel`)
* A button that brings up a color picking dialog to change the background color (`createChangeBackColorButton`)
* A button that resets the message flag to "show" again (`createResetMessageFlagButton`)

All three of these constructor functions are passed capabilities, but capabilities are different in each case.

* The label control is only passed `getFlag` and `setFlag` capabilities 
* The color picking dialog is only passed `getColor` and `setColor` capabilities 
* The button that resets the message flag is only passed the `setFlag` capability

In the main form (`createMainForm`) the complete set of capabilities are passed in, and they are recombined in various ways as needed for the child controls
(`popupMessageCapabilities`, `colorDialogCapabilities`).

In addition, the capability functions are modified:

* A new "SetColor" capability is created from the existing one, with the addition of changing the form's background as well.
* The flag capabilities are converted from the domain type (`MessageFlag`) to bools that can be used directly with the checkbox.

Here's the code:

```fsharp
module UserInterface = 
    open System.Windows.Forms
    open System.Drawing

    let showPopupMessage capabilities owner = 
        let getFlag,setFlag = capabilities 
        let popupMessage = AnnoyingPopupMessage.createForm (getFlag,setFlag) 
        popupMessage.Owner <- owner 
        popupMessage.ShowDialog() |> ignore // don't care about result

    let showColorDialog capabilities owner = 
        let getColor,setColor = capabilities 
        let dlg = new ColorDialog(Color=getColor())
        let result = dlg.ShowDialog(owner)
        if result = DialogResult.OK then
            dlg.Color |> setColor

    let createClickMeLabel capabilities owner = 
        let getFlag,_ = capabilities 
        let ctrl= new Label(Text="Click me", Dock=DockStyle.Fill, TextAlign=ContentAlignment.MiddleCenter)
        ctrl.Click.Add (fun _ -> 
            if getFlag() then showPopupMessage capabilities owner)
        ctrl  // return new control

    let createChangeBackColorButton capabilities owner = 
        let ctrl= new Button(Text="Change background color", Dock=DockStyle.Bottom)
        ctrl.Click.Add (fun _ -> showColorDialog capabilities owner)
        ctrl

    let createResetMessageFlagButton capabilities = 
        let setFlag = capabilities 
        let ctrl= new Button(Text="Show popup message again", Dock=DockStyle.Bottom)
        ctrl.Click.Add (fun _ -> setFlag Config.ShowThisMessageAgain)
        ctrl

    let createMainForm capabilities = 
        // get the individual component capabilities from the parameter
        let getFlag,setFlag,getColor,setColor = capabilities 

        let form = new Form(Text="Capability example", Width=500, Height=300)
        form.BackColor <- getColor() // update the form from the config

        // transform color capability to change form as well
        let newSetColor color = 
            setColor color           // change config
            form.BackColor <- color  // change form as well  

        // transform flag capabilities from domain type to bool
        let getBoolFlag() = 
            getFlag() = Config.ShowThisMessageAgain 
        let setBoolFlag bool = 
            if bool 
            then setFlag Config.ShowThisMessageAgain 
            else setFlag Config.DontShowThisMessageAgain 

        // set up capabilities for child objects
        let colorDialogCapabilities = getColor,newSetColor 
        let popupMessageCapabilities = getBoolFlag,setBoolFlag

        // setup controls with their different capabilities
        let clickMeLabel = createClickMeLabel popupMessageCapabilities form
        let changeColorButton = createChangeBackColorButton colorDialogCapabilities form
        let resetFlagButton = createResetMessageFlagButton setFlag 

        // add controls
        form.Controls.Add clickMeLabel 
        form.Controls.Add changeColorButton
        form.Controls.Add resetFlagButton 

        form  // return form
```

### The startup code

Finally, the top-level module, here called `Startup`, gets some of the capabilities from the Configuration subsystem, and combines them into a tuple that can be passed
to the main form. The `ConnectionString` capabilities are *not* passed in though, so there is no way the form can accidentally show it to a user or update it.

```fsharp
module Startup = 

    // set up capabilities
    let configCapabilities = Config.configurationCapabilities
    let formCapabilities = 
        configCapabilities.GetMessageFlag, 
        configCapabilities.SetMessageFlag,
        configCapabilities.GetBackgroundColor,
        configCapabilities.SetBackgroundColor

    // start
    let form = UserInterface.createMainForm formCapabilities 
    form.ShowDialog() |> ignore
```

<a id="summary"></a>

## Summary of Part 1

As you can see, this code is very similar to an OO system designed with dependency injection. There is no global access to capabilities, only those passed in from the parent.

![Example 1](/assets/img/auth_1.png)

Of course, the use of functions to parameterize behavior like this is nothing special. It's one of the most fundamental functional programming techniques. 
So this code is not really showing any new ideas, rather it is just demonstrating how a standard functional programming approach can be applied to enforce access paths.

Some common questions at this point:

**Question: This seems like extra work. Why do I need to do this at all?**

If you have a simple system, you certainly don't need to do this. But here's where it might be useful:

* You have a system which uses fine-grained authorization already, and you want to make this more explicit and easier to use in practice.
* You have a system which runs at a high privilege but has strict requirements about leaking data or performing actions in an unauthorized context.

In these situations, I believe that is very important to be *explicit* about what the capabilities are at *all points* in the codebase, not just in the UI layer.
This not only helps with compliance and auditing needs, but also has the practical benefit that it makes the code more modular and easier to maintain.

**Question: What's the difference between this approach and dependency injection?**

Dependency injection and a capability-based model have different goals. Dependency injection is all about decoupling, while capabilities are all about controlling access.
As we have seen, both approaches end up promoting similar designs. 

**Question: What happens if I have hundreds of capabilities that I need to pass around?**

It seems like this should be a problem, but in practice it tends not to be.  For one thing, judicious use of partial application means that
capabilities can be baked in to a function before passing it around, so that child objects are not even aware of them.

Secondly, it is very easy -- just a few lines -- to create simple record types that contain a group of capabilities (as I did with the `ConfigurationCapabilities` type)
and pass those around if needed.

**Question: What's to stop someone accessing global capabilities without following this approach?**

Nothing in C# or F# can stop you accessing global public functions. 
Just like other best practices, such as avoiding global variables, we have to rely on self-discipline (and maybe code reviews) to keep us on the straight and narrow path!

But in the [third part of this series](/posts/capability-based-security-3/), we'll look at a way to prevent access to global functions by using access tokens.

**Question: Aren't these just standard functional programming techniques?**

Yes. I'm not claiming to be doing anything clever here!

**Question: These capability functions have side-effects. What's up with that?**

Yes, these capability functions are not pure. The goal here is not about being pure -- it's about being explicit about the provision of capabilities.

Even if we used a pure `IO` context (e.g. in Haskell) it would not help control access to capabilities.
That is, in the context of security, there's a big difference between the capability to change a password or credit card vs. the capability to change a background color configuration,
even though they are both just "IO" from a computation point of view.

Creating pure capabilities is possible but not very easy to do in F#, so I'm going to keep it out of scope for this post.

**Question: What's your response to what (some person) wrote? And why didn't you cite (some paper)?**

This is a blog post, not an academic paper.  I'm not an expert in this area, but just doing some experiments of my own.

More importantly, as I said earlier, my goal here is very different from security experts --
I'm *not* attempting to develop a ideal security model.
Rather, I'm just trying to encourage some *good design* practices that can help pragmatic developers avoid accidental vulnerabilities in their code.

**I've got more questions...**

Some additional questions are answered at the [end of part 2](/posts/capability-based-security-2/#summary), so read those answers first.
Otherwise please add your question in the comments below, and I'll try to address it.

## Further reading

The ideas on capability-based security here are mostly derived from the work of Mark Miller and Marc Stiegler, and the [erights.org](http://www.erights.org/) website,
although my version is cruder and simpler. 

For a more complete understanding, I suggest you follow up on the links below:

* The Wikipedia articles on [Capability-based security](https://en.wikipedia.org/wiki/Capability-based_security) and [Object-capability model](https://en.wikipedia.org/wiki/Object-capability_model) are a good starting point.
* [What is a Capability, Anyway?](https://webcache.googleusercontent.com/search?q=cache:www.eros-os.org/essays/capintro.html) by Jonathan Shapiro of the EROS project. He also discusses ACL-based security vs. a capability-based model.
* ["The Lazy Programmer's Guide to Secure Computing"](http://www.youtube.com/watch?v=eL5o4PFuxTY), a great video on capability-based security by Marc Stiegler. Don't miss the last 5 mins (starting around 1h:02m:10s)!
* ["Object Capabilities for Security"](https://www.youtube.com/watch?v=EGX2I31OhBE), a good talk by David Wagner.

A lot of work has been done on hardening languages for security and safety. For example 
the [E Language](http://www.erights.org/elang/index.html) and [Mark Miller's thesis on the E Language](http://www.erights.org/talks/thesis/markm-thesis.pdf)(PDF);
the [Joe-E Language](https://en.wikipedia.org/wiki/Joe-E) built on top of Java;
Google's [Caja](https://developers.google.com/caja/) built over JavaScript; 
[Emily](http://www.hpl.hp.com/techreports/2006/HPL-2006-116.html), a capability based language derived from OCaml;
and [Safe Haskell](http://research.microsoft.com/en-us/um/people/simonpj/papers/safe-haskell/safe-haskell.pdf)(PDF).

My approach is not about strict safeness so much as proactively designing to avoid unintentional breaches, and the references above do not focus on very much on design specifically.
The most useful thing I have found is a [section on capability patterns in E](http://www.skyhunter.com/marcs/ewalnut.html#SEC45).

Also, if you like this kind of thing, then head over to LtU where there are a number of discussions, such as [this one](http://lambda-the-ultimate.org/node/1635)
and [this one](http://lambda-the-ultimate.org/node/3930) and [this paper](http://lambda-the-ultimate.org/node/2253).

## Coming up next

In the [next post](/posts/capability-based-security-2/), we'll look at how to constrain capabilities based on claims such as the current user's identity and role.

*NOTE: All the code for this post is available as a [gist here](https://gist.github.com/swlaschin/909c5b24bf921e5baa8c#file-capabilitybasedsecurity_configexample-fsx).*

