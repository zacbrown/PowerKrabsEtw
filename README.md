# PowerKrabsEtw
PowerKrabsEtw is a PowerShell module built around the [krabsetw](https://github.com/Microsoft/krabsetw) APIs. It exposes a subset of functionality directly available in krabsetw and is meant to streamline ETW experimentation.

## Notes
This module is currently in an experimental state. This is the first PowerShell API I've written and while I've had great feedback working with [@Lee_Holmes](https://twitter.com/lee_holmes), I know it still needs work.

**Please feel free to file issues if you have suggestions for improving the API.**

# Examples
There are two main supported scenarios right now:
* **`Trace-KrabsEtwProcess`** - think of this as similar to ProcMon filtered on a specific process.
    * This is not yet configurable. The data provided includes the following data sources:
        * IPv4/IPv6 TCP send
        * IPv4/IPv6 UDP send
        * DNS lookups
        * remote thread injections
        * child process creation (via CreateProcess or similar direct means)
        * WMI activity
        * registry activity
        * file activity
        * PowerShell function execution
        * DLL load activity
* Create explicit providers, filters, and traces - this is a more flexible approach and best for experimentation.

**Start powershell.exe with the `-MTA` flag. The module will fail to work otherwise.**:

    powershell.exe -mta

1. Trace a process's lifetime.
```
PS C:\dev\PowerKrabsEtw\PowerKrabsEtw\bin\x64\Debug> import-module .\PowerKrabsEtw
PS C:\dev\PowerKrabsEtw\PowerKrabsEtw\bin\x64\Debug> $events = Trace-ProcessWithEtw -ProcessName powershell.exe
PS C:\dev\PowerKrabsEtw\PowerKrabsEtw\bin\x64\Debug> $events | select -Unique EtwProviderName

EtwProviderName
---------------
Microsoft-Windows-Kernel-Registry
Microsoft-Windows-Kernel-Process
Microsoft-Windows-Kernel-File
Microsoft-Windows-PowerShell

PS C:\dev\PowerKrabsEtw\PowerKrabsEtw\bin\x64\Debug> $events[0]


EtwEventId       : 7
EtwTimestamp     : 11/12/17 11:13:34 PM
EtwProcessId     : 4980
EtwThreadId      : 904
EtwProviderName  : Microsoft-Windows-Kernel-Registry
KeyObject        : 18446603362009679696
Status           : 3221225524
InfoClass        : 2
DataSize         : 524
KeyName          :
ValueName        : 3c74afb9-8d82-44e3-b52c-365dbf48382a
CapturedDataSize : 0
CapturedData     :
```

2. Setup a custom trace session for PowerShell events
```
PS C:\dev\PowerKrabsEtw\demo> Import-Module .\PowerKrabsEtw
>> $trace = New-EtwUserTrace
>> $provider = New-EtwUserProvider -ProviderName "Microsoft-Windows-PowerShell"
>> $filter = New-EtwCallbackFilter -EventId 7937
>> Set-EtwCallbackFilter -UserProvider $provider -Filter $filter
>> Set-EtwUserProvider -Trace $trace -Provider $provider
>>
>> Start-EtwUserTrace -Trace $trace | Where-Object { $_.CommandName -like "invoke-mimikatz" }


EtwEventId      : 7937
EtwTimestamp    : 11/12/17 11:19:47 PM
EtwProcessId    : 5308
EtwThreadId     : 2000
EtwProviderName : Microsoft-Windows-PowerShell
HostProcess     : c:\windows\system32\WindowsPowerShell\v1.0\powershell.exe -NoLogo -mta
CommandName     : invoke-mimikatz
CommandType     : Function
UserName        : ZACBROWNDDDC\zbrown
UserData        :
Payload         : Command invoke-mimikatz is Started.


EtwEventId      : 7937
EtwTimestamp    : 11/12/17 11:19:47 PM
EtwProcessId    : 5308
EtwThreadId     : 2000
EtwProviderName : Microsoft-Windows-PowerShell
HostProcess     : c:\windows\system32\WindowsPowerShell\v1.0\powershell.exe -NoLogo -mta
CommandName     : invoke-mimikatz
CommandType     : Function
UserName        : ZACBROWNDDDC\zbrown
UserData        :
Payload         : Command invoke-mimikatz is Stopped.
```

# Future Plans
* Add ability to specify a directory of YARA and/or SIGMA rules for filtering.

# Known Issues
* If you create many new traces, either by using `Trace-KrabsEtwProcess` or `Start-KrabsEtwProcess`, it is possible to exhaust the available ETW sessions in Windows. The easiest solution is to restart the machine.
    * ETW is best used for long running sessions.

* If you use `Start-KrabsEtwProcess` without specifying the `-TraceTimeLimit` parameter, you won't be able to capture the objects returned. They'll print to the command line nicely, but they won't be processed against the pipeline.
    * In general, it's better to specify the `-TraceTimeLimit` flag for the time being.
