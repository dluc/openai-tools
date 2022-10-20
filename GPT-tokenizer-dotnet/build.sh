set -e

rm -fR Lib/bin/Release Lib/obj/Release
rm -f out/*.nupkg

echo '#### Build ####'
dotnet build --nologo -c Release

echo -e '\n\n#### Test ####'
dotnet test --nologo

echo -e '\n\n#### Package ####'
dotnet pack Lib/Lib.csproj --nologo -s -c Release -o out --include-symbols
