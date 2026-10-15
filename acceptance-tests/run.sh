#!/usr/bin/env bash
#
# The entrypoint of an acceptance-test pack: the one command every invoker runs, whatever test
# framework the pack uses (11_acceptance-tests/03_SUITE_FORMAT.md, "The entrypoint"):
#
#   acceptance-tests/run.sh --type <Release|Acceptance> --target <url> --out <path> [--run-id <run id>]
#
#   --type    the set of tests to run: the Release-typed ones or the Acceptance-typed ones
#   --target  the address of the copy of the app the tests call
#   --out     where the result file is written; a file already there is removed first
#   --run-id  optional: the run id the invoker already has for this run, such as the one the
#             stand-up tool answered, so the result file, the test data and the run's containers
#             all carry the same one; without it, run.sh makes its own
#
# The test identity's credential is not an argument: it arrives as a mounted folder of files, one
# per key, which the family's tests read (03_SUITE_FORMAT.md, "The entrypoint").
#
# It ends with one of three exit codes, fixed forever (05_CONSUMER_CONTRACT.md, "Exit codes"):
#
#   0  Green   every selected test passed
#   1  Red     the run finished and at least one test failed
#   2  Error   the run itself broke before any verdict existed
#
# Codes above 2 are reserved, and this script never chooses one. A run stopped from outside, such
# as by an invoker's deadline, ends with the code of the signal (143 or 137), which a consumer
# reads as an error, never as a pass. Only 0 lets a gate proceed.
#
# Every run has a run id: "run-" and ten lower-case letters or digits, as in run-9f3k2m7q1x. The
# adapter writes it as the result file's Id. Set-up puts it into the name of every record the run
# creates: a short lower-case word for what the record is, the run id, and a number of at least two
# digits that no other record of the run shares, joined by hyphens, as in dsp-run-9f3k2m7q1x-01.
# Tear-down deletes by it, and support finds by it what a failed tear-down left behind.
#
# This file is the template every family's run.sh is made from (J1-Step1 and J1-Step3, decided
# 2026-10-14). A family fills in run_selected_tests and changes nothing else.

set -u

readonly EXIT_GREEN=0
readonly EXIT_RED=1
readonly EXIT_ERROR=2

readonly RUN_ID_START="run-"
readonly RUN_ID_CHARACTERS="0123456789abcdefghijklmnopqrstuvwxyz"
readonly RUN_ID_LENGTH=10

