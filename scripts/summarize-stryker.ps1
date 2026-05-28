<#
.SYNOPSIS
    Summarize a StrykerJS mutation-report.json into compact JSON and Markdown.

.DESCRIPTION
    Reads the Stryker JSON report, tallies mutant statuses per file, computes the
    mutation score (detected / (detected + survived + no-coverage), where detected
    = killed + timeout), and emits a per-file table plus totals. The Markdown is
    written to the host (so CI can append it to $GITHUB_STEP_SUMMARY) and,
    optionally, to files.

.PARAMETER ReportPath
    Path to Stryker's mutation-report.json.

.PARAMETER JsonOutputPath
    Optional path to write the compact JSON summary.

.PARAMETER MarkdownOutputPath
    Optional path to write the Markdown summary.
#>
param(
    [Parameter(Mandatory)]
    [string] $ReportPath,
    [string] $JsonOutputPath,
    [string] $MarkdownOutputPath
)

$ErrorActionPreference = 'Stop'

$report = Get-Content -LiteralPath $ReportPath -Raw | ConvertFrom-Json

function Get-Score {
    param([int] $Killed, [int] $Timeout, [int] $Survived, [int] $NoCoverage)
    $detected = $Killed + $Timeout
    $valid = $detected + $Survived + $NoCoverage
    if ($valid -eq 0) { return $null }
    return [math]::Round(($detected / $valid) * 100, 2)
}

$rows = foreach ($file in $report.files.PSObject.Properties) {
    $mutants = @($file.Value.mutants)
    $killed = @($mutants | Where-Object status -eq 'Killed').Count
    $survived = @($mutants | Where-Object status -eq 'Survived').Count
    $timeout = @($mutants | Where-Object status -eq 'Timeout').Count
    $noCoverage = @($mutants | Where-Object status -eq 'NoCoverage').Count
    $ignored = @($mutants | Where-Object status -eq 'Ignored').Count

    [pscustomobject]@{
        file       = $file.Name
        score      = Get-Score -Killed $killed -Timeout $timeout -Survived $survived -NoCoverage $noCoverage
        killed     = $killed
        survived   = $survived
        timeout    = $timeout
        noCoverage = $noCoverage
        ignored    = $ignored
        total      = $mutants.Count
    }
}

$rows = @($rows | Sort-Object file)

$totals = [pscustomobject]@{
    score      = Get-Score `
        -Killed     (($rows | Measure-Object killed -Sum).Sum) `
        -Timeout    (($rows | Measure-Object timeout -Sum).Sum) `
        -Survived   (($rows | Measure-Object survived -Sum).Sum) `
        -NoCoverage (($rows | Measure-Object noCoverage -Sum).Sum)
    killed     = ($rows | Measure-Object killed -Sum).Sum
    survived   = ($rows | Measure-Object survived -Sum).Sum
    timeout    = ($rows | Measure-Object timeout -Sum).Sum
    noCoverage = ($rows | Measure-Object noCoverage -Sum).Sum
    ignored    = ($rows | Measure-Object ignored -Sum).Sum
    total      = ($rows | Measure-Object total -Sum).Sum
}

$summary = [pscustomobject]@{
    totals = $totals
    files  = $rows
}

if ($JsonOutputPath) {
    $summary | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $JsonOutputPath -Encoding utf8
}

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add("## Angular mutation summary")
$lines.Add("")
$lines.Add("**Overall mutation score: $($totals.score)%** — killed $($totals.killed), survived $($totals.survived), timeout $($totals.timeout), no-coverage $($totals.noCoverage), ignored $($totals.ignored) (total $($totals.total)).")
$lines.Add("")
$lines.Add("| File | Score % | Killed | Survived | Timeout | No cover | Total |")
$lines.Add("| --- | ---: | ---: | ---: | ---: | ---: | ---: |")
foreach ($r in $rows) {
    $lines.Add("| $($r.file) | $($r.score) | $($r.killed) | $($r.survived) | $($r.timeout) | $($r.noCoverage) | $($r.total) |")
}
$markdown = $lines -join "`n"

if ($MarkdownOutputPath) {
    $markdown | Set-Content -LiteralPath $MarkdownOutputPath -Encoding utf8
}

$markdown
