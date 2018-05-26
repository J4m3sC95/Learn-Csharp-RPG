using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;

namespace Engine
{
    public static class PlayerDataMapper
    {
        private static readonly string _connectionString =
            "Data Source=(local);Initial Catalog=SuperAdventure;Integrated Security=True";

        public static Player CreateFromDatabase()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    Player player;

                    // create sql command object
                    using (SqlCommand savedGameCommand = connection.CreateCommand())
                    {
                        savedGameCommand.CommandType = CommandType.Text;
                        savedGameCommand.CommandText = "SELECT TOP 1 * FROM SavedGame";

                        // execute command and check if data is available
                        using (SqlDataReader reader = savedGameCommand.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                return null;
                            }

                            // read data from database and create player
                            reader.Read();

                            int currentHitPoints = (int)reader["CurrentHitPoints"];
                            int maximumHitPoints = (int)reader["MaximumHitPoints"];
                            int gold = (int)reader["Gold"];
                            int experiencePoints = (int)reader["ExperiencePoints"];
                            int currentLocationID = (int)reader["CurrentLocationID"];

                            player = Player.CreatePlayerFromDatabase(currentHitPoints, maximumHitPoints, gold,
                                experiencePoints, currentLocationID);
                        }                            
                    }

                    // read quest table
                    using (SqlCommand questCommand = connection.CreateCommand())
                    {
                        questCommand.CommandType = CommandType.Text;
                        questCommand.CommandText = "SELECT * FROM Quest";                                           

                        using (SqlDataReader reader = questCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    int questID = (int)reader["QuestID"];
                                    bool isCompleted = (bool)reader["IsCompleted"];

                                    PlayerQuest playerQuest = new PlayerQuest(World.QuestByID(questID));
                                    playerQuest.IsCompleted = isCompleted;
                                    player.Quests.Add(playerQuest);
                                }
                            } 
                        }
                    }

                    // read inventory
                    using (SqlCommand inventoryCommand = connection.CreateCommand())
                    {
                        inventoryCommand.CommandType = CommandType.Text;
                        inventoryCommand.CommandText = "SELECT * FROM Inventory";
                        
                        using (SqlDataReader reader = inventoryCommand.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    int inventoryItemID = (int)reader["InventoryItemID"];
                                    int quantity = (int)reader["Quantity"];

                                    player.AddItemToInventory(World.ItemByID(inventoryItemID), quantity);
                                }
                            } 
                        }
                    }

                    return player;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return null;
        }

        public static void SaveToDatabase(Player player)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // insert/update savedgame table
                    using (SqlCommand existingRowCountCommand = connection.CreateCommand())
                    {
                        existingRowCountCommand.CommandType = CommandType.Text;
                        existingRowCountCommand.CommandText = "SELECT count(*) FROM SavedGame";

                        int existingRowCount = (int)existingRowCountCommand.ExecuteScalar();

                        if(existingRowCount == 0)
                        {
                            using (SqlCommand insertSavedGame = connection.CreateCommand())
                            {
                                insertSavedGame.CommandType = CommandType.Text;
                                insertSavedGame.CommandText =
                                    "INSERT INTO SavedGame " +
                                    "(CurrentHitPoints, MaximumHitPoints, Gold, ExperiencePoints, CurrentLocationID) " +
                                    "VALUES " +
                                    "(@CurrentHitPoints, @MaximumHitPoints, @Gold, @ExperiencePoints, @CurrentLocationID)";

                                insertSavedGame.Parameters.Add("@CurrentHitPoints", SqlDbType.Int);
                                insertSavedGame.Parameters.Add("@MaximumHitPoints", SqlDbType.Int);
                                insertSavedGame.Parameters.Add("@Gold", SqlDbType.Int);
                                insertSavedGame.Parameters.Add("@ExperiencePoints", SqlDbType.Int);
                                insertSavedGame.Parameters.Add("@CurrentLocationID", SqlDbType.Int);

                                insertSavedGame.Parameters["@CurrentHitPoints"].Value = player.CurrentHitPoints;
                                insertSavedGame.Parameters["@MaximumHitPoints"].Value = player.MaximumHitPoints;
                                insertSavedGame.Parameters["@Gold"].Value = player.Gold;
                                insertSavedGame.Parameters["@ExperiencePoints"].Value = player.ExperiencePoints;
                                insertSavedGame.Parameters["@CurrentLocationID"].Value = player.CurrentLocation.ID;

                                insertSavedGame.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            using (SqlCommand updateSavedGame = connection.CreateCommand())
                            {
                                updateSavedGame.CommandType = CommandType.Text;
                                updateSavedGame.CommandText =
                                    "UPDATE SavedGame " +
                                    "SET CurrentHitPoints = @CurrentHitPoints, " +
                                    "MaximumHitPoints = @CurrentHitPoints, " +
                                    "Gold = @Gold, " +
                                    "ExperiencePoints = @ExperiencePoints, " +
                                    "CurrentLocationID = @CurrentLocationID";

                                updateSavedGame.Parameters.Add("@CurrentHitPoints", SqlDbType.Int);
                                updateSavedGame.Parameters.Add("@MaximumHitPoints", SqlDbType.Int);
                                updateSavedGame.Parameters.Add("@Gold", SqlDbType.Int);
                                updateSavedGame.Parameters.Add("@ExperiencePoints", SqlDbType.Int);
                                updateSavedGame.Parameters.Add("@CurrentLocationID", SqlDbType.Int);

                                updateSavedGame.Parameters["@CurrentHitPoints"].Value = player.CurrentHitPoints;
                                updateSavedGame.Parameters["@MaximumHitPoints"].Value = player.MaximumHitPoints;
                                updateSavedGame.Parameters["@Gold"].Value = player.Gold;
                                updateSavedGame.Parameters["@ExperiencePoints"].Value = player.ExperiencePoints;
                                updateSavedGame.Parameters["@CurrentLocationID"].Value = player.CurrentLocation.ID;

                                updateSavedGame.ExecuteNonQuery();
                            }
                        }
                    }

                    // delete existing Quests
                    using (SqlCommand deleteQuestsCommand = connection.CreateCommand())
                    {
                        deleteQuestsCommand.CommandType = CommandType.Text;
                        deleteQuestsCommand.CommandText = "DELETE FROM Quest";

                        deleteQuestsCommand.ExecuteNonQuery();
                    }

                    // add player quests
                    foreach(PlayerQuest playerQuest in player.Quests)
                    {
                        using (SqlCommand insertQuestCommand = connection.CreateCommand())
                        {
                            insertQuestCommand.CommandType = CommandType.Text;
                            insertQuestCommand.CommandText =
                                "INSERT INTO Quest (QuestID, IsCompleted)" +
                                "Values (@QUestID, @IsCompleted)";

                            insertQuestCommand.Parameters.Add("@QuestID", SqlDbType.Int);
                            insertQuestCommand.Parameters.Add("@IsCompleted", SqlDbType.Bit);

                            insertQuestCommand.Parameters["@QuestID"].Value = playerQuest.Details.ID;
                            insertQuestCommand.Parameters["@IsCompleted"].Value = playerQuest.IsCompleted;

                            insertQuestCommand.ExecuteNonQuery();
                        }
                    }

                    // delete inventory
                    using (SqlCommand deleteInventoryCommand = connection.CreateCommand())
                    {
                        deleteInventoryCommand.CommandType = CommandType.Text;
                        deleteInventoryCommand.CommandText = "DELETE FROM Inventory";

                        deleteInventoryCommand.ExecuteNonQuery();
                    }

                    foreach(InventoryItem inventoryItem in player.Inventory)
                    {
                        using (SqlCommand insertInventoryCommand = connection.CreateCommand())
                        {
                            insertInventoryCommand.CommandType = CommandType.Text;
                            insertInventoryCommand.CommandText =
                                "INSERT INTO Inventory (InventoryItemID, Quantity)" +
                                "VALUES (@InventoryItemID, @Quantity)";

                            insertInventoryCommand.Parameters.Add("@InventoryItemID", SqlDbType.Int);
                            insertInventoryCommand.Parameters.Add("@Quantity", SqlDbType.Int);

                            insertInventoryCommand.Parameters["@InventoryItemID"].Value = inventoryItem.Details.ID;
                            insertInventoryCommand.Parameters["@Quantity"].Value = inventoryItem.Quantity;

                            insertInventoryCommand.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
