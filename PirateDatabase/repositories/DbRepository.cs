using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using PirateDatabase.Models;

namespace PirateDatabase.repositories;

class DbRepository
{
    private readonly string _connectionString;
    public DbRepository()
    {
        var config = new ConfigurationBuilder()
                       .AddUserSecrets<DbRepository>()
                       .Build();

        _connectionString = config.GetConnectionString("DefaultConnection");
    }

    //Insert syntax: https://neon.tech/postgresql/postgresql-tutorial/postgresql-insert
    public async Task CreateNewPirate(Models.Pirate pirate)
    {

        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var command = new NpgsqlCommand("Insert into pirate (name, rank_id)  Values (@pirate_name, @rank_id)", conn);

            command.Parameters.AddWithValue("pirate_name", pirate.Name);
            command.Parameters.AddWithValue("rank_id", pirate.RankId);
            var result = await command.ExecuteNonQueryAsync();

        }

        // sqlstate syntax: https://github.com/systemvetenskap/gameCollection/blob/main/gameCollectionForelasning/repositories/DbRepository.cs
        catch (PostgresException pgEx)
        {
            if (pgEx.SqlState == "23505")
            {
                throw new Exception($"Piratnamnet '{pirate.Name}' finns redan i databasen. Namn måste vara unikt.", pgEx);
            }
        }

        catch (Exception ex)
        {

            throw new Exception("Ett fel uppstod när piraten skulle sparas.", ex);
        }
    }

    public async Task<List<Rank>> GetAllRanks()
    {
        try
        {
            List<Rank> ranks = new();
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var command = new NpgsqlCommand("Select id, name from rank", conn);

            using (var reader = command.ExecuteReader())
            {
             
                while (reader.Read())
                {
                    Rank rank = new Rank
                    {
                        Id = (int)reader["id"],
                        Name = reader["name"].ToString()
                    };
                    ranks.Add(rank);
                }
                return ranks;
            }

        }

        catch (Exception)
        {
            throw;
        }
        
    }

    /* Jag vill med TRANSACTION först kolla om en pirat är redan på ett skepp, räkna antal pirater på ett skepp och jämföra den med max_crew_number och sen bemanna skeppet*/

    public async Task OmboardApirateToShip(int pirateId, int shipId)
    {

    }
}  



