param(
    [string]$VaultRoot = (Join-Path (Split-Path (Split-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) -Parent) -Parent) "regulatory-source-vault\B6.0-2026-07-20")
)

$ErrorActionPreference = "Stop"
$acquiredAt = [DateTime]::UtcNow.ToString("o")

$sources = @(
    # National EMS licensing and standards baseline.
    @{ Code="ZA-EMS-2017"; Scope="national"; Province="ZA"; Class="Regulation"; Url="https://www.gov.za/sites/default/files/gcis_document/201712/41287gon1320.pdf"; File="41287-gon1320-ems-regulations-2017.pdf" },
    @{ Code="ZA-EMS-STDS-2021-PROPOSED"; Scope="national"; Province="ZA"; Class="Proposed regulation"; Url="https://www.gov.za/sites/default/files/gcis_document/202102/44161gon94.pdf"; File="44161-gon94-proposed-ems-standards-2021.pdf" },
    @{ Code="ZA-EMS-STDS-2022"; Scope="national"; Province="ZA"; Class="Regulation"; Url="https://www.gov.za/sites/default/files/gcis_document/202212/47632gon2819.pdf"; File="47632-gon2819-ems-standards-2022.pdf" },
    @{ Code="ZA-EVENT-EMS-2017"; Scope="national"; Province="ZA"; Class="Regulation"; Url="https://www.gov.za/sites/default/files/gcis_document/201706/40919gon566s.pdf"; File="40919-gon566-event-ems-2017.pdf" },
    @{ Code="ZA-NHA"; Scope="national"; Province="ZA"; Class="Act directory"; Url="https://www.gov.za/documents/national-health-act"; File="national-health-act-official-page.html" },
    @{ Code="ZA-MEDICINES"; Scope="national"; Province="ZA"; Class="Act directory"; Url="https://www.gov.za/documents/medicines-and-related-substances-act"; File="medicines-act-official-page.html" },
    @{ Code="ZA-MED-SCHEDULES"; Scope="national"; Province="ZA"; Class="Official schedules directory"; Url="https://www.gov.za/documents/medicines-and-related-substances-act-schedules-0"; File="medicines-schedules-official-page.html" },
    @{ Code="ZA-NDOH-LICENSING"; Scope="national"; Province="ZA"; Class="Official licensing guidance"; Url="https://www.health.gov.za/licensing-home/"; File="ndoh-medicines-licensing.html" },
    @{ Code="ZA-SAHPRA-DESTRUCTION"; Scope="national"; Province="ZA"; Class="Regulator guideline directory"; Url="https://www.sahpra.org.za/document/guideline-for-destruction-of-medicines-and-scheduled-substances/"; File="sahpra-destruction-guideline-page.html" },
    @{ Code="ZA-HPCSA-EC-REG"; Scope="national"; Province="ZA"; Class="Statutory council administrative source"; Url="https://www.hpcsa.co.za/board/emergency-care/registration"; File="hpcsa-emergency-care-registration.html" },
    @{ Code="ZA-HPCSA-MAINT"; Scope="national"; Province="ZA"; Class="Statutory council administrative source"; Url="https://www.hpcsa.co.za/page-2/maintenance-of-registration"; File="hpcsa-maintenance-registration.html" },
    @{ Code="ZA-HPCSA-CPD"; Scope="national"; Province="ZA"; Class="Statutory council policy directory"; Url="https://www.hpcsa.co.za/board/emergency-care/cpd"; File="hpcsa-emergency-care-cpd.html" },
    @{ Code="ZA-HPCSA-SCOPE"; Scope="national"; Province="ZA"; Class="Statutory scope directory"; Url="https://www.hpcsa.co.za/scope-of-professions"; File="hpcsa-scope-directory.html" },
    @{ Code="ZA-HPCSA-EC-GUIDELINES"; Scope="national"; Province="ZA"; Class="Statutory council guideline directory"; Url="https://www.hpcsa.co.za/board/emergency-care/guidelines"; File="hpcsa-emergency-care-guidelines.html" },
    @{ Code="ZA-OHSC-DIRECTORY"; Scope="national"; Province="ZA"; Class="Regulator source directory"; Url="https://ohsc.org.za/acts-and-bills/"; File="ohsc-acts-and-bills.html" },
    @{ Code="ZA-OHS"; Scope="national"; Province="ZA"; Class="Act directory"; Url="https://www.gov.za/documents/occupational-health-and-safety-act"; File="ohs-act-official-page.html" },
    @{ Code="ZA-HBA"; Scope="national"; Province="ZA"; Class="Regulation"; Url="https://www.labour.gov.za/DocumentCenter/Regulations%20and%20Notices/Regulations/Occupational%20Health%20and%20Safety/Regulations%20for%20Hazardous%20Bilogical%20Agents.pdf"; File="hazardous-biological-agents-regulations.pdf" },
    @{ Code="ZA-HCW"; Scope="national"; Province="ZA"; Class="Regulation"; Url="https://www.gov.za/sites/default/files/gcis_document/201409/37654rg10195gon375.pdf"; File="37654-gon375-health-care-risk-waste.pdf" },
    @{ Code="ZA-POPIA"; Scope="national"; Province="ZA"; Class="Act directory"; Url="https://www.gov.za/documents/protection-personal-information-act"; File="popia-official-page.html" },
    @{ Code="ZA-NRTA"; Scope="national"; Province="ZA"; Class="Act directory"; Url="https://www.gov.za/documents/national-road-traffic-act"; File="national-road-traffic-act-page.html" },
    @{ Code="ZA-PRDP"; Scope="national"; Province="ZA"; Class="Official service guidance"; Url="https://www.gov.za/services/driving-licence/professional-driving-permit"; File="professional-driving-permit-page.html" },
    @{ Code="ZA-IPC"; Scope="national"; Province="ZA"; Class="National guidance directory"; Url="https://knowledgehub.health.gov.za/elibrary/national-infection-prevention-and-control-strategic-framework-2020"; File="national-ipc-framework-page.html" },

    # Provincial official sources. Missing packs remain explicitly incomplete.
    @{ Code="ZA-WC-ACT-2010"; Scope="province"; Province="WC"; Class="Provincial Act"; Url="https://www.westerncape.gov.za/department-premier/files/wcg-blob-files?file=documents%2Fgazette-2010-01-6693-prov-gaz-6693ex-87421cf7.pdf&type=file"; File="wc-ambulance-services-act-2010.pdf" },
    @{ Code="ZA-WC-REGS-2012"; Scope="province"; Province="WC"; Class="Provincial regulation"; Url="https://d7.westerncape.gov.za/sites/www.westerncape.gov.za/files/western_cape_ambulance_services_regulations_2012.pdf"; File="wc-ambulance-services-regulations-2012.pdf" },
    @{ Code="ZA-WC-LICENSING"; Scope="province"; Province="WC"; Class="Official licensing portal"; Url="https://www.westerncape.gov.za/health-wellness/private-health-establishment-licensing-application-adverts"; File="wc-private-health-licensing-adverts.html" },
    @{ Code="ZA-KZN-LICENSING"; Scope="province"; Province="KZN"; Class="Official licensing portal"; Url="https://ems-licensing-nhi.kznhealth.gov.za/landing"; File="kzn-ems-licensing-portal.html" },
    @{ Code="ZA-KZN-EMS"; Scope="province"; Province="KZN"; Class="Official EMS information"; Url="https://www2.kznhealth.gov.za/ems.htm"; File="kzn-ems-information.html" },
    @{ Code="ZA-FS-LICENSING"; Scope="province"; Province="FS"; Class="Official licensing portal"; Url="https://fsh-careinfo.fshealth.gov.za/EMS_Front/"; File="free-state-ems-licensing-portal.html" },
    @{ Code="ZA-GP-NOTICE-2025"; Scope="province"; Province="GP"; Class="Official application notice"; Url="https://cmbinary.gauteng.gov.za/Media?path=Media%2FEMS%2FNotice+of+EMS+Applications+received+as+of+August+2025.pdf"; File="gauteng-ems-applications-2025.pdf" },
    @{ Code="ZA-EC-ADVERTS"; Scope="province"; Province="EC"; Class="Official application notices"; Url="https://www.echealth.gov.za/index.php/ems-adverts"; File="eastern-cape-ems-adverts.html" },
    @{ Code="ZA-LP-HEALTH"; Scope="province"; Province="LP"; Class="Official department source directory"; Url="https://www.ldoh.gov.za/"; File="limpopo-health-home.html" },
    @{ Code="ZA-MP-HEALTH"; Scope="province"; Province="MP"; Class="Official department source directory"; Url="https://www.mpuhealth.gov.za/"; File="mpumalanga-health-home.html" },
    @{ Code="ZA-NW-HEALTH"; Scope="province"; Province="NW"; Class="Official department source directory"; Url="https://health.nwpg.gov.za/"; File="north-west-health-home.html" },
    @{ Code="ZA-NW-EMS-NOTICE"; Scope="province"; Province="NW"; Class="Official licensing notice directory"; Url="https://health.nwpg.gov.za/Tenders"; File="north-west-health-tenders.html" },
    @{ Code="ZA-NC-HEALTH"; Scope="province"; Province="NC"; Class="Official department source directory"; Url="https://www.ncgov.co.za/health"; File="northern-cape-health-page.html" }
)

