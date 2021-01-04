(* ===================================
Code from my series of posts "Six approaches to dependency injection"
=================================== *)

open System


(*
## The requirements

Say that we have some kind of web app with users, and each user has a "profile" with their name, email, preferences, etc.
A use-case for updating their profile might be something like this:

1. Receive a new profile (parsed from a JSON request, say)
2. Read the user's current profile from the database
3. If the profile has changed, update the user's profile in the database
4. If the email has changed, send a verification email message to the user's new email 

We will also add a little bit of logging into the mix.

*)

(* ======================================================================
Common types used thoughout the examples
====================================================================== *)


module Domain =

    type UserId = UserId of int
    type UserName = string
    type EmailAddress = EmailAddress of string

    type Profile = {
        UserId : UserId 
        Name : UserName 
        EmailAddress : EmailAddress 
    }

    type EmailMessage = {
        To : EmailAddress
        Body : string
        }
  

module Infrastructure =
    open Domain 

    type ILogger =
        abstract Info : string -> unit
        abstract Error : string -> unit

    type InfrastructureError =
        | DbError of string
        | SmtpError of string

    type DbConnection = DbConnection of unit // dummy definition

    type IDbService = 
        abstract NewDbConnection : 
            unit -> DbConnection 
        abstract QueryProfile : 
            DbConnection -> UserId -> Async<Result<Profile,InfrastructureError>>
        abstract UpdateProfile : 
            DbConnection -> Profile -> Async<Result<unit,InfrastructureError>>

    type SmtpCredentials = SmtpCredentials of unit // dummy definition

    type IEmailService = 
        abstract SendChangeNotification : 
            SmtpCredentials -> EmailMessage -> Async<Result<unit,InfrastructureError>>

    let globalLogger = {new ILogger with
        member __.Info str = printfn "INFO %s" str
        member __.Error str = printfn "ERROR %s" str
        }

    let defaultDbService = {new IDbService with
        member __.NewDbConnection() = DbConnection()

        member __.QueryProfile dbConnection (UserId userId) = 
            printfn "DB.QueryProfile: %A" userId 
            async {
                let profile = {
                    UserId = UserId userId
                    Name = ""
                    EmailAddress = EmailAddress (sprintf "user%i@example.com" userId)
                    }
                return Ok profile
            }

        member __.UpdateProfile dbConnection profileDto = 
            printfn "DB.UpdateProfile: %A" profileDto
            async {
                return Ok ()
                }
        }

    let defaultSmtpCredentials = SmtpCredentials()  // dummy

    let defaultEmailService = {new IEmailService with
        member __.SendChangeNotification smtpCreditials emailMessage = 
            printfn "Email.SendEmailChangedNotification: %A" emailMessage 
            async {
                return Ok ()
                }
        }

(* ======================================================================
Result library
====================================================================== *)

type AsyncResult<'Success,'Failure> = 
    Async<Result<'Success,'Failure>>

module AsyncResult =
    /// Lift a function to AsyncResult
    let map f (xAS:AsyncResult<_,_>) : AsyncResult<_,_> =
        async {
            let! x = xAS
            return (Result.map f) x
            }

    /// Lift a value to AsyncResult
    let retn x : AsyncResult<_,_> =
        x |> Result.Ok |> async.Return

    let bind (f: 'a -> AsyncResult<'b,'c>) (xAsyncResult : AsyncResult<_, _>) :AsyncResult<_,_> = async {
        let! xResult = xAsyncResult
        match xResult with
        | Ok x -> return! f x
        | Error err -> return (Error err)
        }

[<AutoOpen>]
module AsyncResultComputationExpression =
   
    type AsyncResultBuilder() =
        member __.Return(x) = AsyncResult.retn x
        member __.Bind(x, f) = AsyncResult.bind f x
        member __.ReturnFrom(x) = x
        member this.Zero() = this.Return ()
        member this.Combine (a:AsyncResult<_,_>, b:unit->AsyncResult<_,_>) = AsyncResult.bind b a
        member this.Combine (a:AsyncResult<_,_>, b:AsyncResult<_,_>) = AsyncResult.bind (fun () -> b) a
        member __.Delay(f) = async.Delay(f) 

    let asyncResult = AsyncResultBuilder()

(* ======================================================================
Approach 1. Dependency Retention
====================================================================== *)


module test =
    let step1() = async {return 1}
    let step2 x = async {return ()}

    let y = async {
        let! x =  step1()

        if x > 0 then
           do! step2 x

        }


