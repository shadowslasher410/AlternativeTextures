﻿using AlternativeTextures.Framework.Models;
using AlternativeTextures.Framework.UI;
using AlternativeTextures.Framework.Utilities;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.IO;
using static AlternativeTextures.Framework.Models.AlternativeTextureModel;
using Object = StardewValley.Object;

namespace AlternativeTextures.Framework.Patches.Tools
{
    internal class ToolPatch : PatchTemplate
    {
        private readonly Type _object = typeof(Tool);

        internal ToolPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        internal void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(_object, "get_DisplayName", null), postfix: new HarmonyMethod(GetType(), nameof(GetNamePostfix)));
            harmony.Patch(AccessTools.Method(_object, "get_description", null), postfix: new HarmonyMethod(GetType(), nameof(GetDescriptionPostfix)));


            harmony.Patch(AccessTools.Method(typeof(Item), nameof(Item.canBeTrashed), null), postfix: new HarmonyMethod(GetType(), nameof(CanBeTrashedPostfix)));
            harmony.Patch(AccessTools.Method(_object, nameof(Tool.drawInMenu), new[] { typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float), typeof(StackDrawType), typeof(Color), typeof(bool) }), prefix: new HarmonyMethod(GetType(), nameof(DrawInMenuPrefix)));
            harmony.Patch(AccessTools.Method(_object, nameof(Tool.beginUsing), new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(Farmer) }), prefix: new HarmonyMethod(GetType(), nameof(BeginUsingPrefix)));
        }

        private static void GetNamePostfix(Tool __instance, ref string __result)
        {
            if (__instance.modData.ContainsKey(AlternativeTextures.PAINT_BUCKET_FLAG))
            {
                __result = _helper.Translation.Get("tools.name.paint_bucket");
                return;
            }

            if (__instance.modData.ContainsKey(AlternativeTextures.SCISSORS_FLAG))
            {
                __result = _helper.Translation.Get("tools.name.scissors");
                return;
            }

            if (__instance.modData.ContainsKey(AlternativeTextures.PAINT_BRUSH_FLAG))
            {
                __result = _helper.Translation.Get("tools.name.paint_brush");
                return;
            }

            if (__instance.modData.ContainsKey(AlternativeTextures.SPRAY_CAN_FLAG))
            {
                __result = _helper.Translation.Get("tools.name.spray_can");
                return;
            }

            if (__instance.modData.ContainsKey(AlternativeTextures.CATALOGUE_FLAG))
            {
                __result = _helper.Translation.Get("tools.name.catalogue");
                return;
            }
        }

        private static void GetDescriptionPostfix(Tool __instance, ref string __result)
        {
            if (__instance.modData.ContainsKey(AlternativeTextures.PAINT_BUCKET_FLAG))
            {
                __result = _helper.Translation.Get("tools.description.paint_bucket");
                return;
            }

            if (__instance.modData.ContainsKey(AlternativeTextures.SCISSORS_FLAG))
            {
                __result = _helper.Translation.Get("tools.description.scissors");
                return;
            }

            if (__instance.modData.ContainsKey(AlternativeTextures.PAINT_BRUSH_FLAG))
            {
                __result = _helper.Translation.Get("tools.description.paint_brush");
                return;
            }

            if (__instance.modData.ContainsKey(AlternativeTextures.SPRAY_CAN_FLAG))
            {
                __result = _helper.Translation.Get("tools.description.spray_can");
                return;
            }

            if (__instance.modData.ContainsKey(AlternativeTextures.CATALOGUE_FLAG))
            {
                __result = _helper.Translation.Get("tools.description.catalogue");
                return;
            }
        }

        [HarmonyPriority(Priority.Last)]
        private static void CanBeTrashedPostfix(Item __instance, ref bool __result)
        {
            if (IsAlternativeTextureTool(__instance))
            {
                __result = true;
            }
        }

        private static bool DrawInMenuPrefix(Tool __instance, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
        {
            if (__instance.modData.ContainsKey(AlternativeTextures.PAINT_BUCKET_FLAG))
            {
                spriteBatch.Draw(AlternativeTextures.assetManager.GetPaintBucketTexture(), location + new Vector2(32f, 32f), new Rectangle(0, 0, 16, 16), color * transparency, 0f, new Vector2(8f, 8f), 4f * scaleSize, SpriteEffects.None, layerDepth);

                return false;
            }

            if (__instance.modData.ContainsKey(AlternativeTextures.SCISSORS_FLAG))
            {
                spriteBatch.Draw(AlternativeTextures.assetManager.GetScissorsTexture(), location + new Vector2(32f, 32f), new Rectangle(0, 0, 16, 16), color * transparency, 0f, new Vector2(8f, 8f), 4f * scaleSize, SpriteEffects.None, layerDepth);

                return false;
            }

            if (__instance.modData.ContainsKey(AlternativeTextures.SPRAY_CAN_FLAG))
            {
                spriteBatch.Draw(AlternativeTextures.assetManager.GetSprayCanTexture(__instance.modData.ContainsKey(AlternativeTextures.SPRAY_CAN_RARE)), location + new Vector2(32f, 32f), new Rectangle(0, 0, 16, 16), color * transparency, 0f, new Vector2(8f, 8f), 4f * scaleSize, SpriteEffects.None, layerDepth);

                return false;
            }

            if (__instance.modData.ContainsKey(AlternativeTextures.PAINT_BRUSH_FLAG))
            {
                var scale = __instance.modData.ContainsKey(AlternativeTextures.PAINT_BRUSH_SCALE) ? float.Parse(__instance.modData[AlternativeTextures.PAINT_BRUSH_SCALE]) : 0f;
                var texture = AlternativeTextures.assetManager.GetPaintBrushEmptyTexture();
                if (!String.IsNullOrEmpty(__instance.modData[AlternativeTextures.PAINT_BRUSH_FLAG]))
                {
                    texture = AlternativeTextures.assetManager.GetPaintBrushFilledTexture();
                }
                spriteBatch.Draw(texture, location + new Vector2(32f, 32f), new Rectangle(0, 0, 16, 16), color * transparency, 0f, new Vector2(8f, 8f), 4f * (scaleSize + scale), SpriteEffects.None, layerDepth);

                if (scale > 0f)
                {
                    __instance.modData[AlternativeTextures.PAINT_BRUSH_SCALE] = (scale -= 0.01f).ToString();
                }
                return false;
            }

            if (__instance.modData.ContainsKey(AlternativeTextures.CATALOGUE_FLAG))
            {
                spriteBatch.Draw(AlternativeTextures.assetManager.GetCatalogueTexture(), location + new Vector2(32f, 32f), new Rectangle(0, 0, 16, 16), color * transparency, 0f, new Vector2(8f, 8f), 4f * scaleSize, SpriteEffects.None, layerDepth);

                return false;
            }

            return true;
        }

        private static bool BeginUsingPrefix(Tool __instance, ref bool __result, GameLocation location, int x, int y, Farmer who)
        {
            if (who != Game1.player)
            {
                return true;
            }

            if (__instance.modData.ContainsKey(AlternativeTextures.PAINT_BUCKET_FLAG))
            {
                __result = true;
                return UsePaintBucket(location, x, y, who);
            }

            if (__instance.modData.ContainsKey(AlternativeTextures.SCISSORS_FLAG))
            {
                __result = true;
                return UseScissors(location, x, y, who);
            }

            if (__instance.modData.ContainsKey(AlternativeTextures.SPRAY_CAN_FLAG))
            {
                __result = true;
                return CancelUsing(who);
            }

            if (__instance.modData.ContainsKey(AlternativeTextures.PAINT_BRUSH_FLAG))
            {
                __result = true;
                return CancelUsing(who);
            }

            if (__instance.modData.ContainsKey(AlternativeTextures.CATALOGUE_FLAG))
            {
                __result = true;
                return CancelUsing(who);
            }

            return true;
        }

        private static IClickableMenu GetMenu(Object target, Vector2 position, TextureType textureType, string modelName, string uiName, int textureTileWidth = -1, bool isSprayCan = false)
        {
            if (isSprayCan)
            {
                return new SprayCanMenu(target, position, textureType, modelName, _helper.Translation.Get("tools.name.spray_can"), textureTileWidth: textureTileWidth);
            }

            return new PaintBucketMenu(target, position, textureType, modelName, uiName, textureTileWidth: textureTileWidth);
        }

        internal static bool UsePaintBucket(GameLocation location, int x, int y, Farmer who, bool isSprayCan = false)
        {
            if (location.IsBuildableLocation() && isSprayCan is false)
            {
                var targetedBuilding = location.getBuildingAt(new Vector2(x / 64, y / 64));
                bool isFarmerHouse = false;

                if (location is Farm farm)
                {
                    var farmerHouse = farm.GetMainFarmHouse();

                    // Check for mailbox
                    var mailboxPosition = farm.GetMainMailboxPosition();
                    if (PatchTemplate.IsPositionNearMailbox(location, mailboxPosition, x / 64, y / 64))
                    {
                        var modelType = AlternativeTextureModel.TextureType.Building;
                        if (!location.modData.ContainsKey("AlternativeTextureName.Mailbox") || !location.modData["AlternativeTextureName.Mailbox"].Contains("Mailbox"))
                        {
                            var textureModel = new AlternativeTextureModel() { Owner = AlternativeTextures.DEFAULT_OWNER, Season = Game1.GetSeasonForLocation(Game1.currentLocation).ToString() };

                            location.modData["AlternativeTextureOwner.Mailbox"] = textureModel.Owner;
                            location.modData["AlternativeTextureName.Mailbox"] = String.Concat(textureModel.Owner, ".", $"{modelType}_{"Mailbox"}_{Game1.GetSeasonForLocation(Game1.currentLocation)}");

                            if (!String.IsNullOrEmpty(textureModel.Season))
                            {
                                location.modData["AlternativeTextureSeason.Mailbox"] = Game1.GetSeasonForLocation(Game1.currentLocation).ToString();
                            }

                            location.modData["AlternativeTextureVariation.Mailbox"] = "-1";
                        }

                        bool usedSecondaryTile = string.IsNullOrEmpty(location.doesTileHaveProperty(x / 64, y / 64, "Action", "Buildings")) && location.doesTileHaveProperty(x / 64, (y + 64) / 64, "Action", "Buildings") == "Mailbox";
                        var mailboxObj = new Object("100", 1, isRecipe: false, -1)
                        {
                            TileLocation = new Vector2(x / 64, (y + (usedSecondaryTile ? 64 : 0)) / 64)
                        };

                        foreach (string key in location.modData.Keys)
                        {
                            mailboxObj.modData[key] = location.modData[key];
                        }

                        var modelName = mailboxObj.modData["AlternativeTextureName.Mailbox"].Replace($"{mailboxObj.modData["AlternativeTextureOwner.Mailbox"]}.", String.Empty);
                        if (mailboxObj.modData.ContainsKey("AlternativeTextureSeason.Mailbox") && !String.IsNullOrEmpty(mailboxObj.modData["AlternativeTextureSeason.Mailbox"]))
                        {
                            modelName = GetModelNameWithoutSeason(modelName, mailboxObj.modData["AlternativeTextureSeason.Mailbox"]);
                        }

                        if (AlternativeTextures.textureManager.GetAvailableTextureModels(modelName, Game1.GetSeasonForLocation(Game1.currentLocation)).Count == 0)
                        {
                            Game1.addHUDMessage(new HUDMessage(_helper.Translation.Get("messages.warning.no_textures_for_season", new { itemName = modelName }), 3));
                            return CancelUsing(who);
                        }

                        // Display texture menu
                        Game1.activeClickableMenu = new PaintBucketMenu(mailboxObj, mailboxObj.TileLocation * 64f, TextureType.Craftable, modelName, _helper.Translation.Get("tools.name.paint_bucket"), isSprayCan: false, textureOwnerKey: "AlternativeTextureOwner.Mailbox", textureNameKey: "AlternativeTextureName.Mailbox", textureVariationKey: "AlternativeTextureVariation.Mailbox", textureSeasonKey: "AlternativeTextureSeason.Mailbox", textureDisplayNameKey: "AlternativeTextureDisplayName.Mailbox");

                        return CancelUsing(who);
                    }

                    if (farmerHouse == targetedBuilding)
                    {
                        isFarmerHouse = true;

                        targetedBuilding = new Building();
                        targetedBuilding.buildingType.Value = $"Farmhouse_{Game1.MasterPlayer.HouseUpgradeLevel}";
                        targetedBuilding.tileX.Value = farmerHouse.tileX.Value;
                        targetedBuilding.tileY.Value = farmerHouse.tileY.Value;
                        targetedBuilding.tilesWide.Value = farmerHouse.tilesWide.Value;
                        targetedBuilding.tilesHigh.Value = farmerHouse.tilesHigh.Value;

                        var modelType = AlternativeTextureModel.TextureType.Building;
                        if (!farm.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME) || !farm.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME].Contains(targetedBuilding.buildingType.Value))
                        {
                            var instanceSeasonName = $"{modelType}_{targetedBuilding.buildingType.Value}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
                            AssignDefaultModData(farm, instanceSeasonName, true);
                        }

                        foreach (string key in farm.modData.Keys)
                        {
                            targetedBuilding.modData[key] = farm.modData[key];
                        }
                    }
                }

                if (targetedBuilding != null)
                {
                    // Assign default data if none exists
                    if (!targetedBuilding.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME))
                    {
                        var modelType = AlternativeTextureModel.TextureType.Building;
                        var instanceSeasonName = $"{modelType}_{targetedBuilding.buildingType.Value}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
                        AssignDefaultModData(targetedBuilding, instanceSeasonName, true);
                    }

                    var modelName = targetedBuilding.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME].Replace($"{targetedBuilding.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER]}.", String.Empty);
                    if (targetedBuilding.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_SEASON) && !String.IsNullOrEmpty(targetedBuilding.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON]))
                    {
                        modelName = GetModelNameWithoutSeason(modelName, targetedBuilding.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON]);
                    }

                    if (AlternativeTextures.textureManager.GetAvailableTextureModels(modelName, Game1.GetSeasonForLocation(Game1.currentLocation)).Count == 0)
                    {
                        if (targetedBuilding.GetData() is var data && data is not null && data.Skins is not null && data.Skins.Count > 0)
                        {
                            // Skip no texture warning
                        }
                        else
                        {
                            Game1.addHUDMessage(new HUDMessage(_helper.Translation.Get("messages.warning.no_textures_for_season", new { itemName = modelName }), 3));
                            return CancelUsing(who);
                        }
                    }

                    // Verify this building has a texture we can target
                    if (isFarmerHouse is false)
                    {
                        var texturePath = PathUtilities.NormalizePath(Path.Combine(targetedBuilding.textureName() + ".png"));
                        try
                        {
                            _ = _helper.GameContent.Load<Texture2D>(Path.Combine(targetedBuilding.textureName()));
                            _monitor.Log($"{modelName} has a targetable texture within Buildings: {texturePath}", LogLevel.Trace);
                        }
                        catch (ContentLoadException ex)
                        {
                            Game1.addHUDMessage(new HUDMessage(AlternativeTextures.modHelper.Translation.Get("messages.warning.custom_building_not_supported", new { itemName = modelName }), 3));
                            _monitor.Log($"Failed to load texture for {modelName} at the path {texturePath}: {ex}", LogLevel.Trace);
                            return CancelUsing(who);
                        }
                    }

                    // Display texture menu
                    var buildingObj = new Object(targetedBuilding.buildingType.Value, 1, isRecipe: false, -1)
                    {
                        TileLocation = new Vector2(targetedBuilding.tileX.Value, targetedBuilding.tileY.Value)
                    };
                    buildingObj.modData.SetFromSerialization(targetedBuilding.modData);

                    Game1.activeClickableMenu = GetMenu(buildingObj, buildingObj.TileLocation * 64f, GetTextureType(targetedBuilding), modelName, _helper.Translation.Get("tools.name.paint_bucket"), textureTileWidth: targetedBuilding.tilesWide.Value, isSprayCan: isSprayCan);

                    return CancelUsing(who);
                }
            }

            var targetedObject = GetObjectAt(location, x, y);
            if (targetedObject != null)
            {
                // Assign default data if none exists
                if (!targetedObject.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME))
                {
                    var instanceSeasonName = $"{GetTextureType(targetedObject)}_{GetObjectName(targetedObject)}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
                    AssignDefaultModData(targetedObject, instanceSeasonName, true);
                }

                var itemId = $"{GetTextureType(targetedObject)}_{targetedObject.ItemId}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
                var modelName = targetedObject.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME].Replace($"{targetedObject.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER]}.", String.Empty);
                if (targetedObject.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_SEASON) && !String.IsNullOrEmpty(targetedObject.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON]))
                {
                    itemId = GetModelNameWithoutSeason(itemId, targetedObject.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON]);
                    modelName = GetModelNameWithoutSeason(modelName, targetedObject.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON]);
                }

                if (AlternativeTextures.textureManager.GetAvailableTextureModels(itemId, modelName, Game1.GetSeasonForLocation(Game1.currentLocation)).Count == 0)
                {
                    var instanceSeasonName = $"{GetTextureType(targetedObject)}_{GetObjectName(targetedObject)}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
                    AssignDefaultModData(targetedObject, instanceSeasonName, true);

                    modelName = targetedObject.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME].Replace($"{targetedObject.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER]}.", String.Empty);
                    if (targetedObject.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_SEASON) && !String.IsNullOrEmpty(targetedObject.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON]))
                    {
                        itemId = GetModelNameWithoutSeason(itemId, targetedObject.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON]);
                        modelName = GetModelNameWithoutSeason(modelName, targetedObject.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON]);
                    }

                    if (AlternativeTextures.textureManager.GetAvailableTextureModels(itemId, modelName, Game1.GetSeasonForLocation(Game1.currentLocation)).Count == 0)
                    {
                        Game1.addHUDMessage(new HUDMessage(_helper.Translation.Get("messages.warning.no_textures_for_season", new { itemName = modelName }), 3));
                        return CancelUsing(who);
                    }
                }

                // Display texture menu
                Game1.activeClickableMenu = GetMenu(targetedObject, new Vector2(x, y), GetTextureType(targetedObject), modelName, _helper.Translation.Get("tools.name.paint_bucket"), isSprayCan: isSprayCan);

                return CancelUsing(who);
            }

            var targetedResouceClump = GetResourceClumpAt(location, x, y);
            if (targetedResouceClump != null && targetedResouceClump is GiantCrop giantCrop)
            {
                if (!giantCrop.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME))
                {
                    var instanceName = Game1.objectData.ContainsKey(giantCrop.Id) ? Game1.objectData[giantCrop.Id].Name : String.Empty;
                    instanceName = $"{AlternativeTextureModel.TextureType.GiantCrop}_{instanceName}";
                    var instanceSeasonName = $"{instanceName}_{Game1.GetSeasonForLocation(giantCrop.Location)}";
                    AssignDefaultModData(targetedResouceClump, instanceSeasonName, true);
                }

                var modelName = targetedResouceClump.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME].Replace($"{targetedResouceClump.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER]}.", String.Empty);
                if (targetedResouceClump.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_SEASON) && !String.IsNullOrEmpty(targetedResouceClump.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON]))
                {
                    modelName = GetModelNameWithoutSeason(modelName, targetedResouceClump.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON]);
                }

                if (AlternativeTextures.textureManager.GetAvailableTextureModels(modelName, Game1.GetSeasonForLocation(Game1.currentLocation)).Count == 0)
                {
                    Game1.addHUDMessage(new HUDMessage(_helper.Translation.Get("messages.warning.no_textures_for_season", new { itemName = modelName }), 3));
                    return CancelUsing(who);
                }

                // Display texture menu
                var terrainObj = new Object("100", 1, isRecipe: false, -1)
                {
                    TileLocation = targetedResouceClump.Tile
                };
                terrainObj.modData.SetFromSerialization(targetedResouceClump.modData);

                Game1.activeClickableMenu = GetMenu(terrainObj, terrainObj.TileLocation * 64f, GetTextureType(targetedResouceClump), modelName, _helper.Translation.Get("tools.name.paint_bucket"), isSprayCan: isSprayCan);

                return CancelUsing(who);
            }

            var targetedTerrain = GetTerrainFeatureAt(location, x, y);
            if (targetedTerrain != null)
            {
                if (targetedTerrain is HoeDirt hoeDirt && hoeDirt.crop is null)
                {
                    return CancelUsing(who);
                }

                if (!targetedTerrain.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME))
                {
                    if (targetedTerrain is Flooring flooring)
                    {
                        var instanceSeasonName = $"{AlternativeTextureModel.TextureType.Flooring}_{GetFlooringName(flooring)}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
                        AssignDefaultModData(targetedTerrain, instanceSeasonName, true);
                    }
                    else if (targetedTerrain is Tree tree)
                    {
                        var instanceSeasonName = $"{AlternativeTextureModel.TextureType.Tree}_{GetTreeTypeString(tree)}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
                        AssignDefaultModData(targetedTerrain, instanceSeasonName, true);
                    }
                    else if (targetedTerrain is FruitTree fruitTree)
                    {
                        Dictionary<int, string> data = Game1.content.Load<Dictionary<int, string>>("Data\\fruitTrees");
                        var saplingName = Game1.fruitTreeData.ContainsKey(fruitTree.treeId.Value) ? Game1.objectData[fruitTree.treeId.Value].Name : String.Empty;

                        var instanceSeasonName = $"{AlternativeTextureModel.TextureType.FruitTree}_{saplingName}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
                        AssignDefaultModData(targetedTerrain, instanceSeasonName, true);
                    }
                    else if (targetedTerrain is HoeDirt dirt && dirt.crop is not null)
                    {
                        var instanceName = Game1.objectData.ContainsKey(dirt.crop.netSeedIndex.Value) ? Game1.objectData[dirt.crop.netSeedIndex.Value].Name : String.Empty;
                        var instanceSeasonName = $"{AlternativeTextureModel.TextureType.Crop}_{instanceName}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
                        AssignDefaultModData(targetedTerrain, instanceSeasonName, true);
                    }
                    else if (targetedTerrain is Grass grass)
                    {
                        var instanceSeasonName = $"{AlternativeTextureModel.TextureType.Grass}_Grass_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
                        AssignDefaultModData(targetedTerrain, instanceSeasonName, true);
                    }
                    else if (targetedTerrain is Bush bush)
                    {
                        var instanceSeasonName = $"{AlternativeTextureModel.TextureType.Bush}_{PatchTemplate.GetBushTypeString(bush)}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
                        AssignDefaultModData(targetedTerrain, instanceSeasonName, true);
                    }
                    else
                    {
                        return CancelUsing(who);
                    }
                }

                var modelName = targetedTerrain.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME].Replace($"{targetedTerrain.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER]}.", String.Empty);
                if (targetedTerrain.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_SEASON) && !String.IsNullOrEmpty(targetedTerrain.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON]))
                {
                    modelName = GetModelNameWithoutSeason(modelName, targetedTerrain.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON]);
                }

                if (AlternativeTextures.textureManager.GetAvailableTextureModels(modelName, Game1.GetSeasonForLocation(Game1.currentLocation)).Count == 0)
                {
                    Game1.addHUDMessage(new HUDMessage(_helper.Translation.Get("messages.warning.no_textures_for_season", new { itemName = modelName }), 3));
                    return CancelUsing(who);
                }

                // Display texture menu
                var terrainObj = new Object("100", 1, isRecipe: false, -1)
                {
                    TileLocation = new Vector2(x, y) / 64f
                };
                terrainObj.modData.SetFromSerialization(targetedTerrain.modData);

                Game1.activeClickableMenu = GetMenu(terrainObj, terrainObj.TileLocation * 64f, GetTextureType(targetedTerrain), modelName, _helper.Translation.Get("tools.name.paint_bucket"), isSprayCan: isSprayCan);

                return CancelUsing(who);
            }

            if (location is DecoratableLocation decoratableLocation)
            {
                Point tile = new Point(x / 64, y / 64);

                var wallId = decoratableLocation.GetWallpaperID(tile.X, tile.Y);
                if (string.IsNullOrEmpty(wallId) is false)
                {
                    if (!decoratableLocation.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME) || !decoratableLocation.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME].Contains("Wallpaper"))
                    {
                        var instanceSeasonName = $"{AlternativeTextureModel.TextureType.Decoration}_Wallpaper_{Game1.GetSeasonForLocation(decoratableLocation)}";
                        AssignDefaultModData(decoratableLocation, instanceSeasonName, true);
                    }

                    var modelName = decoratableLocation.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME].Replace($"{decoratableLocation.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER]}.", String.Empty);
                    if (decoratableLocation.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_SEASON) && !string.IsNullOrEmpty(decoratableLocation.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON]))
                    {
                        modelName = GetModelNameWithoutSeason(modelName, decoratableLocation.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON]);
                    }

                    if (AlternativeTextures.textureManager.GetAvailableTextureModels(modelName, Game1.GetSeasonForLocation(Game1.currentLocation)).Count == 0)
                    {
                        Game1.addHUDMessage(new HUDMessage(_helper.Translation.Get("messages.warning.no_textures_for_season", new { itemName = modelName }), 3));
                        return CancelUsing(who);
                    }

                    // Display texture menu
                    var locationObj = new Object("100", 1, isRecipe: false, -1)
                    {
                        TileLocation = Utility.PointToVector2(tile)
                    };
                    locationObj.modData.SetFromSerialization(decoratableLocation.modData);
                    Game1.activeClickableMenu = GetMenu(locationObj, locationObj.TileLocation, GetTextureType(decoratableLocation), modelName, _helper.Translation.Get("tools.name.paint_bucket"), isSprayCan: isSprayCan);

                    return CancelUsing(who);
                }

                var floorId = decoratableLocation.GetFloorID(tile.X, tile.Y);
                if (string.IsNullOrEmpty(floorId) is false)
                {
                    if (!decoratableLocation.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME) || !decoratableLocation.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME].Contains("Floor"))
                    {
                        var instanceSeasonName = $"{AlternativeTextureModel.TextureType.Decoration}_Floor_{Game1.GetSeasonForLocation(decoratableLocation)}";
                        AssignDefaultModData(decoratableLocation, instanceSeasonName, true);
                    }

                    var modelName = decoratableLocation.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME].Replace($"{decoratableLocation.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER]}.", String.Empty);
                    if (decoratableLocation.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_SEASON) && !String.IsNullOrEmpty(decoratableLocation.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON]))
                    {
                        modelName = GetModelNameWithoutSeason(modelName, decoratableLocation.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON]);
                    }

                    if (AlternativeTextures.textureManager.GetAvailableTextureModels(modelName, Game1.GetSeasonForLocation(Game1.currentLocation)).Count == 0)
                    {
                        Game1.addHUDMessage(new HUDMessage(_helper.Translation.Get("messages.warning.no_textures_for_season", new { itemName = modelName }), 3));
                        return CancelUsing(who);
                    }

                    // Display texture menu
                    var locationObj = new Object("100", 1, isRecipe: false, -1)
                    {
                        TileLocation = Utility.PointToVector2(tile)
                    };
                    locationObj.modData.SetFromSerialization(decoratableLocation.modData);
                    Game1.activeClickableMenu = GetMenu(locationObj, locationObj.TileLocation, GetTextureType(decoratableLocation), modelName, _helper.Translation.Get("tools.name.paint_bucket"), isSprayCan: isSprayCan);

                    return CancelUsing(who);
                }
            }

            return CancelUsing(who);
        }

        private static bool UseScissors(GameLocation location, int x, int y, Farmer who)
        {
            var character = GetCharacterAt(location, x, y);
            if (character != null)
            {
                // Assign default data if none exists
                var modelType = AlternativeTextureModel.TextureType.Character;
                if (!character.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME))
                {
                    var instanceSeasonName = $"{modelType}_{GetCharacterName(character)}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
                    AssignDefaultModData(character, instanceSeasonName, true);
                }

                var modelName = character.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME].Replace($"{character.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER]}.", String.Empty);
                if (modelName.Contains(GetCharacterName(character), StringComparison.OrdinalIgnoreCase) is false)
                {
                    modelName = $"{modelType}_{GetCharacterName(character)}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
                }

                if (character.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_SEASON) && !String.IsNullOrEmpty(character.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON]))
                {
                    modelName = GetModelNameWithoutSeason(modelName, character.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON]);
                }

                if (AlternativeTextures.textureManager.GetAvailableTextureModels(modelName, Game1.GetSeasonForLocation(Game1.currentLocation)).Count == 0)
                {
                    if ((character is Pet pet && pet.GetPetData() is var petData && petData is not null && petData.Breeds is not null) || (character is FarmAnimal animal && animal.GetAnimalData() is var animalData && animalData is not null && animalData.Skins is not null))
                    {
                        // Skip no texture warning
                    }
                    else
                    {
                        Game1.addHUDMessage(new HUDMessage(_helper.Translation.Get("messages.warning.no_textures_for_season", new { itemName = modelName }), 3));
                        return CancelUsing(who);
                    }
                }

                // Display texture menu
                var obj = new Object("100", 1, isRecipe: false, -1)
                {
                    Name = character.Name,
                    displayName = character.displayName,
                    TileLocation = character.Tile,
                    Location = location
                };
                obj.modData.SetFromSerialization(character.modData);

                Game1.activeClickableMenu = new PaintBucketMenu(obj, obj.TileLocation * 64f, GetTextureType(character), modelName, uiTitle: _helper.Translation.Get("tools.name.scissors"));

                return CancelUsing(who);
            }
            return CancelUsing(who);
        }

        internal static bool UseTextureCatalogue(Farmer who)
        {
            Game1.activeClickableMenu = new CatalogueMenu(who);

            return CancelUsing(who);
        }

        private static bool CancelUsing(Farmer who)
        {
            who.CanMove = true;
            who.UsingTool = false;
            return false;
        }

        internal static bool IsAlternativeTextureTool(Item item)
        {
            if (item is StardewValley.Tools.GenericTool tool && (tool.modData.ContainsKey(AlternativeTextures.PAINT_BUCKET_FLAG) || tool.modData.ContainsKey(AlternativeTextures.SCISSORS_FLAG) || tool.modData.ContainsKey(AlternativeTextures.PAINT_BRUSH_FLAG) || tool.modData.ContainsKey(AlternativeTextures.SPRAY_CAN_FLAG) || tool.modData.ContainsKey(AlternativeTextures.CATALOGUE_FLAG)))
            {
                return true;
            }

            return false;
        }
    }
}
