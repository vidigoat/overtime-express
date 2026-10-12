#!/bin/zsh
# Helper for driving the Overtime project through the Unity CLI.
export UNITY_NO_BANNER=1 UNITY_NON_INTERACTIVE=1
U=~/.unity/bin/unity; cd ~/Overtime
ev() { $U command eval "$1" 2>&1 | tail -1 | sed -E 's/.*"result":("?[^}]*)\}.*/\1/' | cut -c1-${2:-400}; }
case "$1" in
  compile) $U status --until-ready --project-path ~/Overtime >/dev/null 2>&1
    $U command clear_console >/dev/null 2>&1
    $U recompile --format json 2>/dev/null | python3 -c "
import sys,json
d=json.load(sys.stdin); dd=d.get('data') or {}
print('compile:', dd.get('status'), 'errors', (dd.get('summary') or {}).get('errors'))
for e in (dd.get('errors') or [])[:12]: print(' ', str(e)[:300])"
    $U status --until-ready --project-path ~/Overtime >/dev/null 2>&1 ;;
  play) $U command set_autotick >/dev/null 2>&1; $U command editor_play >/dev/null 2>&1; $U status --until-ready --project-path ~/Overtime >/dev/null 2>&1; ev 'UnityEngine.Application.runInBackground = true; return "playing";' ;;
  stop) $U command editor_stop >/dev/null 2>&1; $U status --until-ready --project-path ~/Overtime >/dev/null 2>&1; echo stopped ;;
  ev) ev "$2" "$3" ;;
  shot) $U command capture_game_view -- --source screen --save_path "Shots/$2.png" --width ${3:-585} --height ${4:-1266} 2>&1 | tail -1 | cut -c1-160 ;;
  when) # when <StaticMember> <value> <shotname> : pause the game the frame the probe exceeds value, screenshot, resume
    $U command wait_for -- --condition "{\"member\":\"Overtime.GameRoot.$2\",\"op\":\"greaterThan\",\"value\":$3}" --timeout_s ${5:-60} --poll_interval_ms 16 --on_met '{"pause":true}' 2>&1 | tail -1 | cut -c1-90
    $U command capture_game_view -- --source screen --save_path "Shots/$4.png" --width 585 --height 1266 2>&1 | tail -1 | cut -c1-60
    ev 'UnityEditor.EditorApplication.isPaused = false; return "resumed";' ;;
  errors) $U command console --format json -- --level error --tail ${2:-6} 2>/dev/null | python3 -c "
import sys,json
d=json.load(sys.stdin)
def find(o):
    if isinstance(o,dict):
        if 'entries' in o: return o['entries']
        for v in o.values():
            r=find(v)
            if r is not None: return r
    if isinstance(o,str) and o.startswith('{'):
        try: return find(json.loads(o))
        except Exception: return None
    if isinstance(o,list):
        for v in o:
            r=find(v)
            if r is not None: return r
    return None
for e in (find(d) or []): print('-', e['message'][:500])
" ;;
esac
