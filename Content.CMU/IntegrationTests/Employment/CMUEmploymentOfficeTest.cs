using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.CMU14.Employment;
using Content.Server.Power.Components;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.CMU14.ColonyEconomy;
using Robust.Shared.Maths;
using Content.Shared.CMU14.Employment;
using Content.Shared.Paper;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.CMU14.Employment;

[TestFixture]
public sealed class CMUEmploymentOfficeTest : GameTest
{
    [Test]
    public async Task HiringWaitsForMatchingStampedPaperAndAdministrator()
    {
        var map = await Pair.CreateTestMap();
        var spawned = new List<EntityUid>();
        try
        {
            await Server.WaitAssertion(() =>
            {
                var entities = SEntMan;
                var system = Server.System<CMUEmploymentConsoleSystem>();
                var access = Server.System<SharedAccessSystem>();
                var ids = Server.System<SharedIdCardSystem>();
                var containers = Server.System<SharedContainerSystem>();
                EntityUid Spawn(string prototype)
                {
                    var uid = entities.SpawnEntity(prototype, map.GridCoords);
                    spawned.Add(uid);
                    return uid;
                }
                EntityUid NamedCard(string name, params ProtoId<AccessLevelPrototype>[] tags)
                {
                    var uid = Spawn("CMUEmploymentBlankId");
                    ids.TryChangeFullName(uid, name, entities.GetComponent<IdCardComponent>(uid));
                    access.TrySetTags(uid, tags);
                    return uid;
                }
                void Insert(EntityUid terminal, EntityUid card, bool operatorSlot)
                    => Assert.That(containers.Insert(card, containers.EnsureContainer<ContainerSlot>(terminal,
                        operatorSlot ? CMUEmploymentConsoleComponent.OperatorSlot : CMUEmploymentConsoleComponent.TargetSlot)), Is.True);

                var head = Spawn("CMMobHuman");
                var administrator = Spawn("CMMobHuman");
                var medical = Spawn("AUDepartmentConsoleMedical");
                var terminal = Spawn("CMUHeadMedicalTerminal");
                var adminTerminal = Spawn("CMUEmploymentConsoleColony");
                entities.GetComponent<ApcPowerReceiverComponent>(terminal).NeedsPower = false;
                entities.GetComponent<ApcPowerReceiverComponent>(adminTerminal).NeedsPower = false;
                var headConsole = entities.GetComponent<CMUEmploymentConsoleComponent>(terminal);
                var adminConsole = entities.GetComponent<CMUEmploymentConsoleComponent>(adminTerminal);
                var employee = NamedCard("Alex Reyes", "AU14AccessOpforCommand");
                var wrongEmployee = NamedCard("Morgan Hale");
                Insert(terminal, NamedCard("Dr. Vale", "AU14AccessColonyHeadPhysician"), true);
                Insert(adminTerminal, NamedCard("Administrator", "AU14AccessColonyAdministrator"), true);
                Insert(terminal, employee, false);
                headConsole.Operator = head;
                adminConsole.Operator = administrator;
                var id = entities.GetComponent<IdCardComponent>(employee);
                var titleBefore = id.LocalizedJobTitle;
                var hire = new CMUEmploymentApplyMessage(entities.GetNetEntity(employee), headConsole.Revision,
                    CMUEmploymentAction.Hire, "Physician", "", new() { "CMAccessColonyMedbay" }, true, true);
                Assert.That(system.TryApply((terminal, headConsole), head, hire), Is.True);
                Assert.That(id.LocalizedJobTitle, Is.EqualTo(titleBefore), "Submitting paperwork must not edit credentials.");
                Assert.That(entities.GetComponent<AccessComponent>(employee).Tags, Does.Contain(new ProtoId<AccessLevelPrototype>("AU14AccessOpforCommand")));
                Assert.That(entities.GetComponent<DepartmentConsoleComponent>(medical).Members, Does.Not.Contain(employee));
                Assert.That(headConsole.LastReceipt!.Pending, Is.True);
                Assert.That(system.TryApply((terminal, headConsole), head, hire), Is.False, "An old submission cannot be replayed.");
                var request = system.GetOfficeState((adminTerminal, adminConsole))!.Requests.Single();
                var document = entities.GetComponent<CMUEmploymentPrintJobComponent>(terminal).Content;
                CMUEmploymentOfficeMessage Approve() => new(CMUEmploymentOfficeAction.Approve, adminConsole.Revision, request.Id, confirmed: true);
                system.HandleOfficeAction((adminTerminal, adminConsole), administrator, Approve());
                Assert.That(id.LocalizedJobTitle, Is.EqualTo(titleBefore), "Approval requires the physical matching document.");
                var paper = Spawn("CMUPaperSEEGSON");
                var paperComp = entities.GetComponent<PaperComponent>(paper);
                Server.System<PaperSystem>().SetContent(paper, document);
                Assert.That(containers.Insert(paper, containers.EnsureContainer<ContainerSlot>(adminTerminal, "CMUEmploymentApprovalPaper")), Is.True);
                paperComp.StampedBy.Add(new StampDisplayInfo { StampedName = "stamp-component-stamped-name-default", StampedColor = Color.Black });
                system.HandleOfficeAction((adminTerminal, adminConsole), administrator, Approve());
                Assert.That(id.LocalizedJobTitle, Is.EqualTo(titleBefore), "Another stamp must not approve a request.");
                paperComp.StampedBy.Add(new StampDisplayInfo { StampedName = "stamp-component-stamped-name-au14-admin", StampedColor = Color.Black });
                Server.System<PaperSystem>().SetContent(paper, document + " altered");
                system.HandleOfficeAction((adminTerminal, adminConsole), administrator, Approve());
                Assert.That(id.LocalizedJobTitle, Is.EqualTo(titleBefore), "Edited paperwork must not become an executable payload.");
                Server.System<PaperSystem>().SetContent(paper, document);
                Insert(adminTerminal, wrongEmployee, false);
                system.HandleOfficeAction((adminTerminal, adminConsole), administrator, Approve());
                Assert.That(id.LocalizedJobTitle, Is.EqualTo(titleBefore), "A different employee card must not be changed.");
                containers.Remove(wrongEmployee, containers.EnsureContainer<ContainerSlot>(adminTerminal, CMUEmploymentConsoleComponent.TargetSlot));
                containers.Remove(employee, containers.EnsureContainer<ContainerSlot>(terminal, CMUEmploymentConsoleComponent.TargetSlot));
                Insert(adminTerminal, employee, false);
                system.HandleOfficeAction((adminTerminal, adminConsole), head, Approve());
                Assert.That(id.LocalizedJobTitle, Is.EqualTo(titleBefore), "The stamp alone does not authorize a different operator.");
                system.HandleOfficeAction((adminTerminal, adminConsole), administrator, Approve());
                Assert.That(id.LocalizedJobTitle, Is.EqualTo("Physician"));
                var tags = entities.GetComponent<AccessComponent>(employee).Tags;
                Assert.That(tags, Does.Contain(new ProtoId<AccessLevelPrototype>("CMAccessColonyMedbay")));
                Assert.That(tags, Does.Not.Contain(new ProtoId<AccessLevelPrototype>("AU14AccessOpforCommand")));
                Assert.That(entities.GetComponent<DepartmentConsoleComponent>(medical).Members, Does.Contain(employee));
                Assert.That(system.GetOfficeState((adminTerminal, adminConsole))!.Requests.Single().Pending, Is.False);
                var historyCount = adminConsole.History.Count;
                system.HandleOfficeAction((adminTerminal, adminConsole), administrator, Approve());
                Assert.That(adminConsole.History.Count, Is.EqualTo(historyCount), "Approved paperwork is single-use.");
            });
        }
        finally
        {
            await Server.WaitPost(() =>
            {
                foreach (var uid in spawned)
                    SEntMan.DeleteEntity(uid);
            });
            await map.CleanUp();
        }
    }

