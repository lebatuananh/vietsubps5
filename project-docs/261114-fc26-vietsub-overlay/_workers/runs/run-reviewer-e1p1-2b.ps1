$root = 'D:\Project\vietsubps5'
$B = Join-Path $root 'project-docs\261114-fc26-vietsub-overlay\_workers'
New-Item -ItemType Directory -Force (Join-Path $B 'streams') | Out-Null
Set-Location $root
Get-Content (Join-Path $B 'briefs\reviewer-e1p1-2b-brief.txt') -Raw -Encoding UTF8 |
  claude -p "Follow the brief on stdin exactly." --resume 6f0ab583-0788-43c6-bb65-76fb5153b73f --model opus --effort high `
    --setting-sources user --settings (Join-Path $B 'worker-settings.json') `
    --permission-mode acceptEdits --add-dir 'C:\Users\lebatuananh\.nuget' --add-dir 'C:\ffmpeg-n8.1.2-win64-gpl-8.1' `
    --add-dir 'C:\Users\lebatuananh\.gradle' --add-dir 'C:\Users\lebatuananh\.android' --add-dir 'C:\Users\lebatuananh\AppData\Local\Android\Sdk' --add-dir 'C:\Program Files\Android\Android Studio\jbr' `
    --allowedTools "Read" "Grep" "Glob" "Write" "Edit" "WebFetch" "WebSearch" "Bash" "PowerShell" `
    --output-format stream-json --verbose *>&1 |
  Out-File (Join-Path $B 'streams\reviewer-e1p1-2b-stream.jsonl') -Encoding utf8
