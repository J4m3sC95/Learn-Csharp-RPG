using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Engine;
using System.IO;

namespace SuperAdenture
{
    public partial class SuperAdventure : Form
    {
        private Player _player;
        private Monster _currentMonster;

        private const string PLAYER_DATA_FILE_NAME = "PlayerData.xml";
       
        public SuperAdventure()
        {
            InitializeComponent();

            if (File.Exists(PLAYER_DATA_FILE_NAME))
            {
                _player = Player.CreatePlayerFromXmlString(File.ReadAllText(PLAYER_DATA_FILE_NAME));
            }
            else
            {
                _player = Player.CreateDefaultPlayer();
            }

            MoveTo(_player.CurrentLocation);
            UpdatePlayerStats();
            
        }

        private void UpdatePlayerStats()
        {
            lblHitPoints.Text = _player.CurrentHitPoints.ToString();
            lblGold.Text = _player.Gold.ToString();
            lblExperience.Text = _player.ExperiencePoints.ToString();
            lblLevel.Text = _player.Level.ToString();
        }

        private void btnNorth_Click(object sender, EventArgs e)
        {
            MoveTo(_player.CurrentLocation.LocationToNorth);
        }

        private void btnEast_Click(object sender, EventArgs e)
        {
            MoveTo(_player.CurrentLocation.LocationToEast);
        }

        private void btnSouth_Click(object sender, EventArgs e)
        {
            MoveTo(_player.CurrentLocation.LocationToSouth);
        }

        private void btnWest_Click(object sender, EventArgs e)
        {
            MoveTo(_player.CurrentLocation.LocationToWest);
        }

        private void UpdateInventoryListInUI()
        {
            //refresh inventory
            dgvInventory.RowHeadersVisible = false;

            dgvInventory.ColumnCount = 2;
            dgvInventory.Columns[0].Name = "Name";
            dgvInventory.Columns[0].Width = 197;
            dgvInventory.Columns[1].Name = "Quantity";

            dgvInventory.Rows.Clear();

            foreach (InventoryItem inventoryItem in _player.Inventory)
            {
                if (inventoryItem.Quantity > 0)
                {
                    dgvInventory.Rows.Add(new[] { inventoryItem.Details.Name, inventoryItem.Quantity.ToString() });
                }
            }
        }

        private void UpdateQuestListInUI()
        {
            //refresh quests
            dgvQuests.RowHeadersVisible = false;

            dgvQuests.ColumnCount = 2;
            dgvQuests.Columns[0].Name = "Name";
            dgvQuests.Columns[0].Width = 197;
            dgvQuests.Columns[1].Name = "Done?";

            dgvQuests.Rows.Clear();

            foreach (PlayerQuest playerQuest in _player.Quests)
            {
                dgvQuests.Rows.Add(new[] { playerQuest.Details.Name, playerQuest.IsCompleted.ToString() });
            }
        }

        private void UpdateWeaponListInUI()
        {
            //refresh weapon combobox

            List<Weapon> weapons = new List<Weapon>();

            foreach (InventoryItem inventoryItem in _player.Inventory)
            {
                if (inventoryItem.Details is Weapon)
                {
                    if (inventoryItem.Quantity > 0)
                    {
                        weapons.Add((Weapon)inventoryItem.Details);
                    }
                }
            }

            if (weapons.Count == 0)
            {
                cboWeapons.Visible = false;
                btnUseWeapon.Visible = false;
            }
            else
            {
                cboWeapons.SelectedIndexChanged -= cboWeapons_SelectedIndexChanged;
                cboWeapons.DataSource = weapons;
                cboWeapons.SelectedIndexChanged += cboWeapons_SelectedIndexChanged;
                cboWeapons.DisplayMember = "Name";
                cboWeapons.ValueMember = "ID";

                if (_player.CurrentWeapon != null)
                {
                    cboWeapons.SelectedItem = _player.CurrentWeapon;
                }
                else
                {
                    cboWeapons.SelectedIndex = 0;
                }
            }            
        }

