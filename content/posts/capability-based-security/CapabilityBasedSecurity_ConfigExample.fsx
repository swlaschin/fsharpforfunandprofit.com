(*
CapabilityBasedSecurity_ConfigExample.fsx

An example of a simple capability-based design.

Related blog post: https://fsharpforfunandprofit.com/posts/capability-based-security/
*)

/// Configuration system
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
        GetConnectionString = fun () -> ConfigStore.ConnectionString
        SetConnectionString = fun connStr -> ConfigStore.ConnectionString <- connStr
        }

/// Logic for constructing an annoying popup message dialog everytime you click the main form
module AnnoyingPopupMessage =
    open System.Windows.Forms

    let createLabel() =
        new Label(Text="You clicked the main window", Dock=DockStyle.Top)

    let createMessageFlagCheckBox capabilities  =
        let getFlag,setFlag = capabilities
        let ctrl= new CheckBox(Text="Don't show this annoying message again", Dock=DockStyle.Bottom)
        ctrl.Checked <- getFlag()
        ctrl.CheckedChanged.Add (fun _ -> ctrl.Checked |> setFlag)
        ctrl    // return new control

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
        ctrl      // return new control

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

    // open another form and the config is remembered
    //form.ShowDialog() |> ignore