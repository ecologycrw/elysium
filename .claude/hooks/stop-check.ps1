# Claude Code Stop hook: before the agent finishes a turn with uncommitted changes,
# runs the docs check (and the build, if code changed). Exit code 2 sends the problems back
# to the agent, which must fix them before finishing. This file is a protected "trava".

[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
if ($env:CLAUDE_PROJECT_DIR) { Set-Location -LiteralPath $env:CLAUDE_PROJECT_DIR }

try {
    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
}
catch {
    $payload = $null
}

# Avoid an endless loop: when this hook already sent the agent back once, let it stop.
if ($payload -and $payload.stop_hook_active) { exit 0 }

$changed = @(git status --porcelain -- src tests tools docs specs templates .claude AGENTS.md CLAUDE.md 2>$null)
if ($changed.Count -eq 0) { exit 0 }

$problems = @()

$checks = & dotnet run --project tools/Elysium.Checks -- docs 2>&1
if ($LASTEXITCODE -ne 0) {
    $problems += 'Docs check failed:'
    $problems += @($checks | ForEach-Object { "  $_" })
}

$codeChanged = @($changed | Where-Object { $_ -match '\.(cs|csproj|props|targets|slnx)"?\s*$' }).Count -gt 0
if ($codeChanged) {
    $build = & dotnet build Elysium.slnx --nologo -v q 2>&1
    if ($LASTEXITCODE -ne 0) {
        $problems += 'Build failed:'
        $problems += @($build | ForEach-Object { "$_" } | Where-Object { $_ -match ' error ' } | Select-Object -Unique -First 20 | ForEach-Object { "  $_" })
    }
}

if ($problems.Count -gt 0) {
    [Console]::Error.WriteLine("stop-check.ps1: fix these problems before finishing (or explain to the owner why you cannot):`n" + ($problems -join "`n"))
    exit 2
}

exit 0
