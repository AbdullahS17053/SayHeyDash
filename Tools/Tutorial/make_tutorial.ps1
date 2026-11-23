<#
  The Say Hey Dash - handover tutorial video (Milestone 5).

  Builds Builds/SayHeyDash_Tutorial.mp4 from:
    - title cards (Tools/Tutorial/cards.txt) rendered with System.Drawing,
    - a real screen recording of the Unity Editor running the guided tour
      (Assets/_Project/Scripts/Editor/Handover/EditorTour.cs),
    - captions and narration from Tools/Tutorial/tutorial_script.txt
      (narration = built-in Windows voice, System.Speech),
    - encoded by Unity's own MediaEncoder (TutorialVideoEncoder.cs). No downloads needed.

  Usage (Windows PowerShell 5.1, Unity closed, nobody touching mouse/keyboard while recording):
    powershell -ExecutionPolicy Bypass -File Tools/Tutorial/make_tutorial.ps1              # all stages
    powershell -ExecutionPolicy Bypass -File Tools/Tutorial/make_tutorial.ps1 -Stage compose
  Stages: assets, record, compose, encode, all.
#>
param(
    [string]$Stage = "all",
    [string]$Unity = "D:\Gaming\6000.3.9f1\Editor\Unity.exe",
    [string]$Voice = "Microsoft Zira Desktop",
    [int]$Fps = 12
)
$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$work = Join-Path $root "Builds\Tutorial_work"
$outMp4 = Join-Path $root "Builds\SayHeyDash_Tutorial.mp4"
$W = 1920; $H = 1080; $Strip = 170
New-Item -ItemType Directory -Force $work | Out-Null

Add-Type -AssemblyName System.Drawing, System.Windows.Forms, System.Speech
if (-not ("TutorialImaging" -as [type])) {
    $src = Get-Content -Raw -Encoding UTF8 (Join-Path $PSScriptRoot "TutorialImaging.cs")
    Add-Type -TypeDefinition $src -ReferencedAssemblies System.Drawing, System.Windows.Forms
}

function Read-Steps {
    Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot "tutorial_script.txt") |
        Where-Object { $_.Trim() -and -not $_.StartsWith("#") } |
        ForEach-Object {
            $p = $_.Split("|")
            [pscustomobject]@{ Id = $p[0].Trim(); Kind = $p[1].Trim(); Caption = $p[2].Trim(); Narration = $p[3].Trim() }
        }
}

function Read-Cards {
    $cards = @{}; $cur = $null
    foreach ($line in Get-Content -Encoding UTF8 (Join-Path $PSScriptRoot "cards.txt")) {
        if (-not $line.Trim() -or $line.StartsWith("#")) { continue }
        if ($line -match '^\[(.+)\]$') { $cur = @{ bullets = @(); big = $false; code = $null; footer = ""; kicker = ""; title = "" }; $cards[$Matches[1]] = $cur; continue }
        $k, $v = $line.Split("=", 2)
        switch ($k) { "bullet" { $cur.bullets += $v } "big" { $cur.big = ($v -eq "1") } default { $cur[$k] = $v } }
    }
    $cards
}

function Get-WavSeconds([string]$path) {
    $b = [IO.File]::ReadAllBytes($path)
    $pos = 12; $rate = 48000; $block = 2
    while ($pos + 8 -le $b.Length) {
        $id = [Text.Encoding]::ASCII.GetString($b, $pos, 4); $size = [BitConverter]::ToInt32($b, $pos + 4)
        if ($id -eq "fmt ") { $rate = [BitConverter]::ToInt32($b, $pos + 12); $block = [BitConverter]::ToInt16($b, $pos + 20) }
        if ($id -eq "data") { return [Math]::Min($size, $b.Length - $pos - 8) / ($rate * $block) }
        $pos += 8 + $size + ($size -band 1)
    }
    throw "No data chunk in $path"
}

