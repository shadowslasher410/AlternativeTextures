﻿using AlternativeTextures.Framework.Interfaces;
using AlternativeTextures.Framework.Models;
using AlternativeTextures.Framework.Patches.Entities;
using AlternativeTextures.Framework.Utilities;
using AlternativeTextures.Framework.Utilities.Extensions;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using static AlternativeTextures.Framework.Models.AlternativeTextureModel;
using Object = StardewValley.Object;

namespace AlternativeTextures.Framework.Patches
{
    internal class PatchTemplate
    {
        internal static IMonitor _monitor;
        internal static IModHelper _helper;

        internal PatchTemplate(IMonitor modMonitor, IModHelper modHelper)
        {
            _monitor = modMonitor;
            _helper = modHelper;
        }

        internal static GenericTool GetPaintBucketTool()
        {
            var paintBucket = new GenericTool();
            paintBucket.modData[AlternativeTextures.PAINT_BUCKET_FLAG] = true.ToString();

            return paintBucket;
        }

        internal static GenericTool GetScissorsTool()
        {
            var scissors = new GenericTool();
            scissors.modData[AlternativeTextures.SCISSORS_FLAG] = true.ToString();

            return scissors;
        }

        internal static GenericTool GetPaintBrushTool()
        {
            var paintBrush = new GenericTool();
            paintBrush.modData[AlternativeTextures.PAINT_BRUSH_FLAG] = null;

            return paintBrush;
        }

        internal static GenericTool GetSprayCanTool(bool isRare = false)
        {
            var sprayCan = new GenericTool();
            sprayCan.modData[AlternativeTextures.SPRAY_CAN_FLAG] = null;

            if (isRare || Game1.random.Next(100) <= 10)
            {
                sprayCan.modData[AlternativeTextures.SPRAY_CAN_RARE] = null;
            }

            return sprayCan;
        }

        internal static GenericTool GetCatalogueTool()
        {
            var catalogue = new GenericTool();
            catalogue.modData[AlternativeTextures.CATALOGUE_FLAG] = null;

            return catalogue;
        }

        internal static string GetModelNameWithoutSeason(string modelName, string season)
        {
            return modelName.ReplaceLastInstance($"_{season}", String.Empty);
        }

        internal static string GetObjectName(Object obj)
        {
            // Perform separate check for DGA objects, before using check for vanilla objects
            if (IsDGAUsed() && AlternativeTextures.apiManager.GetDynamicGameAssetsApi() is IDynamicGameAssetsApi api && api != null)
            {
                var dgaId = api.GetDGAItemId(obj);
                if (dgaId != null)
                {
                    return dgaId;
                }
            }

            if (obj.bigCraftable.Value)
            {
                if (!Game1.bigCraftableData.ContainsKey(obj.ItemId))
                {
                    return obj.name;
                }

                return Game1.bigCraftableData[obj.ItemId].Name;
            }
            else if (obj is Furniture)
            {
                var dataSheet = Game1.content.Load<Dictionary<string, string>>("Data\\Furniture");
                if (!dataSheet.ContainsKey(obj.ItemId))
                {
                    return obj.name;
                }

                return dataSheet[obj.ItemId].Split('/')[0];
            }
            else
            {
                if (!Game1.objectData.ContainsKey(obj.ItemId))
                {
                    return obj.name;
                }

                return Game1.objectData[obj.ItemId].Name;
            }
        }

