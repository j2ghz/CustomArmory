FROM microsoft/dotnet:sdk
      
ARG BUILD_DATE
ARG VCS_REF
LABEL org.label-schema.build-date=$BUILD_DATE \
      org.label-schema.vcs-url="https://github.com/j2ghz/CustomArmory.git" \
      org.label-schema.vcs-ref=$VCS_REF \
      org.label-schema.schema-version="1.0.0-rc1"
      
WORKDIR src/CustomArmory/

RUN dotnet restore CustomArmory.fsproj
RUN dotnet build -c Release CustomArmory.fsproj

ENTRYPOINT ["dotnet", "run", "-c", "Release", "CustomArmory.fsproj"]
