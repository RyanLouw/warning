#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
provider="${1:-sqlserver}"
filter=()
case "$provider" in
  sqlserver)
    : "${TEST_SQLSERVER:?Set TEST_SQLSERVER to the isolated SQL Server master connection described in warningchat/tests/README.md}"
    export TEST_DATABASE_PROVIDER=SqlServer
    ;;
  sqlite)
    export TEST_DATABASE_PROVIDER=Sqlite
    filter=(--filter 'Database!=SqlServer')
    ;;
  *) echo 'Usage: scripts/test.sh [sqlserver|sqlite]' >&2; exit 2 ;;
esac

dotnet restore warningchat/WarningSystems.Tests.slnx -p:EnableCentralConfig=false
dotnet build warningchat/WarningSystems.Tests.slnx -c Release --no-restore -p:EnableCentralConfig=false
if [[ -z "${PLAYWRIGHT_CHROMIUM_EXECUTABLE:-}" ]]; then
  pwsh warningchat/tests/WarningSystems.UiTests/bin/Release/net10.0/playwright.ps1 install --with-deps chromium
fi
# A failed test terminates the command and must block a merge.
dotnet test warningchat/WarningSystems.Tests.slnx -c Release --no-build --no-restore \
  -p:EnableCentralConfig=false "${filter[@]}" \
  --settings warningchat/tests/coverage.runsettings --collect:'XPlat Code Coverage' \
  --logger trx --results-directory TestResults

dotnet tool restore
dotnet reportgenerator '-reports:TestResults/**/coverage.cobertura.xml' '-targetdir:TestResults/coverage' '-reporttypes:Html;Cobertura;JsonSummary;TextSummary'
python3 scripts/check-coverage.py TestResults/coverage/Cobertura.xml
