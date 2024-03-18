$psVersion = $PSVersionTable.PSVersion

# In ra phiên bản PowerShell
Write-Host "PowerShell Version: $($psVersion.Major).$($psVersion.Minor).$($psVersion.Build)"

$ISLOCAL = $env:ISLOCAL
if (-not $ISLOCAL) {
    $ISLOCAL = $true
}

if ($ISLOCAL -eq $true) {
    Write-Host "Assign local variable"

    $localXmlString = Get-Content -Raw -Path "local.config"
	
    # Tạo đối tượng XmlDocument và load chuỗi XML vào nó
    $localXmlDoc = New-Object System.Xml.XmlDocument
    $localXmlDoc.PreserveWhitespace = $true
    $localXmlDoc.LoadXml($localXmlString)

    $TOKEN = $localXmlDoc.configuration.GITHUB_TOKEN
    $OWNER = $localXmlDoc.configuration.REPO_OWNER
    $REPO = $localXmlDoc.configuration.REPO_NAME
    $PR_MESSAGE = $localXmlDoc.configuration.PR_MESSAGE

}
else {
    $TOKEN = $env:GITHUB_TOKEN
    $OWNER = $env:REPO_OWNER
    $REPO = $env:REPO_NAME
    $PR_MESSAGE = $env:PR_MESSAGE
}

if (-not $TOKEN) {
    throw "GITHUB_TOKEN must not be null "
}
if (-not $OWNER) {
    throw "REPO_OWNER must not be null "
}
if (-not $REPO) {
    throw "REPO_NAME must not be null "
}
if (-not $PR_MESSAGE) {
    throw "PR_MESSAGE must not be null "
}

Write-Host "###########__________START CHECKING PULL REQUEST TITLE__________###########"

$prPattern = '^\[#(\d+)\] .+$'
$match = [regex]::Match($PR_MESSAGE, $prPattern)

if ( $match.Success ) {
    $issue_number = $match.Groups[1].Value
    Write-Host "$PR_MESSAGE : matches format"
    Write-Host "issue_number = $issue_number "
    $apiUrl = "https://api.github.com/repos/$OWNER/$REPO/issues/$issue_number"
    $header = @{
        Authorization = "token $TOKEN"
    }
    try {
        $response = Invoke-RestMethod -Uri $apiUrl -Headers $header -Method Get
        $html_url = $response.html_url
        if ($html_url -match "https://github.com/$OWNER/$REPO/issues/\d+") {
            Write-Host "Matched with html_url: $html_url"
        }
        else {
            throw "Id:$issue_number may not be an issue."
        }
    }
    catch {
        throw "Not found issue:$issue_number in $OWNER/$REPO."
    }
}
else {
    Write-Host "$PR_MESSAGE : NOT matches format"
    throw "$PR_MESSAGE NOT matches format"
}

Write-Host "###########__________START RUNNING UNIT TEST__________###########"
dotnet clean
dotnet restore
dotnet test