        private void UpdatePotionsListInUI()
        {
            // refresh potions combobox
            List<HealingPotion> healingPotions = new List<HealingPotion>();

            foreach (InventoryItem inventoryItem in _player.Inventory)
            {
                if (inventoryItem.Details is HealingPotion)
                {
                    if (inventoryItem.Quantity > 0)
                    {
                        healingPotions.Add((HealingPotion)inventoryItem.Details);
                    }

                }
            }

            if (healingPotions.Count == 0)
            {
                cboPotions.Visible = false;
                btnUsePotion.Visible = false;
            }
            else
            {
                cboPotions.DataSource = healingPotions;
                cboPotions.DisplayMember = "Name";
                cboPotions.ValueMember = "ID";

                cboPotions.SelectedIndex = 0;
            }
        }

        private void MoveTo(Location newLocation)
        {
            // does player have required item?
            if (!_player.HasRequiredItemToEnterThisLocation(newLocation))
            {
                rtbMessages.Text += "You must have a " + newLocation.ItemRequiredToEnter.Name + " to enter this location" + Environment.NewLine;
                return;
            }

            // update player location
            _player.CurrentLocation = newLocation;

            // show/hide available buttons
            btnNorth.Visible = (newLocation.LocationToNorth != null);
            btnEast.Visible = (newLocation.LocationToEast != null);
            btnSouth.Visible = (newLocation.LocationToSouth != null);
            btnWest.Visible = (newLocation.LocationToWest != null);

            // show current location
            rtbLocation.Text = newLocation.Name + Environment.NewLine;
            rtbLocation.Text += newLocation.Description + Environment.NewLine;

            // heal player
            _player.CurrentHitPoints = _player.MaximumHitPoints;

            // update hit points in UI
            lblHitPoints.Text = _player.CurrentHitPoints.ToString();

            // is there a quest here?
            if(newLocation.QuestAvailableHere != null)
            {
                bool playerAlreadyHasQuest = _player.HasThisQuest(newLocation.QuestAvailableHere);
                bool playerAlreadyCompletedQuest = _player.CompletedThisQuest(newLocation.QuestAvailableHere);

                if (playerAlreadyHasQuest)
                {
                    if (!playerAlreadyCompletedQuest)
                    {
                        bool playerHasAllItemsToCompleteQuest = _player.HasAllQuestCompletionItems(newLocation.QuestAvailableHere);

                        if (playerHasAllItemsToCompleteQuest)
                        {
                            // display message
                            rtbMessages.Text += Environment.NewLine;
                            rtbMessages.Text += "You complete the " + newLocation.QuestAvailableHere.Name + " quest." + Environment.NewLine;

                            _player.RemoveQuestCompletionItems(newLocation.QuestAvailableHere);

                            // give quest rewards
                            rtbMessages.Text += "You receive:" + Environment.NewLine;
                            rtbMessages.Text += newLocation.QuestAvailableHere.RewardExperiencePoints.ToString() + " experience points" + Environment.NewLine;
                            rtbMessages.Text += newLocation.QuestAvailableHere.RewardGold.ToString() + " gold" + Environment.NewLine;
                            rtbMessages.Text += newLocation.QuestAvailableHere.RewardItem.Name + Environment.NewLine;
                            rtbMessages.Text += Environment.NewLine;

                            _player.AddExperiencePoints(newLocation.QuestAvailableHere.RewardExperiencePoints);
                            _player.Gold += newLocation.QuestAvailableHere.RewardGold;

                            _player.AddItemToInventory(newLocation.QuestAvailableHere.RewardItem);

                            _player.MarkQuestComplete(newLocation.QuestAvailableHere);

                            
                        }
                    }
                }

                else
                {
                    // player doesn't have quest

                    // display messages
                    rtbMessages.Text += "You receive the " + newLocation.QuestAvailableHere.Name + " quest." + Environment.NewLine;
                    rtbMessages.Text += newLocation.QuestAvailableHere.Description + Environment.NewLine;
                    rtbMessages.Text += "To complete it, return with:" + Environment.NewLine;

                    foreach(QuestCompletionItem qci in newLocation.QuestAvailableHere.QuestCompletionItems)
                    {
                        if(qci.Quantity == 1)
                        {
                            rtbMessages.Text += qci.Quantity.ToString() + " " + qci.Details.Name + Environment.NewLine;
                        }
                        else
                        {
                            rtbMessages.Text += qci.Quantity.ToString() + " " + qci.Details.NamePlural + Environment.NewLine;
                        }
                    }
                    rtbMessages.Text += Environment.NewLine;

                    // add the quest to players quest list
                    _player.Quests.Add(new PlayerQuest(newLocation.QuestAvailableHere));

                    
                }
            }

            //does the locatoin have a monster?
            if(newLocation.MonsterLivingHere != null)
            {
                rtbMessages.Text += "You see a " + newLocation.MonsterLivingHere.Name + Environment.NewLine;

                Monster standardMonster = World.MonsterByID(newLocation.MonsterLivingHere.ID);

                _currentMonster = new Monster(standardMonster.ID, standardMonster.Name, standardMonster.MaximumDamage,
                    standardMonster.RewardExperiencePoints, standardMonster.RewardGold, standardMonster.CurrentHitPoints,
                    standardMonster.MaximumHitPoints);

                foreach(LootItem lootItem in standardMonster.LootTable)
                {
                    _currentMonster.LootTable.Add(lootItem);
                }

                cboWeapons.Visible = true;
                cboPotions.Visible = true;
                btnUseWeapon.Visible = true;
                btnUsePotion.Visible = true;
            }
            else
            {
                _currentMonster = null;
                cboWeapons.Visible = false;
                cboPotions.Visible = false;
                btnUseWeapon.Visible = false;
                btnUsePotion.Visible = false;
            }

            UpdateInventoryListInUI();
            UpdateQuestListInUI();
            UpdateWeaponListInUI();
            UpdatePotionsListInUI();

            UpdatePlayerStats();

            ScrollToBottomOfMessages();

        }