module DependencyRetention =
    open Domain
    open Infrastructure

    // val updateCustomerProfile : newProfile:Domain.Profile -> AsyncResult<unit,Infrastructure.InfrastructureError>
    let updateCustomerProfile (newProfile:Profile) :AsyncResult<unit,InfrastructureError> =
        let dbConnection = defaultDbService.NewDbConnection()
        let smtpCredentials = defaultSmtpCredentials
        asyncResult {
            let! currentProfile = defaultDbService.QueryProfile dbConnection newProfile.UserId

            if currentProfile <> newProfile then
                globalLogger.Info("Updating Profile")
                do! defaultDbService.UpdateProfile dbConnection newProfile

            if currentProfile.EmailAddress <> newProfile.EmailAddress then
                let emailMessage = {
                    To = newProfile.EmailAddress
                    Body = "Please verify your email"
                    }
                globalLogger.Info("Sending email")
                do! defaultEmailService.SendChangeNotification smtpCredentials emailMessage 
            }
        

(* ======================================================================
Approach 2. Dependency Rejection
====================================================================== *)

module DependencyRejection =
    open Domain

    // -----------------------------------------------
    // Pure core
    // -----------------------------------------------

    module Pure = 
        let globalLogger = Infrastructure.globalLogger

        type Decision =
            | NoAction
            | UpdateProfileOnly of Profile
            | UpdateProfileAndNotify of Profile * EmailMessage

        // pure code, which is easy to test
        // (assuming globalLogger is allowed)
        let updateCustomerProfile (newProfile:Profile) (currentProfile:Profile) =
            if currentProfile <> newProfile then
                globalLogger.Info("Updating Profile")
                if currentProfile.EmailAddress <> newProfile.EmailAddress then
                    let emailMessage = {
                        To = newProfile.EmailAddress
                        Body = "Please verify your email"
                        }
                    globalLogger.Info("Sending email")
                    UpdateProfileAndNotify (newProfile, emailMessage)
                else
                    UpdateProfileOnly newProfile
            else
                NoAction

    // -----------------------------------------------
    // Impure shell
    // -----------------------------------------------

    module Shell =
        open Infrastructure
        open Pure

        // infrastructure services are hard-coded inline
        let updateCustomerProfile (newProfile:Profile) :AsyncResult<unit,InfrastructureError> =
            let dbConnection = defaultDbService.NewDbConnection()
            let smtpCredentials = defaultSmtpCredentials
            asyncResult {
                // ----------- impure ----------------
                let! currentProfile = defaultDbService.QueryProfile dbConnection newProfile.UserId

                // ----------- pure ----------------
                let decision = Pure.updateCustomerProfile newProfile currentProfile 

                // ----------- impure ----------------
                match decision with
                | NoAction ->
                    ()
                | UpdateProfileOnly profile ->
                    do! defaultDbService.UpdateProfile dbConnection profile 
                | UpdateProfileAndNotify (profile,emailMessage) ->
                    do! defaultDbService.UpdateProfile dbConnection profile 
                    do! defaultEmailService.SendChangeNotification smtpCredentials emailMessage
                }


(* ======================================================================
Approach 3. Dependency Parameterization
====================================================================== *)

module DependencyParameterization =
    open Domain

    // -----------------------------------------------
    // Pure core
    // -----------------------------------------------

    module Pure =
        type ILogger = Infrastructure.ILogger
    
        type Decision =
            | NoAction
            | UpdateProfileOnly of Profile
            | UpdateProfileAndNotify of Profile * EmailMessage

        let updateCustomerProfile (logger:ILogger) (newProfile:Profile) (currentProfile:Profile) =
            if currentProfile <> newProfile then
                logger.Info("Updating Profile")
                if currentProfile.EmailAddress <> newProfile.EmailAddress then
                    let emailMessage = {
                        To = newProfile.EmailAddress
                        Body = "Please verify your email"
                        }
                    logger.Info("Sending email")
                    UpdateProfileAndNotify (newProfile, emailMessage)
                else
                    UpdateProfileOnly newProfile
            else
                NoAction

    // -----------------------------------------------
    // Impure shell
    // -----------------------------------------------

    module Shell =
        open Infrastructure
        open Pure

        type IServices = {
            Logger : ILogger
            DbService : IDbService
            EmailService : IEmailService
            }

        // Uses infrastructure but all interfaces are passed in as parameters
        // This is easy to mock, or to change infrastructure implementation
        let updateCustomerProfile (services:IServices) (newProfile:Profile) :AsyncResult<unit,InfrastructureError> =
            let dbConnection = services.DbService.NewDbConnection()
            let smtpCredentials = defaultSmtpCredentials
            let logger = services.Logger

            asyncResult {
                // ----------- Impure ----------------
                let! currentProfile = services.DbService.QueryProfile dbConnection newProfile.UserId

                // ----------- pure ----------------
                let decision = Pure.updateCustomerProfile logger newProfile currentProfile 

                // ----------- Impure ----------------
                match decision with
                | NoAction ->
                    ()
                | UpdateProfileOnly profile ->
                    do! services.DbService.UpdateProfile dbConnection profile 
                | UpdateProfileAndNotify (profile,emailMessage) ->
                    do! services.DbService.UpdateProfile dbConnection profile 
                    do! services.EmailService.SendChangeNotification smtpCredentials emailMessage
                }
        

        /// Top-level "composition root"
        let updateCustomerProfileApi (newProfile:Profile) =
            let services = {
                Logger = globalLogger
                DbService = defaultDbService
                EmailService = defaultEmailService
                }
            updateCustomerProfile services newProfile

