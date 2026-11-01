cmu-employment-window = Department management
cmu-employment-heading-govfor = GOVFOR // DEPARTMENT MANAGEMENT
cmu-employment-heading-colony = SEEGSON // DEPARTMENT MANAGEMENT
cmu-employment-heading-weyland = WEYLAND-YUTANI // DEPARTMENT MANAGEMENT
cmu-employment-name = Registered name
cmu-employment-title = New job title
cmu-employment-presets = Credential presets
cmu-employment-access = New access permissions
cmu-employment-iff = Authorize this employer's card IFF
cmu-employment-iff-hint = Card authorization does not remove the wearer's innate IFF.
cmu-employment-confirm = I confirm the credential replacement shown above.
cmu-employment-hire = 01  Hire / Transfer
cmu-employment-edit = 02  Edit credentials
cmu-employment-terminate = 03  Terminate
cmu-employment-history = 04  Records
cmu-employment-refresh = Refresh
cmu-employment-cancel = Discard edits
cmu-employment-offline = OFFLINE — power required. Inserted cards can still be ejected.
cmu-employment-authenticated = AUTHENTICATED // {$name}
cmu-employment-locked = LOCKED — insert an authorized operator ID. Reinsert it after logging out.
cmu-employment-insert-operator = Insert operator ID
cmu-employment-eject-operator = Eject operator ID / Log out
cmu-employment-insert-target = Insert target ID
cmu-employment-eject-target = Eject target ID: {$name}
cmu-employment-current-title = Current title: {$title}
cmu-employment-restricted = {$name} — RESTRICTED
cmu-employment-review-replace = Applying REPLACES all access and IFF on this card. Skills, role, objectives and bank balance stay unchanged.
cmu-employment-review-terminate = Terminating CLEARS access and IFF on this card. Previous credentials are not restored.
cmu-employment-history-empty = No changes recorded at this terminal.
cmu-employment-history-entry = {$time} | {$operator}: {$action} {$name} | {$title} | access: {$access} | employer IFF: {$iff} | HUD icon: {$icon}
cmu-employment-unassigned = Unassigned
cmu-employment-hired = hired
cmu-employment-edited = updated
cmu-employment-terminated = terminated
cmu-employment-insert-id = Hold a compatible ID card and ensure the console has power.
cmu-employment-denied = An authorized operator must be at the powered console with their ID inserted.
cmu-employment-stale = The card or session changed. Review the current credentials and confirm again.
cmu-employment-not-employed = This card has not been hired by this employer. Use Hire / Transfer first.
cmu-employment-invalid-text = Enter a job title of 1–40 characters and, for blank cards, a name of 1–64 characters. Formatting and control characters are not allowed.
cmu-employment-invalid-access = This console cannot issue one or more of the requested permissions.
cmu-employment-applied = ID updated. Printing the personnel record.
cmu-employment-printing = PROCESSING // Printing personnel record...
cmu-employment-apply-hire = Issue credentials
cmu-employment-apply-edit = Save changes
cmu-employment-apply-terminate = Terminate employment
cmu-employment-printer-busy = The terminal is finishing its current personnel record.
cmu-employment-printer-unavailable = The terminal's stationery template is unavailable. No credentials were changed.
cmu-employment-no-changes = These credentials are already on the card. No changes or duplicate record were issued.
cmu-employment-form-hire = EMPLOYMENT AUTHORIZATION
cmu-employment-form-edit = PERSONNEL AMENDMENT
cmu-employment-form-terminate = NOTICE OF TERMINATION
cmu-employment-form-brand-govfor = GOVERNMENT FORCES / PERSONNEL SERVICES
cmu-employment-form-brand-colony = COLONY ADMINISTRATION / EMPLOYMENT OFFICE
cmu-employment-form-brand-weyland = WEYLAND-YUTANI / PERSONNEL DIVISION
cmu-employment-form-policy-govfor = Report to the assigned department before beginning duties. Access to a restricted area does not confer military rank, command authority or professional qualifications.
cmu-employment-form-policy-colony = Present this record to the relevant department supervisor when reporting for work. Colony law and local safety procedures remain in force.
cmu-employment-form-policy-weyland = Company access is issued for assigned duties. Company property, research and information remain subject to local corporate security procedures.
cmu-employment-form-icon-retained = Existing job marker retained
cmu-employment-form-no-icon = No assigned job marker
cmu-employment-form-no-access = No door permissions authorized.
cmu-employment-form-iff-authorized = Employer identification authorized on this ID
cmu-employment-form-iff-none = No employer identification authorized on this ID
cmu-employment-form-name = personnel record: {$name}
cmu-employment-form-description = {$action} for {$name}, recorded as {$title}, with {$count} authorized access permissions. The issuing operator and credential details are printed inside.
cmu-employment-form-content = [color=#242824]
    {"["}head=3]{$brand}[/head]
    {"["}head=2]{$type}[/head]
    ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    {"["}bold]DOCUMENT REFERENCE[/bold]  {$reference}
    {"["}bold]ISSUED AT[/bold]  {$time} / round elapsed

    {"["}head=3]01 / EMPLOYEE RECORD[/head]
    {"["}bold]Registered name:[/bold] {$name}
    {"["}bold]Assigned title:[/bold] {$title}
    {"["}bold]Issuing operator:[/bold] {$operator}

    {"["}head=3]02 / AUTHORIZED ACCESS[/head]
    {"["}italic]{$count} permissions programmed on the ID at issue.[/italic]
    {$permissions}

    {"["}head=3]03 / IDENTIFICATION[/head]
    {"["}bold]IFF authorization:[/bold] {$iff}
    {"["}bold]HUD job marker:[/bold] {$icon}

    {"["}head=3]04 / CONDITIONS OF ISSUE[/head]
    {$policy}

    This record replaces the previous credential assignment on the presented ID. Old access and card identification are not retained or restored by termination. Skills, original role and bank balance are unchanged.

    ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    {"["}bold]PERSONNEL OFFICE / MACHINE-ISSUED RECORD[/bold]
    {"["}italic]Keep with your ID. This paper records the transaction; it does not open doors, grant access or prove the card's current status.[/italic]
    {"["}/color]
