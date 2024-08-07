﻿namespace Brandmauer;

public static partial class Endpoint
{
    public static class DynamicDnsHosts
    {
        public static IResult GetAll()
        {
            var data = Database.Use(x => x.DynamicDnsHosts)
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Identifier.Id);

            return Results.Json(data);
        }

        public static IResult Get(long id)
        {
            var data = Database.Use(
                x => x.DynamicDnsHosts.FirstOrDefault(
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
                var newData = x.Create<DynamicDnsHost>();
                x.DynamicDnsHosts.Add(newData);
                x.Save(logging: true);
                return newData;
            });

            return Results.Json(data);
        }

        public static IResult Put(DynamicDnsHost data)
        {
            Database.Use(x =>
            {
                var index = x.DynamicDnsHosts.FindIndex(data.IsReferenceOf);
                x.DynamicDnsHosts[index] = x.Replace(x.DynamicDnsHosts[index], data);
                x.Save(logging: true);
            });

            return Results.Ok();
        }

        public static IResult Delete(long id)
        {
            Database.Use(x =>
            {
                x.DynamicDnsHosts.RemoveAll(x =>
                {
                    x.Dispose();
                    return x.Identifier.Id == id;
                });
                x.Save(logging: true);
            });
            return Results.Ok();
        }
    }
}
