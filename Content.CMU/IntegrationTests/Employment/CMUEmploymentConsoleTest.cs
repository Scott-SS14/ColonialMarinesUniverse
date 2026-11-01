using Content.IntegrationTests.Fixtures;
using Content.Server.CMU14.Employment;
using Content.Server.Power.Components;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Access;
using Content.Shared.CMU14.Employment;
using Content.Shared.Paper;
using Content.Shared.StatusIcon;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.CMU14.Employment;

[TestFixture]
public sealed class CMUEmploymentConsoleTest : GameTest
{
    [TestCase("CMUEmploymentConsoleGovfor", "AU14AccessGovforCommand", "AU14AccessGovforEngineering", "AU14JobIconGovCT")]
    [TestCase("CMUEmploymentConsoleColony", "AU14AccessColonyAdministrator", "CMAccessColonyEngineering", null)]
    [TestCase("CMUEmploymentConsoleWeyland", "CMUAccessWeylandPersonnel", "AU14AccessCorporateWEYU", "RMCJobIconPMCScientist")]
    public async Task HiringReplacesCredentialsAndTerminationDoesNotRestoreThem(
        string consolePrototype, string operatorAccess, string selectedAccess, string? selectedIcon)
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
                var iff = Server.System<GunIFFSystem>();
                var containers = Server.System<SharedContainerSystem>();
                var actor = entities.SpawnEntity("CMMobHuman", map.GridCoords);
                spawned.Add(actor);
                var console = entities.SpawnEntity(consolePrototype, map.GridCoords);
                spawned.Add(console);
                var operatorCard = entities.SpawnEntity("CMUEmploymentBlankId", map.GridCoords);
                spawned.Add(operatorCard);
                var target = entities.SpawnEntity("CMUEmploymentBlankId", map.GridCoords);
                spawned.Add(target);
                entities.GetComponent<ApcPowerReceiverComponent>(console).NeedsPower = false;
                access.TrySetTags(operatorCard, new ProtoId<AccessLevelPrototype>[] { operatorAccess });
                access.TrySetTags(target, new ProtoId<AccessLevelPrototype>[] { "AU14AccessOpforCommand" });
                iff.SetIdFaction((target, entities.EnsureComponent<ItemIFFComponent>(target)), "FactionCLF");
                var id = entities.GetComponent<IdCardComponent>(target);
                id.AccountBalance = 120;
                Assert.That(containers.Insert(operatorCard,
                    containers.EnsureContainer<ContainerSlot>(console, CMUEmploymentConsoleComponent.OperatorSlot)), Is.True);
                Assert.That(containers.Insert(target,
                    containers.EnsureContainer<ContainerSlot>(console, CMUEmploymentConsoleComponent.TargetSlot)), Is.True);
                var component = entities.GetComponent<CMUEmploymentConsoleComponent>(console);
                component.Operator = actor;
                var existingPapers = new HashSet<EntityUid>();
                var printedCount = 0;
                var before = entities.EntityQueryEnumerator<PaperComponent>();
                while (before.MoveNext(out var paperUid, out _))
                    existingPapers.Add(paperUid);
                void CollectPrintouts()
                {
                    var papers = entities.EntityQueryEnumerator<PaperComponent>();
                    while (papers.MoveNext(out var paperUid, out var paper))
                    {
                        if (!existingPapers.Add(paperUid))
                            continue;
                        spawned.Add(paperUid);
                        printedCount++;
                        Assert.That(paper.Content, Does.Contain("Alex Reyes"));
                        Assert.That(paper.EditingDisabled, Is.True);
                    }
                }
                var hire = new CMUEmploymentApplyMessage(entities.GetNetEntity(target), component.Revision,
                    CMUEmploymentAction.Hire, "Contract Mechanic", "Alex Reyes",
                    new List<ProtoId<AccessLevelPrototype>> { selectedAccess }, true, true,
                    selectedIcon == null ? null : new ProtoId<JobIconPrototype>(selectedIcon));

