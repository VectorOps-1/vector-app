# Block 6 South African Private Ambulance Compliance Source Register

Status: Research baseline; B6.1 governance foundation implemented; no source or requirement approved for activation

Verified on: 2026-07-19

Progress authority: `docs/specs/commercial-launch-progress-tracker.md`

## Purpose

This register controls the legal and regulatory sources from which AcuityOps may
build South African Private Ambulance Service Annual Inspection Mode rules. It
does not itself give legal advice and it does not activate compliance rules.

No requirement may be shown as an authoritative legal or licensing requirement
until its exact source, version, clause or page, jurisdiction, service-type
applicability, effective date, and legal-review status have been recorded.

## Mandatory Source States

Every source and every extracted requirement entered into the governed product
registry must carry one of these states:

1. `Draft`: a candidate official source has been identified but is not yet
   authoritative or available to tenant evaluation.
2. `Acquired`: the exact document/version has been retained with a content hash.
3. `ClauseExtracted`: the relevant clause, page, form field, or inspection item
   has been transcribed and independently checked against the retained source.
4. `SourceVerified`: authority, publication details, dates, and jurisdiction
   have been verified.
5. `LegalReviewPending`: technically verified but not approved for authoritative
   client-facing use.
6. `Approved`: approved by qualified South African legal/regulatory review.
7. `Active`: included in a published, versioned compliance pack.
8. `Superseded`, `Withdrawn`, or `Blocked`: retained for history but unavailable
   for new evaluations.

`Discovered` remains a research-register annotation in the tables below. It is
not a governed product lifecycle state and maps to `Draft` only when an
authorized product compliance administrator deliberately creates a registry
record. B6.1 imports or creates none of these research entries automatically.

Official guidance, a regulator portal, a licensing advert, a statute, a service
standard, and an inspection checklist are different source classes. AcuityOps
must never silently treat one class as another.

## National Source Register

