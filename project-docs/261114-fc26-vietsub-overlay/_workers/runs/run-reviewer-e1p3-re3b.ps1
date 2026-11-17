$root = 'D:\Project\vietsubps5'
$B = Join-Path $root 'project-docs\261114-fc26-vietsub-overlay\_workers'
$S = 'D:\Temp\claude\d9d3e1a64600903d\e9665b82-ba7d-4af5-805e-bccc099e6d39\scratchpad'
New-Item -ItemType Directory -Force (Join-Path $B 'streams') | Out-Null
Set-Location $root
Get-Content (Join-Path $B 'briefs\reviewer-e1p3-re3b-brief.txt') -Raw -Encoding UTF8 |
  claude -p "Follow the brief on stdin exactly." --resume 6f0ab583-0788-43c6-bb65-76fb5153b73f --model opus --effort medium `
    --setting-sources user --settings (Join-Path $B 'worker-settings.json') `
    --permission-mode acceptEdits --add-dir 'C:\Users\lebatuananh\.nuget' --add-dir $S `
    --add-dir 'C:\Users\lebatuananh\.gradle' --add-dir 'C:\Users\lebatuananh\.android' `
    --add-dir 'C:\Users\lebatuananh\AppData\Local\Android\Sdk' --add-dir 'C:\Program Files\Android\Android Studio\jbr' `
    --allowedTools "Read" "Grep" "Glob" "Write" "Edit" "WebSearch" "WebFetch" "Bash" "PowerShell" `
    --output-format stream-json --verbose *>&1 |
  Out-File (Join-Path $B 'streams\reviewer-e1p3-re3b-stream.jsonl') -Encoding utf8