        private void btnUseWeapon_Click(object sender, EventArgs e)
        {
            Weapon currentWeapon = (Weapon)cboWeapons.SelectedItem;

            int damageToMonster = RandomNumberGenerator.NumberBetween(currentWeapon.MinimumDamage, currentWeapon.MaximumDamage);

            _currentMonster.CurrentHitPoints -= damageToMonster;

            rtbMessages.Text += "You hit the " + _currentMonster.Name + " for " + damageToMonster.ToString() +
                " points." + Environment.NewLine;

            // is monster dead?
            if (_currentMonster.CurrentHitPoints <= 0)
            {
                rtbMessages.Text += Environment.NewLine;
                rtbMessages.Text += "You defeated the " + _currentMonster.Name + Environment.NewLine;

                // gain experience
                _player.AddExperiencePoints(_currentMonster.RewardExperiencePoints);
                rtbMessages.Text += "You receive " + _currentMonster.RewardExperiencePoints.ToString() +
                    " experience points" + Environment.NewLine;

                // gain gold
                _player.Gold += _currentMonster.RewardGold;
                rtbMessages.Text += "You receive " + _currentMonster.RewardGold.ToString() +
                    " gold" + Environment.NewLine;

                // gain loot
                List<InventoryItem> lootedItems = new List<InventoryItem>();
                foreach (LootItem li in _currentMonster.LootTable)
                {
                    if (RandomNumberGenerator.NumberBetween(1, 100) <= li.DropPercentage)
                    {
                        lootedItems.Add(new InventoryItem(li.Details, 1));
                    }
                }

                // default loot
                if(lootedItems.Count == 0)
                {
                    foreach(LootItem li in _currentMonster.LootTable)
                    {
                        if (li.IsDefaultItem)
                        {
                            lootedItems.Add(new InventoryItem(li.Details, 1));
                        }
                    }
                }

                // add loot to inventory
                foreach(InventoryItem ii in lootedItems)
                {
                    _player.AddItemToInventory(ii.Details);

                    if(ii.Quantity == 1)
                    {
                        rtbMessages.Text += "You loot " + ii.Quantity.ToString() + " " + ii.Details.Name + Environment.NewLine;
                    }
                    else
                    {
                        rtbMessages.Text += "You loot " + ii.Quantity.ToString() + " " + ii.Details.NamePlural + Environment.NewLine;
                    }
                }

                // refresh UI
                UpdatePlayerStats();

                UpdateInventoryListInUI();
                UpdatePotionsListInUI();
                UpdateWeaponListInUI();

                rtbMessages.Text += Environment.NewLine;

                // move to current location for healing
                MoveTo(_player.CurrentLocation);

            }

            else
            {
                // Monster is alive and does damage
                int damageToPlayer = RandomNumberGenerator.NumberBetween(0, _currentMonster.MaximumDamage);
                rtbMessages.Text += "The " + _currentMonster.Name + " did " + damageToPlayer.ToString() +
                    " points of damage." + Environment.NewLine;
                _player.CurrentHitPoints -= damageToPlayer;
                lblHitPoints.Text = _player.CurrentHitPoints.ToString();

                if(_player.CurrentHitPoints <= 0)
                {
                    // Is player dead?
                    rtbMessages.Text += "The " + _currentMonster.Name + " killed you!!" + Environment.NewLine;
                    MoveTo(World.LocationByID(World.LOCATION_ID_HOME));
                }
            }

            ScrollToBottomOfMessages();
        }