                Assert.That(system.TryApply((console, component), actor, hire), Is.True);
                Assert.That(entities.GetComponent<AccessComponent>(target).Tags,
                    Is.EquivalentTo(new ProtoId<AccessLevelPrototype>[] { selectedAccess }));
                Assert.That(iff.HasFaction(target, "FactionCLF"), Is.False);
                Assert.That(iff.HasFaction(target, component.Faction), Is.True);
                Assert.That(id.FullName, Is.EqualTo("Alex Reyes"));
                Assert.That(id.LocalizedJobTitle, Is.EqualTo("Contract Mechanic"));
                Assert.That(id.AccountBalance, Is.EqualTo(120), "Employment must not change the bank balance.");
                if (selectedIcon != null)
                    Assert.That(id.JobIcon, Is.EqualTo(new ProtoId<JobIconPrototype>(selectedIcon)));
                Assert.That(component.History, Has.Count.EqualTo(1));
                var receipt = component.LastReceipt;
                Assert.That(receipt, Is.Not.Null, "A completed transaction must acknowledge the UI.");
                Assert.That(receipt!.Card, Is.EqualTo(entities.GetNetEntity(target)));
                Assert.That(receipt.Action, Is.EqualTo(CMUEmploymentAction.Hire));
                Assert.That(receipt.Name, Is.EqualTo(id.FullName));
                Assert.That(system.TryApply((console, component), actor, hire), Is.False,
                    "Replaying an applied request must not execute it again.");
                Assert.That(component.LastReceipt, Is.SameAs(receipt), "A replay must not create another success receipt.");
                Assert.That(entities.HasComponent<CMUEmploymentPrintJobComponent>(console), Is.True);
                Assert.That(receipt!.Printed, Is.False);
                var power = entities.GetComponent<ApcPowerReceiverComponent>(console);
                power.PowerDisabled = true;
                system.Update(10f);
                Assert.That(entities.HasComponent<CMUEmploymentPrintJobComponent>(console), Is.True,
                    "Power loss must pause the print job without reapplying the credentials.");
                Assert.That(component.LastReceipt!.Printed, Is.False, "A paused printer must not report a completed record.");
                power.PowerDisabled = false;
                system.Update(10f);
                Assert.That(entities.HasComponent<CMUEmploymentPrintJobComponent>(console), Is.False);
                CollectPrintouts();
                receipt = component.LastReceipt;
                Assert.That(receipt!.Printed, Is.True, "The UI may report a printed record only after the paper was created.");
                Assert.That(printedCount, Is.EqualTo(1), "One successful transaction prints one record.");

                var duplicate = new CMUEmploymentApplyMessage(entities.GetNetEntity(target), component.Revision,
                    CMUEmploymentAction.Hire, "Contract Mechanic", "Alex Reyes",
                    new List<ProtoId<AccessLevelPrototype>> { selectedAccess }, true, true,
                    selectedIcon == null ? null : new ProtoId<JobIconPrototype>(selectedIcon));
                Assert.That(system.TryApply((console, component), actor, duplicate), Is.False,
                    "Identical credentials must not issue another personnel record.");
                Assert.That(component.LastReceipt, Is.SameAs(receipt), "An unchanged request must not acknowledge a new transaction.");
                Assert.That(component.History, Has.Count.EqualTo(1));

