param([string]$Server='localhost')
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
function Invoke-EduSql([string]$File) {
    & sqlcmd -S $Server -E -C -I -b -f 65001 -i (Join-Path $projectRoot $File)
    if($LASTEXITCODE -ne 0){throw "SQL failed: $File"}
}
$exists=& sqlcmd -S $Server -E -C -I -b -h -1 -W -Q "SET NOCOUNT ON;SELECT CASE WHEN DB_ID(N'EduPlayV2') IS NULL THEN 0 ELSE 1 END;"
if($LASTEXITCODE -ne 0){throw 'Cannot connect to SQL Server.'}
if(($exists -join '').Trim() -eq '0'){Invoke-EduSql 'Database/V2/001_CreateDatabase.sql'}
foreach($file in @('002_ReferenceData.sql','003_Operations.sql','004_LearningSamples.sql','005_AdminSecurity.sql','006_ConceptLinks.sql','007_DemoFiveQuestions.sql')){
    Invoke-EduSql "Database/V2/$file"
}
Write-Host 'EduPlayV2 ready. GameDB was not modified.'

