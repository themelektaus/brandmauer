﻿namespace Brandmauer;

public static partial class Endpoint
{
    public static class Hosts
    {
        public static IResult GetAll()
        {
            var data = Database.Use(x => x.Hosts);

            return Results.Json(data);
        }

        public static IResult Get(long id)
        {
            var data = Database.Use(x => x.Hosts.FirstOrDefault(x => x.Identifier.Id == id));

            if (data is null)
                return Results.NotFound();

            return Results.Json(data);
        }

        public static IResult Post()
        {
            var data = Database.Use(x =>
            {
                var newData = x.Create<Host>();
                x.Hosts.Add(newData);
                x.Save();
                return newData;
            });

            return Results.Json(data);
        }

        public static IResult Put(Host data)
        {
            Database.Use(x =>
            {
                var index = x.Hosts.FindIndex(x => x.Identifier.Id == data.Identifier.Id);
                x.Hosts[index] = x.Replace(x.Hosts[index], data);
                x.Save();
            });

            ReverseProxyMiddleware.targetCache.Clear();

            return Results.Ok();
        }

        public static IResult Delete(long id)
        {
            Database.Use(x =>
            {
                x.Hosts.RemoveAll(x =>
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
