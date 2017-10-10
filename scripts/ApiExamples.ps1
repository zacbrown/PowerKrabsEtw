

# creating user trace
$trace = New-EtwUserTrace -Name "Some Trace"

# creating kernel trace
# New-EtwKernelTrace -Name "Some Trace"

# create usermode provider
$provider = New-EtwUserProvider -ProviderName "Microsoft-Windows-PowerShell" -AnyFlags 0x10 -AllFlags 0x10
$provider = New-EtwUserProvider -ProviderGuid "{some guid}" -AnyFlags 0x10 -AllFlags 0x10

# create kernelmode provider
# New-EtwKernelProvider...

# enable user provider
Enable-EtwUserProvider -Trace $trace -Provider $provider

# enable kernel provider
# Enable-EtwKernelProvider -Trace $trace -Provider $provider

# start trace
Start-EtwUserTrace -Trace $trace

# stop trace
Stop-EtwUserTrace -Trace $trace

#
# Notes on filtering - if filters exist, the standard OnEvent callback
# is not invoked. If no filters exist, the OnEvent callback will be
# called.
#

# create filter
$filter1 = New-EtwFilter -EventId 1234
$filter1 = New-EtwFilter -ProcessId 4567
$filter1 = New-EtwFilter -PropertyName "ImageName" -AnsiStringContains "foo.exe"
$filter2 = New-EtwFilter -PropertyName "ImageName" -UnicodeStringEndsWith "foo.exe" -Not
$filter2 = New-EtwFilter -PropertyName "ImageName" -AnsiCountedStringStartsWith "foo.exe"
$filter2 = New-EtwFilter -PropertyName "ImageName" -UnicodeCountedStringStartsWith "foo.exe" -Not

# combine filter
$filter = Join-EtwFilter -Filters @($filter1, $filter2) -And
$filter = Join-EtwFilter -Filters @($filter1, $filter2) -Or

# enable filter
Enable-EtwFilter -Provider $provider -Filter $filter