# ---------------------------------------------------------------------- stage: assets
function Invoke-Assets {
    $steps = Read-Steps; $cards = Read-Cards
    $audioDir = Join-Path $work "audio"; $cardDir = Join-Path $work "cards"
    New-Item -ItemType Directory -Force $audioDir, $cardDir | Out-Null

    $tts = New-Object System.Speech.Synthesis.SpeechSynthesizer
    $tts.SelectVoice($Voice); $tts.Rate = 0; $tts.Volume = 100
    $fmt = New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo 48000, ([System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen), ([System.Speech.AudioFormat.AudioChannel]::Mono)
    $timings = @()
    foreach ($s in $steps) {
        $wav = Join-Path $audioDir "$($s.Id).wav"
        $tts.SetOutputToWaveFile($wav, $fmt); $tts.Speak($s.Narration); $tts.SetOutputToNull()
        $sec = Get-WavSeconds $wav
        "{0,-9} {1,5:0.0}s  {2}" -f $s.Id, $sec, $s.Kind
        if ($s.Kind -eq "tour") { $timings += ("{0} {1}" -f $s.Id, [Math]::Round($sec + 1.4, 2).ToString([Globalization.CultureInfo]::InvariantCulture)) }
    }
    $tts.Dispose()
    Set-Content -Encoding ASCII (Join-Path $work "tour_timings.txt") $timings

    $logo = Join-Path $root "Assets\_Project\Art\Branding\FastballFuel_Logo_Alpha.png"
    foreach ($id in $cards.Keys) {
        $c = $cards[$id]
        [TutorialImaging]::RenderCard((Join-Path $cardDir "$id.jpg"), $W, $H, $logo, $c.kicker, $c.title, [string[]]$c.bullets, $c.code, $c.footer, $c.big)
    }
    "Cards: $($cards.Count) in $cardDir"
}

# ---------------------------------------------------------------------- stage: record
function Invoke-Record {
    $frames = Join-Path $work "capture"
    if (Test-Path $frames) { Remove-Item -Recurse -Force $frames }
    $log = Join-Path $work "tour_log.txt"; $go = Join-Path $work "tour_go.flag"; $stop = Join-Path $work "capture_stop.flag"
    Remove-Item -Force $go, $stop, $log -ErrorAction SilentlyContinue
    $timings = Join-Path $work "tour_timings.txt"

    $uargs = @("-projectPath", "`"$root`"", "-executeMethod", "SayHeyDash.EditorTools.Handover.EditorTour.RunFromCommandLine",
              "-tourTimings", "`"$timings`"", "-tourLog", "`"$log`"", "-tourGo", "`"$go`"", "-tourQuit", "-tourRecording")
    $p = Start-Process $Unity -ArgumentList $uargs -PassThru
    "Unity started (pid $($p.Id)); waiting for the editor window..."
    $deadline = (Get-Date).AddMinutes(8)
    do {
        Start-Sleep -Seconds 2; $p.Refresh()
        $ready = $p.MainWindowTitle -like "*SayHeyDash*Unity*"
    } until ($ready -or $p.HasExited -or (Get-Date) -gt $deadline)
    if (-not $ready) { throw "Unity window did not appear (title '$($p.MainWindowTitle)')." }
    Start-Sleep -Seconds 8   # layout, compile, first repaint
    $p.Refresh()
    $front = [TutorialImaging]::BringToFront($p.MainWindowHandle)
    "Editor window: '$($p.MainWindowTitle)' foreground=$front"
    Start-Sleep -Seconds 2
    $area = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
    New-Item -ItemType File $go | Out-Null
    $n = [TutorialImaging]::CaptureLoop($p.Id, $area.X, $area.Y, $area.Width, $area.Height, $Fps, $frames, $stop, 900)
    "Captured $n frames ($($area.Width)x$($area.Height))."
    if (-not $p.HasExited) { "Unity still running - close it before the encode stage." }
}

# ---------------------------------------------------------------------- stage: compose
function Invoke-Compose {
    $steps = Read-Steps
    $tourSteps = @($steps | Where-Object Kind -eq "tour")
    $log = @{}; $end = $null
    foreach ($line in Get-Content (Join-Path $work "tour_log.txt")) {
        $id, $ms = $line.Trim().Split(" ")
        if ($id -eq "end" -or $id -eq "aborted") { $end = [int64]$ms } elseif (-not $log.ContainsKey($id)) { $log[$id] = [int64]$ms }
    }
    if (-not $end) { throw "Tour log has no end marker." }
    $caps = @(Get-ChildItem (Join-Path $work "capture") -Filter *.jpg | ForEach-Object { [pscustomobject]@{ Ms = [int64]$_.BaseName; Path = $_.FullName } } | Sort-Object Ms)
    "Capture frames: $($caps.Count)"

    $videoDir = Join-Path $work "video"
    if (Test-Path $videoDir) { Remove-Item -Recurse -Force $videoDir }
    New-Item -ItemType Directory -Force $videoDir | Out-Null
    $list = New-Object System.Collections.Generic.List[string]
    $audio = New-Object System.Collections.Generic.List[string]
    $inv = [Globalization.CultureInfo]::InvariantCulture
    $t = 0.0

    $tourIndex = 0; $capIdx = 0
    foreach ($s in $steps) {
        $wav = Join-Path $work "audio\$($s.Id).wav"
        $narr = Get-WavSeconds $wav
        if ($s.Kind -eq "card") {
            $lead = 0.5; $dur = $lead + $narr + 1.0
            $audio.Add(("audio {0} {1}" -f $wav, ($t + $lead).ToString("0.000", $inv)))
            $count = [int][Math]::Ceiling($dur * $Fps)
            $card = Join-Path $work "cards\$($s.Id).jpg"
            for ($k = 0; $k -lt $count; $k++) { $list.Add($card) }
            $t += $count / $Fps
        } else {
            $tourIndex++
            $start = $log[$s.Id]
            $next = $end
            $later = @($tourSteps | Where-Object { $log.ContainsKey($_.Id) -and $log[$_.Id] -gt $start } | ForEach-Object { $log[$_.Id] } | Sort-Object)
            if ($later.Count) { $next = $later[0] }
            $segSec = [Math]::Max(($next - $start) / 1000.0, $narr + 0.6)
            $audio.Add(("audio {0} {1}" -f $wav, ($t + 0.3).ToString("0.000", $inv)))
            $count = [int][Math]::Ceiling($segSec * $Fps)
            $label = "IN THE UNITY EDITOR  {2}  {0} / {1}" -f $tourIndex, $tourSteps.Count, [char]0x00B7
            $cache = @{}
            for ($k = 0; $k -lt $count; $k++) {
                $ms = $start + [int64](1000.0 * $k / $Fps)
                while ($capIdx + 1 -lt $caps.Count -and $caps[$capIdx + 1].Ms -le $ms) { $capIdx++ }
                $src = $caps[$capIdx].Path
                if (-not $cache.ContainsKey($src)) {
                    $out = Join-Path $videoDir ("{0}_{1:D5}.jpg" -f $s.Id, $k)
                    [TutorialImaging]::ComposeTourFrame($src, $out, $W, $H, $Strip, $label, $s.Caption)
                    $cache[$src] = $out
                }
                $list.Add($cache[$src])
            }
            $t += $count / $Fps
        }
    }
    $framesList = Join-Path $work "frames.txt"
    [IO.File]::WriteAllLines($framesList, $list)
    $manifest = @("size $W $H", "fps $Fps", "frames $framesList", "out $outMp4") + $audio
    [IO.File]::WriteAllLines((Join-Path $work "manifest.txt"), [string[]]$manifest)
    "Timeline: {0:0.0} s, {1} frames" -f $t, $list.Count
}

# ---------------------------------------------------------------------- stage: encode
function Invoke-Encode {
    $manifest = Join-Path $work "manifest.txt"
    $uargs = @("-batchmode", "-quit", "-projectPath", "`"$root`"", "-executeMethod",
              "SayHeyDash.EditorTools.Handover.TutorialVideoEncoder.EncodeFromCommandLine",
              "-videoManifest", "`"$manifest`"", "-logFile", "`"$work\encode.log`"")
    $p = Start-Process $Unity -ArgumentList $uargs -PassThru
    $p.WaitForExit()
    Select-String -Path "$work\encode.log" -Pattern "TutorialVideoEncoder\]" | Select-Object -Last 2 | ForEach-Object { $_.Line }
    if ($p.ExitCode -ne 0) { throw "Encoder failed (exit $($p.ExitCode)); see $work\encode.log" }
    Get-Item $outMp4 | Select-Object FullName, Length
}

switch ($Stage) {
    "assets"  { Invoke-Assets }
    "record"  { Invoke-Record }
    "compose" { Invoke-Compose }
    "encode"  { Invoke-Encode }
    "all"     { Invoke-Assets; Invoke-Record; Invoke-Compose; Invoke-Encode }
    default   { throw "Unknown stage $Stage" }
}
