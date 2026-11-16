$root = 'D:\Project\vietsubps5'
$B = Join-Path $root 'project-docs\261114-fc26-vietsub-overlay\_workers'
$S = 'D:\Temp\claude\d9d3e1a64600903d\e9665b82-ba7d-4af5-805e-bccc099e6d39\scratchpad\probe'
New-Item -ItemType Directory -Force (Join-Path $B 'streams') | Out-Null
Set-Location $root
Get-Content (Join-Path $B 'briefs\dev-e1p1-fix2a-brief.txt') -Raw -Encoding UTF8 |
  claude -p "Follow the brief on stdin exactly." --resume c22fb9fd-d0fa-4e9d-a847-5dde6c2de3ea --model sonnet --effort medium `
    --setting-sources user --settings (Join-Path $B 'worker-settings.json') `
    --permission-mode acceptEdits --add-dir $S --add-dir 'C:\Users\lebatuananh\.nuget' --add-dir 'C:\ffmpeg-n8.1.2-win64-gpl-8.1' `
    --allowedTools "Read" "Grep" "Glob" "Write" "Edit" "WebFetch" "WebSearch" "Bash" "PowerShell" `
    --output-format stream-json --verbose *>&1 |
  Out-File (Join-Path $B 'streams\dev-e1p1-fix2a-stream.jsonl') -Encoding utf8