| ID | Source | Authority and identifier | Date/effective state | Classification | Exact use in Block 6 | Current state and unresolved work |
| --- | --- | --- | --- | --- | --- | --- |
| ZA-EMS-2017 | [Regulations Relating to Emergency Medical Services](https://www.gov.za/documents/notices/national-health-act-regulations-emergency-medical-services-english-isizulu-01-dec) ([official PDF](https://www.gov.za/sites/default/files/gcis_document/201712/41287gon1320.pdf)) | National Department of Health; National Health Act; GN 1320; Government Gazette 41287 | Published 1 Dec 2017 | Binding national EMS regulation | Primary national licensing and operating baseline for ground ambulance, aeromedical, event, volunteer and EMS education-provider applicability as stated in the Regulations | `SourceVerified`; exact Annexure A/B and every operative clause must be extracted from the final PDF before rule activation. Confirm amendments and current enforceability through legal review. |
| ZA-EMS-STDS-2022 | [Regulations Relating to Standards for Emergency Medical Services](https://www.gov.za/documents/notices/national-health-act-regulations-relating-standards-emergency-medical-services-02) ([official PDF](https://www.gov.za/sites/default/files/gcis_document/202212/47632gon2819.pdf)) | National Department of Health; GN 2819; Government Gazette 47632 | Published 2 Dec 2022 | National EMS service standards | Separate standards-readiness overlay; must not be presented as the annual licensing checklist unless the applicable inspection instrument incorporates it | `Acquired`; source is scan-based. OCR, clause extraction, amendment check, legal classification and relationship to 2017 licensing Regulations are required. |
| ZA-EVENT-EMS-2017 | [Regulations Relating to Emergency Care at Mass Gathering Events](https://www.gov.za/documents/national-health-act-regulations-emergency-care-mass-gathering-events-15-jun-2017-0000) ([official PDF](https://www.gov.za/sites/default/files/gcis_document/201706/40919gon566s.pdf)) | National Department of Health; GN 566; Government Gazette 40919 | Published 15 Jun 2017 | Binding national event-medical regulation | Event-medical service overlay only when the tenant performs applicable mass-gathering work | `SourceVerified`; extract exact scope, provider duties, facilities, staffing, application/approval, inspectorate and Annexure A/B/C requirements before activation. Do not apply it to ordinary ambulance operations without the event-service applicability condition. |
| ZA-NHA-2003 | [National Health Act 61 of 2003](https://www.gov.za/documents/national-health-act) | Parliament / National Department of Health | Act and amendments must be checked as at pack effective date | Primary legislation | Enabling authority, patient rights, confidentiality, health establishment and health-system duties where applicable | `Discovered`; acquire current consolidated text and map only exact EMS-applicable clauses. |
| ZA-HPA-1974 | [Health Professions Act 56 of 1974](https://www.gov.za/documents/medical-dental-and-supplementary-health-service-professions-act-16-oct-1974-0000) | Parliament / HPCSA | Commenced 16 Oct 1974; amended, including by Act 20 of 2023 | Primary professional legislation | Registration, professional-board, scope and conduct authority underpinning HPCSA Emergency Care controls | `SourceVerified` as Act directory; acquire current consolidated text and extract only exact applicable provisions with current board rules. |
| ZA-OHSC-SOURCES | [OHSC Acts, Regulations and Standards](https://ohsc.org.za/acts-and-bills/) | Office of Health Standards Compliance | Current portal, checked 2026-07-19 | Regulator source directory | Confirms official standards sources and future measurement-tool provenance | `SourceVerified` as directory only. No final, publicly identifiable private-EMS inspection measurement tool was located. The app must label the standards overlay as readiness guidance until the current instrument is obtained and verified. |
| ZA-POPIA-2013 | [Protection of Personal Information Act 4 of 2013](https://www.gov.za/documents/protection-personal-information-act) | Parliament / Information Regulator | Commencement and amendments must be captured | Primary legislation | Personal information, special personal information, security safeguards, retention, data-subject and breach obligations applicable to staff, patient and evidence data | `Discovered`; exact applicable clauses and Information Regulator guidance require extraction and legal review. No retention period may be invented from POPIA. |
| ZA-OHS-1993 | [Occupational Health and Safety Act 85 of 1993](https://www.gov.za/documents/occupational-health-and-safety-act) | Department of Employment and Labour | Current consolidated state must be checked | Primary legislation | Employer, workplace, hazard, incident and safety duties applicable to EMS bases and work | `Discovered`; exact applicable clauses, amendments and sector interaction require extraction and legal review. |
| ZA-HBA | [Regulations for Hazardous Biological Agents](https://www.labour.gov.za/DocumentCenter/Regulations%20and%20Notices/Regulations/Occupational%20Health%20and%20Safety/Regulations%20for%20Hazardous%20Bilogical%20Agents.pdf) | Department of Employment and Labour under OHS Act | Gazette/effective date and current amendment state must be captured | Binding regulation | Exposure risk, assessment, controls, training, health surveillance and records where applicable to EMS work | `Acquired`; exact Gazette metadata, clauses and EMS applicability require verification before activation. |
| ZA-HCW-2014 | [Regulations Relating to Health Care Waste Management in Health Establishments](https://www.gov.za/documents/notices/national-health-act-regulations-health-care-waste-management-health) ([official PDF](https://www.gov.za/sites/default/files/gcis_document/201409/37654rg10195gon375.pdf)) | National Department of Health; GN 375; Government Gazette 37654 | Published May 2014; current amendment state must be checked | Binding national regulation | Waste-management plan, segregation, storage, roles, records, transport/treatment evidence and retention where the definitions include the relevant EMS establishment/base | `ClauseExtracted` for core plan, segregation, storage and record duties. Definitions, ambulance/base applicability, amendments and provincial overlays require legal review before activation. |
| ZA-NEMWA-2008 | [National Environmental Management: Waste Act 59 of 2008](https://www.gov.za/documents/national-environmental-management-waste-act) | Parliament / Department of Forestry, Fisheries and the Environment | Commenced in stages from 1 Jul 2009; amended, including by Act 2 of 2022 | Primary environmental legislation | Enabling waste duties and applicable waste-management controls supporting the health-care-waste module | `SourceVerified` as Act directory; acquire current consolidated text and current health-care-risk-waste norms/standards. Do not convert general waste provisions into EMS inspection items without an exact applicability source. |
| ZA-MED-ACT | [Medicines and Related Substances Act: Schedules](https://www.gov.za/documents/medicines-and-related-substances-act-schedules-0) | National Department of Health / SAHPRA | Current schedules change over time; pack must pin a date/version | Primary legislation / schedules | Medicine schedule classification and controlled-medicine applicability | `Discovered`; the mode must use a dated retained schedule, not a live assumption. It must not infer that every private EMS needs a dispensing licence or the same permit. |
| ZA-MED-DESTRUCTION | [Guideline for Destruction of Medicines and Scheduled Substances](https://www.sahpra.org.za/document/guideline-for-destruction-of-medicines-and-scheduled-substances/) | SAHPRA | Version 3, 10 Nov 2025, subject to later updates | Regulator guideline tied to Regulation 44 | Expired/unusable medicine destruction evidence and process where applicable | `SourceVerified`; exact legal effect, applicable medicine schedules, forms and retention evidence require clause/form extraction and legal review. |
| ZA-NRTA-1996 | [National Road Traffic Act 93 of 1996](https://www.gov.za/documents/national-road-traffic-act) | Department of Transport | Current consolidated Act and regulations must be retained | Primary road legislation | Vehicle registration, roadworthiness, operator/driver and emergency-vehicle obligations where exact provisions apply | `Discovered`; current Regulations, ambulance classification, emergency-light/siren and roadworthy provisions require exact extraction. Draft Bills may never be used as active rules. |
| ZA-PRDP | [Professional Driving Permit service](https://www.gov.za/services/driving-licence/professional-driving-permit) | South African Government / transport authorities | Current service guidance checked 2026-07-19 | Official administrative guidance | PrDP categories, medical fitness and operator checks for applicable drivers/vehicles | `SourceVerified` as guidance; exact statutory driver/vehicle applicability must come from current road-traffic law before activation. |
| ZA-IPC-FRAMEWORK-2020 | [National Infection Prevention and Control Strategic Framework](https://knowledgehub.health.gov.za/elibrary/national-infection-prevention-and-control-strategic-framework-2020) | National Department of Health Knowledge Hub | 2020 | National policy/guidance | IPC-readiness and internal best-practice overlay | `SourceVerified` as guidance, not automatically a licensing rule. Activate only as clearly labelled guidance or where an authoritative licensing source incorporates it. |
| ZA-PATIENT-RECORD-SOP | [National SOP for Filing, Archiving and Disposal of Patient Records](https://knowledgehub.health.gov.za/elibrary/national-standard-operating-procedure-filing-archiving-and-disposal-patient-records) | National Department of Health Knowledge Hub | Source version/date must be retained | National SOP/guidance | Records-management guidance and possible evidence baseline | `Discovered`; scope and legal effect for private EMS require review. It must not be used to invent an EMS retention period. |

## HPCSA Emergency Care Source Register

| ID | Source | Classification | Intended use | Current state and unresolved work |
| --- | --- | --- | --- | --- |
| ZA-HPCSA-EC-REG | [Emergency Care registration](https://www.hpcsa.co.za/board/emergency-care/registration) | Statutory council administrative source | Practitioner registration categories and official registration pathway | `SourceVerified` as portal. Exact category/register definitions and current forms must be retained. |
| ZA-HPCSA-EC-FEES | [Emergency Care fees](https://www.hpcsa.co.za/board/emergency-care/fees) | Statutory council administrative source | Current annual fee/APC context | `SourceVerified` for 2026/27 portal content. Rules must not equate payment alone with current good standing. |
| ZA-HPCSA-MAINT | [Maintenance of registration](https://www.hpcsa.co.za/page-2/maintenance-of-registration) | Statutory council administrative guidance | Annual practising certificate, annual cycle, CPD and suspension warnings | `SourceVerified` as portal content; extract exact policy/legal references and effective dates. |
| ZA-HPCSA-CPD | [Continuing Professional Development](https://www.hpcsa.co.za/cpd) and [Emergency Care CPD](https://www.hpcsa.co.za/board/emergency-care/cpd) | Statutory council policy/guidance | CPD cycle, required categories, compliance evidence | `Discovered`; no numeric emergency-care requirement may activate until the current board-specific table/policy is acquired, dated and clause-verified. |
| ZA-HPCSA-SCOPE | [Scope of professions](https://www.hpcsa.co.za/scope-of-professions) | Statutory scope source directory | Practitioner scope, acts/omissions and medicine/equipment competence boundaries | `Discovered`; acquire each current Emergency Care scope/CPG/formulary source. Do not infer clinical scope from role title or historical qualification labels. |

## Provincial Overlay Register

Each tenant compliance profile must select the province in which the licensed
base/service operates. National rules and the selected provincial overlay are
evaluated separately. A province with an incomplete source pack must display
`Source pack incomplete` and cannot produce an authoritative inspection-ready
claim.

| Province | Official sources located | What is verified | Missing before activation | Pack state |
| --- | --- | --- | --- | --- |
| Western Cape | [Western Cape Ambulance Services Regulations, 2012](https://d7.westerncape.gov.za/sites/www.westerncape.gov.za/files/western_cape_ambulance_services_regulations_2012.pdf); [Private health establishment licensing/application adverts](https://www.westerncape.gov.za/health-wellness/private-health-establishment-licensing-application-adverts) | Annual inspection/renewal, inspection access, defect notices, licence/token display, monthly information, PCR per patient and manager requirements are present in the 2012 source | Current coexistence or supersession against the 2017 national Regulations; amendments; current forms, fee schedule, inspection checklist and departmental practice | `ClauseExtracted; LegalReviewPending` |
| KwaZulu-Natal | [EMS Licensing and NHI portal](https://ems-licensing-nhi.kznhealth.gov.za/landing); [KZN EMS information](https://www2.kznhealth.gov.za/ems.htm) | Official licensing portal references the national EMS Regulations and requests company documentation | Current application forms, fee schedule, exact annual inspection/renewal checklist, district conditions and any provincial directives | `SourceVerified portal; pack incomplete` |
| Free State | [Free State EMS licensing portal](https://fsh-careinfo.fshealth.gov.za/EMS_Front/) | Portal exposes new/renewal applications and base, vehicle and personnel capture | Exact current inspection checklist, documentary evidence list, directives, fee schedule and departmental process source | `SourceVerified portal; pack incomplete` |
| Gauteng | [Gauteng EMS applications/public notice](https://cmbinary.gauteng.gov.za/Media?path=Media%2FEMS%2FNotice+of+EMS+Applications+received+as+of+August+2025.pdf) | Current official activity proves a provincial application/licensing process | Official application pack, renewal rules, fee schedule, exact inspection checklist, district/service conditions and directives | `Discovered; pack incomplete` |
| Eastern Cape | [Eastern Cape EMS adverts](https://www.echealth.gov.za/index.php/ems-adverts) | Official applications/public-comment channel located | Current application/renewal forms, fees, inspection checklist, provincial directives and service conditions | `Discovered; pack incomplete` |
| Limpopo | No complete, current, primary private-EMS licensing/inspection pack located in the public-source sweep | None sufficient for active rules | Department-confirmed licensing contact, current application/renewal forms, fees, inspection tool and directives | `Blocked: official source pack unavailable` |
| Mpumalanga | No complete, current, primary private-EMS licensing/inspection pack located in the public-source sweep | None sufficient for active rules | Department-confirmed licensing contact, current application/renewal forms, fees, inspection tool and directives | `Blocked: official source pack unavailable` |
| North West | No complete, current, primary private-EMS licensing/inspection pack located in the public-source sweep | None sufficient for active rules | Department-confirmed licensing contact, current application/renewal forms, fees, inspection tool and directives | `Blocked: official source pack unavailable` |
| Northern Cape | No complete, current, primary private-EMS licensing/inspection pack located in the public-source sweep | None sufficient for active rules | Department-confirmed licensing contact, current application/renewal forms, fees, inspection tool and directives | `Blocked: official source pack unavailable` |

## Western Cape Clause Baseline

The following exact anchors are suitable for detailed extraction, but remain
`LegalReviewPending` until current applicability is confirmed:

| Topic | 2012 Regulations anchor | Required extraction |
| --- | --- | --- |
| Licence issue/renewal | Regulation 9 | Inspection prerequisite, certificate/token, expiry, annual renewal and application timing |
| Inspection powers/frequency | Regulation 10 | Access, record inspection, annual inspection and unannounced inspection |
| Non-compliance action | Regulation 11 | Defect notice, remedy process, suspension/cancellation and procedural safeguards |
| Licence/token display | Regulation 15 | Display location and evidence requirements |
| Monthly reporting and PCR | Regulation 16 | Response-time data, staff/registration data, ambulance counts, adverse incidents and PCR requirements |
| Management and operational duties | Regulation 17 and following | Manager qualification, record/confidentiality, safety and change-notification duties |

## Required Source Metadata

Every retained source/version record must contain:

- official title;
- issuing authority and regulator;
- official URL and stable document identifier;
- Gazette, government notice, form or circular number where applicable;
- exact clause, regulation, annexure, schedule, page or form-field anchor;
- publication, commencement, effective, repeal and supersession dates;
- country, province, district and regulator jurisdiction;
- source classification: legislation, regulation, standard, official form,
  inspection tool, directive, policy, guidance or client policy;
- service-type and licence-category applicability;
- acquisition and verification date;
- retained-file content hash and storage reference;
- amendment/supersession status;
- extraction reviewer and independent checker;
- legal-review decision, reviewer, date and limitations;
- uncertainty/conflict note;
- rule-pack versions that consume the source.

## Unresolved Regulatory Questions

These are hard gates, not implementation assumptions:

1. Confirm the current legal relationship among the 2017 national EMS
   Regulations, the 2022 EMS standards, existing provincial regulations and
   current provincial licensing practice.
2. Obtain the current official Annexure A/B requirements and any amendments for
   the exact private ground-ambulance service categories to be supported.
3. Obtain each province's current application, renewal, fee and inspection
   instrument directly from the relevant Department of Health when it is not
   publicly available.
4. Confirm whether OHSC has a current EMS measurement tool and how it relates
   to provincial annual licensing inspection.
5. Obtain current Emergency Care Board scope, CPD, CPG and medicine/formulary
   documents by registration category.
6. Confirm medicine permit/licence, procurement, possession, storage,
   administration, record and destruction duties by operator/service model.
7. Confirm exact roadworthiness, vehicle classification, licence/token,
   emergency-light/siren, driver-licence and PrDP requirements.
8. Confirm health-care-waste regulation applicability to ambulances, bases and
   storage locations and identify all provincial environmental overlays.
9. Confirm patient-care-record, operational-record and evidence retention
   periods from authoritative sources; POPIA alone does not supply one universal
   EMS retention period.
10. Confirm insurer/indemnity, adverse-event, complaint, clinical-governance,
    infection-control, quality-improvement and data-submission obligations and
    whether each is law, licence condition, standard or guidance.

## Source Acquisition And Activation Gate

Before a requirement pack can become `Active`:

1. Download the official source and calculate a content hash.
2. Record all mandatory metadata.
3. Extract every applicable clause/item with exact anchors.
4. Independently compare the extraction to the retained official source.
5. Record explicit applicability and exclusions.
6. Record conflicts without combining or choosing silently.
7. Obtain qualified South African legal/regulatory review.
8. Obtain a private-EMS operational subject-matter review.
9. Run deterministic rule tests and source/version tests.
10. Publish a signed, versioned pack. Existing completed sessions retain the
    old pack version and are never silently reevaluated.

## Prohibited Source Practices

- No blogs, vendor summaries, tender specifications, job adverts, public EMS
  fleet lists or social-media posts may create legal requirements.
- No national, provincial, OHSC, HPCSA or client-policy requirement may be
  merged without preserving its source and classification.
- No missing provincial source may be replaced by another province's rules.
- No legal deadline, retention period, medicine entitlement, licence class or
  inspection outcome may be inferred.
- No requirement may be called compliant merely because AcuityOps has a field
  with a value; the rule-specific evidence and verification method must pass.
