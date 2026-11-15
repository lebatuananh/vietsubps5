$root = 'D:\Project\vietsubps5'
$B = Join-Path $root 'project-docs\261114-fc26-vietsub-overlay\_workers'
New-Item -ItemType Directory -Force (Join-Path $B 'streams') | Out-Null
Set-Location $root
Get-Content (Join-Path $B 'briefs\sa-discovery-r1-brief.txt') -Raw -Encoding UTF8 |
  claude -p "Follow the brief on stdin exactly." --model opus --effort high `
    --setting-sources user --settings (Join-Path $B 'worker-settings.json') `
    --permission-mode acceptEdits `
    --allowedTools "Read" "Grep" "Glob" "Write" "Edit" "WebSearch" "WebFetch" "Bash(git status:*)" "Bash(git log:*)" "PowerShell(Get-Command:*)" "PowerShell(Get-CimInstance:*)" "PowerShell(Get-ChildItem:*)" "Bash(where:*)" `
    --output-format stream-json --verbose *>&1 |
  Out-File (Join-Path $B 'streams\sa-discovery-r1-stream.jsonl') -Encoding utf8