    [Test]
    public async Task PublicIdentificationDoesNotGrantHiringPayrollOrCustodyAuthority()
    {
        var map = await Pair.CreateTestMap();
        var spawned = new List<EntityUid>();
        try
        {
            await Server.WaitAssertion(() =>
            {
                var entities = SEntMan;
                var system = Server.System<CMUEmploymentConsoleSystem>();
                var containers = Server.System<SharedContainerSystem>();
                EntityUid Spawn(string prototype)
                {
                    var uid = entities.SpawnEntity(prototype, map.GridCoords);
                    spawned.Add(uid);
                    return uid;
                }
                var actor = Spawn("CMMobHuman");
                var terminal = Spawn("CMUSecurityManagementTerminal");
                var card = Spawn("CMUEmploymentBlankId");
                var target = Spawn("CMUEmploymentBlankId");
                var department = Spawn("AUDepartmentConsoleSecurity");
                entities.GetComponent<ApcPowerReceiverComponent>(terminal).NeedsPower = false;
                var console = entities.GetComponent<CMUEmploymentConsoleComponent>(terminal);
                var id = entities.GetComponent<IdCardComponent>(card);
                Server.System<SharedIdCardSystem>().TryChangeFullName(card, "Public visitor", id);
                containers.Insert(card, containers.EnsureContainer<ContainerSlot>(terminal, CMUEmploymentConsoleComponent.OperatorSlot));
                containers.Insert(target, containers.EnsureContainer<ContainerSlot>(terminal, CMUEmploymentConsoleComponent.TargetSlot));
                console.Operator = actor;
                Assert.That(system.GetOfficeState((terminal, console))!.PublicSession, Is.True);
                Assert.That(system.GetOfficeState((terminal, console))!.Leader, Is.False);
                Assert.That(system.TryApply((terminal, console), actor, new(entities.GetNetEntity(target), console.Revision,
                    CMUEmploymentAction.Hire, "Deputy", "New employee", new() { "CMAccessColonyBrig" }, true, true)), Is.False);
                var salary = entities.GetComponent<DepartmentConsoleComponent>(department).DefaultSalary;
                Assert.That(system.TryPayrollAction((terminal, console), actor, new(CMUEmploymentPayrollAction.DefaultSalary,
                    console.Revision, 0, entities.GetNetEntity(department), null, salary + 100, true)), Is.False);
                Assert.That(entities.GetComponent<DepartmentConsoleComponent>(department).DefaultSalary, Is.EqualTo(salary));
                system.HandleOfficeAction((terminal, console), actor, new(CMUEmploymentOfficeAction.Book, console.Revision,
                    text: "Trespass", minutes: 5, confirmed: true));
                Assert.That(system.GetOfficeState((terminal, console))!.Jail, Is.Empty);
                system.HandleOfficeAction((terminal, console), actor, new(CMUEmploymentOfficeAction.Report, console.Revision,
                    text: "A broken door at the eastern landing pad."));
                Assert.That(system.GetOfficeState((terminal, console))!.Requests, Has.Count.EqualTo(1));
                Assert.That(entities.HasComponent<CMUEmploymentPrintJobComponent>(terminal), Is.True);
                Server.System<SharedAccessSystem>().TrySetTags(card, new ProtoId<AccessLevelPrototype>[] { "CMAccessColonyBrig" });
                Server.System<SharedIdCardSystem>().TryChangeFullName(target, "Inmate", entities.GetComponent<IdCardComponent>(target));
                entities.RemoveComponent<CMUEmploymentPrintJobComponent>(terminal);
                system.HandleOfficeAction((terminal, console), actor, new(CMUEmploymentOfficeAction.Book, console.Revision,
                    text: "Trespass", minutes: 5, confirmed: true));
                var booking = system.GetOfficeState((terminal, console))!.Jail.Single();
                Assert.That(booking.Released, Is.False);
                system.HandleOfficeAction((terminal, console), actor, new(CMUEmploymentOfficeAction.Release, console.Revision,
                    booking.Id, "Sentence commuted", confirmed: true));
                Assert.That(system.GetOfficeState((terminal, console))!.Jail.Single().Released, Is.True);
            });
        }
        finally
        {
            await Server.WaitPost(() =>
            {
                foreach (var uid in spawned)
                    SEntMan.DeleteEntity(uid);
            });
            await map.CleanUp();
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task PayrollRequestsWaitForApprovalAndRejectChangedDepartmentData(bool stale)
    {
        var map = await Pair.CreateTestMap();
        var spawned = new List<EntityUid>();
        try
        {
            await Server.WaitAssertion(() =>
            {
                var entities = SEntMan;
                var system = Server.System<CMUEmploymentConsoleSystem>();
                var containers = Server.System<SharedContainerSystem>();
                EntityUid Spawn(string prototype)
                {
                    var uid = entities.SpawnEntity(prototype, map.GridCoords);
                    spawned.Add(uid);
                    return uid;
                }
                var actor = Spawn("CMMobHuman");
                var terminal = Spawn("CMUHeadEngineeringTerminal");
                var adminTerminal = Spawn("CMUEmploymentConsoleColony");
                var departmentUid = Spawn("AUDepartmentConsoleEngineering");
                var headId = Spawn("CMUEmploymentBlankId");
                var adminId = Spawn("CMUEmploymentColonyOperatorId");
                entities.GetComponent<ApcPowerReceiverComponent>(terminal).NeedsPower = false;
                entities.GetComponent<ApcPowerReceiverComponent>(adminTerminal).NeedsPower = false;
                Server.System<SharedAccessSystem>().TrySetTags(headId, new ProtoId<AccessLevelPrototype>[] { "AU14AccessColonyHeadEngineer" });
                containers.Insert(headId, containers.EnsureContainer<ContainerSlot>(terminal, CMUEmploymentConsoleComponent.OperatorSlot));
                containers.Insert(adminId, containers.EnsureContainer<ContainerSlot>(adminTerminal, CMUEmploymentConsoleComponent.OperatorSlot));
                var console = entities.GetComponent<CMUEmploymentConsoleComponent>(terminal);
                var admin = entities.GetComponent<CMUEmploymentConsoleComponent>(adminTerminal);
                console.Operator = actor;
                admin.Operator = actor;
                var department = entities.GetComponent<DepartmentConsoleComponent>(departmentUid);
                var before = department.DefaultSalary;
                Assert.That(system.TryColonyAction((terminal, console), actor,
                    new(CMUEmploymentColonyAction.Refresh, console.Revision)), Is.True);
                Assert.That(system.TryPayrollAction((terminal, console), actor,
                    new(CMUEmploymentPayrollAction.DefaultSalary, console.Revision, console.ColonySnapshot!.Version,
                        entities.GetNetEntity(departmentUid), null, before + 25, true)), Is.True);
                Assert.That(department.DefaultSalary, Is.EqualTo(before), "A head's submission must not change payroll immediately.");
                var request = system.GetOfficeState((adminTerminal, admin))!.Requests.Single();
                var content = entities.GetComponent<CMUEmploymentPrintJobComponent>(terminal).Content;
                var paperUid = Spawn("CMUPaperSEEGSON");
                var paper = entities.GetComponent<PaperComponent>(paperUid);
                Server.System<PaperSystem>().SetContent(paperUid, content);
                paper.StampedBy.Add(new StampDisplayInfo { StampedName = "stamp-component-stamped-name-au14-admin", StampedColor = Color.Black });
                containers.Insert(paperUid, containers.EnsureContainer<ContainerSlot>(adminTerminal, "CMUEmploymentApprovalPaper"));
                if (stale)
                    department.DefaultSalary++;
                system.HandleOfficeAction((adminTerminal, admin), actor,
                    new(CMUEmploymentOfficeAction.Approve, admin.Revision, request.Id, confirmed: true));
                Assert.That(department.DefaultSalary, Is.EqualTo(stale ? before + 1 : before + 25));
                Assert.That(system.GetOfficeState((adminTerminal, admin))!.Requests.Single().Pending, Is.False);
            });
        }
        finally
        {
            await Server.WaitPost(() =>
            {
                foreach (var uid in spawned)
                    SEntMan.DeleteEntity(uid);
            });
            await map.CleanUp();
        }
    }

}
