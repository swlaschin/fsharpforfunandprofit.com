open System



#r "nuget:Terminal.Gui"
open Terminal.Gui

Application.Init()
let top = Application.Top

// helpers
let ( !! ) (str:string) = NStack.ustring.Make str
let ustr (str:string) = NStack.ustring.Make str
let pos i = Pos.At i
let dim i = Dim.Sized i

// Creates the top-level window to show
let win = new Window (!!"MyApp",X = pos 0, Y = pos 1)
win.Width <- Dim.Fill()
win.Height <- Dim.Fill()
top.Add(win)

let Quit () =
    MessageBox.Query (50, 7, ustr "Quit Demo", ustr "Are you sure you want to quit this demo?", ustr "Yes", ustr "No") = 0

let Close () =
    MessageBox.ErrorQuery (50, 7, ustr "Error", ustr "There is nothing to close", ustr "Ok")
    |> ignore

let Open () =
    let d = new OpenDialog (ustr "Open", ustr "Open a file", AllowsMultipleSelection = true)
    Application.Run d
    if not d.Canceled
        then MessageBox.Query (50, 7, ustr "Selected File", ustr (System.String.Join (", ", d.FilePaths)), ustr "Ok") |> ignore

let NewFile () =
    let okButton = new Button (ustr "Ok", true)
    okButton.add_Clicked (Action (Application.RequestStop))
    let cancelButton = new Button (ustr "Cancel", true)
    cancelButton.add_Clicked (Action (Application.RequestStop))

    let d = new Dialog (ustr "New File", 50, 20, okButton, cancelButton)
    let ml2 = new Label (1, 1, ustr "Mouse Debug Line")
    d.Add ml2
    Application.Run d

// Creates a menubar, the item "New" has a help menu.
let menu = new MenuBar [|
    MenuBarItem (!!"_File",
      [|
      MenuItem (!!"_New", !!"Creates new file", Action NewFile)
      MenuItem (!!"_Close", !!"", Action Close)
      MenuItem (!!"_Quit", !!"", fun () -> if (Quit()) then top.Running <- false )
      |])
    MenuBarItem (!!"_Edit",
      [|
      MenuItem (!!"_Copy", !!"", null)
      MenuItem (!!"C_ut", !!"", null)
      MenuItem (!!"_Paste", !!"", null)
      |])
    |]
top.Add (menu);

let login =  new Label (!!"Login: ", X = pos 3, Y = pos 2)
let password = new Label (!!"Password: ", X = Pos.Left (login),Y = Pos.Top(login) + pos 1)
let loginText = new TextField (!!"")
loginText.X <- Pos.Right(password)
loginText.Y <- Pos.Top(login)
loginText.Width <- dim 40
let passText = new TextField ("")
passText.Secret = true
passText.X <- Pos.Left (loginText)
passText.Y <- Pos.Top (password)
passText.Width <- Dim.Width (loginText)

// The ones laid out like an australopithecus, with Absolute positions:
let checkBox = new CheckBox (3, 6, !!"Remember me")
let radioGroup = new RadioGroup (3, 8, [|!!"_Personal"; !!"_Company" |])
let pbOk = new Button (3, 14, !!"Ok")
let pbCancel = new Button (10, 14, !!"Cancel")
let label = new Label (3, 18, !!"Press F9 or ESC plus 9 to activate the menubar")

// Add some controls,
win.Add (login, password, loginText, passText,checkBox,radioGroup, pbOk,pbCancel,label)

Application.Run ()
// Application.Shutdown()
