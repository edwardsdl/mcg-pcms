FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5016

# Create a non-privileged user to run the application
RUN useradd -m appuser
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR "/src"

# We first copy the project files over and restore NuGet packages. If we copy all of the source code over at once,
# every time a file changes, we invalidate Docker's build cache and have to pull down all the NuGet packages 
# again. That's slow.
COPY ["../src/Mcg.Pcms.Api/Mcg.Pcms.Api.csproj", "Mcg.Pcms.Api/"]
COPY ["../src/Mcg.Pcms.Core/Mcg.Pcms.Core.csproj", "Mcg.Pcms.Core/"]
COPY ["../src/Mcg.Pcms.Infrastructure/Mcg.Pcms.Infrastructure.csproj", "Mcg.Pcms.Infrastructure/"]
RUN dotnet restore "Mcg.Pcms.Api/Mcg.Pcms.Api.csproj"

# Now we copy over the rest of our source files. We should probably add a .dockerignore to speed this up too, but
# it's crunch time!
COPY ["../src/", "."]

# Now that we've got everything moved over into the build stage, we can compile and publish the application.
WORKDIR "/src/Mcg.Pcms.Api"
RUN dotnet publish "Mcg.Pcms.Api.csproj" -c Release -o /app/publish

# Now copy the build artifacts into the final image. We should be ready to go! 
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Mcg.Pcms.Api.dll"]