cmu-employment-yes = authorized
cmu-employment-no = none
cmu-employment-preset-visitor = Visitor
cmu-employment-preset-contractor = Contractor
cmu-employment-preset-engineering = Engineering contractor
cmu-employment-preset-medical = Medical contractor
cmu-employment-preset-security = Security auxiliary
cmu-employment-preset-logistics = Logistics contractor
cmu-employment-title-visitor = Visitor
cmu-employment-title-contractor = Company Contractor
cmu-employment-title-engineering = Contract Engineer
cmu-employment-title-medical = Contract Medic
cmu-employment-title-security = Security Auxiliary
cmu-employment-title-logistics = Logistics Contractor
cmu-employment-weyland-personnel-access = Company personnel administration
cmu-employment-operations = OPERATIONS
cmu-employment-readers = CARD READERS
cmu-employment-reader-hint = Hold an ID, then select its reader. Select an occupied reader to eject.
cmu-employment-reader-operator = [A] Operator ID
cmu-employment-reader-operator-eject = [A] Eject operator
cmu-employment-reader-target = [B] Employee ID
cmu-employment-reader-target-eject = [B] Eject employee
cmu-employment-brand-govfor = GOVFOR / PERSONNEL SERVICES
cmu-employment-brand-colony = SEEGSON / COLONY SYSTEMS
cmu-employment-brand-weyland = WEYLAND-YUTANI / PERSONNEL
cmu-employment-footer-govfor = AUTHORIZED PERSONNEL ONLY
cmu-employment-footer-colony = WORKING TOGETHER FOR A SAFER COLONY
cmu-employment-footer-weyland = BUILDING BETTER WORLDS
cmu-employment-search = Filter permissions...
cmu-employment-clear = Clear
cmu-employment-access-count = {$count} selected
cmu-employment-access-standard = STANDARD ACCESS
cmu-employment-access-sensitive = RESTRICTED ACCESS
cmu-employment-sensitive-hint = {$name}: grants sensitive access. Command or administrator access may also authorize other consoles.
cmu-employment-access-corporate = Company facilities
cmu-employment-access-corporate-flight = Company flight operations
cmu-employment-access-corporate-liaison = Company liaison
cmu-employment-access-colony-chemistry = Colony chemistry
cmu-employment-job-icon = HUD job icon
cmu-employment-job-icon-hint = Changes the job marker shown by the existing HUD. Does not grant door access, skills, rank or IFF.
cmu-employment-invalid-icon = This terminal cannot issue the selected HUD job icon.
cmu-employment-preset-rifleman = Rifleman
cmu-employment-title-rifleman = Contract Rifleman
cmu-employment-preset-pilot = Pilot
cmu-employment-title-pilot = Contract Pilot
cmu-employment-preset-vehicle = Vehicle crew
cmu-employment-title-vehicle = Contract Vehicle Crew
cmu-employment-preset-weapons = Weapons specialist
cmu-employment-title-weapons = Contract Weapons Specialist
cmu-employment-preset-research = Research
cmu-employment-title-research = Contract Researcher
cmu-employment-preset-shopkeeper = Shopkeeper
cmu-employment-title-shopkeeper = Shopkeeper
cmu-employment-preset-corporate-flight = Flight operations
cmu-employment-title-corporate-flight = Company Flight Contractor
cmu-employment-preset-liaison = Company liaison
cmu-employment-title-liaison = Company Liaison
ent-CMUEmploymentConsoleGovfor = GOVFOR employment console
    .desc = Programs GOVFOR employment credentials, door access and identification codes.
