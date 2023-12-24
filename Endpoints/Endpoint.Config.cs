namespace Brandmauer;

public static partial class Endpoint
{
    public static class Config
    {
        public static IResult Get()
        {
            var data = Database.Use(x => x.Config);

            return Results.Json(data);
        }

        public static IResult Set(Brandmauer.Config data)
        {
            Database.Use(x =>
            {
                x.Config = x.Replace(x.Config, data);
                x.Save(logging: true);
            });

            return Results.Ok();
        }
    }
}
