[CmdletBinding()]
param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
$lessonNames = @(
    'COC1_L1.asset', 'COC1_L2.asset', 'COC1_L3.asset', 'COC1_L4.asset',
    'COC2_L1.asset', 'COC2_L2.asset', 'COC2_L3.asset', 'COC2_L4.asset',
    'COC2_L5.asset', 'COC2_L6.asset', 'COC2_L7.asset', 'COC2_L8.asset',
    'COC2_L9.asset'
)
$lessonPaths = $lessonNames | ForEach-Object { Join-Path $ProjectRoot "Assets/Scripts/$_" }

$literalCorrections = [ordered]@{
    'techniquesfor' = 'techniques for'
    'icluding' = 'including'
    'Sysytem' = 'System'
    'FIre' = 'Fire'
    'unneccessary' = 'unnecessary'
    'unecessary' = 'unnecessary'
    'TPower Supply' = 'Power Supply'
    'Tools, Equipments' = 'Tools, Equipment'
    'Testing Devices (Diagnostic tools)' = 'Testing Devices (Diagnostic Tools)'
    'spays ink' = 'sprays ink'
    'Advance Technology Attachment' = 'Advanced Technology Attachment'
    'Input Output' = 'Input/Output'
    "Working with PC’s" = 'Working with PCs'
    "Working with PC's" = 'Working with PCs'
    "Data Comm’s" = "DataComm's"
    "Data Comm's" = "DataComm's"
    'wall Plates' = 'wall plates'
    'RJ 45' = 'RJ45'
    'Lan Tester' = 'LAN tester'
    'Add- in' = 'Add-in'
    'Graphics / Video' = 'Graphics/Video'
    'hardcopy' = 'hard copy'
    'table top' = 'tabletop'
    'Point-out' = 'Point out'
    'Explain The PC system' = 'Explain the PC system'
    'What is network design' = 'Explain network design'
    'What is network' = 'Explain computer networks'
    'Plan cable route using designed network' = 'Plan a cable route using a network design'
    'Identify materials used for networking' = 'Identify materials used in networking'
    'Identify the uses of each material' = 'Explain the use of each networking material'
    'How to use the network materials' = 'Demonstrate how to use networking materials'
    'Ensuring no unnecessary Damage' = 'Ensuring No Unnecessary Damage'
    'Creating a testing Regimen' = 'Creating a Testing Regimen'
    'Blind Sports' = 'Blind Spots'
    'Network Analysis?' = 'Network Analysis'
    'Identify how to ensure that no unnecessary damage occurred while performing installation work and complies with requirements' = 'Explain how to prevent unnecessary damage while performing installation work in compliance with requirements'
    'Perform installation work and ensure no unnecessary damage occurred and complies with requirements' = 'Perform installation work in compliance with requirements while preventing unnecessary damage'
    'Identify ways of disposing excess components and materials used in networking' = 'Identify ways to dispose of excess components and materials used in networking'
    'Follow proper ways of segregation of disposal' = 'Follow proper methods for segregating waste for disposal'
    'Follow the 5S Principles in working' = 'Apply the 5S principles in the workplace'
    "Don't forget to follow the 5s in working" = 'Follow the 5S Principles at Work'
    'Do not forget to follow the 5S in working' = 'Follow the 5S Principles at Work'
    'Is a system designed to prevent workplace illnesses and injuries.' = 'This system is designed to prevent workplace illnesses and injuries.'
    "At work you can use these three Think Safe steps to help prevent`naccidents. Using the Think Safe Steps." = 'Use these three Think Safe steps at work to help prevent accidents.'
    'Forms are used to give specific details with regards to the accidents happened in the laboratory during practical activities.' = 'Accident report forms provide specific details about incidents that occur in the laboratory during practical activities.'
    'may cause short circuit' = 'may cause a short circuit'
    'fuses with those proper ratings' = 'fuses with the proper ratings'
    'metal fragmented' = 'metal fragments'
    'Working area should have ventilations, trash can, fire exit and capable of being disinfect.' = 'The work area should have proper ventilation, a trash can, a fire exit, and surfaces that can be disinfected.'
    'Port hub /Port' = 'Port/Hub'
    'Auotomatic Voltage Regulator' = 'Automatic Voltage Regulator'
    'Accelerated Graphic Port' = 'Accelerated Graphics Port'
    'Advance Technology Extended' = 'Advanced Technology Extended'
    'Basic input Output System' = 'Basic Input/Output System'
    'Peripheral Component Interconnector' = 'Peripheral Component Interconnect'
    'Power On Self-Test' = 'Power-On Self-Test'
    'Personal System 2' = 'Personal System/2'
    'mother board' = 'motherboard'
    'disk drivers' = 'disk drives'
    'different manufactures' = 'different manufacturers'
    'from factor' = 'form factor'
    'A(Such' = 'A (such'
    'B (Such' = 'B (such'
    'C(Such' = 'C (such'
    'PS/2- The' = '<b>PS/2</b> — The'
    'USB-USB (Universal Serial Bus) is' = '<b>USB (Universal Serial Bus)</b> — USB is'
    'Firewire- Firewire' = '<b>FireWire</b> — FireWire'
    'Look Figure X' = 'See Figure X'
    'to the corresponding pin on.' = 'to the corresponding pins on the motherboard.'
    'How many kinds of Networks?' = 'Types of Networks'
    'no. of computers' = 'number of computers'
    '10 or less users' = '10 or fewer users'
    'Wide area network' = 'wide area network'
    'Networking in any different way needs materials to be used to in order to work.' = 'Every network installation requires suitable materials to function correctly.'
    'One of the materials are the network cable to be used.' = 'Network cables are among the essential materials used in an installation.'
    'The following are the example of a guided media network cables:' = 'Examples of guided network media include:'
    '8. LAN Card (NIC)- Network Interface Card' = '<b>8. LAN Card (NIC)</b> — A network interface card'
    'Support materials store and protect' = 'These support structures organize and protect'
    'switches, routers, server,' = 'switches, routers, servers,'
    'Wifi (CARD/USB)' = 'Wi-Fi (Card/USB)'
    'In order to set up the networking materials, there are also several tools to be used to accomplish computer networking. manner.' = 'Several tools are also required to install and maintain a computer network.'
    'Tools to be used in networking' = 'Tools Used in Networking'
    'LAN tester- covers the fields of installation and network control.' = '<b>LAN tester</b> — Used for network installation and diagnostics.'
    'connected to port and link connectivity' = 'identify the connected port, and verify link connectivity'
    'impact action”.' = 'impact action.'
    'Take away any liquid near your working area' = 'Remove all liquids from the work area'
    'Do not use excessive force if things do not quite slip into place.' = 'Do not use excessive force if components do not fit into place.'
    'Hold on the handle of the crimping tool and not the area were the blades are located.' = 'Hold the crimping tool by its handles and keep your hands away from the blades.'
    'Contingency measures during workplace accidents, and other emergencies are recognized.' = 'Follow established contingency procedures during workplace accidents and other emergencies.'
    'Contingency measures during workplace accidents, fire and other emergencies are recognized.' = 'Follow established contingency procedures during workplace accidents, fires, and other emergencies.'
    'Use brush, compressed air or blower in cleaning the computer system.' = 'Use a brush, compressed air, or a blower to clean the computer system.'
    'made of few thin wires' = 'made of a few thin wires'
    'thus it used for patch cables' = 'so it is used for patch cables'
    'Plenum Coated CAT5' = 'plenum-rated Cat 5'
    "Ensuring a Specific Level of Cabling Performance UTP" = "<size=115%><b>Ensuring a Specific Level of Cabling Performance</b></size>`n`nUTP"
    'Category 3–,5e–, or 6-compliant' = 'Category 3, 5e, or 6 compliant'
    "cross-connect blocks All patch" = "cross-connect blocks`n`nAll patch"
    'requirements for connecting hardware to insure compatibility' = 'requirements for connecting hardware to ensure compatibility'
    'Possible questions to ask to ensure that there are no unnecessary damages occurred while work installations are:' = 'Use the following questions to confirm that no unnecessary damage occurred during installation:'
    "every one's work" = "everyone's work"
    'consistently- perpetual cleaning' = 'consistently through ongoing cleaning'
    'Keep/Store in box or any storage and not in a garbage bin.' = 'Store unused cable in a box or other storage container rather than a garbage bin.'
    'informations' = 'information'
    'equipments' = 'equipment'
    "there’s who can" = 'there is someone who can'
    "there's who can" = 'there is someone who can'
    'assess to carry out' = 'access to carry out'
    "Don’t" = 'Do not'
}