ent-CMUEmploymentConsoleColony = colony employment console
    .desc = Programs colony employment credentials, door access and identification codes.
ent-CMUEmploymentConsoleWeyland = Weyland-Yutani employment console
    .desc = Programs Company employment credentials, door access and identification codes.
ent-CMUEmploymentBlankId = blank employment ID
    .desc = An unregistered ID card for issuing employment credentials.
ent-CMUEmploymentWeylandOperatorId = Company personnel authorization ID
ent-CMUEmploymentGovforOperatorId = GOVFOR personnel authorization ID
ent-CMUEmploymentColonyOperatorId = colony personnel authorization ID
cmu-employment-icon-au14jobicongovrifleman = Rifleman
cmu-employment-icon-au14jobiconafn = Automatic rifleman
cmu-employment-icon-au14jobiconrfn = Rifleman (legacy)
cmu-employment-icon-au14jobicongovweaponsspecialist = Weapons specialist
cmu-employment-icon-au14jobiconcorpsman = Corpsman
cmu-employment-icon-au14jobicondoctor = Doctor
cmu-employment-icon-au14jobiconauxtech = Logistics technician
cmu-employment-icon-au14jobicongovct = Combat technician
cmu-employment-icon-au14jobiconmp = Military police
cmu-employment-icon-au14jobiconftl = Fireteam leader
cmu-employment-icon-au14jobiconsquadsgt = Squad sergeant
cmu-employment-icon-au14jobiconplatoonsgt = Platoon sergeant
cmu-employment-icon-au14jobiconrto = Radio operator
cmu-employment-icon-au14jobiconpilotofficer = Pilot
cmu-employment-icon-au14jobicondcc = Dropship crew chief
cmu-employment-icon-au14jobicongovtankcrewman = Vehicle crew
cmu-employment-icon-au14jobicongovtankcommander = Vehicle commander
cmu-employment-icon-au14jobiconopsofc = Operations officer
cmu-employment-icon-au14jobicongoveo = Engineering officer
cmu-employment-icon-au14jobicongovio = Intelligence officer
cmu-employment-icon-au14jobicongovcmp = Chief of military police
cmu-employment-icon-au14jobicongovmo = Medical officer
cmu-employment-icon-au14jobicongovlo = Logistics officer
cmu-employment-icon-au14jobicongovadjutant = Adjutant
cmu-employment-icon-au14jobicongovadvisor = Advisor
cmu-employment-icon-au14jobiconplatco = Platoon commander
cmu-employment-icon-au14jobiconcorporateassistant = Corporate assistant
cmu-employment-icon-cmjobiconliaison = Corporate liaison
cmu-employment-icon-rmcjobiconpmcscientist = Company scientist
cmu-employment-icon-rmcjobiconpmcgoonstandard = Company security
cmu-employment-icon-rmcjobiconpmcmedic = Company medic
cmu-employment-icon-cmjobiconexecspec = Executive specialist
cmu-employment-icon-cmjobiconexecsuper = Executive supervisor
cmu-employment-icon-cmjobiconexecassman = Assistant manager
cmu-employment-icon-cmjobiconexecdivman = Division manager
cmu-employment-icon-cmjobiconexecchief = Chief manager
cmu-employment-icon-cmjobicondepdirector = Deputy director
cmu-employment-icon-cmjobicondirector = Director

