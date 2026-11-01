using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.CMU14.ColonyEconomy;
using Content.Server.CMU14.Employment;
using Content.Server.Power.Components;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.CMU14.ColonyEconomy;
using Content.Shared.CMU14.Employment;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.CMU14.Employment;

[TestFixture]
public sealed class CMUEmploymentColonyTest : GameTest
{
    [Test]
    public async Task DepartmentTransfersAndPaymentsRequireFreshAuthorizedRequests()
    {
        var map = await Pair.CreateTestMap();
        var spawned = new List<EntityUid>();
        float? originalBudget = null;
        try
        {
            await Server.WaitAssertion(() =>
            {
                var entities = SEntMan;
                var system = Server.System<CMUEmploymentConsoleSystem>();
                var access = Server.System<SharedAccessSystem>();
                var containers = Server.System<SharedContainerSystem>();
                var budget = Server.System<ColonyBudgetSystem>();
                originalBudget = budget.GetBudget();
                EntityUid Spawn(string prototype)
                {
                    var uid = entities.SpawnEntity(prototype, map.GridCoords);
                    spawned.Add(uid);
                    return uid;
                }
                var actor = Spawn("CMMobHuman");
                var stranger = Spawn("CMMobHuman");
                var terminal = Spawn("CMUEmploymentConsoleColony");
                var auth = Spawn("CMUEmploymentColonyOperatorId");
                var employee = Spawn("CMUEmploymentBlankId");
                var medicalUid = Spawn("AUDepartmentConsoleMedical");
                var engineeringUid = Spawn("AUDepartmentConsoleEngineering");
                var corporateUid = Spawn("AUDepartmentConsoleCorporate");
                var console = entities.GetComponent<CMUEmploymentConsoleComponent>(terminal);
                entities.GetComponent<ApcPowerReceiverComponent>(terminal).NeedsPower = false;
                var medical = entities.GetComponent<DepartmentConsoleComponent>(medicalUid);
                var engineering = entities.GetComponent<DepartmentConsoleComponent>(engineeringUid);
                var corporate = entities.GetComponent<DepartmentConsoleComponent>(corporateUid);
                corporate.ColonyManaged = false;
                medical.Members.Add(employee);
                medical.SalaryOverrides[employee] = 90;
                engineering.DepartmentBudget = 1000;
                Assert.That(medical.DepartmentAccessLevel, Is.Not.Null);
                Assert.That(engineering.DepartmentAccessLevel, Is.Not.Null);
                access.TrySetTags(employee, new[] { medical.DepartmentAccessLevel!.Value });
                var id = entities.GetComponent<IdCardComponent>(employee);
                Server.System<SharedIdCardSystem>().TryChangeFullName(employee, "Alex Reyes", id, actor);
                id.AccountBalance = 120;
                containers.Insert(auth, containers.EnsureContainer<ContainerSlot>(terminal, CMUEmploymentConsoleComponent.OperatorSlot));
                containers.Insert(employee, containers.EnsureContainer<ContainerSlot>(terminal, CMUEmploymentConsoleComponent.TargetSlot));
                console.Operator = actor;
                Assert.That(system.TryColonyAction((terminal, console), actor,
                    new(CMUEmploymentColonyAction.Refresh, console.Revision)), Is.True);
                Assert.That(console.ColonySnapshot!.Departments.Any(entry => entry.Console == entities.GetNetEntity(corporateUid)), Is.False);
                CMUEmploymentPayrollMessage Request(CMUEmploymentPayrollAction action, int amount = 0, bool confirmed = true)
                    => new(action, console.Revision, console.ColonySnapshot!.Version, entities.GetNetEntity(engineeringUid),
                        entities.GetNetEntity(employee), amount, confirmed);

                var transfer = Request(CMUEmploymentPayrollAction.Transfer);
                Assert.That(system.TryPayrollAction((terminal, console), stranger, transfer), Is.False);
                Assert.That(system.TryPayrollAction((terminal, console), actor, Request(CMUEmploymentPayrollAction.Transfer, confirmed: false)), Is.False);
                Assert.That(system.TryPayrollAction((terminal, console), actor, transfer), Is.True);
                Assert.That(medical.Members, Does.Not.Contain(employee));
                Assert.That(medical.SalaryOverrides.ContainsKey(employee), Is.False);
                Assert.That(engineering.Members, Does.Contain(employee));
                var tags = entities.GetComponent<AccessComponent>(employee).Tags;
                Assert.That(tags, Does.Contain(engineering.DepartmentAccessLevel!.Value));
                Assert.That(tags, Does.Not.Contain(medical.DepartmentAccessLevel!.Value));
                Assert.That(system.TryPayrollAction((terminal, console), actor, transfer), Is.False, "A transfer request cannot be replayed.");
                Assert.That(system.TryPayrollAction((terminal, console), actor, Request(CMUEmploymentPayrollAction.IndividualSalary, 75)), Is.True);
                Assert.That(engineering.SalaryOverrides[employee], Is.EqualTo(75));

                var payment = Request(CMUEmploymentPayrollAction.PayEmployee);
                var bankBefore = id.AccountBalance;
                var taxBudgetBefore = budget.GetBudget();
                var departmentBefore = engineering.DepartmentBudget;
                Assert.That(system.TryPayrollAction((terminal, console), actor, payment), Is.True);
                Assert.That(engineering.DepartmentBudget, Is.EqualTo(departmentBefore - 75));
                Assert.That(id.AccountBalance - bankBefore + budget.GetBudget() - taxBudgetBefore,
                    Is.EqualTo(75).Within(0.01), "Gross pay must become employee net pay plus colony income tax.");
                var balanceAfter = id.AccountBalance;
                Assert.That(system.TryPayrollAction((terminal, console), actor, payment), Is.False);
                Assert.That(id.AccountBalance, Is.EqualTo(balanceAfter), "Replaying payroll must not pay twice.");

                var stale = Request(CMUEmploymentPayrollAction.DefaultSalary, 60);
                engineering.DefaultSalary++;
                Assert.That(system.TryPayrollAction((terminal, console), actor, stale), Is.False,
                    "Changes at another department console must invalidate an old payroll view.");
                console.ColonyPayrollReadyAt = TimeSpan.Zero;
                engineering.DepartmentBudget = 10;
                console.ColonyRefreshAt = TimeSpan.Zero;
                Assert.That(system.TryColonyAction((terminal, console), actor,
                    new(CMUEmploymentColonyAction.Refresh, console.Revision)), Is.True);
                Assert.That(system.TryPayrollAction((terminal, console), actor, Request(CMUEmploymentPayrollAction.PayEmployee)), Is.False);
                Assert.That(id.AccountBalance, Is.EqualTo(balanceAfter));
                Assert.That(engineering.DepartmentBudget, Is.EqualTo(10));

                budget.AddToBudget(200);
                var colonyBefore = budget.GetBudget();
                var funding = Request(CMUEmploymentPayrollAction.FundDepartment, 100);
                Assert.That(system.TryPayrollAction((terminal, console), actor, funding), Is.True);
                Assert.That(budget.GetBudget(), Is.EqualTo(colonyBefore - 100));
                Assert.That(engineering.DepartmentBudget, Is.EqualTo(110));
                Assert.That(system.TryPayrollAction((terminal, console), actor, funding), Is.False);
                entities.GetComponent<ApcPowerReceiverComponent>(terminal).PowerDisabled = true;
                Assert.That(system.TryPayrollAction((terminal, console), actor, Request(CMUEmploymentPayrollAction.DefaultSalary, 20)), Is.False);
            });
        }
        finally
        {
            await Server.WaitPost(() =>
            {
                if (originalBudget is { } amount)
                {
                    var budget = Server.System<ColonyBudgetSystem>();
                    budget.AddToBudget(amount - budget.GetBudget());
                }
                for (var i = spawned.Count - 1; i >= 0; i--)
                {
                    if (SEntMan.EntityExists(spawned[i]))
                        SEntMan.DeleteEntity(spawned[i]);
                }
            });
        }
    }
}
