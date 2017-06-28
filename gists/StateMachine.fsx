(*
StateMachine.fsx

Related blog post: http://fsharpforfunandprofit.com/posts/calculator-complete-v2/
*)

module StateMachineExample =

    type State = 
        | AState of AStateData
        | BState of BStateData
        | CState
    and AStateData = 
        {something:int}
    and BStateData = 
        {somethingElse:int}

    type InputEvent = 
        | XEvent
        | YEvent of YEventData
        | ZEvent
    and YEventData =
        {eventData:string}

module StateMachineExampleImplemementation =
    open StateMachineExample

(*
    let transition (currentState,inputEvent) =
        match currentState,inputEvent with
        | AState, XEvent -> // new state
        | AState, YEvent -> // new state
        | AState, ZEvent -> // new state
        | BState, XEvent -> // new state
        | BState, YEvent -> // new state
        | CState, XEvent -> // new state
        | CState, ZEvent -> // new state
*)

(*
    let aStateHandler stateData inputEvent = 
        match inputEvent with
        | XEvent -> // new state
        | YEvent _ -> // new state
        | ZEvent -> // new state

    let bStateHandler stateData inputEvent = 
        match inputEvent with
        | XEvent -> // new state
        | YEvent _ -> // new state
        | ZEvent -> // new state

    let cStateHandler inputEvent = 
        match inputEvent with
        | XEvent -> // new state
        | YEvent _ -> // new state
        | ZEvent -> // new state

    let transition (currentState,inputEvent) =
        match currentState with
        | AState stateData -> 
            // new state
            aStateHandler stateData inputEvent 
        | BState stateData -> 
            // new state
            bStateHandler stateData inputEvent 
        | CState -> 
            // new state
            cStateHandler inputEvent 
*)

    let aStateHandler stateData inputEvent = 
        match inputEvent with
        | XEvent -> 
            // transition to B state
            BState {somethingElse=stateData.something}
        | YEvent _ -> 
            // stay in A state
            AState stateData 
        | ZEvent -> 
            // transition to C state
            CState 

    let bStateHandler stateData inputEvent = 
        match inputEvent with
        | XEvent -> 
            // stay in B state
            BState stateData 
        | YEvent _ -> 
            // transition to C state
            CState 

    let cStateHandler inputEvent = 
        match inputEvent with
        | XEvent -> 
            // stay in C state
            CState
        | ZEvent -> 
            // transition to B state
            BState {somethingElse=42}

    let transition (currentState,inputEvent) =
        match currentState with
        | AState stateData -> 
            aStateHandler stateData inputEvent 
        | BState stateData -> 
            bStateHandler stateData inputEvent 
        | CState -> 
            cStateHandler inputEvent 

// ========================================
// fixed up version that handles all events

module StateMachineExampleImplemementation_V2 =
    open StateMachineExample

    let aStateHandler stateData inputEvent = 
        match inputEvent with
        | XEvent -> 
            // transition to B state
            BState {somethingElse=stateData.something}
        | YEvent _ -> 
            // stay in A state
            AState stateData 
        | ZEvent -> 
            // transition to C state
            CState 

    let bStateHandler stateData inputEvent = 
        match inputEvent with
        | XEvent 
        | ZEvent -> 
            // stay in B state
            BState stateData 
        | YEvent _ -> 
            // transition to C state
            CState 

    let cStateHandler inputEvent = 
        match inputEvent with
        | XEvent  
        | YEvent _ -> 
            // stay in C state
            CState
        | ZEvent -> 
            // transition to B state
            BState {somethingElse=42}

    let transition (currentState,inputEvent) =
        match currentState with
        | AState stateData -> 
            aStateHandler stateData inputEvent 
        | BState stateData -> 
            bStateHandler stateData inputEvent 
        | CState -> 
            cStateHandler inputEvent 