# The family's part. Runs the tests of the given type against the target and has the adapter
# write the result file. Takes the test type, the target, the result file's full path and the run
# id. Answers 0 when every test passed, 1 when at least one failed, anything else when the run
# broke.
run_selected_tests() {
    # The .NET family's part: the pack is a TUnit project, and a test's type is its Type property.
    # It builds the adapter and the tests, runs the tests of the asked type with the testing
    # platform's TRX report, and has the adapter write the result file. The tests' own console
    # output stays in the run's scratch folder, which goes when the run ends: the result file is
    # the run's record, and every line of it is redacted.
    local test_type="$1" target="$2" result_path="$3" run_id="$4"
    local pack_folder work started_at commit adapter tests tests_exit_code
    started_at=$(date -u +%Y-%m-%dT%H:%M:%SZ)
    pack_folder=$(cd "$(dirname -- "${BASH_SOURCE[0]}")" && pwd) || return 2

    if ! command -v dotnet >/dev/null 2>&1; then
        echo "run.sh: dotnet is not installed here, so the .NET pack cannot run" >&2
        return 2
    fi

    if [ -n "${ACCEPTANCE_TESTS_CREDENTIALS:-}" ] && [ ! -d "$ACCEPTANCE_TESTS_CREDENTIALS" ]; then
        echo "run.sh: the credential folder in ACCEPTANCE_TESTS_CREDENTIALS does not exist: $ACCEPTANCE_TESTS_CREDENTIALS" >&2
        return 2
    fi

    # The run's scratch folder is named for what it is, under TMPDIR, so whatever a run ever
    # leaves behind can be found. Its path is written into the trap now, since the function's
    # variables are gone by the time the part's subshell ends and the trap runs.
    work=$(mktemp -d "${TMPDIR:-/tmp}"/acceptance-tests.XXXXXXXXXX) || return 2
    trap "rm -rf -- $(printf '%q' "$work")" EXIT
    mkdir "$work/recordings" || return 2

    # Nothing about the run goes to the makers of the test tools, and no report but the result file
    # is written: the testing platform's telemetry, TUnit's own reports and its upload are off.
    export DOTNET_CLI_TELEMETRY_OPTOUT=1 TESTINGPLATFORM_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 \
        TUNIT_DISABLE_LOGO=true TUNIT_DISABLE_HTML_REPORTER=true TUNIT_DISABLE_JSON_REPORT=true \
        TUNIT_DISABLE_JUNIT_REPORTER=true TUNIT_DISABLE_GITHUB_REPORTER=true TUNIT_DISABLE_ARTIFACT_UPLOAD=true

    commit=$(git -C "$pack_folder" rev-parse HEAD 2>/dev/null) || commit=""
    local shared=(--type "$test_type" --target "$target" --run-id "$run_id" --out "$result_path"
        --started-at "$started_at" --commit "$commit" --pipeline-run-id "${GITHUB_RUN_ID:-${BUILD_BUILDID:-}}")

    if ! dotnet build "$pack_folder/Adapter/Adapter.csproj" --configuration Release --nologo >"$work/adapter-build.log" 2>&1; then
        echo "run.sh: the pack's adapter does not build, so no result file can be written:" >&2
        grep -F ': error ' "$work/adapter-build.log" | sort -u >&2
        return 2
    fi
    adapter=$(dotnet msbuild "$pack_folder/Adapter/Adapter.csproj" -getProperty:TargetPath -p:Configuration=Release) || return 2

    if ! dotnet build "$pack_folder/AcceptanceTests/AcceptanceTests.csproj" --configuration Release --nologo >"$work/tests-build.log" 2>&1; then
        dotnet "$adapter" build-failed "${shared[@]}" --build-log "$work/tests-build.log"
        return 2
    fi
    tests=$(dotnet msbuild "$pack_folder/AcceptanceTests/AcceptanceTests.csproj" -getProperty:TargetPath -p:Configuration=Release) || return 2

    ACCEPTANCE_TESTS_TARGET="$target" ACCEPTANCE_TESTS_RUN_ID="$run_id" ACCEPTANCE_TESTS_RECORDINGS="$work/recordings" \
        dotnet "$tests" --treenode-filter "/*/*/*/*[Type=$test_type]" --report-trx --report-trx-filename tests.trx \
        --results-directory "$work" --progress off >"$work/tests.log" 2>&1
    tests_exit_code=$?

    dotnet "$adapter" report "${shared[@]}" --test-report "$work/tests.trx" --recordings "$work/recordings" \
        --tests-exit-code "$tests_exit_code" --test-log "$work/tests.log"
}

# Says what is wrong with the call, shows how to call, and ends the run as an error.
refuse_call() {
    echo "run.sh: $1" >&2
    echo "usage: run.sh --type <Release|Acceptance> --target <url> --out <path> [--run-id <run id>]" >&2
    exit "$EXIT_ERROR"
}

