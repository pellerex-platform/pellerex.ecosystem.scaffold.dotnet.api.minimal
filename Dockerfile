FROM --platform=$TARGETARCH mcr.microsoft.com/dotnet/aspnet:10.0 AS base

RUN apt-get update && apt-get install -y libgdiplus

WORKDIR /app
EXPOSE <port-number>
ENV ASPNETCORE_URLS=http://*:<port-number>

FROM --platform=$TARGETARCH mcr.microsoft.com/dotnet/sdk:10.0 AS build

ENV DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER=0

WORKDIR /src

COPY ["api/RepoUniquePascalIdentifier.csproj", "api/"]

RUN dotnet restore \
	-s https://api.nuget.org/v3/index.json \
	-s https://pkgs.dev.azure.com/Pellerex/Public/_packaging/CommonLibrary/nuget/v3/index.json \
	"api/RepoUniquePascalIdentifier.csproj"

COPY . . 

RUN dotnet build "api/RepoUniquePascalIdentifier.csproj" -c Release -o /app/build

FROM build AS publish

RUN dotnet publish "api/RepoUniquePascalIdentifier.csproj" -c Release -o /app/publish

# The unit tests, which the build runs before it builds the image (.github/workflows/ci.yaml,
# infrastructure/azure-containers-pipelines.yml). The default build leaves out this stage and
# the next one: the runtime stage below needs neither.
FROM build AS unit-tests

# The tests start the app, so its log store setting is emptied, as for a local run of the
# acceptance tests (acceptance-tests/standup.yaml): a build sends nothing to a deployed
# environment's logs.
ENV Monitoring__AzureApplicationConnectionString=""

# TUnit writes the results as JUnit XML, and Coverlet measures the lines of the code whose
# sources are in this repository. Coverlet stamps its file name with the time, so the one file
# is copied to a fixed name. The tests' exit code goes into a file instead of failing the stage,
# so the results are kept when a test fails, and a test that does not compile leaves a code
# that is not 0. A run that finds no test at all is not a failure: the testing platform's code
# for it (8) is given as 0, and every other code is kept as the runner gives it.
# Generated database migrations (Migrations folders) are left out, to measure code people write.
RUN mkdir -p /unit-test-results/test-coverage/unit-tests /unit-test-results/test-coverage/coverage; \
	TUNIT_ENABLE_JUNIT_REPORTER=true dotnet run --project "unit-tests/RepoUniquePascalIdentifier.UnitTests.csproj" -c Release -- \
		--junit-output-path /unit-test-results/test-coverage/unit-tests/junit.xml \
		--coverlet --coverlet-output-format cobertura --coverlet-exclude-assemblies-without-sources MissingAll \
		--coverlet-exclude-by-file "**/Migrations/**/*.cs" \
		--results-directory /tmp/unit-test-run \
		--ignore-exit-code 8; \
	echo $? > /unit-test-results/unit-tests-exit-code; \
	cp /tmp/unit-test-run/coverage.cobertura.*.xml /unit-test-results/test-coverage/coverage/coverage.cobertura.xml || true

# Only the results, so a local export of this stage holds the test-coverage folder and the exit
# code file at its root.
FROM scratch AS unit-test-results

COPY --from=unit-tests /unit-test-results /

FROM base AS final

WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "RepoUniquePascalIdentifier.dll"]
