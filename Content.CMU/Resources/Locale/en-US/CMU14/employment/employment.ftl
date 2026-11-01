cmu-employment-window = Employment credentials
cmu-employment-heading-govfor = GOVFOR // PERSONNEL AUTHORIZATION
cmu-employment-heading-colony = SEEGSON // COLONY EMPLOYMENT
cmu-employment-heading-weyland = WEYLAND-YUTANI // PERSONNEL AUTHORIZATION
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
