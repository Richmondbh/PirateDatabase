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


    /* Jag vill först kolla om en pirat är redan på ett skepp, 
     * räkna antal pirater på ett skepp och
     * jämföra den med max_crew_number och sen kan hen bemanna skeppet
     * Ja ville använda transaction men tror att det är liksom "overkill"/inte nödvändigt
      */

    /*  
     (int) som cast funkade inte så provade ConvertToInt32:https://stackoverflow.com/questions/745172/better-way-to-cast-object-to-int
     */
    public async Task OmboardPirateToShip(int pirateId, int shipId)
    {
        //Jag ska kolla om en pirate är bemannad
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var pirateCheckCommand = new NpgsqlCommand("Select ship_id from pirate Where id =@pirateId", conn);
        pirateCheckCommand.Parameters.AddWithValue("pirateId", pirateId);
        var idCheck =  await pirateCheckCommand.ExecuteScalarAsync();

        //DB null check: https://github.com/systemvetenskap/gameCollection/blob/main/gameCollectionForelasning/repositories/DbRepository.cs
        if (idCheck!=null && idCheck != DBNull.Value)
        {
            throw new Exception("Piraten är redan bemannad på ett skepp");
        }

        //Nu ska jag kolla hur många pirater är på ett skepp

        var shipCheckCommand = new NpgsqlCommand(" Select Count (*) from pirate where ship_id = @shipId ", conn);
        shipCheckCommand.Parameters.AddWithValue("shipId", shipId);
        int shipCheck = Convert.ToInt32(await shipCheckCommand.ExecuteScalarAsync());

        //Kolla hur många pirater ett skepp kan ta
        // Join syntax:https://neon.tech/postgresql/postgresql-tutorial/postgresql-inner-join

        var shipCapacityCommand = new NpgsqlCommand("Select  st.max_crew_number from ship s " +
                                                     "join ship_type st on" +
                                                     " s.ship_type_id = st.id " +
                                                     "where s.id = @shipId", conn);
        shipCapacityCommand.Parameters.AddWithValue("@shipId", shipId);

        int capacityCheck = Convert.ToInt32(await shipCapacityCommand.ExecuteScalarAsync());

        if (shipCheck >= capacityCheck)
        {
            throw new Exception("Skeppet är fullt och kan inte bemannas");
        }

        //Nu ska piraten bemanna ett skepp
        //Update syntax: https://neon.tech/postgresql/postgresql-tutorial/postgresql-update

        var OmboardCommand = new NpgsqlCommand("Update pirate Set ship_id =@ShipId Where id =@pirateId",conn);
        OmboardCommand.Parameters.AddWithValue("shipId", shipId);
        OmboardCommand.Parameters.AddWithValue("pirateId", pirateId);

        await OmboardCommand.ExecuteNonQueryAsync();

    }

    public async Task<List<Ship>> GetAllShips()
    {
        try
        {
            List<Ship> ships = new();
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var command = new NpgsqlCommand("Select id, name From ship",conn);
            using(var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    Ship ship = new Ship
                    {
                        Id = (int)reader["id"],
                        Name = reader["name"].ToString()
                    };
                    ships.Add(ship);
                }
            }
            return ships;
        }

        catch (Exception)
        {
            throw;
        }
    }

    public async Task<List<Pirate>> GetAllPirates()
    {
        try
        {
            List<Pirate> pirates = new();
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var command = new NpgsqlCommand("Select id, name From pirate",conn);
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    Pirate pirate = new Pirate
                    {
                        Id = (int)reader["id"],
                        Name = reader["name"].ToString()
                    };
                    pirates.Add(pirate);
                }
            }
            return pirates;
        }

        catch (Exception)
        {
            throw;
        }
    }
}
 