                var terminate = new CMUEmploymentApplyMessage(entities.GetNetEntity(target), component.Revision,
                    CMUEmploymentAction.Terminate, string.Empty, string.Empty, new(), false, true);
                Assert.That(system.TryApply((console, component), actor, terminate), Is.True);
                Assert.That(component.LastReceipt, Is.Not.SameAs(receipt));
                Assert.That(component.LastReceipt!.Action, Is.EqualTo(CMUEmploymentAction.Terminate));
                Assert.That(component.LastReceipt.Card, Is.EqualTo(entities.GetNetEntity(target)));
                Assert.That(entities.GetComponent<AccessComponent>(target).Tags, Is.Empty);
                Assert.That(iff.HasFaction(target, component.Faction), Is.False);
                Assert.That(iff.HasFaction(target, "FactionCLF"), Is.False);
                Assert.That(id.AccountBalance, Is.EqualTo(120));
                Assert.That(id.JobIcon, Is.EqualTo(new ProtoId<JobIconPrototype>("JobIconUnknown")));
                Assert.That(component.History, Has.Count.EqualTo(2));
                system.Update(10f);
                CollectPrintouts();
                Assert.That(printedCount, Is.EqualTo(2), "Termination prints one additional record.");
            });
        }
        finally
        {
            await Server.WaitPost(() =>
            {
                for (var i = spawned.Count - 1; i >= 0; i--)
                {
                    if (SEntMan.EntityExists(spawned[i]))
                        SEntMan.DeleteEntity(spawned[i]);
                }
            });
        }
    }

    [Test]
    public async Task InvalidOrUnauthenticatedRequestsDoNotChangeTheCard()
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
                var iff = Server.System<GunIFFSystem>();
                var containers = Server.System<SharedContainerSystem>();
                EntityUid Spawn(string prototype)
                {
                    var uid = entities.SpawnEntity(prototype, map.GridCoords);
                    spawned.Add(uid);
                    return uid;
                }
                var actor = Spawn("CMMobHuman");
                var stranger = Spawn("CMMobHuman");
                var console = Spawn("CMUEmploymentConsoleGovfor");
                var operatorCard = Spawn("CMUEmploymentBlankId");
                var target = Spawn("CMUEmploymentBlankId");
                var component = entities.GetComponent<CMUEmploymentConsoleComponent>(console);
                var power = entities.GetComponent<ApcPowerReceiverComponent>(console);
                power.NeedsPower = false;
                access.TrySetTags(operatorCard, new ProtoId<AccessLevelPrototype>[] { "AU14AccessGovforCommand" });
                access.TrySetTags(target, new ProtoId<AccessLevelPrototype>[] { "AU14AccessOpfor" });
                iff.SetIdFaction((target, entities.EnsureComponent<ItemIFFComponent>(target)), "FactionCLF");
                var auth = containers.EnsureContainer<ContainerSlot>(console, CMUEmploymentConsoleComponent.OperatorSlot);
                containers.Insert(operatorCard, auth);
                containers.Insert(target, containers.EnsureContainer<ContainerSlot>(console, CMUEmploymentConsoleComponent.TargetSlot));
                component.Operator = actor;
                CMUEmploymentApplyMessage Request(string tag, int? revision = null, bool confirmed = true)
                    => new(entities.GetNetEntity(target), revision ?? component.Revision, CMUEmploymentAction.Hire,
                        "Contractor", "Alex Reyes", new List<ProtoId<AccessLevelPrototype>> { tag }, true, confirmed);

                Assert.That(system.TryApply((console, component), actor, Request("AU14AccessOpforCommand")), Is.False);
                Assert.That(system.TryApply((console, component), actor, Request("AU14AccessGovfor", confirmed: false)), Is.False);
                Assert.That(system.TryApply((console, component), actor, Request("AU14AccessGovfor", -1)), Is.False);
                Assert.That(system.TryApply((console, component), stranger, Request("AU14AccessGovfor")), Is.False);
                var invalidIcon = new CMUEmploymentApplyMessage(entities.GetNetEntity(target), component.Revision,
                    CMUEmploymentAction.Hire, "Contractor", "Alex Reyes",
                    new List<ProtoId<AccessLevelPrototype>> { "AU14AccessGovfor" }, true, true,
                    new ProtoId<JobIconPrototype>("RMCJobIconPMCScientist"));
                Assert.That(system.TryApply((console, component), actor, invalidIcon), Is.False,
                    "A GOVFOR terminal must not accept an icon outside its configured choices.");
                power.PowerDisabled = true;
                Assert.That(system.TryApply((console, component), actor, Request("AU14AccessGovfor")), Is.False);
                power.PowerDisabled = false;
                access.TrySetTags(operatorCard, Array.Empty<ProtoId<AccessLevelPrototype>>());
                Assert.That(system.TryApply((console, component), actor, Request("AU14AccessGovfor")), Is.False);
                containers.Remove(operatorCard, auth);
                Assert.That(system.TryApply((console, component), actor, Request("AU14AccessGovfor")), Is.False);

                Assert.That(entities.GetComponent<AccessComponent>(target).Tags,
                    Is.EquivalentTo(new ProtoId<AccessLevelPrototype>[] { "AU14AccessOpfor" }));
                Assert.That(iff.HasFaction(target, "FactionCLF"), Is.True);
                Assert.That(component.History, Is.Empty);
                Assert.That(component.LastReceipt, Is.Null, "Rejected requests must not produce success feedback.");
            });
        }
        finally
        {
            await Server.WaitPost(() =>
            {
                for (var i = spawned.Count - 1; i >= 0; i--)
                {
                    if (SEntMan.EntityExists(spawned[i]))
                        SEntMan.DeleteEntity(spawned[i]);
                }
            });
        }
    }
}