        internal static string GetCharacterName(Character character)
        {
            if (character is Child child)
            {
                if (child.Age >= 3)
                {
                    return $"{CharacterPatch.TODDLER_NAME_PREFIX}_{(child.Gender == 0 ? "Male" : "Female")}_{(child.darkSkinned.Value ? "Dark" : "Light")}";
                }
                return $"{CharacterPatch.BABY_NAME_PREFIX}_{(child.darkSkinned.Value ? "Dark" : "Light")}";
            }

            if (character is FarmAnimal animal)
            {
                var animalName = animal.type.Value;
                if (animal.isBaby())
                {
                    animalName = "Baby" + (animal.type.Value.Equals("Duck") ? "White Chicken" : animal.type.Value);
                }
                else if (animal.GetAnimalData() is not null && string.IsNullOrEmpty(animal.GetAnimalData().HarvestedTexture) is false && string.IsNullOrEmpty(animal.currentProduce.Value) is true)
                {
                    animalName = "Sheared" + animalName;
                }
                return animalName;
            }

            if (character is Horse horse)
            {
                // Tractor mod compatibility: -794739 is the ID used by Tractor Mod for determining if a Stable is really a garage
                if (horse.modData.ContainsKey("Pathoschild.TractorMod"))
                {
                    return "Tractor";
                }

                return "Horse";
            }

            if (character is Pet pet)
            {
                return pet.petType.Value;
            }

            return character.Name;
        }

        internal static string GetBuildingName(Building building)
        {
            // Tractor mod compatibility: -794739 is the ID used by Tractor Mod for determining if a Stable is really a garage
            if (building.maxOccupants.Value == -794739)
            {
                return "Tractor Garage";
            }
            else if (building.buildingType.Value == "Farmhouse")
            {
                return $"{building.buildingType.Value}_{Game1.MasterPlayer.HouseUpgradeLevel}";
            }

            return building.buildingType.Value;
        }

        internal static Object GetObjectAt(GameLocation location, int x, int y)
        {
            // If object is furniture and currently has something on top of it, check that instead
            foreach (var furniture in location.furniture.Where(c => c.heldObject.Value != null))
            {
                if (furniture.boundingBox.Value.Contains(x, y))
                {
                    return furniture.heldObject.Value;
                }
            }

            // Prioritize checking non-rug furniture first
            foreach (var furniture in location.furniture.Where(c => c.furniture_type.Value != Furniture.rug))
            {
                if (furniture.boundingBox.Value.Contains(x, y))
                {
                    return furniture;
                }
            }

            // Replicating GameLocation.getObjectAt, but doing objects before rugs
            // Doing this so the object on top of rugs are given instead of the latter
            var tile = new Vector2(x / 64, y / 64);
            if (location.objects.ContainsKey(tile))
            {
                return location.objects[tile];
            }

            return location.getObjectAt(x, y);
        }

        internal static Building GetBuildingAt(GameLocation location, int x, int y)
        {
            Vector2 tile = new Vector2(x / 64, y / 64);
            if (location.buildings.FirstOrDefault(b => b.occupiesTile(tile)) is Building building && building != null)
            {
                return building;
            }

            return null;
        }

        internal static TerrainFeature GetTerrainFeatureAt(GameLocation location, int x, int y)
        {
            Vector2 tile = new Vector2(x / 64, y / 64);
            if (!location.terrainFeatures.ContainsKey(tile))
            {
                if (location.largeTerrainFeatures is not null)
                {
                    return location.largeTerrainFeatures.FirstOrDefault(t => t is not null && t.Tile == tile);
                }
                return null;
            }

            return location.terrainFeatures[tile];
        }

        internal static ResourceClump GetResourceClumpAt(GameLocation location, int x, int y)
        {
            Vector2 tile = new Vector2(x / 64, y / 64);
            if (!location.resourceClumps.Any(r => r.occupiesTile((int)tile.X, (int)tile.Y)))
            {
                return null;
            }

            return location.resourceClumps.First(r => r.occupiesTile((int)tile.X, (int)tile.Y));
        }

        internal static Character GetCharacterAt(GameLocation location, int x, int y)
        {
            var tileLocation = new Vector2(x / 64, y / 64);
            var rectangle = new Rectangle(x, y, 64, 64);
            if (location.IsBuildableLocation())
            {
                foreach (var animal in location.animals.Values)
                {
                    if (animal.GetBoundingBox().Intersects(rectangle))
                    {
                        return animal;
                    }
                }
            }
            if (location is AnimalHouse animalHouse)
            {
                foreach (var animal in animalHouse.animals.Values)
                {
                    if (animal.GetBoundingBox().Intersects(rectangle))
                    {
                        return animal;
                    }
                }
            }

            foreach (var specialCharacter in location.characters.Where(c => c is Horse || c is Pet))
            {
                if (specialCharacter is Horse horse && horse.GetBoundingBox().Intersects(rectangle))
                {
                    return horse;
                }

                if (specialCharacter is Pet pet && pet.GetBoundingBox().Intersects(rectangle))
                {
                    return pet;
                }
            }

            return location.isCharacterAtTile(tileLocation);
        }