(* ======================================================================
Approach 4. Dependency Injection -- OO Style
====================================================================== *)

module DependencyInjection =
    open Domain

    // -----------------------------------------------
    // Pure core
    // -----------------------------------------------

    module Pure =

        type ILogger = Infrastructure.ILogger

        type Decision =
            | NoAction
            | UpdateProfileOnly of Profile
            | UpdateProfileAndNotify of Profile * EmailMessage

        let updateCustomerProfile (logger:ILogger) (newProfile:Profile) (currentProfile:Profile) =
                if currentProfile <> newProfile then
                    logger.Info("Updating Profile")
                    if currentProfile.EmailAddress <> newProfile.EmailAddress then
                        let emailMessage = {
                            To = newProfile.EmailAddress
                            Body = "Please verify your email"
                            }
                        logger.Info("Sending email")
                        UpdateProfileAndNotify (newProfile, emailMessage)
                    else
                        UpdateProfileOnly newProfile
                else
                    NoAction

    // -----------------------------------------------
    // Impure shell
    // -----------------------------------------------

    module Shell =
        
        open Infrastructure
        open Pure

        type IServices = {
            Logger : ILogger
            DbService : IDbService
            EmailService : IEmailService
            }

        // define a class with a constructor that accepts the dependencies
        type MyWorkflow (services:IServices) =

            member this.UpdateCustomerProfile (newProfile:Profile) =
                let dbConnection = services.DbService.NewDbConnection()
                let smtpCredentials = defaultSmtpCredentials
                let logger = services.Logger

                asyncResult {
                    // ----------- Impure ----------------
                    let! currentProfile = services.DbService.QueryProfile dbConnection newProfile.UserId

                    // ----------- pure ----------------
                    let decision = Pure.updateCustomerProfile logger newProfile currentProfile 

                    // ----------- Impure ----------------
                    match decision with
                    | NoAction ->
                        ()
                    | UpdateProfileOnly profile ->
                        do! services.DbService.UpdateProfile dbConnection profile 
                    | UpdateProfileAndNotify (profile,emailMessage) ->
                        do! services.DbService.UpdateProfile dbConnection profile 
                        do! services.EmailService.SendChangeNotification smtpCredentials emailMessage
                    }
        

        /// Top-level "composition root"
        let updateCustomerProfileApi (newProfile:Profile) =
            let services = {
                Logger = globalLogger
                DbService = defaultDbService
                EmailService = defaultEmailService
                }
            let myWorkflow = MyWorkflow(services)
            myWorkflow.UpdateCustomerProfile newProfile


(* ======================================================================
Approach 4b. Dependency Injection -- Reader style
====================================================================== *)

type Reader<'env,'a> = Reader of action:('env -> 'a)

module Reader =
    /// Run a Reader with a given environment
    let run env (Reader action)  = 
        action env  // simply call the inner function

    /// Create a Reader which returns the environment itself
    let ask = Reader id 

    /// Map a function over a Reader 
    let map f reader = 
        Reader (fun env -> f (run env reader))

    /// flatMap a function over a Reader 
    let bind f reader =
        let newAction env =
            let x = run env reader 
            run env (f x)
        Reader newAction

    /// Transform a Reader's environment.
    /// Known as `withReader` in Haskell
    let withEnv (f:'env2->'env1) reader = 
        Reader (fun env' -> (run (f env') reader))


