# ConcatCommands
A Visual Studio extension that adds the command
```
Tools.ConcatCommands
```
to the **Command Window**. With this command you can execute multiple commands sequentially i.e.

![Tools.ConcatCommands View.C#Interactive && View.F#Interactive && nav https://www.microsoft.com](https://raw.githubusercontent.com/christianscheer/ConcatCommands/master/PreviewImage.png "usage example")

The sub commands
- must be seperated by `&&`
- can have arguments of their own
- are executed in the order they're passed in
- are executed as is (no additional waiting and no check if the prior command was successful)

## More about commands
[Command Window](https://docs.microsoft.com/en-us/visualstudio/ide/reference/command-window?view=vs-2019)

[Visual Studio commands](https://docs.microsoft.com/en-us/visualstudio/ide/reference/visual-studio-commands?view=vs-2019)

[Visual Studio Command Aliases](https://docs.microsoft.com/en-us/visualstudio/ide/reference/visual-studio-command-aliases?view=vs-2019)