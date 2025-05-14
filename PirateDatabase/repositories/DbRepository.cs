using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using PirateDatabase.Models;
using System.Windows.Controls;

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

      
     ///(int) som cast funkade inte så provade ConvertToInt32:https://stackoverflow.com/questions/745172/better-way-to-cast-object-to-int
    
    public async Task OmboardPirateToShip(int pirateId, int shipId)
    {
        try
        {
            ///Jag ska kolla om en pirate är bemannad
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var pirateCheckCommand = new NpgsqlCommand("Select ship_id from pirate Where id =@pirateId", conn);
            pirateCheckCommand.Parameters.AddWithValue("pirateId", pirateId);
            var idCheck = await pirateCheckCommand.ExecuteScalarAsync();

            ///DB null check: https://github.com/systemvetenskap/gameCollection/blob/main/gameCollectionForelasning/repositories/DbRepository.cs
            if (idCheck != null && idCheck != DBNull.Value)
            {
                throw new Exception("Piraten är redan bemannad på ett skepp");
            }

            ///Nu ska jag kolla hur många pirater är på ett skepp

            var shipCheckCommand = new NpgsqlCommand(" Select Count (*) from pirate where ship_id = @shipId ", conn);
            shipCheckCommand.Parameters.AddWithValue("shipId", shipId);
            int shipCheck = Convert.ToInt32(await shipCheckCommand.ExecuteScalarAsync());

            ///Kolla hur många pirater ett skepp kan ta
            /// Join syntax:https://neon.tech/postgresql/postgresql-tutorial/postgresql-inner-join

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

            ///Nu ska piraten bemanna ett skepp
            ///Update syntax: https://neon.tech/postgresql/postgresql-tutorial/postgresql-update

            var OmboardCommand = new NpgsqlCommand("Update pirate Set ship_id =@ShipId Where id =@pirateId", conn);
            OmboardCommand.Parameters.AddWithValue("shipId", shipId);
            OmboardCommand.Parameters.AddWithValue("pirateId", pirateId);

            await OmboardCommand.ExecuteNonQueryAsync();
        }

        catch (Exception)
        {
            throw;
        }


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

    public async Task<List<ShipType>> GetAllShipTypes()
    {
        try
        {
            List<ShipType> shipTypes = new();
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            using var command = new NpgsqlCommand("Select id, name From ship_type", conn);
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    ShipType shipType = new ShipType
                    {
                        Id = (int)reader["id"],
                        Name = reader["name"].ToString()
                    };
                    
                    shipTypes.Add(shipType);
                }
            }
            return shipTypes;
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

    public async Task<List<Pirate>> SearchFörPirateOrParrot(string nameSearch)
    {
        try
        {
            List<Pirate> pirateSearch = new List<Pirate>();
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();



            using var command = new NpgsqlCommand("SELECT P.NAME AS PIRATE_NAME, S.NAME AS SHIP_NAME, " +
                  "(SELECT COUNT(*) FROM PIRATE WHERE SHIP_ID = S.ID) AS CREW_NUMBER, " +
                  "R.NAME AS RANK_NAME FROM  PIRATE  P " +
                  "LEFT JOIN SHIP S ON P.SHIP_ID = S.ID " +
                  "JOIN PIRATE_RANK R ON P.RANK_ID = R.ID " +
                  "LEFT JOIN PARROT PR ON P.ID = PR.PIRATE_ID " +
                  "WHERE P.NAME ILIKE @NAMESEARCH OR PR.NAME ILIKE @NAMESEARCH;", conn);

            command.Parameters.AddWithValue("nameSearch", nameSearch);

            using (var reader = await command.ExecuteReaderAsync())
            {


                while (await reader.ReadAsync())
                {
                  Pirate pirate  =  new Pirate
                    {
                        Name = reader["pirate_name"].ToString(),
                        ShipName = reader["ship_name"].ToString(),
                        RankName = reader["rank_name"].ToString(),
                        CrewNumber = Convert.ToInt32(reader["crew_number"])                                     //ConvertFromDBVal<int?>(reader["crew_number"])

                    };
                    pirateSearch.Add(pirate);
                }
            }
            return pirateSearch;
        }

        catch (Exception)
        {
            throw;
        }


    }


    //Jag vill med Transaction hantera sänkning av skeppet med slumpmässig överlevnad för besättningen.
    public async Task<Ship> SinkShip(int shipId)
    {
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        using var transaction = await conn.BeginTransactionAsync();


        try
        {

            ///Jag vill uppdatera ship tabell/sjunka skepet när man välja ett skepp
            var sinkingCommand = new NpgsqlCommand("Update ship Set is_sunken = True Where id = @shipId", conn, transaction);
            sinkingCommand.Parameters.AddWithValue("shipId", shipId);
            await sinkingCommand.ExecuteNonQueryAsync();

            ///Nu ska jag räkna hur många som var på skeppet
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
                return new Ship { TotalPirateCount=pirateIds.Count, SurvivorsCount = 0, DeadCount = 0 }; 
            }

            ///Nu ska jag slumpa från listan

            Random random = new Random();
            List<int> survivors = new List<int>();

            foreach (int pirateid in pirateIds)
            {
                if (random.Next(2) == 0)
                {
                    survivors.Add(pirateid);
                }
            }

            /// jag ska kolla om pirater finns i survivors listan
            List<int> lostAtSea = new List<int>();
            foreach (int pirateId in pirateIds)
            {
                bool isASurvivor = false;

                foreach (int survivorId in survivors)
                {
                    if (pirateId == survivorId)
                    {
                        isASurvivor = true;
                        break;
                    }
                }

                if (!isASurvivor)
                {
                    lostAtSea.Add(pirateId);
                }
            }

            /// Uppdatera skeppet status
            foreach (int survivorId in survivors)
            {
                var survivorsCommand = new NpgsqlCommand("Update pirate Set ship_id = Null Where id = @pirateId", conn, transaction);
                survivorsCommand.Parameters.AddWithValue("pirateId", survivorId);
                await survivorsCommand.ExecuteNonQueryAsync();
            }

            ///  uppdatera pirat status
            foreach (int deadPirateId in lostAtSea)
            {
                var perishedCommand = new NpgsqlCommand("Update pirate Set is_dead_at_sea = True, ship_id = Null,time_of_death=Now() Where id = @pirateId", conn, transaction);
                perishedCommand.Parameters.AddWithValue("pirateId", deadPirateId);
                await perishedCommand.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();

            return new Ship
            {
                TotalPirateCount= pirateIds.Count,
                SurvivorsCount = survivors.Count,
                DeadCount = lostAtSea.Count
            };
        }

        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new Exception("Något gick fel vid transaktionen, ingen ändring sparades.", ex);
        }
    }

    public async Task<string> UpdateCrewNumber(int shipId, int maxCrewNumber)
    {
        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var updateCommand = new NpgsqlCommand("Update ship_type Set max_crew_number = @maxCrewNumber " +
                                                   "Where id = (Select ship_type_id From ship where id= @shipId)", conn);
            updateCommand.Parameters.AddWithValue("maxCrewNumber", maxCrewNumber);
            updateCommand.Parameters.AddWithValue("shipId", shipId);
            await updateCommand.ExecuteNonQueryAsync();


            var checkShipTypeCommand = new NpgsqlCommand("Select st.name From Ship s Join ship_type st " +
                                                         "On s.ship_type_id =st.id Where s.id =@shipId", conn);
            checkShipTypeCommand.Parameters.AddWithValue("shipId", shipId);

            var shipTypeName = await checkShipTypeCommand.ExecuteScalarAsync();


            if (shipTypeName == null)
            {
                throw new Exception("Skeppstyp kunde inte hämtas.");
            }
            return shipTypeName.ToString();


        }

        catch (Exception)
        {
            throw;
        }

    }


    public async Task UpdateShipType(int shipId, int shipTypeId)
    {
        try
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            var ShipTypeUpdateCommand = new NpgsqlCommand("Update ship Set ship_type_id = @shipTypeId Where id = @shipId", conn);
            ShipTypeUpdateCommand.Parameters.AddWithValue("shipTypeId", shipTypeId);
            ShipTypeUpdateCommand.Parameters.AddWithValue("shipId", shipId);
            await ShipTypeUpdateCommand.ExecuteNonQueryAsync();
        }

        catch (Exception)
        {
            throw;
        }
    }
}

