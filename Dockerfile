FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /app

ARG BUILD_DATE
ARG VCS_REF

LABEL org.label-schema.build-date=$BUILD_DATE \
      org.label-schema.vcs-url="https://github.com/rossf7/label-schema-automated-build.git" \
      org.label-schema.vcs-ref=$VCS_REF \
      org.label-schema.schema-version="1.0.0-rc1"
      
# copy csproj and restore as distinct layers

COPY src/CustomArmory/*.fsproj .
RUN dotnet restore

# copy everything else and build app
COPY src/CustomArmory/. .
WORKDIR /app
RUN dotnet publish -c Release -o out


FROM microsoft/dotnet:2.1-aspnetcore-runtime AS runtime
WORKDIR /app
COPY --from=build /app/out ./
ENTRYPOINT ["dotnet", "aspnetapp.dll"]
