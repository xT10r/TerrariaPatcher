﻿using System;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using PluginLoader;
using Terraria;
using Terraria.ID;
using Keys = Microsoft.Xna.Framework.Input.Keys;

namespace TranscendPlugins
{
    public class NPC : MarshalByRefObject, IPluginChatCommand
    {
        private Keys toggleKey, increaseKey, decreaseKey;
        private int previousMaxSpawns;
        private int previousSpawnRate;

        private FieldInfo defaultMaxSpawns;
        private int DefaultMaxSpawns
        {
            get { return (int)defaultMaxSpawns.GetValue(null); }
            set { defaultMaxSpawns.SetValue(null, value); }
        }

        private int internalSpawnRatePercent;
        private FieldInfo defaultSpawnRate;
        private int DefaultSpawnRate
        {
            get
            {
                return internalSpawnRatePercent;
                /*var val = (int) defaultSpawnRate.GetValue(null);
                if (val == 0) return int.MaxValue;
                return 60000 / val;*/
            }
            set
            {
                internalSpawnRatePercent = value;
                if (value == 0)
                    defaultSpawnRate.SetValue(null, int.MaxValue);
                else
                    defaultSpawnRate.SetValue(null, 60000 / value);
            }
        }
        
        public NPC()
        {
            var npc = Assembly.GetEntryAssembly().GetType("Terraria.NPC");
            defaultMaxSpawns = npc.GetField("defaultMaxSpawns", BindingFlags.Static | BindingFlags.NonPublic);
            defaultSpawnRate = npc.GetField("defaultSpawnRate", BindingFlags.Static | BindingFlags.NonPublic);

            DefaultMaxSpawns = int.Parse(IniAPI.ReadIni("Spawning", "SpawnLimit", "5", writeIt: true));
            DefaultSpawnRate = int.Parse(IniAPI.ReadIni("Spawning", "SpawnRate", "100", writeIt: true));

            if (!Keys.TryParse(IniAPI.ReadIni("NPC", "Toggle", "N", writeIt: true), out toggleKey))
                toggleKey = Keys.N;
            if (!Keys.TryParse(IniAPI.ReadIni("NPC", "Increase", "OemPlus", writeIt: true), out increaseKey))
                increaseKey = Keys.OemPlus;
            if (!Keys.TryParse(IniAPI.ReadIni("NPC", "Decrease", "OemMinus", writeIt: true), out decreaseKey))
                decreaseKey = Keys.OemMinus;

            Color purple = Color.Purple;
            Loader.RegisterHotkey(() =>
            {
                if (Loader.IsControlModifierKeyDown())
                {
                    ModifySpawnLimit(-1);
                    Main.NewText("Spawn limit: " + DefaultMaxSpawns, purple.R, purple.G, purple.B, false);
                }
                else
                {
                    ModifySpawnRate(-20);
                    Main.NewText("Spawn rate: " + DefaultSpawnRate + "%", purple.R, purple.G, purple.B, false);
                }
            }, decreaseKey);

            Loader.RegisterHotkey(() =>
            {
                if (Loader.IsControlModifierKeyDown())
                {
                    ModifySpawnLimit(1);
                    Main.NewText("Spawn limit: " + DefaultMaxSpawns, purple.R, purple.G, purple.B, false);
                }
                else
                {
                    ModifySpawnRate(20);
                    Main.NewText("Spawn rate: " + DefaultSpawnRate + "%", purple.R, purple.G, purple.B, false);
                }
            }, increaseKey); 

            Loader.RegisterHotkey(() =>
            {
                if (DefaultMaxSpawns > 0)
                {
                    previousMaxSpawns = DefaultMaxSpawns;
                    previousSpawnRate = DefaultSpawnRate;
                    DefaultMaxSpawns = 0;
                    DefaultSpawnRate = 0;
                    KillAllNPCs();
                }
                else
                {
                    DefaultMaxSpawns = previousMaxSpawns;
                    DefaultSpawnRate = previousSpawnRate;
                }
                Main.NewText("Spawn rate: " + DefaultSpawnRate + "%", purple.R, purple.G, purple.B, false);
                Main.NewText("Spawn limit: " + DefaultMaxSpawns, purple.R, purple.G, purple.B, false);
            }, toggleKey);
        }

        private void ModifySpawnRate(int rate)
        {
            DefaultSpawnRate += rate;
            if (DefaultSpawnRate < 0) DefaultSpawnRate = 0;
            if (DefaultSpawnRate > 1000) DefaultSpawnRate = 1000;

            IniAPI.WriteIni("Spawning", "SpawnRate", DefaultSpawnRate.ToString());
        }

        private void ModifySpawnLimit(int rate)
        {
            DefaultMaxSpawns += rate;
            if (DefaultMaxSpawns < 0) DefaultMaxSpawns = 0;
            if (DefaultMaxSpawns > 150) DefaultMaxSpawns = 150;
            if (DefaultMaxSpawns == 0) KillAllNPCs();

            IniAPI.WriteIni("Spawning", "SpawnLimit", DefaultMaxSpawns.ToString());
        }

        private void KillAllNPCs()
        {
            for (int i = 0; i < Main.npc.Length; i++)
            {
                if (Main.npc[i] != null && !Main.npc[i].townNPC)
                {
                    Main.npc[i].life = 0;
                    if (Main.netMode == 2)
                        NetMessage.SendData(23, -1, -1, "", i, 0f, 0f, 0f, 0);
                }
            }
        }

        public bool OnChatCommand(string command, string[] args)
        {
            if (command != "npc") return false;

            if (args.Length < 1 || args.Length > 2 || args[0] == "help")
            {
                Main.NewText("Usage:");
                Main.NewText("  /npc id [count]");
                Main.NewText("  /npc name [count]");
                Main.NewText("  /npc help");
                Main.NewText("Example:");
                Main.NewText("  /npc 21");
                Main.NewText("  /npc 21 20");
                Main.NewText("  /npc Skeleton 20");
                return true;
            }

            int npcId;
            if (!int.TryParse(args[0], out npcId))
            {
                var field = typeof(NPCID).GetFields().FirstOrDefault(info => info.Name.ToLower() == args[0].ToLower());
                if (field != null)
                    npcId = Convert.ToInt32(field.GetValue(null));
            }
            if (npcId == 0)
            {
                Main.NewText("Invalid NPCID.");
                return true;
            }

            int count = 1;
            if (args.Length == 2)
            {
                if (!int.TryParse(args[1], out count))
                {
                    Main.NewText("Invalid count.");
                    return true;
                }
            }

            var player = Main.player[Main.myPlayer];
            var x = (int)player.Center.X;
            var y = (int)player.Center.Y - 150;
            for (int i = 0; i < count; i++)
            {
                if (npcId < 0)
                    Main.npc[Terraria.NPC.NewNPC(x, y, 1)].netDefaults(npcId); // special slime
                else
                    Terraria.NPC.NewNPC(x, y, npcId);
            }
            return true;
        }
    }
}
