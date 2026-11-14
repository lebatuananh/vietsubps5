#!/bin/bash
# Watcher nền cho một lượt worker (delegating-workers.md §5). In đúng MỘT dòng kết thúc rồi thoát:
#   DONE     report có dòng đầu bắt đầu bằng <done-prefix>, và tiến trình <pid> đã thoát (không truyền pid thì bỏ điều kiện này)
#   DONE-IDLE report đã xong nhưng tiến trình còn sống và stream đứng ≥ 5 phút ⇒ Leader dừng đúng process tree rồi dùng artifact
#   FAILED   event lỗi có cấu trúc trong stream (JSON type error/turn.failed, dòng AGY_ERROR/error:), hoặc pid biến mất khi report chưa xong
#   STUCK    stream và report đứng yên ≥ idleSec, hoặc quá trần tổng (sub: 115 phút)
#   POLL-BROKEN  3 lần liên tiếp không đọc được stream
#   TICK     (chỉ mode api) đã chạy maxSec giây mà chưa kết thúc ⇒ Leader kiểm nhanh rồi đặt lại
#   CHECK    (mode sub, khi truyền checkAt) tới mốc ngó lại mà chưa kết thúc ⇒ Leader đọc đuôi stream, ổn thì bật lại với mốc kế
#
# Hai chế độ theo cách Leader trả tiền:
#   sub  (mặc định) Leader chạy gói subscription (Claude Code, Codex…). Ngó lại theo nhịp giãn dần như người giao việc:
#        checkAt = 120 → 300 → 900 → 1800 (lặp 1800). Mỗi lần CHECK Leader đọc đuôi stream: đi đúng hướng chưa, có kẹt không.
#        Không truyền checkAt ⇒ chỉ lên tiếng ở nhánh kết thúc như trước.
#   api  Leader chạy qua API trả theo token có cache prompt ngắn (OpenRouter): chờ lâu là nạp lại toàn bộ context
#        ⇒ thoát TICK sau ≤ 230 s để Leader kiểm rồi đặt lại.
#
# usage: watch-worker.sh <report> <done-prefix> <stream> [mode=sub|api] [pid] [idleSec=1500] [checkAt]
f="$1"; prefix="$2"; s="$3"; mode="${4:-sub}"; pid="$5"; idle="${6:-1500}"; checkAt="${7:-0}"
case "$mode" in api) maxsec=230; poll=15 ;; *) maxsec=6900; poll=30 ;; esac
start=$(date +%s); lastsig=""; lastchange=$start; reportDoneAt=""; fails=0

alive() { [ -z "$pid" ] && return 1; tasklist //FI "PID eq $pid" 2>/dev/null | grep -q " $pid "; }

while true; do
  now=$(date +%s)
  if [ ! -r "$s" ]; then
    fails=$((fails+1)); [ "$fails" -ge 3 ] && { echo "POLL-BROKEN: cannot read $s"; exit 0; }
  else
    fails=0
    fail=$(node -e "const fs=require('fs');try{for(const l of fs.readFileSync(process.argv[1],'utf8').split(/\r?\n/)){if(/^(AGY_ERROR|error:)/.test(l)){console.log(l.slice(0,300));break}try{const j=JSON.parse(l.replace(/^﻿/,''));if(j.type==='error'||j.type==='turn.failed'){console.log(JSON.stringify(j).slice(0,300));break}}catch(e){}}}catch(e){}" "$s" 2>/dev/null)
    [ -n "$fail" ] && { echo "FAILED: $fail"; exit 0; }
  fi

  first=$(head -1 "$f" 2>/dev/null | tr -d '\r' | sed -e 's/^\xEF\xBB\xBF//' -e 's/^#* *//')
  done=0; [ -n "$first" ] && [[ "$first" == "$prefix"* ]] && done=1
  sig="$(stat -c %s "$s" 2>/dev/null)-$(stat -c %Y "$f" 2>/dev/null)"
  [ "$sig" != "$lastsig" ] && { lastsig=$sig; lastchange=$now; }
  size=$(stat -c %s "$s" 2>/dev/null || echo 0)

  if [ $done -eq 1 ]; then
    [ -z "$reportDoneAt" ] && reportDoneAt=$now
    alive || { echo "DONE: $first (stream=${size}B, $(( (now-start)/60 ))m)"; exit 0; }
    [ $((now-lastchange)) -ge 300 ] && { echo "DONE-IDLE: report done, pid $pid still alive, stream idle $((now-lastchange))s"; exit 0; }
  else
    [ -n "$pid" ] && ! alive && { echo "FAILED: pid $pid exited without report (stream=${size}B)"; exit 0; }
  fi

  [ $((now-lastchange)) -ge $idle ] && { echo "STUCK: no progress ${idle}s (stream=${size}B)"; exit 0; }
  if [ "$mode" != "api" ] && [ "$checkAt" -gt 0 ] && [ $((now-start)) -ge "$checkAt" ]; then
    case "$checkAt" in 120) nxt=300 ;; 300) nxt=900 ;; *) nxt=1800 ;; esac
    echo "CHECK: ${checkAt}s, stream=${size}B, last change $((now-lastchange))s ago, next checkAt=$nxt"; exit 0
  fi
  if [ $((now-start)) -ge $maxsec ]; then
    [ "$mode" = "api" ] && echo "TICK: still running ${maxsec}s, stream=${size}B" || echo "STUCK: over $((maxsec/60))m total (stream=${size}B)"
    exit 0
  fi
  sleep $poll
done