        internal static string GetFlooringName(Flooring floor)
        {
            return Game1.objectData.ContainsKey(floor.GetData().ItemId) ? Game1.objectData[floor.GetData().ItemId].Name : string.Empty;
        }

        internal static string GetTreeTypeString(Tree tree)
        {
            switch (tree.treeType.Value)
            {
                case Tree.bushyTree:
                    return "Oak";
                case Tree.leafyTree:
                    return "Maple";
                case Tree.pineTree:
                    return "Pine";
                case Tree.mahoganyTree:
                    return "Mahogany";
                case Tree.mushroomTree:
                    return "Mushroom";
                case Tree.palmTree:
                    return "Palm_1";
                case Tree.palmTree2:
                    return "Palm_2";
                default:
                    return String.Empty;
            }
        }

        internal static string GetBushTypeString(Bush bush)
        {
            switch (bush.size.Value)
            {
                case 0:
                    return "Small";
                case 1:
                    return bush.townBush.Value ? "Town" : "Medium";
                case 2:
                    return "Large";
                case 3:
                    return "Tea";
                case 4:
                    return "Walnut";
                default:
                    return String.Empty;
            }
        }

        internal static TextureType GetTextureType(object obj)
        {
            switch (obj)
            {
                case Character character:
                    return TextureType.Character;
                case Flooring floor:
                    return TextureType.Flooring;
                case Tree tree:
                    return TextureType.Tree;
                case FruitTree fruitTree:
                    return TextureType.FruitTree;
                case Grass grass:
                    return TextureType.Grass;
                case Bush bush:
                    return TextureType.Bush;
                case ResourceClump resourceClump:
                    return TextureType.GiantCrop;
                case TerrainFeature hoeDirt:
                    return TextureType.Crop;
                case Building building:
                    return TextureType.Building;
                case Furniture furniture:
                    return TextureType.Furniture;
                case Object craftable:
                    return TextureType.Craftable;
                case DecoratableLocation location:
                    return TextureType.Decoration;
                default:
                    return TextureType.Unknown;
            }
        }

        internal static bool HasCachedTextureName<T>(T type, bool probe = false)
        {
            if (type is Object obj && obj.modData.ContainsKey("AlternativeTextureNameCached"))
            {
                if (!probe)
                {
                    obj.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = obj.modData["AlternativeTextureNameCached"];
                    obj.modData.Remove("AlternativeTextureNameCached");
                }

                return true;
            }

            return false;
        }

        internal static bool IsPositionNearMailbox(GameLocation location, Point mailboxPosition, int x, int y)
        {
            bool isNearMailbox = (mailboxPosition.X == x) && (mailboxPosition.Y == y || mailboxPosition.Y == y + 1);
            return isNearMailbox;
        }

        internal static bool IsDGAUsed()
        {
            return _helper.ModRegistry.IsLoaded("spacechase0.DynamicGameAssets");
        }

        internal static bool IsSolidFoundationsUsed()
        {
            return _helper.ModRegistry.IsLoaded("PeacefulEnd.SolidFoundations");
        }

