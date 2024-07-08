if not exist "..\Build" mkdir ..\Build
if not exist "..\Build\Appx" mkdir ..\Build\Appx

MakeAppx pack /o /f MappingFile.txt /p ..\Build\Appx\PPSwitcher.appx
copy /y ..\Build\Appx\PPSwitcher.appx ..\Build\Appx\PPSwitcher_ns.appx
signtool.exe sign -f ..\Build\Certs\my.pfx -fd SHA256 -v ..\Build\Appx\PPSwitcher.appx