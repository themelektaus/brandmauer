#if LINUX
namespace Brandmauer;

public static partial class Endpoint
{
    public static class Build
    {
        public static IResult Preview()
        {
            return Results.Text(GenerateBuilder().ToString());
        }

        static string LastBuild;

        public static IResult Dirty()
        {
            LastBuild ??= Database.Use(x => x.Config.LastBuild);

            return Results.Json(GenerateBuilder().ToString() != LastBuild);
        }

        public static IResult Apply(HttpRequest request)
        {
            return ApplyInternal(request, clear: false);
        }

        static IResult ApplyInternal(HttpRequest request, bool clear)
        {
            var builder = GenerateBuilder();
            builder.clear = clear;
            var build = builder.ToString();
            var result = ShellCommand.Execute(build);
            if (result.StatusCode == ShellCommand.Result._Status.OK)
            {
                LastBuild = Database.Use(x =>
                {
                    x.Config.LastBuild = build;
                    x.Save(logging: true);
                    return build;
                });
            }
            return result.ToResult(request);
        }

        public static IResult Clear(HttpRequest request)
        {
            return ApplyInternal(request, clear: true);
        }

        static IpTablesBuilder GenerateBuilder()
        {
            var builder = new IpTablesBuilder();

            var rules = Database.Use(x => x.Rules);
            foreach (var rule in rules)
                builder.filterInputs.AddRange(IpTablesFilter.From(rule));

            var natRoutes = Database.Use(x => x.NatRoutes);
            foreach (var natRoute in natRoutes)
                builder.natPreroutings.AddRange(
                    IpTablesNat.From(rules, natRoute)
                );

            return builder;
        }
    }
}
#endif