        internal static bool IsDGAObject(object obj)
        {
            if (IsDGAUsed() && AlternativeTextures.apiManager.GetDynamicGameAssetsApi() is IDynamicGameAssetsApi api && api != null)
            {
                var dgaId = api.GetDGAItemId(obj);
                if (dgaId != null)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsTextureRandomnessEnabled<T>(T type)
        {
            switch (type)
            {
                case Flooring:
                    return AlternativeTextures.modConfig.UseRandomTexturesWhenPlacingFlooring;
                case FruitTree:
                    return AlternativeTextures.modConfig.UseRandomTexturesWhenPlacingFruitTree;
                case Tree:
                    return AlternativeTextures.modConfig.UseRandomTexturesWhenPlacingTree;
                case HoeDirt:
                    return AlternativeTextures.modConfig.UseRandomTexturesWhenPlacingHoeDirt;
                case Grass:
                    return AlternativeTextures.modConfig.UseRandomTexturesWhenPlacingGrass;
                case Furniture:
                    return AlternativeTextures.modConfig.UseRandomTexturesWhenPlacingFurniture;
                case Object obj:
                    if (obj is not null && obj.Name == "Artifact Spot")
                    {
                        return AlternativeTextures.modConfig.UseRandomTexturesWhenSpawningArtifactSpots;
                    }
                    return AlternativeTextures.modConfig.UseRandomTexturesWhenPlacingObject;
                case FarmAnimal:
                    return AlternativeTextures.modConfig.UseRandomTexturesWhenPlacingFarmAnimal;
                case Monster:
                    return AlternativeTextures.modConfig.UseRandomTexturesWhenPlacingMonster;
                case Building:
                    return AlternativeTextures.modConfig.UseRandomTexturesWhenPlacingBuilding;
            }

            return true;
        }

        internal static bool AssignDefaultModData<T>(T type, string modelName, bool trackSeason = false, bool trackSheetId = false)
        {
            if (HasCachedTextureName(type))
            {
                return false;
            }

            var textureModel = new AlternativeTextureModel() { Owner = AlternativeTextures.DEFAULT_OWNER, Season = trackSeason ? Game1.GetSeasonForLocation(Game1.currentLocation).ToString() : String.Empty };
            switch (type)
            {
                case Object obj:
                    AssignObjectModData(obj, modelName, textureModel, -1, trackSeason, trackSheetId);
                    return true;
                case TerrainFeature terrain:
                    AssignTerrainFeatureModData(terrain, modelName, textureModel, -1, trackSeason);
                    return true;
                case Character character:
                    AssignCharacterModData(character, modelName, textureModel, -1, trackSeason);
                    return true;
                case Building building:
                    AssignBuildingModData(building, modelName, textureModel, -1, trackSeason);
                    return true;
                case DecoratableLocation decoratableLocation:
                    AssignDecoratableLocationModData(decoratableLocation, modelName, textureModel, -1, trackSeason);
                    return true;
                case GameLocation gameLocation when gameLocation.IsBuildableLocation():
                    AssignGameLocationModData(gameLocation, modelName, textureModel, -1, trackSeason);
                    return true;
            }

            return false;
        }

        internal static bool AssignModData<T>(T type, string modelName, bool trackSeason = false, bool trackSheetId = false)
        {
            if (HasCachedTextureName(type) || IsTextureRandomnessEnabled(type) is false)
            {
                return false;
            }

            var textureModel = AlternativeTextures.textureManager.GetRandomTextureModel(modelName);

            var selectedVariation = Game1.random.Next(-1, textureModel.Variations);
            if (textureModel.DefaultVariation is not null)
            {
                selectedVariation = textureModel.DefaultVariation.Value;
            }
            else if (textureModel.ManualVariations.Count() > 0)
            {
                var weightedSelection = textureModel.ManualVariations.Where(v => v.ChanceWeight > Game1.random.NextDouble()).ToList();
                if (weightedSelection.Count > 0)
                {
                    var randomWeightedSelection = Game1.random.Next(!textureModel.ManualVariations.Any(v => v.Id == -1) ? -1 : 0, weightedSelection.Count());
                    selectedVariation = randomWeightedSelection == -1 ? -1 : weightedSelection[randomWeightedSelection].Id;
                }
                else
                {
                    return AssignDefaultModData<T>(type, modelName, trackSeason, trackSheetId);
                }
            }

            switch (type)
            {
                case Object obj:
                    AssignObjectModData(obj, modelName, textureModel, selectedVariation, trackSeason, trackSheetId);
                    return true;
                case TerrainFeature terrain:
                    AssignTerrainFeatureModData(terrain, modelName, textureModel, selectedVariation, trackSeason);
                    return true;
                case Character character:
                    AssignCharacterModData(character, modelName, textureModel, selectedVariation, trackSeason);
                    return true;
                case Building building:
                    AssignBuildingModData(building, modelName, textureModel, selectedVariation, trackSeason);
                    return true;
                case DecoratableLocation decoratableLocation:
                    AssignDecoratableLocationModData(decoratableLocation, modelName, textureModel, selectedVariation, trackSeason);
                    return true;
            }

            return false;
        }

        private static void AssignObjectModData(Object obj, string modelName, AlternativeTextureModel textureModel, int variation, bool trackSeason = false, bool trackSheetId = false)
        {
            obj.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = textureModel.Owner;
            obj.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = String.Concat(textureModel.Owner, ".", modelName);

            if (trackSeason && !String.IsNullOrEmpty(textureModel.Season))
            {
                obj.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON] = Game1.GetSeasonForLocation(Game1.currentLocation).ToString();
            }

            if (trackSheetId)
            {
                obj.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SHEET_ID] = obj.ParentSheetIndex.ToString();
            }

            obj.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = variation.ToString();
        }

