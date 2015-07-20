﻿using System;
using PluginLoader;
using Terraria;

namespace GTRPlugins
{
    public class InventoryEnhancementPlugin : MarshalByRefObject, IPluginPlayerGetItem, IPluginUpdate, IPluginPlayerQuickBuff
    {
        public InventoryEnhancementPlugin()
        {
            InventoryEnhancements.Init();
        }

        public bool OnPlayerGetItem(Player player, Item newItem, out Item resultItem)
        {
            if (InventoryEnhancements.config.TrashList.Contains(newItem.type) && InventoryEnhancements.config.AutoTrash && Main.netMode == 0 && player.whoAmI == Main.myPlayer)
            {
                player.trashItem = newItem;
                resultItem = new Item();
                return true;
            }
            resultItem = null;
            return false;
        }

        public void OnUpdate()
        {
            InventoryEnhancements.Update(null);
        }

        public bool OnPlayerQuickBuff(Player player)
        {
            if (player.noItems) return true;

            if (player.chest != -1)
            {
                int num = 0;
                for (int i = 0; i < 40; i++)
                {
                    Chest chest;
                    if (player.chest > -1) chest = Main.chest[player.chest];
                    else if (player.chest == -2) chest = player.bank;
                    else chest = player.bank2;
                    if (player.CountBuffs() == 22) return true;

                    if (chest.item[i].stack > 0 && chest.item[i].type > 0 && chest.item[i].buffType > 0 && !chest.item[i].summon && chest.item[i].buffType != 90)
                    {
                        int num2 = chest.item[i].buffType;
                        bool flag = true;
                        for (int j = 0; j < 22; j++)
                        {
                            if (num2 == 27 && (player.buffType[j] == num2 || player.buffType[j] == 101 || player.buffType[j] == 102))
                            {
                                flag = false;
                                break;
                            }
                            if (player.buffType[j] == num2)
                            {
                                flag = false;
                                break;
                            }
                            if (Main.meleeBuff[num2] && Main.meleeBuff[player.buffType[j]])
                            {
                                flag = false;
                                break;
                            }
                        }
                        if (Main.lightPet[chest.item[i].buffType] || Main.vanityPet[chest.item[i].buffType])
                        {
                            for (int k = 0; k < 22; k++)
                            {
                                if (Main.lightPet[player.buffType[k]] && Main.lightPet[chest.item[i].buffType])
                                {
                                    flag = false;
                                }
                                if (Main.vanityPet[player.buffType[k]] && Main.vanityPet[chest.item[i].buffType])
                                {
                                    flag = false;
                                }
                            }
                        }
                        if (chest.item[i].mana > 0 && flag)
                        {
                            if (player.statMana >= (int)((float)chest.item[i].mana * player.manaCost))
                            {
                                player.manaRegenDelay = (int)player.maxRegenDelay;
                                player.statMana -= (int)((float)chest.item[i].mana * player.manaCost);
                            }
                            else
                            {
                                flag = false;
                            }
                        }
                        if (player.whoAmI == Main.myPlayer && chest.item[i].type == 603 && !Main.cEd)
                        {
                            flag = false;
                        }
                        if (num2 == 27)
                        {
                            num2 = Main.rand.Next(3);
                            if (num2 == 0)
                            {
                                num2 = 27;
                            }
                            if (num2 == 1)
                            {
                                num2 = 101;
                            }
                            if (num2 == 2)
                            {
                                num2 = 102;
                            }
                        }
                        if (flag)
                        {
                            num = chest.item[i].useSound;
                            int num3 = chest.item[i].buffTime;
                            if (num3 == 0)
                            {
                                num3 = 3600;
                            }
                            player.AddBuff(num2, num3, true);
                            if (chest.item[i].consumable)
                            {
                                chest.item[i].stack--;
                                if (chest.item[i].stack <= 0)
                                {
                                    chest.item[i].type = 0;
                                    chest.item[i].name = "";
                                }
                            }
                        }
                    }
                }
                if (Main.netMode == 1)
                {
                    if (player.chest < 0)
                    {
                        for (int i = 0; i < 40; i++)
                        {
                            NetMessage.SendData(32, -1, -1, "", player.chest, (float)i, 0f, 0f, 0, 0, 0);
                        }
                    }
                    else NetMessage.SendData(33, -1, -1, "", Main.player[Main.myPlayer].chest, 0f, 0f, 0f, 0, 0, 0);
                }
                if (num > 0)
                {
                    Main.PlaySound(2, (int)player.position.X, (int)player.position.Y, num);
                    Recipe.FindRecipes();
                }
            }

            return false;
        }
    }
}