cmu-employment-brand-selected = {$name} / PERSONNEL SERVICES
cmu-employment-govfor-uscm = USCM
cmu-employment-govfor-lacn = LACN
cmu-employment-govfor-upp = UPP
cmu-employment-govfor-bear = BEAR
cmu-employment-govfor-rmc = ROYAL MARINES
cmu-employment-govfor-weyu = WEYLAND-YUTANI PMC
cmu-employment-govfor-cmbciu = COLONIAL MARSHAL BUREAU
cmu-employment-govfor-hazops = HAZOPS
cmu-employment-govfor-vaipo = VANGUARD'S ARROW
cmu-employment-govfor-prodigysf = PRODIGY SECURITY

cmu-employment-model = PERS / 03
cmu-employment-cabinet-caption = PERSONNEL ADMINISTRATION • AUTHORIZED USE ONLY
cmu-employment-power-on = ● POWER
cmu-employment-power-off = ○ OFFLINE
cmu-employment-printer-ready = DOCUMENT OUTPUT
cmu-employment-printer-working = PRINTING RECORD...
cmu-employment-boot-skip = Continue
cmu-employment-boot-memory = MEMORY CHECK ................ OK
cmu-employment-boot-readers = ID INTERFACE ................ OK
cmu-employment-boot-ready = PERSONNEL SERVICES .......... READY

cmu-employment-preview = Review credentials
cmu-employment-preview-back = Back to editor
cmu-employment-diff-heading = ACCESS TAG COMPARISON
cmu-employment-diff-summary = +{ $added } added / −{ $removed } removed / { $kept } kept
cmu-employment-diff-added = TO BE ADDED
cmu-employment-diff-removed = TO BE REMOVED
cmu-employment-diff-kept = UNCHANGED
cmu-employment-diff-hint = This compares access tags. Existing group permissions and card-based IFF are also replaced. Nothing changes until confirmed.
cmu-employment-card-draft = CREDENTIAL PREVIEW / DRAFT
cmu-employment-card-revoked = REVOCATION PREVIEW
cmu-employment-card-iff = Employer IFF authorized
cmu-employment-card-no-iff = No employer card IFF
cmu-employment-design-govfor = CONTROLLED PERSONNEL RECORD. Verify the bearer and assignment before issuing credentials.
cmu-employment-design-colony = CIVIL REGISTRY SERVICES. Department permissions apply to the inserted ID card.
cmu-employment-design-weyland = WEYLAND-YUTANI. Authorized personnel only. Building Better Worlds.
cmu-employment-reader-empty = No card inserted
cmu-employment-stage-insert = 01 / INSERT ID
cmu-employment-stage-read = 01 / ID LOADED
cmu-employment-stage-draft = 02 / REVIEW DRAFT
cmu-employment-stage-waiting = 02 / AWAITING SERVER
cmu-employment-stage-saved = 02 / ID WRITTEN
cmu-employment-stage-output = 03 / DOCUMENT OUT
cmu-employment-stage-printing = 03 / PRINTING
cmu-employment-stage-printed = 03 / RECORD PRINTED
cmu-employment-stamp-issued = CREDENTIALS ISSUED
cmu-employment-stamp-updated = CREDENTIALS UPDATED
cmu-employment-stamp-revoked = CREDENTIALS REVOKED
cmu-employment-stamp-printed = PERSONNEL RECORD PRINTED

cmu-employment-model-govfor = PERS-03 / CONTROLLED
cmu-employment-model-colony = REG-03 / CIVIL SERVICES
cmu-employment-model-weyland = HR-03 / COMPANY NETWORK