# Makes a fresh run id from the system's random source. A byte at or above the largest multiple
# of the number of characters is skipped, so every character is equally likely.
make_run_id() {
    local largest_fair_byte=$((256 - 256 % ${#RUN_ID_CHARACTERS}))
    local suffix="" random_bytes byte
    while [ "${#suffix}" -lt "$RUN_ID_LENGTH" ]; do
        random_bytes=$(od -An -N16 -tu1 /dev/urandom) || return 1
        [ -n "$random_bytes" ] || return 1
        for byte in $random_bytes; do
            if [ "$byte" -lt "$largest_fair_byte" ] && [ "${#suffix}" -lt "$RUN_ID_LENGTH" ]; then
                suffix="$suffix${RUN_ID_CHARACTERS:byte % ${#RUN_ID_CHARACTERS}:1}"
            fi
        done
    done
    printf '%s%s\n' "$RUN_ID_START" "$suffix"
}

test_type="" target="" result_path="" run_id=""
while [ "$#" -gt 0 ]; do
    case "$1" in
        --type) given=test_type ;;
        --target) given=target ;;
        --out) given=result_path ;;
        --run-id) given=run_id ;;
        *) refuse_call "unknown argument: $1" ;;
    esac
    [ "$#" -ge 2 ] && [ -n "$2" ] || refuse_call "$1 needs a value"
    [ -z "${!given}" ] || refuse_call "$1 is given twice"
    printf -v "$given" '%s' "$2"
    shift 2
done

[ -n "$test_type" ] || refuse_call "--type is missing"
[ -n "$target" ] || refuse_call "--target is missing"
[ -n "$result_path" ] || refuse_call "--out is missing"

case "$test_type" in
    Release | Acceptance) ;;
    *) refuse_call "--type is Release or Acceptance, not $test_type" ;;
esac

target_pattern='^https?://[^/[:space:]]+(/[^[:space:]]*)?$'
[[ $target =~ $target_pattern ]] || refuse_call "--target is an http or https address, not $target"

# The characters are listed one by one rather than as a range, which in some locales also takes
# capital letters.
run_id_pattern="^$RUN_ID_START[$RUN_ID_CHARACTERS]{$RUN_ID_LENGTH}\$"
if [ -n "$run_id" ] && [[ ! $run_id =~ $run_id_pattern ]]; then
    refuse_call "--run-id is run- and $RUN_ID_LENGTH lower-case letters or digits, such as run-9f3k2m7q1x, not $run_id"
fi

case "$result_path" in
    */) refuse_call "--out names a folder, not a file: $result_path" ;;
esac
[ ! -d "$result_path" ] || refuse_call "--out names a folder, not a file: $result_path"
result_folder=$(dirname -- "$result_path")
[ -d "$result_folder" ] || refuse_call "the folder for --out does not exist: $result_folder"

# The family gets the full path, so a change of folder inside its part cannot misplace the file.
if ! full_result_folder=$(cd "$result_folder" && pwd); then
    refuse_call "the folder for --out cannot be entered: $result_folder"
fi
result_path="$full_result_folder/$(basename -- "$result_path")"

if ! rm -f -- "$result_path"; then
    echo "run.sh: a file is already at $result_path and cannot be removed" >&2
    exit "$EXIT_ERROR"
fi

if [ -z "$run_id" ] && ! run_id=$(make_run_id); then
    echo "run.sh: the system's random source cannot be read, so no run id can be made" >&2
    exit "$EXIT_ERROR"
fi

# The family's part runs in a subshell, so even an exit inside it comes back here to be read.
(run_selected_tests "$test_type" "$target" "$result_path" "$run_id")
case "$?" in
    0) exit_code=$EXIT_GREEN ;;
    1) exit_code=$EXIT_RED ;;
    *) exit_code=$EXIT_ERROR ;;
esac

# A verdict comes with its result file; a run that wrote none broke. Whoever reads the file checks
# it against the result schema (04_RUN_MODES.md, "Bounds and failure kinds").
if [ "$exit_code" -ne "$EXIT_ERROR" ] && [ ! -s "$result_path" ]; then
    echo "run.sh: the tests finished but wrote no result file at $result_path" >&2
    exit_code=$EXIT_ERROR
fi

echo "run.sh: $run_id ended with exit code $exit_code" >&2
exit "$exit_code"
