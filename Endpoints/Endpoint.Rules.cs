#if DEBUG || LINUX
namespace Brandmauer;

public static partial class Endpoint
{
    public static class Rules
    {
        public static IResult GetAll()
        {
            var data = Database.Use(x => x.Rules)
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Identifier.Id);

            return Results.Json(data);
        }

        public static IResult Get(long id)
        {
            var data = Database.Use(
                x => x.Rules.FirstOrDefault(
                    x => x.Identifier.Id == id
                )
            );

            if (data is null)
                return Results.NotFound();

            return Results.Json(data);
        }

        public static IResult Post()
        {
            var data = Database.Use(x =>
            {
                var newData = x.Create<Rule>();
                x.Rules.Add(newData);
                x.Save();
                return newData;
            });

            return Results.Json(data);
        }

        public static IResult Put(Rule data)
        {
            Database.Use(x =>
            {
                var index = x.Rules.FindIndex(data.IsReferenceOf);
                x.Rules[index] = x.Replace(x.Rules[index], data);
                x.Save();
            });

            return Results.Ok();
        }

        public static IResult Delete(long id)
        {
            Database.Use(x =>
            {
                x.Rules.RemoveAll(x =>
                {
                    x.Dispose();
                    return x.Identifier.Id == id;
                });
                x.Save();
            });
            return Results.Ok();
        }
    }
}
#endif
