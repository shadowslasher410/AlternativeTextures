﻿using AlternativeTextures.Framework.External.ContentPatcher;
using AlternativeTextures.Framework.External.GenericModConfigMenu;
using AlternativeTextures.Framework.Interfaces.API;
using AlternativeTextures.Framework.Managers;
using AlternativeTextures.Framework.Models;
using AlternativeTextures.Framework.Patches;
using AlternativeTextures.Framework.Patches.Buildings;
using AlternativeTextures.Framework.Patches.Entities;
using AlternativeTextures.Framework.Patches.GameLocations;
using AlternativeTextures.Framework.Patches.ShopLocations;
using AlternativeTextures.Framework.Patches.SpecialObjects;
using AlternativeTextures.Framework.Patches.StandardObjects;
using AlternativeTextures.Framework.Patches.Tools;
using AlternativeTextures.Framework.Utilities;
using AlternativeTextures.Framework.Utilities.Extensions;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.GameData;
using StardewValley.GameData.GiantCrops;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AlternativeTextures
{
    public class AlternativeTextures : Mod
    {
        // Core modData keys
        internal const string TEXTURE_TOKEN_HEADER = "AlternativeTextures/Textures/";
        internal const string TOOL_TOKEN_HEADER = "AlternativeTextures/Tools/";
        internal const string DEFAULT_OWNER = "Stardew.Default";
        internal const string ENABLED_SPRAY_CAN_TEXTURES = "Stardew.Default";

        // Compatibility keys
        internal const string TOOL_CONVERSION_COMPATIBILITY = "AlternativeTextures.HasConvertedMilkPails";
        internal const string TYPE_FIX_COMPATIBILITY = "AlternativeTextures.HasFixedBadObjectTyping";

        // Tool related keys
        internal const string PAINT_BUCKET_FLAG = "AlternativeTextures.PaintBucketFlag";
        internal const string OLD_PAINT_BUCKET_FLAG = "AlternativeTexturesPaintBucketFlag";
        internal const string PAINT_BRUSH_FLAG = "AlternativeTextures.PaintBrushFlag";
        internal const string PAINT_BRUSH_SCALE = "AlternativeTextures.PaintBrushScale";
        internal const string SCISSORS_FLAG = "AlternativeTextures.ScissorsFlag";
        internal const string SPRAY_CAN_FLAG = "AlternativeTextures.SprayCanFlag";
        internal const string SPRAY_CAN_RARE = "AlternativeTextures.SprayCanRare";
        internal const string SPRAY_CAN_RADIUS = "AlternativeTextures.SprayCanRadius";
        internal const string CATALOGUE_FLAG = "AlternativeTextures.CatalogueFlag";

        // Shared static helpers
        internal static IMonitor monitor;
        internal static IModHelper modHelper;
        internal static Multiplayer multiplayer;
        internal static ModConfig modConfig;

        // Managers
        internal static TextureManager textureManager;
        internal static MessageManager messageManager;
        internal static ApiManager apiManager;
        internal static AssetManager assetManager;

        // Utilities
        internal static FpsCounter fpsCounter;
        private static Api _api;

        // Tool related variables
        private Point _lastSprayCanTile = new();

        // Debugging flags
        private bool _displayFPS = false;

        public override void Entry(IModHelper helper)
        {
            // Set up the monitor, helper and multiplayer
            monitor = Monitor;
            modHelper = helper;
            multiplayer = helper.Reflection.GetField<Multiplayer>(typeof(Game1), "multiplayer").GetValue();

            // Setup our managers
            textureManager = new TextureManager(monitor, helper);
            messageManager = new MessageManager(monitor, helper, ModManifest.UniqueID);
            apiManager = new ApiManager(monitor);
            assetManager = new AssetManager(helper);

            // Setup our utilities
            fpsCounter = new FpsCounter();
            _api = new Api(this);

            // Load our Harmony patches
            try
            {
                var harmony = new Harmony(this.ModManifest.UniqueID);

                // Apply texture override related patches
                new GameLocationPatch(monitor, helper).Apply(harmony);
                new ObjectPatch(monitor, helper).Apply(harmony);
                new FencePatch(monitor, helper).Apply(harmony);
                new HoeDirtPatch(monitor, helper).Apply(harmony);
                new CropPatch(monitor, helper).Apply(harmony);
                new GiantCropPatch(monitor, helper).Apply(harmony);
                new GrassPatch(monitor, helper).Apply(harmony);
                new TreePatch(monitor, helper).Apply(harmony);
                new FruitTreePatch(monitor, helper).Apply(harmony);
                new ResourceClumpPatch(monitor, helper).Apply(harmony);
                new BushPatch(monitor, helper).Apply(harmony);
                new FlooringPatch(monitor, helper).Apply(harmony);
                new FurniturePatch(monitor, helper).Apply(harmony);
                new BedFurniturePatch(monitor, helper).Apply(harmony);
                new FishTankFurniturePatch(monitor, helper).Apply(harmony);

                // Start of special objects
                new ChestPatch(monitor, helper).Apply(harmony);
                new CrabPotPatch(monitor, helper).Apply(harmony);
                new IndoorPotPatch(monitor, helper).Apply(harmony);
                new PhonePatch(monitor, helper).Apply(harmony);
                new TorchPatch(monitor, helper).Apply(harmony);
                new WoodChipperPatch(monitor, helper).Apply(harmony);

                // Start of entity patches
                new CharacterPatch(monitor, helper).Apply(harmony);
                new ChildPatch(monitor, helper).Apply(harmony);
                new FarmAnimalPatch(monitor, helper).Apply(harmony);
                new HorsePatch(monitor, helper).Apply(harmony);
                new PetPatch(monitor, helper).Apply(harmony);
                new MonsterPatch(monitor, helper).Apply(harmony);

                // Start of building patches
                new BuildingPatch(monitor, helper).Apply(harmony);
                new ShippingBinPatch(monitor, helper).Apply(harmony);

                // Start of location patches
                new GameLocationPatch(monitor, helper).Apply(harmony);
                new ShopBuilderPatch(monitor, helper).Apply(harmony);

                // Paint tool related patches
                new ToolPatch(monitor, helper).Apply(harmony);
            }
            catch (Exception e)
            {
                Monitor.Log($"Issue with Harmony patching: {e}", LogLevel.Error);
                return;
            }

            // Add in our debug commands
            helper.ConsoleCommands.Add("at_spawn_monsters", "Spawns monster(s) of a specified type and quantity at the current location.\n\nUsage: at_spawn_monsters [MONSTER_ID] (QUANTITY)", this.DebugSpawnMonsters);
            helper.ConsoleCommands.Add("at_spawn_gc", "Spawns a giant crop based given harvest product id (e.g. Melon == 254).\n\nUsage: at_spawn_gc [HARVEST_ID]", this.DebugSpawnGiantCrop);
            helper.ConsoleCommands.Add("at_spawn_rc", "Spawns a resource clump based given resource name (e.g. Stump).\n\nUsage: at_spawn_rc [RESOURCE_NAME]", this.DebugSpawnResourceClump);
            helper.ConsoleCommands.Add("at_spawn_child", "Spawns a child. Potentially buggy / gamebreaking, do not use. \n\nUsage: at_spawn_child [AGE] [IS_MALE] [SKIN_TONE]", this.DebugSpawnChild);
            helper.ConsoleCommands.Add("at_set_age", "Sets age for all children in location. Potentially buggy / gamebreaking, do not use. \n\nUsage: at_set_age [AGE]", this.DebugSetAge);
            helper.ConsoleCommands.Add("at_display_fps", "Displays FPS counter. Use again to disable. \n\nUsage: at_display_fps", delegate { _displayFPS = !_displayFPS; });
            helper.ConsoleCommands.Add("at_paint_shop", "Shows the carpenter shop with the paint bucket for sale.\n\nUsage: at_paint_shop", this.DebugShowPaintShop);
            helper.ConsoleCommands.Add("at_set_object_texture", "Sets the texture of the object below the player.\n\nUsage: at_set_object_texture [TEXTURE_ID] (VARIATION_NUMBER) (SEASON)", this.DebugSetTexture);
            helper.ConsoleCommands.Add("at_clear_texture", "Clears the texture of the object below the player.\n\nUsage: at_clear_texture", this.DebugClearTexture);
            helper.ConsoleCommands.Add("at_reload", "Reloads all Alternative Texture content packs.\n\nUsage: at_reload", delegate { this.LoadContentPacks(); });

            // Hook into GameLoop events
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;

            // Hook into Input events
            helper.Events.Input.ButtonsChanged += OnButtonChanged;
            helper.Events.Input.ButtonPressed += OnButtonPressed;

            // Hook into Display events
            helper.Events.Display.Rendered += OnDisplayRendered;

            // Hook into the Content events
            helper.Events.Content.AssetRequested += OnContentAssetRequested;
            helper.Events.Content.AssetReady += OnContentAssetReady;

            // Hook into Multiplayer events
            helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
        }

        private void OnModMessageReceived(object sender, ModMessageReceivedEventArgs e)
        {
            if (e.FromModID == ModManifest.UniqueID)
            {
                messageManager.HandleIncomingMessage(e);
            }
        }

        private void OnContentAssetReady(object sender, AssetReadyEventArgs e)
        {
            var asset = e.Name;
            if (assetManager.toolKeyToData.ContainsKey(asset.Name))
            {
                assetManager.toolKeyToData[asset.Name].Texture = Helper.GameContent.Load<Texture2D>(asset);
            }
            else if (textureManager.GetTextureByToken(asset.Name) is Texture2D texture && texture is not null)
            {
                var loadedTexture = Helper.GameContent.Load<Texture2D>(asset.Name);

                textureManager.UpdateTexture(asset.Name, loadedTexture);
            }
        }

        private void OnContentAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.DataType == typeof(Texture2D))
            {
                var asset = e.Name;
                if (textureManager.GetModelByToken(asset.Name) is TokenModel tokenModel && tokenModel is not null)
                {
                    var originalTexture = tokenModel.AlternativeTexture.GetTexture(tokenModel.Variation);
                    var clonedTexture = originalTexture.CreateSelectiveCopy(Game1.graphics.GraphicsDevice, new Rectangle(0, 0, originalTexture.Width, originalTexture.Height));
                    e.LoadFrom(() => clonedTexture, AssetLoadPriority.Exclusive);
                }
                else if (assetManager.toolKeyToData.ContainsKey(asset.Name))
                {
                    e.LoadFromModFile<Texture2D>(assetManager.toolKeyToData[asset.Name].FilePath, AssetLoadPriority.Exclusive);
                }
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/AdditionalWallpaperFlooring") && textureManager.GetValidTextureNamesWithSeason().Count > 0)
            {
                e.Edit(asset =>
                {
                    List<ModWallpaperOrFlooring> moddedDecorations = asset.GetData<List<ModWallpaperOrFlooring>>();

                    foreach (var textureModel in textureManager.GetAllTextures().Where(t => t.IsDecoration() && !moddedDecorations.Any(d => d.Id == t.GetId())))
                    {
                        var decoration = new ModWallpaperOrFlooring()
                        {
                            Id = textureModel.GetId(),
                            Texture = $"{AlternativeTextures.TEXTURE_TOKEN_HEADER}{textureModel.GetTokenId()}",
                            IsFlooring = String.Equals(textureModel.ItemName, "Floor", StringComparison.OrdinalIgnoreCase),
                            Count = textureModel.GetVariations()
                        };

                        moddedDecorations.Add(decoration);
                    }
                });
            }
        }

        private void OnDisplayRendered(object sender, RenderedEventArgs e)
        {
            if (!_displayFPS)
            {
                return;
            }

            fpsCounter.OnRendered(sender, e);
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (Game1.activeClickableMenu is null && Game1.player.CurrentTool is GenericTool tool && e.Button is SButton.MouseRight)
            {
                var xTile = (int)e.Cursor.Tile.X * 64;
                var yTile = (int)e.Cursor.Tile.Y * 64;

                if (tool.modData.ContainsKey(PAINT_BRUSH_FLAG))
                {
                    Helper.Input.Suppress(e.Button);

                    RightClickPaintBrush(tool, xTile, yTile);
                }
                else if (tool.modData.ContainsKey(SPRAY_CAN_FLAG))
                {
                    Helper.Input.Suppress(e.Button);

                    if (RightClickSprayCan(tool, xTile, yTile))
                    {
                        ToolPatch.UsePaintBucket(Game1.player.currentLocation, xTile, yTile, Game1.player, true);
                    }
                }
            }
        }

        private void OnButtonChanged(object sender, ButtonsChangedEventArgs e)
        {
            if (Game1.activeClickableMenu is null && Game1.player.CurrentTool is GenericTool tool && e.Held.Contains(SButton.MouseLeft))
            {
                var xTile = (int)e.Cursor.Tile.X * 64;
                var yTile = (int)e.Cursor.Tile.Y * 64;

                if (tool.modData.ContainsKey(PAINT_BRUSH_FLAG))
                {
                    LeftClickPaintBrush(tool, xTile, yTile);
                }
                else if (tool.modData.ContainsKey(SPRAY_CAN_FLAG))
                {
                    LeftClickSprayCan(tool, xTile, yTile);
                }
                else if (tool.modData.ContainsKey(CATALOGUE_FLAG))
                {
                    ToolPatch.UseTextureCatalogue(Game1.player);
                }
            }
        }

        private void RightClickPaintBrush(GenericTool tool, int xTile, int yTile)
        {
            // Verify that a supported object exists at the tile
            var placedObject = PatchTemplate.GetObjectAt(Game1.currentLocation, xTile, yTile);
            if (placedObject is null)
            {
                var resourceClump = PatchTemplate.GetResourceClumpAt(Game1.currentLocation, xTile, yTile);
                var terrainFeature = PatchTemplate.GetTerrainFeatureAt(Game1.currentLocation, xTile, yTile);
                if (resourceClump is GiantCrop giantCrop)
                {
                    var modelType = AlternativeTextureModel.TextureType.GiantCrop;
                    var instanceName = Game1.objectData.ContainsKey(giantCrop.Id) ? Game1.objectData[giantCrop.Id].Name : String.Empty;
                    if (!giantCrop.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME) || !giantCrop.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION))
                    {
                        // Assign default modData
                        var instanceSeasonName = $"{instanceName}_{Game1.GetSeasonForLocation(giantCrop.Location)}";
                        PatchTemplate.AssignDefaultModData(giantCrop, instanceSeasonName, true);
                    }

                    Game1.addHUDMessage(new HUDMessage(modHelper.Translation.Get("messages.info.texture_copied"), 2) { timeLeft = 1000 });
                    tool.modData[PAINT_BRUSH_FLAG] = $"{modelType}_{instanceName}";
                    tool.modData[PAINT_BRUSH_SCALE] = 0.5f.ToString();
                    tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = giantCrop.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER];
                    tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = giantCrop.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME];
                    tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = giantCrop.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION];
                }
                else if (terrainFeature is Flooring flooring)
                {
                    var modelType = AlternativeTextureModel.TextureType.Flooring;
                    if (!flooring.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME) || !flooring.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION))
                    {
                        // Assign default modData
                        var instanceSeasonName = $"{modelType}_{PatchTemplate.GetFlooringName(flooring)}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
                        PatchTemplate.AssignDefaultModData(flooring, instanceSeasonName, true);
                    }

                    Game1.addHUDMessage(new HUDMessage(modHelper.Translation.Get("messages.info.texture_copied"), 2) { timeLeft = 1000 });
                    tool.modData[PAINT_BRUSH_FLAG] = $"{modelType}_{PatchTemplate.GetFlooringName(flooring)}";
                    tool.modData[PAINT_BRUSH_SCALE] = 0.5f.ToString();
                    tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = flooring.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER];
                    tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = flooring.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME];
                    tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = flooring.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION];
                }
                else if (terrainFeature is HoeDirt hoeDirt && hoeDirt.crop is not null)
                {
                    var modelType = AlternativeTextureModel.TextureType.Crop;
                    var instanceName = Game1.objectData.ContainsKey(hoeDirt.crop.netSeedIndex.Value) ? Game1.objectData[hoeDirt.crop.netSeedIndex.Value].Name : String.Empty;
                    if (!hoeDirt.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME) || !hoeDirt.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION))
                    {
                        // Assign default modData
                        var instanceSeasonName = $"{modelType}_{instanceName}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
                        PatchTemplate.AssignDefaultModData(hoeDirt, instanceSeasonName, true);
                    }

                    Game1.addHUDMessage(new HUDMessage(modHelper.Translation.Get("messages.info.texture_copied"), 2) { timeLeft = 1000 });
                    tool.modData[PAINT_BRUSH_FLAG] = $"{modelType}_{instanceName}";
                    tool.modData[PAINT_BRUSH_SCALE] = 0.5f.ToString();
                    tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = hoeDirt.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER];
                    tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = hoeDirt.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME];
                    tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = hoeDirt.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION];
                }
                else if (terrainFeature is Grass grass)
                {
                    var modelType = AlternativeTextureModel.TextureType.Grass;
                    if (!grass.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME) || !grass.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION))
                    {
                        // Assign default modData
                        var instanceSeasonName = $"{modelType}_Grass_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
                        PatchTemplate.AssignDefaultModData(grass, instanceSeasonName, true);
                    }

                    Game1.addHUDMessage(new HUDMessage(modHelper.Translation.Get("messages.info.texture_copied"), 2) { timeLeft = 1000 });
                    tool.modData[PAINT_BRUSH_FLAG] = $"{modelType}_Grass";
                    tool.modData[PAINT_BRUSH_SCALE] = 0.5f.ToString();
                    tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = grass.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER];
                    tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = grass.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME];
                    tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = grass.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION];
                }
                else if (terrainFeature is Bush bush)
                {
                    var modelType = AlternativeTextureModel.TextureType.Bush;
                    if (!bush.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME) || !bush.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION))
                    {
                        // Assign default modData
                        var instanceSeasonName = $"{modelType}_{PatchTemplate.GetBushTypeString(bush)}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
                        PatchTemplate.AssignDefaultModData(bush, instanceSeasonName, true);
                    }

                    Game1.addHUDMessage(new HUDMessage(modHelper.Translation.Get("messages.info.texture_copied"), 2) { timeLeft = 1000 });
                    tool.modData[PAINT_BRUSH_FLAG] = $"{modelType}_{PatchTemplate.GetBushTypeString(bush)}";
                    tool.modData[PAINT_BRUSH_SCALE] = 0.5f.ToString();
                    tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = bush.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER];
                    tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = bush.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME];
                    tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = bush.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION];
                }
                else
                {
                    tool.modData[PAINT_BRUSH_FLAG] = String.Empty;
                    tool.modData[PAINT_BRUSH_SCALE] = 0.5f.ToString();
                    if (terrainFeature != null)
                    {
                        Game1.addHUDMessage(new HUDMessage(modHelper.Translation.Get("messages.warning.brush_not_supported"), 3) { timeLeft = 2000 });
                    }
                    else
                    {
                        Game1.addHUDMessage(new HUDMessage(modHelper.Translation.Get("messages.info.cleared_brush"), 2) { timeLeft = 1000 });
                    }
                }
            }
            else
            {
                var modelType = placedObject is Furniture ? AlternativeTextureModel.TextureType.Furniture : AlternativeTextureModel.TextureType.Craftable;
                if (!placedObject.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME) || !placedObject.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION))
                {
                    var instanceSeasonName = $"{modelType}_{PatchTemplate.GetObjectName(placedObject)}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
                    PatchTemplate.AssignDefaultModData(placedObject, instanceSeasonName, true);
                }

                Game1.addHUDMessage(new HUDMessage(modHelper.Translation.Get("messages.info.texture_copied"), 2) { timeLeft = 1000 });
                tool.modData[PAINT_BRUSH_FLAG] = $"{modelType}_{PatchTemplate.GetObjectName(placedObject)}";
                tool.modData[PAINT_BRUSH_SCALE] = 0.5f.ToString();
                tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = placedObject.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER];
                tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = placedObject.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME];
                tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = placedObject.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION];
            }
        }

        private void LeftClickPaintBrush(GenericTool tool, int xTile, int yTile)
        {
            if (String.IsNullOrEmpty(tool.modData[PAINT_BRUSH_FLAG]))
            {
                Game1.addHUDMessage(new HUDMessage(modHelper.Translation.Get("messages.warning.brush_is_empty"), 3) { timeLeft = 2000 });
            }
            else
            {
                // Verify that a supported object exists at the tile
                var placedObject = PatchTemplate.GetObjectAt(Game1.currentLocation, xTile, yTile);
                if (placedObject is null)
                {
                    var resourceClump = PatchTemplate.GetResourceClumpAt(Game1.currentLocation, xTile, yTile);
                    var terrainFeature = PatchTemplate.GetTerrainFeatureAt(Game1.currentLocation, xTile, yTile);
                    if (resourceClump is GiantCrop giantCrop)
                    {
                        var modelType = AlternativeTextureModel.TextureType.GiantCrop;
                        var instanceName = Game1.objectData.ContainsKey(giantCrop.Id) ? Game1.objectData[giantCrop.Id].Name : String.Empty;
                        if (tool.modData[PAINT_BRUSH_FLAG] == $"{modelType}_{instanceName}")
                        {
                            giantCrop.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER];
                            giantCrop.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME];
                            giantCrop.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION];
                        }
                        else
                        {
                            Game1.addHUDMessage(new HUDMessage(modHelper.Translation.Get("messages.warning.invalid_copied_texture", new { textureName = tool.modData[PAINT_BRUSH_FLAG] }), 3) { timeLeft = 2000 });
                        }
                    }
                    else if (terrainFeature is Flooring flooring)
                    {
                        var modelType = AlternativeTextureModel.TextureType.Flooring;
                        if (tool.modData[PAINT_BRUSH_FLAG] == $"{modelType}_{PatchTemplate.GetFlooringName(flooring)}")
                        {
                            flooring.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER];
                            flooring.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME];
                            flooring.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION];
                        }
                        else
                        {
                            Game1.addHUDMessage(new HUDMessage(modHelper.Translation.Get("messages.warning.invalid_copied_texture", new { textureName = tool.modData[PAINT_BRUSH_FLAG] }), 3) { timeLeft = 2000 });
                        }
                    }
                    else if (terrainFeature is HoeDirt hoeDirt && hoeDirt.crop is not null)
                    {
                        var modelType = AlternativeTextureModel.TextureType.Crop;
                        var instanceName = Game1.objectData.ContainsKey(hoeDirt.crop.netSeedIndex.Value) ? Game1.objectData[hoeDirt.crop.netSeedIndex.Value].Name : String.Empty;
                        if (tool.modData[PAINT_BRUSH_FLAG] == $"{modelType}_{instanceName}")
                        {
                            hoeDirt.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER];
                            hoeDirt.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME];
                            hoeDirt.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION];
                        }
                        else
                        {
                            Game1.addHUDMessage(new HUDMessage(modHelper.Translation.Get("messages.warning.invalid_copied_texture", new { textureName = tool.modData[PAINT_BRUSH_FLAG] }), 3) { timeLeft = 2000 });
                        }
                    }
                    else if (terrainFeature is Grass grass)
                    {
                        var modelType = AlternativeTextureModel.TextureType.Grass;
                        if (tool.modData[PAINT_BRUSH_FLAG] == $"{modelType}_Grass")
                        {
                            grass.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER];
                            grass.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME];
                            grass.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION];
                        }
                        else
                        {
                            Game1.addHUDMessage(new HUDMessage(modHelper.Translation.Get("messages.warning.invalid_copied_texture", new { textureName = tool.modData[PAINT_BRUSH_FLAG] }), 3) { timeLeft = 2000 });
                        }
                    }
                    else if (terrainFeature is Bush bush)
                    {
                        var modelType = AlternativeTextureModel.TextureType.Bush;
                        if (tool.modData[PAINT_BRUSH_FLAG] == $"{modelType}_{PatchTemplate.GetBushTypeString(bush)}")
                        {
                            bush.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER];
                            bush.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME];
                            bush.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION];
                        }
                        else
                        {
                            Game1.addHUDMessage(new HUDMessage(modHelper.Translation.Get("messages.warning.invalid_copied_texture", new { textureName = tool.modData[PAINT_BRUSH_FLAG] }), 3) { timeLeft = 2000 });
                        }
                    }
                    else if (terrainFeature != null)
                    {
                        Game1.addHUDMessage(new HUDMessage(modHelper.Translation.Get("messages.warning.paint_not_placeable"), 3) { timeLeft = 2000 });
                    }
                }
                else
                {
                    var modelType = placedObject is Furniture ? AlternativeTextureModel.TextureType.Furniture : AlternativeTextureModel.TextureType.Craftable;
                    if (tool.modData[PAINT_BRUSH_FLAG] == $"{modelType}_{PatchTemplate.GetObjectName(placedObject)}")
                    {
                        placedObject.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER];
                        placedObject.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME];
                        placedObject.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = tool.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION];
                    }
                    else
                    {
                        Game1.addHUDMessage(new HUDMessage(modHelper.Translation.Get("messages.warning.invalid_copied_texture", new { textureName = tool.modData[PAINT_BRUSH_FLAG] }), 3) { timeLeft = 2000 });
                    }
                }
            }
        }

        private bool RightClickSprayCan(GenericTool tool, int xTile, int yTile)
        {
            // Verify that a supported object exists at the tile
            var cachedFlag = String.Empty;
            if (tool.modData.ContainsKey(SPRAY_CAN_FLAG))
            {
                cachedFlag = tool.modData[SPRAY_CAN_FLAG];
            }

            var placedObject = PatchTemplate.GetObjectAt(Game1.currentLocation, xTile, yTile);
            if (placedObject is null)
            {
                var resourceClump = PatchTemplate.GetResourceClumpAt(Game1.currentLocation, xTile, yTile);
                var terrainFeature = PatchTemplate.GetTerrainFeatureAt(Game1.currentLocation, xTile, yTile);
                if (resourceClump is GiantCrop giantCrop)
                {
                    var modelType = AlternativeTextureModel.TextureType.GiantCrop;
                    var instanceName = Game1.objectData.ContainsKey(giantCrop.Id) ? Game1.objectData[giantCrop.Id].Name : String.Empty;
                    if (!giantCrop.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME) || !giantCrop.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION))
                    {
                        // Assign default modData
                        var instanceSeasonName = $"{instanceName}_{Game1.GetSeasonForLocation(giantCrop.Location)}";
                        PatchTemplate.AssignDefaultModData(giantCrop, instanceSeasonName, true);
                    }

                    tool.modData[SPRAY_CAN_FLAG] = $"{modelType}_{instanceName}";
                }
                else if (terrainFeature is Flooring flooring)
                {
                    var modelType = AlternativeTextureModel.TextureType.Flooring;
                    if (!flooring.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME) || !flooring.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION))
                    {
                        // Assign default modData
                        var instanceSeasonName = $"{modelType}_{PatchTemplate.GetFlooringName(flooring)}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
                        PatchTemplate.AssignDefaultModData(flooring, instanceSeasonName, true);
                    }

                    tool.modData[SPRAY_CAN_FLAG] = $"{modelType}_{PatchTemplate.GetFlooringName(flooring)}";
                }
                else if (terrainFeature is HoeDirt hoeDirt && hoeDirt.crop is not null)
                {
                    var modelType = AlternativeTextureModel.TextureType.Crop;
                    var instanceName = Game1.objectData.ContainsKey(hoeDirt.crop.netSeedIndex.Value) ? Game1.objectData[hoeDirt.crop.netSeedIndex.Value].Name : String.Empty;
                    if (!hoeDirt.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME) || !hoeDirt.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION))
                    {
                        // Assign default modData
                        var instanceSeasonName = $"{modelType}_{instanceName}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
                        PatchTemplate.AssignDefaultModData(hoeDirt, instanceSeasonName, true);
                    }

                    tool.modData[SPRAY_CAN_FLAG] = $"{modelType}_{instanceName}";
                }
                else if (terrainFeature is Grass grass)
                {
                    var modelType = AlternativeTextureModel.TextureType.Grass;
                    if (!grass.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME) || !grass.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION))
                    {
                        // Assign default modData
                        var instanceSeasonName = $"{modelType}_Grass_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
                        PatchTemplate.AssignDefaultModData(grass, instanceSeasonName, true);
                    }

                    tool.modData[SPRAY_CAN_FLAG] = $"{modelType}_Grass";
                }
                else if (terrainFeature is Tree tree)
                {
                    var modelType = AlternativeTextureModel.TextureType.Tree;
                    if (!tree.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME) || !tree.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION))
                    {
                        // Assign default modData
                        var instanceSeasonName = $"{AlternativeTextureModel.TextureType.Tree}_{PatchTemplate.GetTreeTypeString(tree)}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
                        PatchTemplate.AssignDefaultModData(tree, instanceSeasonName, true);
                    }

                    tool.modData[SPRAY_CAN_FLAG] = $"{modelType}_{PatchTemplate.GetTreeTypeString(tree)}";
                }
                else if (terrainFeature is FruitTree fruitTree)
                {
                    var modelType = AlternativeTextureModel.TextureType.FruitTree;
                    var saplingName = Game1.objectData.ContainsKey(fruitTree.treeId.Value) ? Game1.objectData[fruitTree.treeId.Value].Name : String.Empty;
                    if (!fruitTree.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME) || !fruitTree.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION))
                    {
                        // Assign default modData
                        var instanceSeasonName = $"{AlternativeTextureModel.TextureType.FruitTree}_{saplingName}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
                        PatchTemplate.AssignDefaultModData(fruitTree, instanceSeasonName, true);
                    }

                    tool.modData[SPRAY_CAN_FLAG] = $"{modelType}_{saplingName}";
                }
                else
                {
                    if (Game1.currentLocation.IsBuildableLocation())
                    {
                        var targetedBuilding = Game1.currentLocation.getBuildingAt(new Vector2(xTile / 64, yTile / 64));
                        if (targetedBuilding != null)
                        {
                            Game1.addHUDMessage(new HUDMessage(modHelper.Translation.Get("messages.warning.spray_can_not_supported"), 3) { timeLeft = 2000 });
                            return false;
                        }
                    }
                    else
                    {
                        Game1.addHUDMessage(new HUDMessage(modHelper.Translation.Get("messages.warning.spray_can_not_supported"), 3) { timeLeft = 2000 });
                        return false;
                    }
                }
            }
            else
            {
                var modelType = placedObject is Furniture ? AlternativeTextureModel.TextureType.Furniture : AlternativeTextureModel.TextureType.Craftable;
                if (!placedObject.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME) || !placedObject.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION))
                {
                    var instanceSeasonName = $"{modelType}_{PatchTemplate.GetObjectName(placedObject)}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
                    PatchTemplate.AssignDefaultModData(placedObject, instanceSeasonName, true);
                }

                tool.modData[SPRAY_CAN_FLAG] = $"{modelType}_{PatchTemplate.GetObjectName(placedObject)}";
            }

            if (cachedFlag != tool.modData[SPRAY_CAN_FLAG])
            {
                Game1.player.modData[ENABLED_SPRAY_CAN_TEXTURES] = null;
            }

            return true;
        }

        private void LeftClickSprayCan(GenericTool tool, int xTile, int yTile)
        {
            if (_lastSprayCanTile.X == xTile && _lastSprayCanTile.Y == yTile)
            {
                return;
            }
            _lastSprayCanTile = new Point(xTile, yTile);

            if (Game1.player.modData.ContainsKey(ENABLED_SPRAY_CAN_TEXTURES) is false || String.IsNullOrEmpty(Game1.player.modData[ENABLED_SPRAY_CAN_TEXTURES]))
            {
                Game1.addHUDMessage(new HUDMessage(modHelper.Translation.Get("messages.warning.spray_can_is_empty"), 3) { timeLeft = 2000 });
            }
            else
            {
                var selectedModelsToVariations = JsonConvert.DeserializeObject<Dictionary<string, SelectedTextureModel>>(Game1.player.modData[ENABLED_SPRAY_CAN_TEXTURES]);

                if (selectedModelsToVariations.Count == 0)
                {
                    Game1.addHUDMessage(new HUDMessage(modHelper.Translation.Get("messages.warning.spray_can_is_empty"), 3) { timeLeft = 2000 });
                    return;
                }

                int tileRadius = 1;
                if (Game1.player.modData.ContainsKey(SPRAY_CAN_RADIUS) is false || int.TryParse(Game1.player.modData[SPRAY_CAN_RADIUS], out tileRadius) is false)
                {
                    Game1.player.modData[SPRAY_CAN_RADIUS] = "1";
                }
                tileRadius = tileRadius > 0 ? tileRadius - 1 : tileRadius;

                // Convert to standard game tiles
                xTile /= 64;
                yTile /= 64;
                for (int x = xTile - tileRadius; x <= xTile + tileRadius; x++)
                {
                    for (int y = yTile - tileRadius; y <= yTile + tileRadius; y++)
                    {
                        var actualX = x * 64;
                        var actualY = y * 64;

                        // Select random texture
                        Random random = new Random(Guid.NewGuid().GetHashCode());
                        var selectedModelIndex = random.Next(0, selectedModelsToVariations.Count);
                        var actualSelectedModel = selectedModelsToVariations.ElementAt(selectedModelIndex).Value;
                        var selectedVariationIndex = random.Next(0, actualSelectedModel.Variations.Count);
                        var actualSelectedVariation = actualSelectedModel.Variations[selectedVariationIndex].ToString();

                        // Verify that a supported object exists at the tile
                        var resourceClump = PatchTemplate.GetResourceClumpAt(Game1.currentLocation, actualX, actualY);
                        var terrainFeature = PatchTemplate.GetTerrainFeatureAt(Game1.currentLocation, actualX, actualY);
                        if (resourceClump is GiantCrop giantCrop)
                        {
                            var modelType = AlternativeTextureModel.TextureType.GiantCrop;
                            var instanceName = Game1.objectData.ContainsKey(giantCrop.Id) ? Game1.objectData[giantCrop.Id].Name : String.Empty;
                            if (tool.modData[SPRAY_CAN_FLAG] == $"{modelType}_{instanceName}")
                            {
                                giantCrop.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = actualSelectedModel.Owner;
                                giantCrop.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = actualSelectedModel.TextureName;
                                giantCrop.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = actualSelectedVariation;
                                continue;
                            }
                        }
                        else if (terrainFeature is Flooring flooring)
                        {
                            var modelType = AlternativeTextureModel.TextureType.Flooring;
                            if (tool.modData[SPRAY_CAN_FLAG] == $"{modelType}_{PatchTemplate.GetFlooringName(flooring)}")
                            {
                                flooring.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = actualSelectedModel.Owner;
                                flooring.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = actualSelectedModel.TextureName;
                                flooring.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = actualSelectedVariation;
                                continue;
                            }
                        }
                        if (terrainFeature is HoeDirt hoeDirt && hoeDirt.crop is not null)
                        {
                            var modelType = AlternativeTextureModel.TextureType.Crop;
                            var instanceName = Game1.objectData.ContainsKey(hoeDirt.crop.netSeedIndex.Value) ? Game1.objectData[hoeDirt.crop.netSeedIndex.Value].Name : String.Empty;
                            if (tool.modData[SPRAY_CAN_FLAG] == $"{modelType}_{instanceName}")
                            {
                                hoeDirt.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = actualSelectedModel.Owner;
                                hoeDirt.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = actualSelectedModel.TextureName;
                                hoeDirt.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = actualSelectedVariation;
                                continue;
                            }
                        }
                        if (terrainFeature is Grass grass)
                        {
                            var modelType = AlternativeTextureModel.TextureType.Grass;
                            if (tool.modData[SPRAY_CAN_FLAG] == $"{modelType}_Grass")
                            {
                                grass.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = actualSelectedModel.Owner;
                                grass.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = actualSelectedModel.TextureName;
                                grass.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = actualSelectedVariation;
                                continue;
                            }
                        }
                        if (terrainFeature is Tree tree)
                        {
                            var modelType = AlternativeTextureModel.TextureType.Tree;
                            if (tool.modData[SPRAY_CAN_FLAG] == $"{modelType}_{PatchTemplate.GetTreeTypeString(tree)}")
                            {
                                tree.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = actualSelectedModel.Owner;
                                tree.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = actualSelectedModel.TextureName;
                                tree.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = actualSelectedVariation;
                                continue;
                            }
                            else
                            {
                                Game1.addHUDMessage(new HUDMessage(modHelper.Translation.Get("messages.warning.invalid_copied_texture", new { textureName = tool.modData[SPRAY_CAN_FLAG] }), 3) { timeLeft = 2000 });
                            }
                        }
                        if (terrainFeature is FruitTree fruitTree)
                        {
                            var modelType = AlternativeTextureModel.TextureType.FruitTree;
                            var saplingName = Game1.fruitTreeData.ContainsKey(fruitTree.treeId.Value) ? Game1.objectData[fruitTree.treeId.Value].Name : String.Empty;
                            if (tool.modData[SPRAY_CAN_FLAG] == $"{modelType}_{saplingName}")
                            {
                                fruitTree.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = actualSelectedModel.Owner;
                                fruitTree.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = actualSelectedModel.TextureName;
                                fruitTree.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = actualSelectedVariation;
                                continue;
                            }
                            else
                            {
                                Game1.addHUDMessage(new HUDMessage(modHelper.Translation.Get("messages.warning.invalid_copied_texture", new { textureName = tool.modData[SPRAY_CAN_FLAG] }), 3) { timeLeft = 2000 });
                            }
                        }

                        var placedObject = PatchTemplate.GetObjectAt(Game1.currentLocation, actualX, actualY);
                        if (placedObject is not null)
                        {
                            var modelType = placedObject is Furniture ? AlternativeTextureModel.TextureType.Furniture : AlternativeTextureModel.TextureType.Craftable;
                            if (tool.modData[SPRAY_CAN_FLAG] == $"{modelType}_{PatchTemplate.GetObjectName(placedObject)}")
                            {
                                placedObject.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = actualSelectedModel.Owner;
                                placedObject.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = actualSelectedModel.TextureName;
                                placedObject.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = actualSelectedVariation;
                                continue;
                            }
                        }
                    }
                }
            }
        }

        public override object GetApi()
        {
            return _api;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // Set our default configuration file
            modConfig = Helper.ReadConfig<ModConfig>();

            if (Helper.ModRegistry.IsLoaded("spacechase0.MoreGiantCrops"))
            {
                apiManager.HookIntoMoreGiantCrops(Helper);
            }

            if (Helper.ModRegistry.IsLoaded("spacechase0.DynamicGameAssets"))
            {
                apiManager.HookIntoDynamicGameAssets(Helper);
            }

            if (Helper.ModRegistry.IsLoaded("Pathoschild.ContentPatcher") && apiManager.HookIntoContentPatcher(Helper))
            {
                apiManager.GetContentPatcherApi().RegisterToken(ModManifest, "Textures", new TextureToken(textureManager, assetManager));
                apiManager.GetContentPatcherApi().RegisterToken(ModManifest, "Tools", new ToolToken(textureManager, assetManager));
            }

            // Load any owned content packs
            this.LoadContentPacks();

            Monitor.Log($"Finished loading Alternative Textures content packs", LogLevel.Debug);

            // Register tools
            foreach (var tool in assetManager.toolKeyToData.ToList())
            {
                assetManager.toolKeyToData[tool.Key].Texture = Helper.GameContent.Load<Texture2D>(tool.Key);
            }

            // Hook into GMCM, if applicable
            if (Helper.ModRegistry.IsLoaded("spacechase0.GenericModConfigMenu") && apiManager.HookIntoGenericModConfigMenu(Helper))
            {
                Framework.Interfaces.IGenericModConfigMenuApi configApi = apiManager.GetGenericModConfigMenuApi();
                configApi.Register(ModManifest, () => modConfig = new ModConfig(), () => Helper.WriteConfig(modConfig));

                // Register the standard settings
                configApi.AddSectionTitle(ModManifest, () => Helper.Translation.Get("gmcm.random_textures"));
                configApi.AddBoolOption(ModManifest, () => modConfig.UseRandomTexturesWhenSpawningArtifactSpots, value => modConfig.UseRandomTexturesWhenSpawningArtifactSpots = value, () => Helper.Translation.Get("gmcm.artifact_spots"));
                configApi.AddBoolOption(ModManifest, () => modConfig.UseRandomTexturesWhenPlacingFlooring, value => modConfig.UseRandomTexturesWhenPlacingFlooring = value, () => Helper.Translation.Get("gmcm.flooring"));
                configApi.AddBoolOption(ModManifest, () => modConfig.UseRandomTexturesWhenPlacingFruitTree, value => modConfig.UseRandomTexturesWhenPlacingFruitTree = value, () => Helper.Translation.Get("gmcm.fruit_trees"));
                configApi.AddBoolOption(ModManifest, () => modConfig.UseRandomTexturesWhenPlacingTree, value => modConfig.UseRandomTexturesWhenPlacingTree = value, () => Helper.Translation.Get("gmcm.trees"));
                configApi.AddBoolOption(ModManifest, () => modConfig.UseRandomTexturesWhenPlacingHoeDirt, value => modConfig.UseRandomTexturesWhenPlacingHoeDirt = value, () => Helper.Translation.Get("gmcm.hoe_dirt"));
                configApi.AddBoolOption(ModManifest, () => modConfig.UseRandomTexturesWhenPlacingGrass, value => modConfig.UseRandomTexturesWhenPlacingGrass = value, () => Helper.Translation.Get("gmcm.grass"));
                configApi.AddBoolOption(ModManifest, () => modConfig.UseRandomTexturesWhenPlacingFurniture, value => modConfig.UseRandomTexturesWhenPlacingFurniture = value, () => Helper.Translation.Get("gmcm.furniture"));
                configApi.AddBoolOption(ModManifest, () => modConfig.UseRandomTexturesWhenPlacingObject, value => modConfig.UseRandomTexturesWhenPlacingObject = value, () => Helper.Translation.Get("gmcm.objects"));
                configApi.AddBoolOption(ModManifest, () => modConfig.UseRandomTexturesWhenPlacingFarmAnimal, value => modConfig.UseRandomTexturesWhenPlacingFarmAnimal = value, () => Helper.Translation.Get("gmcm.farm_animals"));
                configApi.AddBoolOption(ModManifest, () => modConfig.UseRandomTexturesWhenPlacingMonster, value => modConfig.UseRandomTexturesWhenPlacingMonster = value, () => Helper.Translation.Get("gmcm.monsters"));
                configApi.AddBoolOption(ModManifest, () => modConfig.UseRandomTexturesWhenPlacingBuilding, value => modConfig.UseRandomTexturesWhenPlacingBuilding = value, () => Helper.Translation.Get("gmcm.buildings"));

                IEnumerable<IContentPack> contentPacks = Helper.ContentPacks.GetOwned();
                // Create the page labels for each content pack's page
                configApi.AddSectionTitle(ModManifest, () => Helper.Translation.Get("gmcm.content_packs"));
                foreach (IContentPack contentPack in contentPacks)
                {
                    configApi.AddPageLink(ModManifest, string.Concat("> ", CleanContentPackNameForConfig(contentPack.Manifest.Name)), () => contentPack.Manifest.Description, () => contentPack.Manifest.UniqueID);
                }

                // Add the content pack owner pages
                foreach (IContentPack contentPack in contentPacks)
                {
                    configApi.AddPage(ModManifest, contentPack.Manifest.UniqueID, () => CleanContentPackNameForConfig(contentPack.Manifest.Name));

                    // Create a page label for each TextureType under this content pack
                    configApi.AddSectionTitle(ModManifest, () => Helper.Translation.Get("gmcm.categories"));
                    foreach (string textureType in textureManager.GetAllTextures().Where(t => t.Owner == contentPack.Manifest.UniqueID).Select(t => t.GetTextureType()).Distinct().OrderBy(t => t))
                    {
                        configApi.AddPageLink(ModManifest, string.Concat("> ", textureType), () => string.Empty, () => string.Concat(contentPack.Manifest.UniqueID, ".", textureType));
                    }

                    // Create a page label for each model under this content pack
                    foreach (AlternativeTextureModel model in textureManager.GetAllTextures().Where(t => t.Owner == contentPack.Manifest.UniqueID).OrderBy(t => t.GetTextureType()).ThenBy(t => t.ItemName))
                    {
                        configApi.AddPage(ModManifest, string.Concat(model.Owner, ".", model.GetTextureType()));

                        // Create page label for each model
                        string description = string.Concat(Helper.Translation.Get("gmcm.type") + model.GetTextureType() + "\n" + Helper.Translation.Get("gmcm.seasons") + (string.IsNullOrEmpty(model.Season) ? Helper.Translation.Get("gmcm.all") : model.Season) + "\n" + Helper.Translation.Get("gmcm.variations") + model.GetVariations());
                        configApi.AddPageLink(ModManifest, string.Concat("> ", model.ItemName), () => description, model.GetId);
                    }

                    // Add the AlternativeTextureModel pages
                    foreach (AlternativeTextureModel model in textureManager.GetAllTextures().Where(t => t.Owner == contentPack.Manifest.UniqueID))
                    {
                        configApi.AddPage(ModManifest, model.GetId());

                        for (int variation = 0; variation < model.GetVariations(); variation++)
                        {
                            // Add general description label
                            string description = string.Concat(Helper.Translation.Get("gmcm.type") + model.GetTextureType() + "\n" + Helper.Translation.Get("gmcm.seasons") + (string.IsNullOrEmpty(model.Season) ? Helper.Translation.Get("gmcm.all") : model.Season));
                            configApi.AddSectionTitle(ModManifest, () => Helper.Translation.Get("gmcm.variations"), () => description);

                            // Add the reference image for the alternative texture
                            var sourceRect = new Rectangle(0, model.GetTextureOffset(variation), model.TextureWidth, model.TextureHeight);
                            switch (model.GetTextureType())
                            {
                                case "Decoration":
                                    bool isFloor = model.ItemName.Equals("Floor", StringComparison.OrdinalIgnoreCase);
                                    int decorationOffset = isFloor ? 8 : 16;
                                    sourceRect = new Rectangle(variation % decorationOffset * model.TextureWidth, variation / decorationOffset * model.TextureHeight, model.TextureWidth, model.TextureHeight);
                                    break;
                            }

                            int scale = 4;
                            if (model.TextureHeight >= 64)
                            {
                                scale = 2;
                            }
                            if (model.TextureHeight >= 128)
                            {
                                scale = 1;
                            }
                            configApi.AddImage(ModManifest, () => Game1.content.Load<Texture2D>("{AlternativeTextures.TEXTURE_TOKEN_HEADER}{model.GetTokenId(variation)}"), sourceRect, scale);

                            // Add our custom widget, which passes over the required data needed to flag the TextureId with the appropriate Variation 
                            bool wasClicking = false;
                            TextureWidget textureWidget = new() { TextureId = model.GetId(), Variation = variation, Enabled = !modConfig.IsTextureVariationDisabled(model.GetId(), variation) };
                            void widgetUpdate()
                            {
                                TextureWidget widget = textureWidget;

                                Rectangle bounds = new(OptionsCheckbox.sourceRectChecked.X, OptionsCheckbox.sourceRectChecked.Y, OptionsCheckbox.sourceRectChecked.Width * 4, OptionsCheckbox.sourceRectChecked.Width * 4);
                                bool isHovering = bounds.Contains(Game1.getOldMouseX(), Game1.getOldMouseY());

                                bool isClicking = Game1.input.GetMouseState().LeftButton == ButtonState.Pressed;
                                if (isHovering && isClicking && !wasClicking)
                                {
                                    widget.Enabled = !widget.Enabled;
                                }
                                wasClicking = isClicking;
                            };
                            void widgetDraw(SpriteBatch b, Vector2 pos)
                            {
                                TextureWidget widget = textureWidget;
                                b.Draw(Game1.mouseCursors, pos, widget.Enabled ? OptionsCheckbox.sourceRectChecked : OptionsCheckbox.sourceRectUnchecked, Color.White, 0, Vector2.Zero, 4, SpriteEffects.None, 0);
                            };
                            void widgetSave()
                            {
                                TextureWidget widget = textureWidget;

                                modConfig.SetTextureStatus(widget.TextureId, widget.Variation, widget.Enabled);
                            };
                            configApi.AddSectionTitle(ModManifest, () => string.Empty, () => string.Empty);
                            configApi.AddComplexOption(ModManifest, () => Helper.Translation.Get("gmcm.enabled"), widgetDraw, () => Helper.Translation.Get("gmcm.show_alt_texture"), widgetUpdate, widgetSave);

                            configApi.AddSectionTitle(ModManifest, () => string.Empty, () => string.Empty);
                        }
                    }
                }
            }
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            // Backwards compatibility logic
            if (!Game1.player.modData.ContainsKey(TOOL_CONVERSION_COMPATIBILITY))
            {
                Monitor.Log($"Converting old Paint Buckets into generic tools...", LogLevel.Debug);
                Game1.player.modData[TOOL_CONVERSION_COMPATIBILITY] = true.ToString();
                ConvertPaintBucketsToGenericTools(Game1.player);
            }
            if (!Game1.player.modData.ContainsKey(TYPE_FIX_COMPATIBILITY))
            {
                Monitor.Log($"Fixing bad object and bigcraftable typings...", LogLevel.Debug);
                Game1.player.modData[TYPE_FIX_COMPATIBILITY] = true.ToString();
                FixBadObjectTyping();
            }
        }

        private void LoadContentPacks()
        {
            Stopwatch collectiveLoadingStopwatch = Stopwatch.StartNew();

            // Load owned content packs
            foreach (IContentPack contentPack in Helper.ContentPacks.GetOwned())
            {
                Monitor.Log($"Loading textures from pack: {contentPack.Manifest.Name} {contentPack.Manifest.Version} by {contentPack.Manifest.Author}", LogLevel.Debug);

                Stopwatch individualLoadingStopwatch = Stopwatch.StartNew();
                try
                {
                    var textureFolders = new DirectoryInfo(Path.Combine(contentPack.DirectoryPath, "Textures")).GetDirectories("*", SearchOption.AllDirectories);
                    if (textureFolders.Count() == 0)
                    {
                        Monitor.Log($"No sub-folders found under Textures for the content pack {contentPack.Manifest.Name}!", LogLevel.Warn);
                        continue;
                    }

                    // Load in the alternative textures
                    foreach (var textureFolder in textureFolders)
                    {
                        if (!File.Exists(Path.Combine(textureFolder.FullName, "texture.json")))
                        {
                            if (textureFolder.GetDirectories().Count() == 0)
                            {
                                Monitor.Log($"Content pack {contentPack.Manifest.Name} is missing a texture.json under {textureFolder.Name}!", LogLevel.Warn);
                            }

                            continue;
                        }

                        var parentFolderName = textureFolder.Parent.FullName.Replace(contentPack.DirectoryPath + Path.DirectorySeparatorChar, String.Empty);
                        var modelPath = Path.Combine(parentFolderName, textureFolder.Name, "texture.json");

                        var baseModel = contentPack.ReadJsonFile<AlternativeTextureModel>(modelPath);
                        baseModel.Owner = contentPack.Manifest.UniqueID;
                        baseModel.PackName = contentPack.Manifest.Name;
                        baseModel.Author = contentPack.Manifest.Author;

                        // Add to ItemId to CollectiveIds if ItemName is given or add to ItemName to CollectiveNames if ItemName is given
                        if (String.IsNullOrEmpty(baseModel.ItemId) is false)
                        {
                            baseModel.CollectiveIds.Add(baseModel.ItemId);
                        }
                        else if (String.IsNullOrEmpty(baseModel.ItemName) is false)
                        {
                            baseModel.CollectiveNames.Add(baseModel.ItemName);
                        }

                        // Handle SDV and framework related changes
                        string originalItemName = baseModel.ItemName;
                        if (baseModel.HandleNameChanges() is List<string> changedNames && changedNames.Count > 0)
                        {
                            foreach (var changedName in changedNames)
                            {
                                Monitor.Log($"The texture {baseModel.ItemName} from {contentPack.Manifest.Name} has an outdated ItemName that was handled automatically: {originalItemName} -> {changedName}", LogLevel.Trace);
                            }
                        }

                        var originalType = baseModel.Type;
                        if (baseModel.HandleTypeChanges())
                        {
                            Monitor.Log($"The texture {baseModel.ItemName} from {contentPack.Manifest.Name} has an outdated Type that was handled automatically: {originalType} -> {baseModel.Type}", LogLevel.Trace);
                        }

                        // Combine the two collective lists
                        var collectedCollective = new List<dynamic>();
                        foreach (string itemName in baseModel.CollectiveNames)
                        {
                            collectedCollective.Add(new { Name = itemName, IsId = false });
                        }
                        foreach (string itemId in baseModel.CollectiveIds)
                        {
                            collectedCollective.Add(new { Name = itemId, IsId = true });
                        }

                        // Attempt to add an instance of each season
                        var seasons = baseModel.Seasons;
                        for (int s = 0; s < 4; s++)
                        {
                            if ((seasons.Count() == 0 && s > 0) || (seasons.Count() > 0 && s >= seasons.Count()))
                            {
                                continue;
                            }

                            // Attempt to add each instance under CollectiveNames
                            foreach (var textureData in collectedCollective)
                            {
                                // Parse the model and assign it the content pack's owner
                                AlternativeTextureModel textureModel = baseModel.ShallowCopy();

                                // Set the ItemName or ItemId depending on IsId flag
                                if (textureData.IsId is true)
                                {
                                    textureModel.ItemId = textureData.Name;
                                }
                                else
                                {
                                    // Override Grass Alternative Texture pack ItemName to always be Grass, in order to be compatible with translations 
                                    textureModel.ItemName = textureModel.Type.ToString() == "Grass" ? "Grass" : textureData.Name;
                                }

                                // Verify that ItemName or ItemNames is given
                                if (collectedCollective.Count() == 0)
                                {
                                    Monitor.Log($"Unable to add alternative texture for {textureModel.Owner}: Missing the ItemName, ItemId, CollectiveNames or CollectiveIds property! See the log for additional details.", LogLevel.Warn);
                                    Monitor.Log($"Unable to add alternative texture for {textureModel.Owner}: Missing the ItemName, ItemId, CollectiveNames or CollectiveIds property found in the following path: {textureFolder.FullName}", LogLevel.Trace);
                                    continue;
                                }

                                // Add the UniqueId to the top-level Keywords
                                textureModel.Keywords.Add(contentPack.Manifest.UniqueID);

                                // Add the top-level Keywords to any ManualVariations.Keywords
                                foreach (var variation in textureModel.ManualVariations)
                                {
                                    variation.Keywords.AddRange(textureModel.Keywords);
                                }

                                // Set the season (if any)
                                textureModel.Season = seasons.Count() == 0 ? String.Empty : seasons[s];

                                // Set the ModelName and TextureId
                                textureModel.ModelName = String.IsNullOrEmpty(textureModel.Season) ? String.Concat(textureModel.GetTextureType(), "_", textureModel.ItemName) : String.Concat(textureModel.GetTextureType(), "_", textureModel.ItemName, "_", textureModel.Season);
                                textureModel.TextureId = String.Concat(textureModel.Owner, ".", textureModel.ModelName);

                                // Verify we are given a texture and if so, track it
                                if (!File.Exists(Path.Combine(textureFolder.FullName, "texture.png")))
                                {
                                    // No texture.png found, may be using split texture files (texture_1.png, texture_2.png, etc.)
                                    var textureFilePaths = Directory.GetFiles(textureFolder.FullName, "texture_*.png")
                                        .Select(t => Path.GetFileName(t))
                                        .Where(t => t.Any(char.IsDigit))
                                        .OrderBy(t => Int32.Parse(Regex.Match(t, @"\d+").Value));

                                    if (textureFilePaths.Count() == 0)
                                    {
                                        Monitor.Log($"Unable to add alternative texture for item {textureModel.ItemName} from {contentPack.Manifest.Name}: No associated texture.png or split textures (texture_1.png, texture_2.png, etc.) given. See the log for additional details.", LogLevel.Warn);
                                        Monitor.Log($"Unable to add alternative texture for item {textureModel.ItemName} from {contentPack.Manifest.Name}: No associated texture.png or split textures (texture_1.png, texture_2.png, etc.) found in the following path: {textureFolder.FullName}", LogLevel.Trace);
                                        continue;
                                    }
                                    else if (textureModel.IsDecoration())
                                    {
                                        Monitor.Log($"Unable to add alternative texture for item {textureModel.ItemName} from {contentPack.Manifest.Name}: Split textures (texture_1.png, texture_2.png, etc.) are not allowed for Decoration types (wallpapers / floors). See the log for additional details.", LogLevel.Warn);
                                        Monitor.Log($"Unable to add alternative texture for item {textureModel.ItemName} from {contentPack.Manifest.Name}: Split textures (texture_1.png, texture_2.png, etc.) are not allowed for Decoration types (wallpapers / floors). Located in the following path: {textureFolder.FullName}", LogLevel.Trace);
                                        continue;
                                    }

                                    if (textureModel.GetVariations() < textureFilePaths.Count())
                                    {
                                        Monitor.Log($"Warning for alternative texture for item {textureModel.ItemName} from {contentPack.Manifest.Name}: There are less variations specified in texture.json than split textures files. See the log for additional details.", LogLevel.Warn);
                                        Monitor.Log($"Warning for alternative texture for item {textureModel.ItemName} from {contentPack.Manifest.Name}: There are less variations specified in texture.json than split textures files found in the following path: {textureFolder.FullName}", LogLevel.Trace);
                                    }
                                    else if (textureModel.IsManualVariationsValid() is false)
                                    {
                                        Monitor.Log($"Unable to add alternative texture for item {textureModel.ItemName} from {contentPack.Manifest.Name}: ManualVariations is used but does not start with ID == 0 (the propery should be zero-indexed). See the log for additional details.", LogLevel.Warn);
                                        Monitor.Log($"Unable to add alternative texture for item {textureModel.ItemName} from {contentPack.Manifest.Name}: ManualVariations is used but does not start with ID == 0 (the propery should be zero-indexed). Adjust the ID order so that it starts with ID = 0. Located in the following path: {textureFolder.FullName}", LogLevel.Trace);
                                        continue;
                                    }

                                    // Load in the first texture_#.png to get its dimensions for creating stitchedTexture
                                    if (!StitchTexturesToModel(textureModel, contentPack, Path.Combine(parentFolderName, textureFolder.Name), textureFilePaths.Take(textureModel.GetVariations())))
                                    {
                                        continue;
                                    }

                                    textureModel.TileSheetPath = contentPack.ModContent.GetInternalAssetName(Path.Combine(parentFolderName, textureFolder.Name, textureFilePaths.First())).Name;
                                }
                                else
                                {
                                    // Load in the single vertical texture
                                    textureModel.TileSheetPath = contentPack.ModContent.GetInternalAssetName(Path.Combine(parentFolderName, textureFolder.Name, "texture.png")).Name;
                                    Texture2D singularTexture = contentPack.ModContent.Load<Texture2D>(textureModel.TileSheetPath);
                                    if (singularTexture.Height >= AlternativeTextureModel.MAX_TEXTURE_HEIGHT)
                                    {
                                        Monitor.Log($"Unable to add alternative texture for {textureModel.Owner}: The texture {textureModel.TextureId} has a height larger than 16384!\nPlease split it into individual textures (e.g. texture_0.png, texture_1.png, etc.) to resolve this issue. See the log for additional details.", LogLevel.Warn);
                                        Monitor.Log($"Unable to add alternative texture for {textureModel.Owner}: The texture {textureModel.TextureId} has a height larger than 16384!\nPlease split it into individual textures (e.g. texture_0.png, texture_1.png, etc.) to resolve this issue. Located in the following path: {textureFolder.FullName}", LogLevel.Trace);
                                        continue;
                                    }
                                    else if (textureModel.IsDecoration())
                                    {
                                        if (singularTexture.Width < 256)
                                        {
                                            Monitor.Log($"Unable to add alternative texture for {textureModel.ItemName} from {contentPack.Manifest.Name}: The required image width is 256 for Decoration types (wallpapers / floors). Please correct the image's width manually. See the log for additional details.", LogLevel.Warn);
                                            Monitor.Log($"Unable to add alternative texture for {textureModel.ItemName} from {contentPack.Manifest.Name}: The required image width is 256 for Decoration types (wallpapers / floors). Please correct the image's width manually at the following path: {textureFolder.FullName}", LogLevel.Trace);
                                            continue;
                                        }

                                        textureModel.Textures[0] = singularTexture;
                                    }
                                    else if (!SplitVerticalTexturesToModel(textureModel, contentPack.Manifest.Name, singularTexture))
                                    {
                                        continue;
                                    }
                                }

                                // Track the texture model
                                textureManager.AddAlternativeTexture(textureModel);

                                // Log it
                                if (modConfig.OutputTextureDataToLog)
                                {
                                    Monitor.Log(textureModel.ToString(), LogLevel.Trace);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Monitor.Log($"Error loading content pack {contentPack.Manifest.Name}: {ex}", LogLevel.Error);
                }

                individualLoadingStopwatch.Stop();
                monitor.Log($"[{contentPack.Manifest.Name}] finished loading in {Math.Round(individualLoadingStopwatch.ElapsedMilliseconds / 1000f, 2)} seconds", LogLevel.Trace);
            }

            // Clear the wallpaper / flooring cache
            Helper.GameContent.InvalidateCache("Data/AdditionalWallpaperFlooring");

            collectiveLoadingStopwatch.Stop();
            monitor.Log($"Finished loading all content packs in {Math.Round(collectiveLoadingStopwatch.ElapsedMilliseconds / 1000f, 2)} seconds", LogLevel.Trace);
        }

        internal bool SplitVerticalTexturesToModel(AlternativeTextureModel textureModel, string contentPackName, Texture2D verticalTexture)
        {
            try
            {
                for (int v = 0; v < textureModel.GetVariations(); v++)
                {
                    var extractRectangle = new Rectangle(0, textureModel.TextureHeight * v, verticalTexture.Width, textureModel.TextureHeight);
                    Color[] extractPixels = new Color[extractRectangle.Width * extractRectangle.Height];

                    if (verticalTexture.Bounds.Contains(extractRectangle) is false)
                    {
                        int maxVariationsPossible = verticalTexture.Height / textureModel.TextureHeight;

                        Monitor.Log($"Unable to add alternative texture for item {textureModel.ItemName} from {contentPackName}: More variations specified ({textureModel.GetVariations()}) than given ({maxVariationsPossible})", LogLevel.Warn);
                        return false;
                    }

                    // Get the required pixels
                    verticalTexture.GetData(0, extractRectangle, extractPixels, 0, extractPixels.Length);

                    // Set the required pixels
                    var extractedTexture = new Texture2D(Game1.graphics.GraphicsDevice, extractRectangle.Width, extractRectangle.Height);
                    extractedTexture.SetData(extractPixels);

                    textureModel.Textures[v] = (extractedTexture);
                }
            }
            catch (Exception exception)
            {
                Monitor.Log($"Unable to add alternative texture for item {textureModel.ItemName} from {contentPackName}: Unhandled framework error: {exception}", LogLevel.Warn);
                return false;
            }

            return true;
        }

        private bool StitchTexturesToModel(AlternativeTextureModel textureModel, IContentPack contentPack, string rootPath, IEnumerable<string> textureFilePaths)
        {
            Texture2D baseTexture = contentPack.ModContent.Load<Texture2D>(Path.Combine(rootPath, textureFilePaths.First()));

            // If there is only one split texture file, skip the rest of the logic to avoid issues
            if (textureFilePaths.Count() == 1 || textureModel.GetVariations() == 1)
            {
                if (textureModel.GetVariations() == 1 && textureFilePaths.Count() > 1)
                {
                    Monitor.Log($"Detected more split textures ({textureFilePaths.Count()}) than specified variations ({textureModel.GetVariations()}) for {textureModel.TextureId} from {contentPack.Manifest.Name}", LogLevel.Warn);
                }

                textureModel.Textures[0] = baseTexture;
                return true;
            }

            try
            {
                int variation = 0;
                foreach (var textureFilePath in textureFilePaths)
                {
                    var splitTexture = contentPack.ModContent.Load<Texture2D>(Path.Combine(rootPath, textureFilePath));
                    textureModel.Textures[variation] = splitTexture;

                    variation++;
                }
            }
            catch (Exception exception)
            {
                Monitor.Log($"Unable to add alternative texture for item {textureModel.ItemName} from {contentPack.Manifest.Name}: Unhandled framework error: {exception}", LogLevel.Warn);
                return false;
            }

            return true;
        }

        private void DebugSpawnMonsters(string command, string[] args)
        {
            if (args.Length == 0)
            {
                Monitor.Log($"Missing required arguments: [MONSTER_ID]", LogLevel.Warn);
                return;
            }

            int amountToSpawn = 1;
            if (args.Length > 1 && Int32.TryParse(args[1], out amountToSpawn) is false)
            {
                Monitor.Log($"Invalid count given for (QUANTITY)", LogLevel.Warn);
                return;
            }
            Type monsterType = Type.GetType("StardewValley.Monsters." + args[0] + ",Stardew Valley");

            Monitor.Log(Game1.player.Tile.ToString(), LogLevel.Debug);
            for (int i = 0; i < amountToSpawn; i++)
            {
                var monster = Activator.CreateInstance(monsterType, new object[] { Game1.player.Tile }) as Monster;
                monster.Position = Game1.player.Position;
                Game1.currentLocation.characters.Add(monster);
            }
        }

        private void DebugSpawnGiantCrop(string command, string[] args)
        {
            if (args.Length == 0)
            {
                Monitor.Log($"Missing required arguments: [HARVEST_ID]", LogLevel.Warn);
                return;
            }

            if (!(Game1.currentLocation.GetData()?.CanPlantHere ?? Game1.currentLocation.IsFarm) || (Game1.currentLocation is not Farm && !Game1.currentLocation.HasMapPropertyWithValue("AllowGiantCrops")))
            {
                Monitor.Log($"Command can only be used on a plantable location allowing giant crops.", LogLevel.Warn);
                return;
            }

            GameLocation gameLocation = Game1.currentLocation;

            foreach (var tile in gameLocation.terrainFeatures.Pairs.Where(t => t.Value is HoeDirt))
            {
                Crop crop = (tile.Value as HoeDirt).crop;

                if (crop is null || crop.indexOfHarvest.Value != args[0])
                {
                    continue;
                }

                if (crop.TryGetGiantCrops(out var giantCrops))
                {
                    Vector2 vector = crop.tilePosition;
                    Point point = Utility.Vector2ToPoint(vector);

                    foreach (KeyValuePair<string, GiantCropData> item in giantCrops)
                    {
                        string key = item.Key;
                        GiantCropData value = item.Value;
                        bool flag = true;

                        for (int i = point.Y; i < point.Y + value.TileSize.Y; i++)
                        {
                            for (int j = point.X; j < point.X + value.TileSize.X; j++)
                            {
                                Vector2 key2 = new(j, i);

                                if (!gameLocation.terrainFeatures.TryGetValue(key2, out TerrainFeature terrainFeature) || terrainFeature is not HoeDirt hoeDirt2 || hoeDirt2.crop?.indexOfHarvest.Value != crop.indexOfHarvest.Value)
                                {
                                    flag = false;
                                    break;
                                }
                            }
                            if (!flag)
                            {
                                break;
                            }
                        }
                        if (!flag)
                        {
                            continue;
                        }
                        for (int k = point.Y; k < point.Y + value.TileSize.Y; k++)
                        {
                            for (int l = point.X; l < point.X + value.TileSize.X; l++)
                            {
                                Vector2 key3 = new(l, k);

                                ((HoeDirt)gameLocation.terrainFeatures[key3]).crop = null;
                            }
                        }
                        gameLocation.resourceClumps.Add(new GiantCrop(key, vector));
                        break;
                    }
                }
            }
        }

        private void DebugSpawnResourceClump(string command, string[] args)
        {
            if (args.Length == 0)
            {
                Monitor.Log($"Missing required arguments: [RESOURCE_NAME]", LogLevel.Warn);
                return;
            }

            if (!Game1.currentLocation.IsOutdoors)
            {
                Monitor.Log($"Command can only be used outdoors.", LogLevel.Warn);
                return;
            }

            if (args[0].ToLower() != "stump")
            {
                Monitor.Log($"That resource isn't supported.", LogLevel.Warn);
                return;
            }

            Game1.currentLocation.resourceClumps.Add(new ResourceClump(600, 2, 2, Game1.player.Tile + new Vector2(1, 1)));
        }

        private void DebugSpawnChild(string command, string[] args)
        {
            if (args.Length < 2)
            {
                Monitor.Log($"Missing required arguments: [AGE] [IS_MALE] [SKIN_TONE]", LogLevel.Warn);
                return;
            }

            var age = -1;
            if (!int.TryParse(args[0], out age) || age < 0)
            {
                Monitor.Log($"Invalid number given: {args[0]}", LogLevel.Warn);
                return;
            }

            var isMale = false;
            if (args[1].ToLower() == "true")
            {
                isMale = true;
            }

            var hasDarkSkin = false;
            if (args[2].ToLower() == "dark")
            {
                hasDarkSkin = true;
            }

            var child = new Child("Test", isMale, hasDarkSkin, Game1.player);
            child.Position = Game1.player.Position;
            child.Age = age;
            Game1.currentLocation.characters.Add(child);
        }

        private void DebugSetAge(string command, string[] args)
        {
            if (args.Length == 0)
            {
                Monitor.Log($"Missing required arguments: [AGE]", LogLevel.Warn);
                return;
            }

            var age = -1;
            if (!int.TryParse(args[0], out age))
            {
                Monitor.Log($"Invalid number given: {args[0]}", LogLevel.Warn);
                return;
            }

            foreach (var child in Game1.currentLocation.characters.Where(c => c is Child))
            {
                child.Age = 3;
            }
        }

        private void DebugShowPaintShop(string command, string[] args)
        {
            var items = new Dictionary<ISalable, ItemStockInformation>()
            {
                { PatchTemplate.GetPaintBucketTool(), new ItemStockInformation(500, 1) },
                { PatchTemplate.GetScissorsTool(), new ItemStockInformation(500, 1) },
                { PatchTemplate.GetPaintBrushTool(), new ItemStockInformation(500, 1) },
                { PatchTemplate.GetSprayCanTool(true), new ItemStockInformation(500, 1) },
                { PatchTemplate.GetCatalogueTool(), new ItemStockInformation(500, 1) }
            };
            Game1.activeClickableMenu = new ShopMenu("Alternative Textures Debug", items);
        }

        private void DebugSetTexture(string command, string[] args)
        {
            if (args.Length == 0)
            {
                Monitor.Log($"Missing required arguments: [TEXTURE_ID]", LogLevel.Warn);
                return;
            }

            string season = null;
            if (args.Length > 1)
            {
                season = args[1];
            }

            int variation = 0;
            if (args.Length > 2 && Int32.TryParse(args[2], out int parsedVariation))
            {
                variation = parsedVariation;
            }

            var objectBelowPlayer = PatchTemplate.GetObjectAt(Game1.currentLocation, (int)(Game1.player.Tile.X * 64), (int)(Game1.player.Tile.Y + 1) * 64);
            if (objectBelowPlayer is null)
            {
                Monitor.Log($"No object detected below the player!", LogLevel.Warn);
                return;
            }
            monitor.Log($"Attempting to change texture of {objectBelowPlayer.Name} to {args[0]}", LogLevel.Debug);

            _api.SetTextureForObject(objectBelowPlayer, args[0], season, variation);
        }

        private void DebugClearTexture(string command, string[] args)
        {
            var objectBelowPlayer = PatchTemplate.GetObjectAt(Game1.currentLocation, (int)(Game1.player.Tile.X * 64), (int)(Game1.player.Tile.Y + 1) * 64);
            if (objectBelowPlayer is null)
            {
                Monitor.Log($"No object detected below the player!", LogLevel.Warn);
                return;
            }
            monitor.Log($"Clearing the texture of {objectBelowPlayer.Name}", LogLevel.Debug);

            _api.ClearTextureForObject(objectBelowPlayer);
        }

        private string CleanContentPackNameForConfig(string contentPackName)
        {
            return contentPackName.Replace("[", String.Empty).Replace("]", String.Empty);
        }

        private void ConvertPaintBucketsToGenericTools(Farmer who)
        {
            // Check player's inventory first
            for (int i = 0; i < who.MaxItems; i++)
            {
                if (who.Items[i] is MilkPail milkPail && milkPail.modData.ContainsKey(OLD_PAINT_BUCKET_FLAG))
                {
                    who.Items[i] = PatchTemplate.GetPaintBucketTool();
                }
            }

            foreach (var location in Game1.locations)
            {
                ConvertStoredPaintBucketsToGenericTools(who, location);

                if (location.buildings is not null)
                {
                    foreach (var building in location.buildings)
                    {
                        GameLocation indoorLocation = building.indoors.Value;
                        if (indoorLocation is null)
                        {
                            continue;
                        }

                        ConvertStoredPaintBucketsToGenericTools(who, indoorLocation);
                    }
                }
            }
        }

        private void ConvertStoredPaintBucketsToGenericTools(Farmer who, GameLocation location)
        {
            foreach (var chest in location.Objects.Pairs.Where(p => p.Value is Chest).Select(p => p.Value as Chest).ToList())
            {
                if (chest.isEmpty())
                {
                    continue;
                }

                if (chest.SpecialChestType == Chest.SpecialChestTypes.JunimoChest)
                {
                    var actual_items = chest.GetItemsForPlayer(who.UniqueMultiplayerID);
                    for (int j = actual_items.Count - 1; j >= 0; j--)
                    {
                        if (actual_items[j] is MilkPail milkPail && milkPail.modData.ContainsKey(OLD_PAINT_BUCKET_FLAG))
                        {
                            actual_items[j] = PatchTemplate.GetPaintBucketTool();
                        }
                    }
                }
                else
                {
                    for (int i = chest.Items.Count - 1; i >= 0; i--)
                    {
                        if (chest.Items[i] is MilkPail milkPail && milkPail.modData.ContainsKey(OLD_PAINT_BUCKET_FLAG))
                        {
                            chest.Items[i] = PatchTemplate.GetPaintBucketTool();
                        }
                    }
                }
            }
        }

        private void FixBadObjectTyping()
        {
            foreach (var location in Game1.locations)
            {
                ConvertBadTypedObjectToNormalType(location);

                if (location.buildings is not null)
                {
                    foreach (var building in location.buildings)
                    {
                        GameLocation indoorLocation = building.indoors.Value;
                        if (indoorLocation is null)
                        {
                            continue;
                        }

                        ConvertBadTypedObjectToNormalType(indoorLocation);
                    }
                }
            }
        }

        private void ConvertBadTypedObjectToNormalType(GameLocation location)
        {
            foreach (var obj in location.objects.Values.Where(o => o.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME)))
            {
                if (obj.Type == "Craftable" || obj.Type == "Unknown")
                {
                    if (obj.bigCraftable.Value && Game1.bigCraftableData.TryGetValue(obj.ItemId, out var bigObjectInfo))
                    {
                        obj.Type = "Craftable";
                    }
                    else if (!obj.bigCraftable.Value && Game1.objectData.TryGetValue(obj.ItemId, out var objectInfo))
                    {
                        obj.Type = objectInfo.Type;
                        obj.Category = objectInfo.Category;
                    }
                }
            }
        }
    }
}