$provinceDirectories = @("national", "WC", "EC", "FS", "GP", "KZN", "LP", "MP", "NW", "NC")
foreach ($directory in $provinceDirectories) {
    New-Item -ItemType Directory -Path (Join-Path $VaultRoot $directory) -Force | Out-Null
}

$manifest = foreach ($source in $sources) {
    $directory = if ($source.Scope -eq "national") { "national" } else { $source.Province }
    $sourceDirectory = Join-Path (Join-Path $VaultRoot $directory) $source.Code
    $originalDirectory = Join-Path $sourceDirectory "original"
    New-Item -ItemType Directory -Path $originalDirectory -Force | Out-Null
    $target = Join-Path $originalDirectory $source.File

    $status = "Acquired"
    $errorMessage = ""
    try {
        if (-not (Test-Path -LiteralPath $target) -or (Get-Item -LiteralPath $target).Length -eq 0) {
            & curl.exe --location --fail --silent --show-error --max-time 15 --retry 1 --user-agent "AcuityOps-Regulatory-Source-Acquisition/1.0" --output $target $source.Url
            if ($LASTEXITCODE -ne 0) { throw "curl exited with code $LASTEXITCODE" }
        }
    }
    catch {
        $status = "AcquisitionFailed"
        $errorMessage = $_.Exception.Message
        if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Force }
    }

    $hash = ""
    $size = 0
    if ($status -eq "Acquired") {
        $item = Get-Item -LiteralPath $target
        $size = $item.Length
        $hash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash.ToLowerInvariant()
    }

    [pscustomobject]@{
        SourceCode = $source.Code
        Scope = $source.Scope
        Province = $source.Province
        SourceClass = $source.Class
        OfficialUrl = $source.Url
        RetainedFile = if ($status -eq "Acquired") { $target } else { "" }
        Bytes = $size
        SHA256 = $hash
        AcquiredUtc = $acquiredAt
        Status = $status
        Error = $errorMessage
        ReviewState = "Unreviewed"
        AuthorityState = "NotActive"
    }
}

$manifestPath = Join-Path $VaultRoot "source-manifest.csv"
$manifest | Export-Csv -LiteralPath $manifestPath -NoTypeInformation -Encoding UTF8

$summary = [pscustomobject]@{
    Batch = "B6.0"
    CreatedUtc = $acquiredAt
    VaultRoot = $VaultRoot
    TotalSources = $manifest.Count
    Acquired = @($manifest | Where-Object Status -eq "Acquired").Count
    Failed = @($manifest | Where-Object Status -ne "Acquired").Count
    ManifestSHA256 = (Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash.ToLowerInvariant()
    LegalApproval = "NotPerformed"
    OperationalApproval = "NotPerformed"
    RequirementActivation = "Prohibited"
}
$summary | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $VaultRoot "acquisition-summary.json") -Encoding UTF8

$manifest | Format-Table SourceCode, Province, Status, Bytes -AutoSize
$summary | Format-List
