$root = 'D:\Project\vietsubps5'
$B = Join-Path $root 'project-docs\261114-fc26-vietsub-overlay\_workers'
New-Item -ItemType Directory -Force (Join-Path $B 'streams') | Out-Null
Set-Location $root
Get-Content (Join-Path $B 'briefs\sa-mockup-r2-brief.txt') -Raw -Encoding UTF8 |
  claude -p "Follow the brief on stdin exactly." --resume b118b56d-eabb-42df-bd25-4a007a6ce832 --model opus --effort medium `
    --setting-sources user --settings (Join-Path $B 'worker-settings.json') `
    --permission-mode acceptEdits --add-dir 'C:\ffmpeg-n8.1.2-win64-gpl-8.1' `
    --allowedTools "Read" "Grep" "Glob" "Write" "Edit" "WebSearch" "WebFetch" "Bash" "PowerShell" "Bash(git status:*)" "Bash(git log:*)" "PowerShell(Get-Command:*)" "PowerShell(Get-CimInstance:*)" "PowerShell(Get-ChildItem:*)" "Bash(where:*)" `
    --output-format stream-json --verbose *>&1 |
  Out-File (Join-Path $B 'streams\sa-mockup-r2-stream.jsonl') -Encoding utf8
