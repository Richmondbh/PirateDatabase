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

    public async Task<List<PirateRank>> GetAllRanks()
    {
        try
        {
            List<PirateRank> ranks = new();
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var command = new NpgsqlCommand("Select id, name from pirate_rank", conn);

            using (var reader = command.ExecuteReader())
            {

                while (reader.Read())
                {
                    PirateRank rank = new PirateRank
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
        var idCheck = await pirateCheckCommand.ExecuteScalarAsync();

        //DB null check: https://github.com/systemvetenskap/gameCollection/blob/main/gameCollectionForelasning/repositories/DbRepository.cs
        if (idCheck != null && idCheck != DBNull.Value)
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

        var OmboardCommand = new NpgsqlCommand("Update pirate Set ship_id =@ShipId Where id =@pirateId", conn);
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

            using var command = new NpgsqlCommand("Select id, name From ship", conn);
            using (var reader = command.ExecuteReader())
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

            using var command = new NpgsqlCommand("Select id, name From pirate", conn);
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

    //ILIKE syntax: https://neon.tech/postgresql/postgresql-tutorial/postgresql-like
    //Textsearch syntax:https://www.postgresql.org/docs/current/textsearch-controls.htm

    public async Task<Pirate> SearchFörPirateOrParrot(string nameSearch)
    {
        try
        {
            Pirate pirateSearch = null;
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();



            using var command = new NpgsqlCommand("SELECT P.NAME AS PIRATE_NAME, S.NAME AS SHIP_NAME, " +
                  "(SELECT COUNT(*) FROM PIRATE WHERE SHIP_ID = S.ID) AS CREW_NUMBER, " +
                  "R.NAME AS RANK_NAME FROM  PIRATE  P " +
                  "LEFT JOIN SHIP S ON P.SHIP_ID = S.ID " +
                  "JOIN PIRATE_RANK R ON P.RANK_ID = R.ID " +
                  "LEFT JOIN PARROT PR ON P.ID = PR.PIRATE_ID " +
                  "WHERE P.NAME ILIKE @NAMESEARCH OR PR.NAME ILIKE @NAMESEARCH LIMIT 1;", conn);


            command.Parameters.AddWithValue("nameSearch", nameSearch);

            using (var reader = await command.ExecuteReaderAsync())
            {


                while (await reader.ReadAsync())
                {
                    pirateSearch = new Pirate
                    {
                        Name = reader["pirate_name"].ToString(),
                        ShipName = reader["ship_name"].ToString(),
                        RankName = reader["rank_name"].ToString(),
                        CrewNumber = Convert.ToInt32(reader["crew_number"])                                     //ConvertFromDBVal<int?>(reader["crew_number"])

                    };

                }
            }
            return pirateSearch;
        }

        catch (Exception)
        {
            throw;
        }


    }

    //https://github.com/systemvetenskap/gameCollection/blob/main/gameCollectionForelasning/repositories/DbRepository.cs
    private static T? ConvertFromDBVal<T>(object obj)
    {
        if (obj == null || obj == DBNull.Value)
        {
            return default; // returns the default value for the type
        }
        return (T)obj;
    }

    //https://github.com/systemvetenskap/gameCollection/blob/main/gameCollectionForelasning/repositories/DbRepository.cs
    private static object ConvertToDBVal<T>(object obj)
    {
        if (obj == null || obj == string.Empty)
        {
            return DBNull.Value;
        }
        return (T)obj;
    }


    //Jag vill med Transaction hantera sänkning av skeppet med slumpmässig överlevnad för besättningen.
    public async Task SinkShip(int shipId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        using var transaction = await conn.BeginTransactionAsync();


        try
        {

            //Jag vill uppdatera ship tabell/sjunka skepet när man välja ett skepp
            var sinkingCommand = new NpgsqlCommand("Update ship Set is_sunken = TRUE Where id = @shipId", conn, transaction);
            sinkingCommand.Parameters.AddWithValue("shipId", shipId);
            await sinkingCommand.ExecuteNonQueryAsync();

            //Nu ska jag räkna hur många som var på skeppet
            var piratesCountCommand = new NpgsqlCommand("Select id From pirate Where ship_id = @shipId", conn, transaction);
            piratesCountCommand.Parameters.AddWithValue("shipId", shipId);

            List<int> pirateIds = new List<int>();

            using (var reader = piratesCountCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    pirateIds.Add((int)reader["id"]);
                }
            }

            if (pirateIds.Count == 0)
            {
                await transaction.CommitAsync();
                return;
            }

            //Nu ska jag slumpa från listan

            Random random = new Random();
            List<int> survivors = new List<int>();

            foreach (int pirateid in pirateIds)
            {
                if (random.Next(2) == 0)
                {
                    survivors.Add(pirateid);
                }
            }

            // updatera ship med survivors
            foreach (int survivorId in survivors)
            {
                var surviversCommand = new NpgsqlCommand("Update pirate Set ship_id=null where id =@pirateId", conn, transaction);
                surviversCommand.Parameters.AddWithValue("pirateId", survivorId);

                await surviversCommand.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }

        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new Exception("Något gick fel vid transaktionen, ingen ändring sparades.", ex);
        }
    }
}

