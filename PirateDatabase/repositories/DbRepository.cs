using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace PirateDatabase.DbRepository
{
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
    }    
}   
