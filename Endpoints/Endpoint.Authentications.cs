﻿namespace Brandmauer;

public static partial class Endpoint
{
    public static class Authentications
    {
        public static IResult GetAll()
        {
            var data = Database.Use(x => x.Authentications)
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Identifier.Id);

            return Results.Json(data);
        }

        public static IResult Get(long id)
        {
            var data = Database.Use(
                x => x.Authentications.FirstOrDefault(
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
                var newData = x.Create<Authentication>();
                x.Authentications.Add(newData);
                x.Save();
                return newData;
            });

            return Results.Json(data);
        }

        public static IResult Put(Authentication data)
        {
            Database.Use(x =>
            {
                var index = x.Authentications.FindIndex(data.IsReferenceOf);
                x.Authentications[index] = x.Replace(x.Authentications[index], data);
                x.Save();
            });

            return Results.Ok();
        }

        public static IResult Delete(long id)
        {
            Database.Use(x =>
            {
                x.Authentications.RemoveAll(x =>
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