function Get-LeadingCount([string]$Line) {
    if ($Line -match '^(\s*)') { return $Matches[1].Length }
    return 0
}

function Test-QuotedClosed([string]$Text, [char]$Quote) {
    $trimmed = $Text.TrimEnd()
    if ($trimmed.Length -lt 2 -or $trimmed[$trimmed.Length - 1] -ne $Quote) { return $false }
    if ($Quote -eq "'") { return $true }
    $slashes = 0
    for ($i = $trimmed.Length - 2; $i -ge 0 -and $trimmed[$i] -eq '\'; $i--) { $slashes++ }
    return ($slashes % 2) -eq 0
}

function Join-YamlFlowLines([string[]]$Parts) {
    $builder = [System.Text.StringBuilder]::new()
    $pendingBlank = 0
    foreach ($raw in $Parts) {
        $part = $raw.Trim()
        if ($part.Length -eq 0) { $pendingBlank++; continue }
        if ($builder.Length -gt 0) {
            if ($pendingBlank -gt 0) {
                [void]$builder.Append("`n" * $pendingBlank)
            } else {
                [void]$builder.Append(' ')
            }
        }
        [void]$builder.Append($part)
        $pendingBlank = 0
    }
    return $builder.ToString()
}

function Read-YamlScalar([string[]]$Lines, [int]$Start, [string]$Initial, [int]$Indent) {
    $end = $Start
    $value = $Initial.Trim()
    if ($value.StartsWith('"') -or $value.StartsWith("'")) {
        $quote = $value[0]
        $parts = [System.Collections.Generic.List[string]]::new()
        [void]$parts.Add($value)
        while (-not (Test-QuotedClosed ($parts -join "`n") $quote)) {
            $end++
            if ($end -ge $Lines.Count) { throw "Unterminated YAML scalar at line $($Start + 1)." }
            [void]$parts.Add($Lines[$end])
        }
        $folded = Join-YamlFlowLines $parts.ToArray()
        if ($quote -eq '"') {
            $jsonCompatible = [regex]::Replace($folded, '\\x([0-9A-Fa-f]{2})', { param($match) "\u00$($match.Groups[1].Value)" })
            $jsonCompatible = $jsonCompatible.Replace('\N', '\u0085').Replace('\_', '\u00A0').Replace('\L', '\u2028').Replace('\P', '\u2029')
            $jsonCompatible = $jsonCompatible.Replace('\a', '\u0007').Replace('\v', '\u000B').Replace('\e', '\u001B')
            try { $decoded = ConvertFrom-Json -InputObject $jsonCompatible }
            catch { throw "Invalid double-quoted YAML scalar at line $($Start + 1): $($_.Exception.Message)" }
        } else {
            $decoded = $folded.Substring(1, $folded.Length - 2).Replace("''", "'")
        }
        return [pscustomobject]@{ Value = $decoded; End = $end }
    }

    $parts = [System.Collections.Generic.List[string]]::new()
    [void]$parts.Add($value)
    while ($end + 1 -lt $Lines.Count) {
        $next = $Lines[$end + 1]
        if ($next.Trim().Length -gt 0 -and (Get-LeadingCount $next) -le $Indent) { break }
        $end++
        [void]$parts.Add($next)
    }
    return [pscustomobject]@{ Value = (Join-YamlFlowLines $parts.ToArray()); End = $end }
}

function ConvertTo-YamlQuoted([string]$Value) {
    return ConvertTo-Json -InputObject ([string]$Value) -Compress
}

function Normalize-Text([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) { return '' }
    $text = $Value.Replace("`r`n", "`n").Replace("`r", "`n").Replace("`t", ' ')
    $text = $text.Replace([char]0x00A0, ' ').Replace([char]0x202F, ' ')
    foreach ($item in $literalCorrections.GetEnumerator()) { $text = $text.Replace($item.Key, $item.Value) }
    $text = [regex]::Replace($text, 'Electro-Static', 'Electrostatic', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
    $text = [regex]::Replace($text, 'Anti Static', 'anti-static', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
    $text = [regex]::Replace($text, 'Screw drivers', 'screwdrivers', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
    $text = [regex]::Replace($text, 'Multi-meter', 'multimeter', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
    $text = [regex]::Replace($text, '\bCat5e\b', 'Cat 5e', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
    $text = [regex]::Replace($text, "Don’t", 'do not', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
    $text = [regex]::Replace($text, '\b[Ll][Aa][Nn]\b', 'LAN')
    $text = [regex]::Replace($text, '\bOHS standard\b', 'OHS Standards')
    $text = [regex]::Replace($text, 'Cabinets and Raceway Racks+\b', 'Cabinets and Raceway Racks')
    $text = [regex]::Replace($text, 'Make sure that the pins are properly aligned when connecting a cable connector(?!\.)', 'Make sure that the pins are properly aligned when connecting a cable connector.')
    $text = [regex]::Replace($text, '\b5s\b', '5S', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
    $text = [regex]::Replace($text, '\bRj45\b', 'RJ45', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
    $text = [regex]::Replace($text, '[ ]{2,}', ' ')
    $text = [regex]::Replace($text, ' +([,.;:!?])', '$1')
    $text = [regex]::Replace($text, '([,.;:!?])(?=[A-Za-z])', '$1 ')
    $text = [regex]::Replace($text, '[ ]*\n[ ]*', "`n")
    $text = [regex]::Replace($text, '\n{3,}', "`n`n")
    return $text.Trim()
}

function Uppercase-First([string]$Value) {
    for ($i = 0; $i -lt $Value.Length; $i++) {
        if (-not [char]::IsLetter($Value[$i])) { continue }
        if ([char]::IsUpper($Value[$i])) { return $Value }
        return $Value.Substring(0, $i) + [char]::ToUpperInvariant($Value[$i]) + $Value.Substring($i + 1)
    }
    return $Value
}

function Clean-SingleLine([string]$Value, [bool]$KeepBullet) {
    $text = (Normalize-Text $Value).Replace("`n", ' ')
    $text = [regex]::Replace($text, '\s{2,}', ' ').Trim()
    if (-not $KeepBullet) { $text = [regex]::Replace($text, '^[•\-–—→●]\s*', '') }
    return $text
}

function Clean-Objective([string]$Value) {
    $text = Clean-SingleLine $Value $true
    $text = [regex]::Replace($text, '^[•\-–—→●]\s*', '').Trim()
    $text = Uppercase-First $text
    if ($text -notmatch '[.!?]$') { $text += '.' }
    return "• $text"
}

function Strip-Tags([string]$Value) { return [regex]::Replace($Value, '<.*?>', '') }

function Format-Definition([string]$Line) {
    if ($Line.StartsWith('<') -or $Line.StartsWith('•')) { return $Line }
    $match = [regex]::Match($Line, "^([A-Za-z0-9][A-Za-z0-9 /&()'’.+\-]{1,52})\s+(?:-|–|—)\s+(?:is\s+|it\s+is\s+)?(.+)$")
    if (-not $match.Success) { return $Line }
    $term = $match.Groups[1].Value.Trim()
    $explanation = Uppercase-First $match.Groups[2].Value.Trim()
    return "<b>$term</b> — $explanation"
}

function Clean-Slide([string]$Value) {
    $text = Normalize-Text $Value
    if ($text.Length -eq 0) { return $text }
    $lines = [System.Collections.Generic.List[string]]::new()
    foreach ($line in $text.Split("`n")) { [void]$lines.Add($line.Trim()) }
    while ($lines.Count -gt 0 -and [string]::IsNullOrWhiteSpace($lines[0])) { $lines.RemoveAt(0) }
    while ($lines.Count -gt 0 -and [string]::IsNullOrWhiteSpace($lines[$lines.Count - 1])) { $lines.RemoveAt($lines.Count - 1) }

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ([string]::IsNullOrEmpty($line)) { continue }
        $line = [regex]::Replace($line, '^[•●→*]\s*', '• ')
        $line = [regex]::Replace($line, '^[-–—]\s*', '• ')
        $line = [regex]::Replace($line, '^(\d+)\.\)\s*', '$1. ')
        $line = [regex]::Replace($line, '^(\d+)\)\s*', '$1. ')
        $line = [regex]::Replace($line, '^([a-z])\)\s*', '$1. ', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
        $line = [regex]::Replace($line, '^Step\s+(\d+)\s*[.\-–—:]?\s*(.+)$', '<b>Step $1: $2</b>', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
        $line = [regex]::Replace($line, '^(\d+\.\s+[^:]{2,60}:)\s*$', '<b>$1</b>')
        $lines[$i] = (Format-Definition $line).Trim()
    }

    $first = -1
    for ($i = 0; $i -lt $lines.Count; $i++) { if (-not [string]::IsNullOrWhiteSpace($lines[$i])) { $first = $i; break } }
    if ($first -ge 0 -and $first + 1 -lt $lines.Count -and [string]::IsNullOrWhiteSpace($lines[$first + 1]) -and -not $lines[$first].Contains('<b>')) {
        $plain = (Strip-Tags $lines[$first]).Trim().TrimEnd('.', ':')
        $words = ($plain -split '\s+' | Where-Object { $_ }).Count
        if ($plain.Length -le 90 -and $words -le 10 -and $plain -notmatch '^\d+[.)]\s' -and -not $plain.StartsWith('•')) {
            $lines[$first] = "<size=115%><b>$plain</b></size>"
        }
    }
    $result = ($lines -join "`n").Trim()
    $result = [regex]::Replace($result, '\n{3,}', "`n`n")
    $result = [regex]::Replace($result, '(</size>\n\n)([a-z])', { param($match) $match.Groups[1].Value + $match.Groups[2].Value.ToUpperInvariant() })
    $result = [regex]::Replace($result, '(<b>15\. LED</b> — )Liquid Crystal Display', '$1Light-Emitting Diode')
    return $result
}

function Get-ProtectedSignature([string]$Text) {
    $matches = [regex]::Matches($Text, '(?m)^\s+(?:lessonID|cocID|requiredGoldLessonID|subtopicID|slideType|image):.*$')
    return ($matches | ForEach-Object { $_.Value.TrimEnd() }) -join "`n"
}

function Get-TextFields([string[]]$Lines) {
    $fields = [System.Collections.Generic.List[object]]::new()
    $insideObjectives = $false
    for ($i = 0; $i -lt $Lines.Count; $i++) {
        $line = $Lines[$i]
        if ($line -match '^  learningObjectives:\s*$') { $insideObjectives = $true; continue }
        if ($insideObjectives -and $line -match '^  [A-Za-z]') { $insideObjectives = $false }

        if ($insideObjectives -and $line -match '^(  - )(.+)$') {
            $scalar = Read-YamlScalar $Lines $i $Matches[2] 2
            [void]$fields.Add([pscustomobject]@{ Start=$i; End=$scalar.End; Prefix=$Matches[1]; Kind='objective'; Value=$scalar.Value })
            $i = $scalar.End
            continue
        }
        if ($line -match '^(\s*)(lessonTitle|description|subtopicName|content):\s*(.*)$') {
            $prefix = "$($Matches[1])$($Matches[2]): "
            $scalar = Read-YamlScalar $Lines $i $Matches[3] $Matches[1].Length
            [void]$fields.Add([pscustomobject]@{ Start=$i; End=$scalar.End; Prefix=$prefix; Kind=$Matches[2]; Value=$scalar.Value })
            $i = $scalar.End
        }
    }
    return $fields
}

if ($lessonPaths.Count -ne 13 -or ($lessonPaths | Where-Object { -not (Test-Path -LiteralPath $_) }).Count -gt 0) {
    throw 'The explicit 13-lesson allowlist is incomplete.'
}

$initial = @{}
$slideCount = 0
foreach ($path in $lessonPaths) {
    $raw = [IO.File]::ReadAllText($path)
    $initial[$path] = Get-ProtectedSignature $raw
    $slideCount += [regex]::Matches($raw, '(?m)^    slideType:').Count
}
if ($slideCount -ne 223) { throw "Expected 223 slides; found $slideCount." }

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
$backupFolder = Join-Path $ProjectRoot ".lesson-backups/$stamp"
[IO.Directory]::CreateDirectory($backupFolder) | Out-Null
$manifest = [System.Collections.Generic.List[string]]::new()
[void]$manifest.Add('ARDENT LessonData pre-cleanup backup')
[void]$manifest.Add("Created: $([DateTime]::Now.ToString('O'))")
foreach ($path in $lessonPaths) {
    Copy-Item -LiteralPath $path -Destination (Join-Path $backupFolder ([IO.Path]::GetFileName($path)))
    if (Test-Path -LiteralPath "$path.meta") { Copy-Item -LiteralPath "$path.meta" -Destination (Join-Path $backupFolder "$([IO.Path]::GetFileName($path)).meta") }
    [void]$manifest.Add("$([IO.Path]::GetFileName($path))`t$((Get-FileHash -Algorithm SHA256 -LiteralPath $path).Hash)")
}
[IO.File]::WriteAllLines((Join-Path $backupFolder 'manifest.txt'), $manifest, [Text.UTF8Encoding]::new($false))

$changedFields = 0
$changedLessons = 0
foreach ($path in $lessonPaths) {
    $raw = [IO.File]::ReadAllText($path)
    $lines = [regex]::Split($raw, '\r?\n')
    $fields = @(Get-TextFields $lines)
    $lessonChanged = $false
    foreach ($field in ($fields | Sort-Object Start -Descending)) {
        switch ($field.Kind) {
            'lessonTitle' { $cleaned = Clean-SingleLine $field.Value $false }
            'description' { $cleaned = Uppercase-First (Clean-SingleLine $field.Value $false) }
            'subtopicName' { $cleaned = (Clean-SingleLine $field.Value $false).TrimEnd('.', ':') }
            'objective' { $cleaned = Clean-Objective $field.Value }
            'content' { $cleaned = Clean-Slide $field.Value }
        }
        if ($cleaned -ceq $field.Value) { continue }
        $replacement = $field.Prefix + (ConvertTo-YamlQuoted $cleaned)
        $before = if ($field.Start -gt 0) { $lines[0..($field.Start - 1)] } else { @() }
        $after = if ($field.End + 1 -lt $lines.Count) { $lines[($field.End + 1)..($lines.Count - 1)] } else { @() }
        $lines = @($before) + @($replacement) + @($after)
        $changedFields++
        $lessonChanged = $true
    }
    if ($lessonChanged) {
        $newRaw = ($lines -join "`r`n")
        $newSignature = Get-ProtectedSignature $newRaw
        if ($newSignature -ne $initial[$path]) {
            $oldParts = @($initial[$path] -split "`n")
            $newParts = @($newSignature -split "`n")
            $limit = [Math]::Max($oldParts.Count, $newParts.Count)
            for ($signatureIndex = 0; $signatureIndex -lt $limit; $signatureIndex++) {
                $oldPart = if ($signatureIndex -lt $oldParts.Count) { $oldParts[$signatureIndex] } else { '<missing>' }
                $newPart = if ($signatureIndex -lt $newParts.Count) { $newParts[$signatureIndex] } else { '<missing>' }
                if ($oldPart -ne $newPart) {
                    throw "Protected structure changed before save: $path at item $signatureIndex; before=[$oldPart], after=[$newPart]"
                }
            }
        }
        [IO.File]::WriteAllText($path, $newRaw, [Text.UTF8Encoding]::new($false))
        $changedLessons++
    }
}

$finalSlides = 0
foreach ($path in $lessonPaths) {
    $raw = [IO.File]::ReadAllText($path)
    if ((Get-ProtectedSignature $raw) -ne $initial[$path]) { throw "Protected structure changed after save: $path" }
    $finalSlides += [regex]::Matches($raw, '(?m)^    slideType:').Count
    $lines = [regex]::Split($raw, '\r?\n')
    foreach ($field in @(Get-TextFields $lines)) {
        if ($null -eq $field.Value) { throw "Null text field in $path" }
        if ($field.Value.Contains("`r") -or $field.Value.Contains("`t")) { throw "Unnormalized whitespace in $path" }
        if (([regex]::Matches($field.Value, '<b>').Count -ne [regex]::Matches($field.Value, '</b>').Count) -or
            ([regex]::Matches($field.Value, '<size=').Count -ne [regex]::Matches($field.Value, '</size>').Count)) {
            throw "Unbalanced TextMeshPro markup in $path"
        }
    }
}
if ($finalSlides -ne 223) { throw "Post-cleanup slide count changed to $finalSlides." }

$validation = @(
    'Validation: PASS',
    'Lessons: 13',
    'Slides: 223',
    "Changed lessons: $changedLessons",
    "Changed text fields: $changedFields",
    'Protected fields unchanged: lesson IDs, COC IDs, unlock IDs, subtopic IDs, slide ordering, slide types, images'
)
[IO.File]::WriteAllLines((Join-Path $backupFolder 'validation.txt'), $validation, [Text.UTF8Encoding]::new($false))
Write-Output ($validation -join "`n")
Write-Output "Backup: $backupFolder"