cmu-employment-app-personnel = Personnel
cmu-employment-app-manifest = Manifest
cmu-employment-app-comms = Communications
cmu-employment-app-payroll = Payroll
cmu-employment-manifest-hint = Station records are grouped by the existing manifest system. Department rosters and payroll use the department consoles' registered ID cards.
cmu-employment-manifest-load = Insert an authorized operator ID and select Refresh.
cmu-employment-manifest-updated = Last refreshed: { $time }
cmu-employment-manifest-station = Station manifest
cmu-employment-manifest-empty = No station manifest is available. The terminal must belong to a station with personnel records. Department rosters may still be available.
cmu-employment-department-button = { $name } ({ $count })
cmu-employment-department-member = { $name } — { $title }
cmu-employment-department-empty = No registered employees.
cmu-employment-comms-hint = Uses the colony's existing announcement channel and emergency sirens. Announcements retain the normal recipient filtering.
cmu-employment-broadcast-caption = Colony announcement
cmu-employment-broadcast-placeholder = Enter an announcement (up to 512 characters)...
cmu-employment-broadcast-count = { $count } / 512 characters
cmu-employment-broadcast-confirm = I confirm this colony-wide announcement.
cmu-employment-comms-access-hint = Communications also require Colony Command access on the inserted operator ID. Announcements have a 10-second delay between sends.
cmu-employment-comms-denied = Colony Command access is required on the operator ID.
cmu-employment-comms-cooldown = Please wait before using communications again.
cmu-employment-comms-invalid = Enter a plain-text announcement between 1 and 512 characters.
cmu-employment-comms-sent = Colony announcement sent.
cmu-employment-siren-confirm = I confirm this change to the colony sirens.
cmu-employment-siren-active = EMERGENCY SIREN: ACTIVE
cmu-employment-siren-inactive = EMERGENCY SIREN: INACTIVE
cmu-employment-siren-start = Activate emergency sirens
cmu-employment-siren-stop = Silence emergency sirens
cmu-employment-payroll-heading = DEPARTMENTS & PAYROLL
cmu-employment-payroll-hint = Payments use the existing department budgets, income tax and ID balances. Underfunded departments are skipped. Payouts have a 10-second delay. Salary and funding inputs accept 0–1,000,000 credits.
cmu-employment-payroll-default = Department default salary
cmu-employment-payroll-individual = Selected employee salary
cmu-employment-payroll-funding = Transfer funds from the colony budget
cmu-employment-payroll-set-default = Set default
cmu-employment-payroll-set-individual = Set salary
cmu-employment-payroll-clear = Use default
cmu-employment-payroll-fund = Fund department
cmu-employment-payroll-transfer = Transfer inserted employee ID into this department
cmu-employment-payroll-confirm = I confirm the selected department, employee and operation.
cmu-employment-payroll-pay-employee = Pay selected employee
cmu-employment-payroll-pay-department = Disburse department payroll
cmu-employment-payroll-pay-colony = Disburse all colony payroll
cmu-employment-payroll-budget = Department budget: ${ $amount }
cmu-employment-payroll-employee = { $name } — ${ $salary } ({ $mode })
cmu-employment-payroll-override = override
cmu-employment-payroll-default-mode = default
cmu-employment-payroll-stale = The department data changed. Review the refreshed list before continuing.
cmu-employment-payroll-cooldown = Please wait before disbursing another payment.
cmu-employment-payroll-failed = No change was made. Check the employee, salary and department funds, then refresh.
cmu-employment-payroll-applied = Department operation processed. Payroll balances have been refreshed.

cmu-employment-role-constable = Constable
cmu-employment-role-deputy = Deputy
cmu-employment-role-security-officer = Security officer
cmu-employment-role-physician = Physician
cmu-employment-role-nurse = Nurse
cmu-employment-role-pharmacist = Pharmacist
cmu-employment-role-engineer = Engineer
cmu-employment-role-maintenance-technician = Maintenance technician
cmu-employment-role-cargo-technician = Cargo technician
cmu-employment-role-dock-worker = Dock worker
cmu-employment-role-chef = Chef
cmu-employment-role-bartender = Bartender
cmu-employment-role-janitor = Janitor
cmu-employment-role-botanist = Botanist
cmu-employment-role-farm-worker = Farm worker
cmu-employment-role-lumber-worker = Lumber worker
cmu-employment-role-machine-operator = Machine operator
cmu-employment-role-civilian-contractor = Civilian contractor
cmu-employment-role-clerk = Clerk
cmu-employment-role-administrative-clerk = Administrative clerk