[<AutoOpen>]
module ReaderCE =
    type ReaderBuilder() =
        member __.Return(x) = Reader (fun _ -> x)
        member __.Bind(x,f) = Reader.bind f x
        member __.Zero() = Reader (fun _ -> ())
        member this.Combine (a,b) = Reader.bind b a
        

    // the builder instance
    let reader = ReaderBuilder()

module ReaderInjection =
    open Domain

    // -----------------------------------------------
    // Pure core
    // -----------------------------------------------

    module Pure =

        type ILogger = Infrastructure.ILogger

        type Decision =
            | NoAction
            | UpdateProfileOnly of Profile
            | UpdateProfileAndNotify of Profile * EmailMessage

        let updateCustomerProfile (newProfile:Profile) (currentProfile:Profile) :Reader<ILogger,Decision> = 
            reader {
                let! (logger:ILogger) = Reader.ask

                let decision = 
                    if currentProfile <> newProfile then
                        logger.Info("Updating Profile")
                        if currentProfile.EmailAddress <> newProfile.EmailAddress then
                            let emailMessage = {
                                To = newProfile.EmailAddress
                                Body = "Please verify your email"
                                }
                            logger.Info("Sending email")
                            UpdateProfileAndNotify (newProfile, emailMessage)
                        else
                            UpdateProfileOnly newProfile
                    else
                        NoAction

                return decision
            }

    // -----------------------------------------------
    // Impure shell WITHOUT using Reader for top-level IO
    // -----------------------------------------------

    module Shell_v1 =
    
        open Infrastructure
        open Pure

        type IServices = {
            Logger : ILogger
            DbService : IDbService
            EmailService : IEmailService
            }

        // Infrastructure services are passed in as a parameter
        let updateCustomerProfile (services:IServices) (newProfile:Profile) :AsyncResult<unit,InfrastructureError> =
            let dbConnection = services.DbService.NewDbConnection()
            let smtpCredentials = defaultSmtpCredentials
            let logger = services.Logger

            asyncResult {
                // ----------- impure ----------------
                let! currentProfile = services.DbService.QueryProfile dbConnection newProfile.UserId

                // ----------- pure ----------------
                let decision = 
                    Pure.updateCustomerProfile newProfile currentProfile 
                    |> Reader.run logger

                // ----------- impure ----------------
                match decision with
                | NoAction ->
                    ()
                | UpdateProfileOnly profile ->
                    do! services.DbService.UpdateProfile dbConnection profile 
                | UpdateProfileAndNotify (profile,emailMessage) ->
                    do! services.DbService.UpdateProfile dbConnection profile 
                    do! services.EmailService.SendChangeNotification smtpCredentials emailMessage
                }


        /// Top-level "composition root"
        let updateCustomerProfileApi (newProfile:Profile) =
            let services = {
                Logger = globalLogger
                DbService = defaultDbService
                EmailService = defaultEmailService
                }
            updateCustomerProfile services newProfile

    // -----------------------------------------------
    // Impure shell using Reader for top-level I/O
    // -----------------------------------------------

    module Shell_v2 =
        
        open Infrastructure
        open Pure

        type IServices = {
            Logger : ILogger
            DbService : IDbService
            EmailService : IEmailService
            }

        // first step in our mini-app
        let getProfile (userId:UserId) :Reader<IServices, AsyncResult<Profile,InfrastructureError>> = 
            reader {
                let! (services:IServices) = Reader.ask
                let dbConnection = services.DbService.NewDbConnection()
                return services.DbService.QueryProfile dbConnection userId
            }

        // last step in our mini-app
        let handleDecision (decision:Decision) :Reader<IServices, AsyncResult<unit,InfrastructureError>> = 
            reader {
                let! (services:IServices) = Reader.ask
                let dbConnection = services.DbService.NewDbConnection()
                let smtpCredentials = defaultSmtpCredentials
                let action = asyncResult {
                    match decision with
                    | NoAction ->
                        ()
                    | UpdateProfileOnly profile ->
                        do! services.DbService.UpdateProfile dbConnection profile 
                    | UpdateProfileAndNotify (profile,emailMessage) ->
                        do! services.DbService.UpdateProfile dbConnection profile 
                        do! services.EmailService.SendChangeNotification smtpCredentials emailMessage
                    }
                return action
            }

        // Infrastructure services are passed in via a Reader
        let updateCustomerProfile (newProfile:Profile) = 
            reader {
                let! (services:IServices) = Reader.ask
                let getLogger services = services.Logger

                return asyncResult {
                    // ----------- impure ----------------
                    let! currentProfile = 
                        getProfile newProfile.UserId   
                        |> Reader.run services

                    // ----------- pure ----------------
                    let decision = 
                        Pure.updateCustomerProfile newProfile currentProfile 
                        |> Reader.withEnv getLogger
                        |> Reader.run services
                
                    // ----------- impure ----------------
                    do! (handleDecision decision) |> Reader.run services   
                    }
            }

        /// Top-level "composition root"
        let updateCustomerProfileApi (newProfile:Profile) =
            let services = {
                Logger = globalLogger
                DbService = defaultDbService
                EmailService = defaultEmailService
                }
            
            (updateCustomerProfile newProfile)
            |> Reader.run services