        private void btnUsePotion_Click(object sender, EventArgs e)
        {
            // use potion
            HealingPotion potion = (HealingPotion)cboPotions.SelectedItem;
            _player.CurrentHitPoints += potion.AmountToHeal;

            if(_player.CurrentHitPoints > _player.MaximumHitPoints)
            {
                _player.CurrentHitPoints = _player.MaximumHitPoints;
            }

            // remove from inventory
            foreach(InventoryItem ii in _player.Inventory)
            {
                if(ii.Details.ID == potion.ID)
                {
                    ii.Quantity--;
                    break;
                }
            }
            rtbMessages.Text += "You drink a " + potion.Name + Environment.NewLine;

            // Monster does damage
            int damageToPlayer = RandomNumberGenerator.NumberBetween(0, _currentMonster.MaximumDamage);
            rtbMessages.Text += "The " + _currentMonster.Name + " did " + damageToPlayer.ToString() +
                " points of damage." + Environment.NewLine;
            _player.CurrentHitPoints -= damageToPlayer;
            lblHitPoints.Text = _player.CurrentHitPoints.ToString();

            UpdateInventoryListInUI();
            UpdatePotionsListInUI();

            if (_player.CurrentHitPoints <= 0)
            {
                // Is player dead?
                rtbMessages.Text += "The " + _currentMonster.Name + " killed you!!" + Environment.NewLine;
                MoveTo(World.LocationByID(World.LOCATION_ID_HOME));
            }

            ScrollToBottomOfMessages();
        }

        private void ScrollToBottomOfMessages()
        {
            rtbMessages.SelectionStart = rtbMessages.Text.Length;
            rtbMessages.ScrollToCaret();
        }

        private void SuperAdventure_FormClosing(object sender, FormClosingEventArgs e)
        {
            File.WriteAllText(PLAYER_DATA_FILE_NAME, _player.ToXmlString());
        }

        private void cboWeapons_SelectedIndexChanged(object sender, EventArgs e)
        {
            _player.CurrentWeapon = (Weapon)cboWeapons.SelectedItem;
        }
    }
}