cmu-employment-office-heading = DEPARTMENT MANAGEMENT
cmu-employment-office-services = Public services
cmu-employment-office-requests = Requests & approvals
cmu-employment-office-jail = Custody records
cmu-employment-security-brand = { $force } / DEPARTMENT MANAGEMENT
cmu-employment-department-brand = { $name } / DEPARTMENT MANAGEMENT
cmu-employment-request-submitted = REQUEST SUBMITTED
cmu-employment-request-hiring = { $action }: { $title }. Proposed access: { $permissions }. Employer IFF: { $iff }. Enrollment in the requesting department is included.
cmu-employment-request-payroll = Department operation: { $action }. Amount: { $amount }. Current budgets, salaries and membership must still match when approved.
cmu-employment-office-busy = The printer or request desk is busy. Please wait before submitting again.
cmu-employment-office-full = The active request or custody ledger is full.
cmu-employment-office-paper-description = An administrative request. Approval requires the Colony Administrator's stamp and the matching server-held request.
cmu-employment-office-stamp-required = Insert the matching request paper bearing the Colony Administrator's stamp.
cmu-employment-office-id-required = Insert the matching employee ID. Its credentials must still match the submitted request.
cmu-employment-office-document =
    {"["}head=2]DEPARTMENT MANAGEMENT REQUEST[/head]
    Reference: { $reference }
    Submitted: { $time }
    Department: { $department }
    Requested by: { $requester }
    Subject: { $subject }

    { $description }

    No change takes effect until approved. The Colony Administrator must stamp this form and insert it at an administrator terminal. Hiring, editing, dismissal and transfer requests require the matching employee ID. Altered or already-decided requests cannot be applied.
cmu-employment-request-status-pending = Pending approval
cmu-employment-request-status-approved = Approved
cmu-employment-request-status-rejected = Rejected
cmu-employment-request-status-stale = Stale — resubmit
cmu-employment-office-services-heading = IDENTIFICATION & PUBLIC SERVICES
cmu-employment-office-services-hint = Insert your named ID in the operator reader to submit a report or service request. Hiring and management tools require the department leader's access. Officer custody records remain restricted.
cmu-employment-office-report-placeholder = Describe your report or request (up to 512 characters)...
cmu-employment-office-report-send = Submit and print request
cmu-employment-office-requests-heading = REQUESTS & APPROVALS
cmu-employment-office-paper = Insert approval paper
cmu-employment-office-paper-eject = Eject approval paper
cmu-employment-office-confirm = I have reviewed the request and the matching document.
cmu-employment-office-approve = Approve stamped request
cmu-employment-office-reject = Reject request
cmu-employment-office-reprint = Reprint request
cmu-employment-office-no-requests = No requests are available for this ID or department.
cmu-employment-office-identified = IDENTIFIED / { $name }
cmu-employment-jail-heading = CUSTODY & SENTENCE REGISTER
cmu-employment-jail-hint = Insert the inmate's ID in the employee reader. Enter the charges and sentence. Timers track the sentence; they do not lock or unlock cell doors. Time served still requires an officer to record release.
cmu-employment-jail-minutes = Sentence in minutes (1–120)
cmu-employment-jail-reason = Charges or release reason (up to 512 characters)...
cmu-employment-jail-confirm = I confirm this custody entry and its reason.
cmu-employment-jail-book = Book inserted inmate ID
cmu-employment-jail-release = Record selected inmate's release
cmu-employment-jail-invalid = Insert a named inmate ID and choose a sentence from 1 to 120 minutes.
cmu-employment-jail-booking-paper = Custody booking record
cmu-employment-jail-released = Released
cmu-employment-jail-served = Time served — awaiting release
cmu-employment-jail-time = Remaining: { $time }
cmu-employment-jail-document =
    {"["}head=2]CUSTODY BOOKING RECORD[/head]
    Inmate: { $name }
    Booking officer: { $officer }
    Charges: { $charges }
    Sentence: { $minutes } minutes
    Due: { $due }

    This record tracks the sentence and does not operate cell doors. Release must be recorded by an authorized officer.
cmu-employment-payroll-action-transfer = Department transfer
cmu-employment-payroll-action-defaultsalary = Department default salary
cmu-employment-payroll-action-individualsalary = Individual salary
cmu-employment-payroll-action-clearoverride = Remove salary override
cmu-employment-payroll-action-payemployee = Employee payment
cmu-employment-payroll-action-paydepartment = Department payroll
cmu-employment-payroll-action-paycolony = Colony payroll
cmu-employment-payroll-action-funddepartment = Department funding

cmu-employment-office-duplicate = A pending request already exists for that card or department action.
cmu-employment-request-employee = { $department } / { $name }

cmu-employment-request-dismissal = Dismissal: clear this card's door permissions and employer IFF, reset its title and job marker, and remove it from department payroll.

cmu-employment-office-submitted = Request submitted, printed and faxed to the Colony Administrator.
cmu-employment-office-no-fax = Request submitted and printing. No administrator fax was found; deliver the paper by hand or have the administrator reprint it from the request list.