(* ======================================================================
Approach 5. Dependency Interpretation
====================================================================== *)

//----------------------------------------
// A generic program that does not know about specific instructions
//----------------------------------------

module GenericProgram =

    // 1. Define a instruction interface that contains a "map" 
    type IInstruction<'a> =
        abstract member Map : ('a->'b) -> IInstruction<'b> 

    // 2, Use the interface in the Program type
    type Program<'a> =
        | Instruction of IInstruction<Program<'a>>
        | Stop of 'a

    // 3. Define the corresponding "bind" 
    module Program =
        let rec bind f program = 
            match program with
            | Instruction inst -> 
                inst.Map (bind f) |> Instruction 
            | Stop x -> f x

    // 4. Define the computation expression
    type ProgramBuilder() =
        member __.Return(x) = Stop x 
        member __.ReturnFrom(x) = x 
        member __.Bind(x,f) = Program.bind f x
        member __.Zero() = Stop ()
        member this.Combine (a:Program<_>, b:unit->Program<_>) = Program.bind b a
        member this.Combine (a:Program<_>, b:Program<_>) = Program.bind (fun () -> b) a

    // and the builder instance
    let program = ProgramBuilder()

//----------------------------------------
// A specific program based on the common requirements
//----------------------------------------

module DependencyInterpretation =

    open Domain
    open GenericProgram

    // -----------------------------------------------
    // Instructions used in the pure core
    // -----------------------------------------------

    module PureInstructions =

        type LoggerInstruction<'a> =
            | LogInfo of string  * next:(unit -> 'a)
            | LogError of string * next:(unit -> 'a)
            interface IInstruction<'a> with
                member this.Map f  = 
                    match this with
                    | LogInfo (str,next) -> 
                        LogInfo (str,next >> f)
                    | LogError (str,next) -> 
                        LogError (str,next >> f)
                    :> IInstruction<_> 

        // helpers to use within the computation expression
        let logInfo str = Instruction (LogInfo (str,Stop))
        let logError str = Instruction (LogError (str,Stop))

    // -----------------------------------------------
    // Pure core
    // -----------------------------------------------

    module Pure =
        
        open PureInstructions

        type Decision =
            | NoAction
            | UpdateProfileOnly of Profile
            | UpdateProfileAndNotify of Profile * EmailMessage

        let updateCustomerProfile (newProfile:Profile) (currentProfile:Profile) :Program<Decision> = 
            if currentProfile <> newProfile then program {
                do! logInfo("Updating Profile")
                if currentProfile.EmailAddress <> newProfile.EmailAddress then 
                    let emailMessage = {
                        To = newProfile.EmailAddress
                        Body = "Please verify your email"
                        }
                    do! logInfo("Sending email")
                    return UpdateProfileAndNotify (newProfile, emailMessage) 
                else 
                    return UpdateProfileOnly newProfile
                }
            else program {
                return NoAction
                }
                

    // -----------------------------------------------
    // Instructions used in the impure shell
    // -----------------------------------------------

    module ImpureInstructions =

        // 1. Define the set of instructions we want to support, and their map
        type DbInstruction<'a> =
            | QueryProfile of UserId * next:(Profile -> 'a)
            | UpdateProfile of Profile * next:(unit -> 'a)
            interface IInstruction<'a> with
                member this.Map f  = 
                    match this with
                    | QueryProfile (userId,next) -> 
                        QueryProfile (userId,next >> f)
                    | UpdateProfile (profile,next) -> 
                        UpdateProfile (profile, next >> f)
                    :> IInstruction<_> 

        type EmailInstruction<'a> =
            | SendChangeNotification of EmailMessage * next:(unit-> 'a)
            interface IInstruction<'a> with
                member this.Map f  = 
                    match this with
                    | SendChangeNotification (message,next) -> 
                        SendChangeNotification (message,next >> f)
                    :> IInstruction<_> 

        // helpers to use within the computation expression
        let queryProfile userId = Instruction (QueryProfile(userId,Stop))
        let updateProfile profile = Instruction (UpdateProfile(profile,Stop))
        let sendChangeNotification message = Instruction (SendChangeNotification(message,Stop))

    // -----------------------------------------------
    // Impure shell
    // -----------------------------------------------

    module Shell =
        
        open Pure
        open ImpureInstructions

        let getProfile (userId:UserId) :Program<Profile> = 
            program {
                return! queryProfile userId
            }

        let handleDecision (decision:Decision) :Program<unit> = 
            match decision with
            | NoAction ->
                program.Zero()
            | UpdateProfileOnly profile ->
                updateProfile profile 
            | UpdateProfileAndNotify (profile,emailMessage) ->
                program {
                do! updateProfile profile 
                do! sendChangeNotification emailMessage
                }

        let updateCustomerProfile (newProfile:Profile) = 
            program {
                let! currentProfile = getProfile newProfile.UserId 
                let! decision = Pure.updateCustomerProfile newProfile currentProfile 
                do! handleDecision decision
            }

    // -----------------------------------------------
    // The interpreter
    // -----------------------------------------------

    module Interpreter =

        open PureInstructions
        open ImpureInstructions
        open Infrastructure

        // modular interpreter for LoggerInstruction
        let interpretLogger interpret inst =
            match inst with
            | LogInfo (str, next) -> 
                globalLogger.Info str
                let newProgramAS = next() |> asyncResult.Return
                interpret newProgramAS 
            | LogError (str, next) -> 
                globalLogger.Error str
                let newProgramAS = next() |> asyncResult.Return
                interpret newProgramAS 

        // modular interpreter for DbInstruction
        let interpretDbInstruction (dbConnection:DbConnection) interpret inst =
            match inst with
            | QueryProfile (userId, next) -> 
                let profileAS = defaultDbService.QueryProfile dbConnection userId
                let newProgramAS = (AsyncResult.map next) profileAS
                interpret newProgramAS  // returns an :AsyncResult<'a,InfrastructureError>
            | UpdateProfile (profile, next) -> 
                let unitAS = defaultDbService.UpdateProfile dbConnection profile
                let newProgramAS = (AsyncResult.map next) unitAS 
                interpret newProgramAS 

        // modular interpreter for EmailInstruction
        let interpretEmailInstruction (smtpCredentials:SmtpCredentials) interpret inst =
            match inst with
            | SendChangeNotification (message, next) -> 
                let unitAS = defaultEmailService.SendChangeNotification smtpCredentials message
                let newProgramAS = (AsyncResult.map next) unitAS 
                interpret newProgramAS 

        let interpret program =
            // 1. get the extra parameters and partially apply them to make all the interpreters 
            // have a consistent shape
            let smtpCredentials = defaultSmtpCredentials
            let dbConnection = defaultDbService.NewDbConnection()
            let interpretDbInstruction' = interpretDbInstruction dbConnection 
            let interpretEmailInstruction' = interpretEmailInstruction smtpCredentials 

            // 2. define a recursive loop function. It has signature:
            //     AsyncResult<Program<'a>,InfrastructureError>) -> AsyncResult<'a,InfrastructureError> 
            let rec loop programAS = 
                asyncResult {
                    let! program = programAS 
                    return! 
                        match program with
                        | Instruction inst ->
                            match inst with
                            | :? LoggerInstruction<Program<_>> as inst -> interpretLogger loop inst
                            | :? DbInstruction<Program<_>> as inst -> interpretDbInstruction' loop inst
                            | :? EmailInstruction<Program<_>> as inst -> interpretEmailInstruction' loop inst
                            | _ -> failwithf "unknown instruction type %O" (inst.GetType())
                        | Stop value -> 
                            value |> asyncResult.Return
                    }

            // 3. start the loop
            let initialProgram = program |> asyncResult.Return
            loop initialProgram 


        /// Top-level "composition root"
        let updateCustomerProfileApi (newProfile:Profile) =
            Shell.updateCustomerProfile newProfile
            |> interpret
            |> Async.RunSynchronously
            


