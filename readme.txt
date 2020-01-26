dotnet new sln --name ne_webapi_akka.sln
dotnet sln add ne
dotnet sln add Logging2
dotnet sln list
dotnet add .\ne\ne.csproj reference .\Logging2\Logging2.csproj