        private static void AssignTerrainFeatureModData(TerrainFeature terrain, string modelName, AlternativeTextureModel textureModel, int variation, bool trackSeason = false)
        {
            terrain.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = textureModel.Owner;
            terrain.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = String.Concat(textureModel.Owner, ".", modelName);

            if (trackSeason && !String.IsNullOrEmpty(textureModel.Season))
            {
                terrain.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON] = Game1.GetSeasonForLocation(terrain.Location).ToString();
            }

            terrain.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = variation.ToString();
        }

        private static void AssignCharacterModData(Character character, string modelName, AlternativeTextureModel textureModel, int variation, bool trackSeason = false)
        {
            character.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = textureModel.Owner;
            character.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = String.Concat(textureModel.Owner, ".", modelName);

            if (trackSeason && !String.IsNullOrEmpty(textureModel.Season))
            {
                character.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON] = Game1.GetSeasonForLocation(character.currentLocation).ToString();
            }

            character.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = variation.ToString();
        }

        private static void AssignBuildingModData(Building building, string modelName, AlternativeTextureModel textureModel, int variation, bool trackSeason = false)
        {
            building.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = textureModel.Owner;
            building.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = String.Concat(textureModel.Owner, ".", modelName);

            if (trackSeason && !String.IsNullOrEmpty(textureModel.Season))
            {
                building.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON] = Game1.GetSeasonForLocation(Game1.currentLocation).ToString();
            }

            building.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = variation.ToString();
        }

        private static void AssignDecoratableLocationModData(DecoratableLocation decoratableLocation, string modelName, AlternativeTextureModel textureModel, int variation, bool trackSeason = false)
        {
            decoratableLocation.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = textureModel.Owner;
            decoratableLocation.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = String.Concat(textureModel.Owner, ".", modelName);

            if (trackSeason && !String.IsNullOrEmpty(textureModel.Season))
            {
                decoratableLocation.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON] = Game1.GetSeasonForLocation(Game1.currentLocation).ToString();
            }

            decoratableLocation.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = variation.ToString();
        }

        private static void AssignGameLocationModData(GameLocation gameLocation, string modelName, AlternativeTextureModel textureModel, int variation, bool trackSeason = false)
        {
            gameLocation.modData[ModDataKeys.ALTERNATIVE_TEXTURE_OWNER] = textureModel.Owner;
            gameLocation.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME] = String.Concat(textureModel.Owner, ".", modelName);

            if (trackSeason && !String.IsNullOrEmpty(textureModel.Season))
            {
                gameLocation.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SEASON] = Game1.GetSeasonForLocation(Game1.currentLocation).ToString();
            }

            gameLocation.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = variation.ToString();
        }
    }
}
