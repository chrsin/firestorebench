FROM mcr.microsoft.com/dotnet/sdk:8.0-noble-amd64

WORKDIR /app
COPY . /app/

ENTRYPOINT ["dotnet", "run", "statestorebenchmark.csproj", "--configuration=Release"]