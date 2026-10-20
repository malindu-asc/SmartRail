using System.Reflection;
using NetArchTest.Rules;

namespace SmartRail.ArchitectureTests;

// Fails the build when a layer or module boundary from the backend skill is broken.
public class ModuleBoundaryTests
{
    private static readonly string[] Modules = ["Identity", "Network", "Journeys", "Operations", "Notifications", "Ticketing"];
    private static readonly string[] Layers = ["Domain", "Application", "Infrastructure", "Api"];

    public static TheoryData<string> ModuleNames()
    {
        var data = new TheoryData<string>();
        foreach (var module in Modules) data.Add(module);
        return data;
    }

    private static Assembly Load(string module, string layer) =>
        Assembly.Load($"SmartRail.Modules.{module}.{layer}");

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Domain_has_no_framework_dependencies(string module)
    {
        var result = Types.InAssembly(Load(module, "Domain"))
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "MediatR", "Microsoft.AspNetCore", "SmartRail.Application.Core")
            .GetResult();

        Assert.True(result.IsSuccessful, Failing(result));
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Domain_does_not_depend_on_outer_layers(string module)
    {
        var result = Types.InAssembly(Load(module, "Domain"))
            .ShouldNot()
            .HaveDependencyOnAny(
                $"SmartRail.Modules.{module}.Application",
                $"SmartRail.Modules.{module}.Infrastructure",
                $"SmartRail.Modules.{module}.Api")
            .GetResult();

        Assert.True(result.IsSuccessful, Failing(result));
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Api_does_not_depend_on_infrastructure(string module)
    {
        var result = Types.InAssembly(Load(module, "Api"))
            .ShouldNot()
            .HaveDependencyOn($"SmartRail.Modules.{module}.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, Failing(result));
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Module_only_uses_other_modules_through_contracts(string module)
    {
        var forbidden = Modules
            .Where(other => other != module)
            .SelectMany(other => Layers.Select(layer => $"SmartRail.Modules.{other}.{layer}"))
            .ToArray();

        foreach (var layer in Layers)
        {
            var result = Types.InAssembly(Load(module, layer))
                .ShouldNot()
                .HaveDependencyOnAny(forbidden)
                .GetResult();

            Assert.True(result.IsSuccessful, $"{module}.{layer}: {Failing(result)}");
        }
    }

    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Contracts_contain_no_logic_dependencies(string module)
    {
        var result = Types.InAssembly(Load(module, "Contracts"))
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "MediatR", "Microsoft.AspNetCore", $"SmartRail.Modules.{module}.Domain")
            .GetResult();

        Assert.True(result.IsSuccessful, Failing(result));
    }

    private static string Failing(TestResult result) =>
        result.FailingTypeNames is null ? string.Empty : "Failing types: " + string.Join(", ", result.FailingTypeNames);
}
