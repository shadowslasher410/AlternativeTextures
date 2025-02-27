﻿using AlternativeTextures.Framework.Models;
using AlternativeTextures.Framework.Utilities;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System;
using Object = StardewValley.Object;

namespace AlternativeTextures.Framework.Patches.StandardObjects
{
    internal class ObjectPatch : PatchTemplate
    {
        private readonly Type _object = typeof(Object);

        internal ObjectPatch(IMonitor modMonitor, IModHelper modHelper) : base(modMonitor, modHelper)
        {

        }

        internal void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(_object, nameof(Object.draw), new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) }), prefix: new HarmonyMethod(GetType(), nameof(DrawPrefix)));
            harmony.Patch(AccessTools.Method(_object, nameof(Object.drawPlacementBounds), new[] { typeof(SpriteBatch), typeof(GameLocation) }), prefix: new HarmonyMethod(GetType(), nameof(DrawPlacementBoundsPrefix)));
            harmony.Patch(AccessTools.Method(_object, nameof(Object.DayUpdate), null), postfix: new HarmonyMethod(GetType(), nameof(DayUpdatePostfix)));
            harmony.Patch(AccessTools.Method(_object, nameof(Object.rot), null), postfix: new HarmonyMethod(GetType(), nameof(RotPostfix)));
            harmony.Patch(AccessTools.Method(_object, nameof(Object.placementAction), new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(Farmer) }), postfix: new HarmonyMethod(GetType(), nameof(PlacementActionPostfix)));

            harmony.Patch(AccessTools.Constructor(_object, new[] { typeof(string), typeof(int), typeof(bool), typeof(int), typeof(int) }), postfix: new HarmonyMethod(GetType(), nameof(ObjectPostfix)));

            if (PatchTemplate.IsDGAUsed())
            {
                try
                {
                    if (Type.GetType("DynamicGameAssets.Game.CustomObject, DynamicGameAssets") is Type dgaObjectType && dgaObjectType != null)
                    {
                        harmony.Patch(AccessTools.Method(dgaObjectType, nameof(Object.draw), new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) }), prefix: new HarmonyMethod(GetType(), nameof(DrawPrefix)));
                        harmony.Patch(AccessTools.Method(dgaObjectType, nameof(Object.placementAction), new[] { typeof(GameLocation), typeof(int), typeof(int), typeof(Farmer) }), postfix: new HarmonyMethod(GetType(), nameof(PlacementActionPostfix)));
                    }

                    if (Type.GetType("DynamicGameAssets.Game.CustomBigCraftable, DynamicGameAssets") is Type dgaCraftableType && dgaCraftableType != null)
                    {
                        harmony.Patch(AccessTools.Method(dgaCraftableType, nameof(Object.draw), new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) }), prefix: new HarmonyMethod(GetType(), nameof(DrawPrefix)));
                    }
                }
                catch (Exception ex)
                {
                    _monitor.Log($"Failed to patch Dynamic Game Assets in {this.GetType().Name}: AT may not be able to override certain DGA object types!", LogLevel.Warn);
                    _monitor.Log($"Patch for DGA failed in {this.GetType().Name}: {ex}", LogLevel.Trace);
                }
            }
        }

        internal static bool DrawPrefix(Object __instance, SpriteBatch spriteBatch, int x, int y, float alpha = 1f)
        {
            if (__instance.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME))
            {
                var textureModel = AlternativeTextures.textureManager.GetSpecificTextureModel(__instance.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME]);
                if (textureModel is null)
                {
                    return true;
                }

                var textureVariation = Int32.Parse(__instance.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION]);
                if (textureVariation == -1 || AlternativeTextures.modConfig.IsTextureVariationDisabled(textureModel.GetId(), textureVariation))
                {
                    return true;
                }
                var textureOffset = textureModel.GetTextureOffset(textureVariation);

                // Get the current X index for the source tile
                var xTileOffset = __instance.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_SHEET_ID) ? __instance.ParentSheetIndex - Int32.Parse(__instance.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SHEET_ID]) : 0;
                if (__instance.showNextIndex.Value)
                {
                    xTileOffset += 1;
                }

                // Override xTileOffset if AlternativeTextureModel has an animation
                if (textureModel.HasAnimation(textureVariation))
                {
                    if (!__instance.modData.ContainsKey("AlternativeTextureCurrentFrame") || !__instance.modData.ContainsKey("AlternativeTextureFrameIndex") || !__instance.modData.ContainsKey("AlternativeTextureFrameDuration") || !__instance.modData.ContainsKey("AlternativeTextureElapsedDuration"))
                    {
                        __instance.modData["AlternativeTextureCurrentFrame"] = "0";
                        __instance.modData["AlternativeTextureFrameIndex"] = "0";
                        __instance.modData["AlternativeTextureFrameDuration"] = textureModel.GetAnimationDataAtIndex(textureVariation, 0).Duration.ToString();// Animation.ElementAt(0).Duration.ToString();
                        __instance.modData["AlternativeTextureElapsedDuration"] = "0";
                    }

                    var currentFrame = Int32.Parse(__instance.modData["AlternativeTextureCurrentFrame"]);
                    var frameIndex = Int32.Parse(__instance.modData["AlternativeTextureFrameIndex"]);
                    var frameDuration = Int32.Parse(__instance.modData["AlternativeTextureFrameDuration"]);
                    var elapsedDuration = Int32.Parse(__instance.modData["AlternativeTextureElapsedDuration"]);

                    bool isMachineActive = __instance.MinutesUntilReady > 0;
                    if (elapsedDuration >= frameDuration || textureModel.IsFrameValid(textureVariation, currentFrame, isMachineActive) is false)
                    {
                        frameIndex = textureModel.GetNextValidFrameFromIndex(textureVariation, frameIndex, isMachineActive);

                        var animationData = textureModel.GetAnimationDataAtIndex(textureVariation, frameIndex);
                        currentFrame = animationData.Frame;

                        __instance.modData["AlternativeTextureCurrentFrame"] = currentFrame.ToString();
                        __instance.modData["AlternativeTextureFrameIndex"] = frameIndex.ToString();
                        __instance.modData["AlternativeTextureFrameDuration"] = animationData.Duration.ToString();
                        __instance.modData["AlternativeTextureElapsedDuration"] = "0";
                    }
                    else
                    {
                        __instance.modData["AlternativeTextureElapsedDuration"] = (elapsedDuration + Game1.currentGameTime.ElapsedGameTime.Milliseconds).ToString();
                    }

                    xTileOffset = currentFrame;
                }
                xTileOffset *= textureModel.TextureWidth;

                if (__instance.bigCraftable.Value)
                {
                    // Get required draw values
                    Vector2 scaleFactor = __instance.getScale() * 4f;
                    Vector2 position = Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, y * 64 - 64));
                    Rectangle destination = new Rectangle((int)(position.X - scaleFactor.X / 2f) + ((__instance.shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), (int)(position.Y - scaleFactor.Y / 2f) + ((__instance.shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), (int)(64f + scaleFactor.X), (int)(128f + scaleFactor.Y / 2f));
                    float draw_layer = Math.Max(0f, (float)((y + 1) * 64 - 24) / 10000f) + (float)x * 1E-05f;

                    // Handle outliers for draw
                    if (__instance.ParentSheetIndex == 105 || __instance.ParentSheetIndex == 264)
                    {
                        draw_layer = Math.Max(0f, (float)((y + 1) * 64 + 2) / 10000f) + (float)x / 1000000f;
                    }
                    if (__instance.ParentSheetIndex == 272)
                    {
                        spriteBatch.Draw(textureModel.GetTexture(textureVariation), destination, new Rectangle(16 + xTileOffset * 2, textureOffset, 16, 32), Color.White * alpha, 0f, Vector2.Zero, SpriteEffects.None, draw_layer);
                        spriteBatch.Draw(textureModel.GetTexture(textureVariation), position + new Vector2(8.5f, 12f) * 4f, new Rectangle(32 + xTileOffset * 2, textureOffset, 16, 32), Color.White * alpha, (float)Game1.currentGameTime.TotalGameTime.TotalSeconds * -1.5f, new Vector2(7.5f, 15.5f), 4f, SpriteEffects.None, draw_layer + 1E-05f);
                        return false;
                    }

                    // Perform base game draw logic
                    spriteBatch.Draw(textureModel.GetTexture(textureVariation), destination, new Rectangle(xTileOffset, textureOffset, textureModel.TextureWidth, textureModel.TextureHeight), Color.White * alpha, 0f, Vector2.Zero, SpriteEffects.None, draw_layer);

                    // Replicate the extra draw logic from the base game
                    if (__instance.Name.Equals("Loom") && __instance.MinutesUntilReady > 0)
                    {
                        spriteBatch.Draw(textureModel.GetTexture(textureVariation), __instance.getLocalPosition(Game1.viewport) + new Vector2(32f, 0f), new Rectangle(32, textureOffset, 16, 16), Color.White * alpha, __instance.scale.X, new Vector2(8f, 8f), 4f, SpriteEffects.None, Math.Max(0f, (float)((y + 1) * 64) / 10000f + 0.0001f + (float)x * 1E-05f));
                    }
                    if (__instance.isLamp.Value && Game1.isDarkOut(Game1.currentLocation))
                    {
                        spriteBatch.Draw(Game1.mouseCursors, position + new Vector2(-32f, -32f), new Rectangle(88, 1779, 32, 32), Color.White * 0.75f, 0f, Vector2.Zero, 4f, SpriteEffects.None, Math.Max(0f, (float)((y + 1) * 64 - 20) / 10000f) + (float)x / 1000000f);
                    }
                    if (__instance.ParentSheetIndex == 126 && __instance.Quality != 0)
                    {
                        spriteBatch.Draw(FarmerRenderer.hatsTexture, position + new Vector2(-3f, -6f) * 4f, new Rectangle((__instance.Quality - 1) * 20 % FarmerRenderer.hatsTexture.Width, (__instance.Quality - 1) * 20 / FarmerRenderer.hatsTexture.Width * 20 * 4, 20, 20), Color.White * alpha, 0f, Vector2.Zero, 4f, SpriteEffects.None, Math.Max(0f, (float)((y + 1) * 64 - 20) / 10000f) + (float)x * 1E-05f);
                    }
                }
                else if (!Game1.eventUp || (Game1.CurrentEvent != null && !Game1.CurrentEvent.isTileWalkedOn(x, y)))
                {
                    if (__instance.ParentSheetIndex == 590)
                    {
                        Vector2 position2 = Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + 32 + ((__instance.shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), y * 64 + 32 + ((__instance.shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0)));
                        Color color = Color.White * alpha;
                        Vector2 origin = new Vector2(8f, 8f);

                        int artifactOffset = ((Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 1200.0 <= 400.0) ? ((int)(Game1.currentGameTime.TotalGameTime.TotalMilliseconds % 400.0 / 100.0) * 16) : 0);
                        spriteBatch.Draw(textureModel.GetTexture(textureVariation), position2, new Rectangle(artifactOffset, textureOffset, 16, 16), color, 0f, origin, (__instance.scale.Y > 1f) ? __instance.getScale().Y : 4f, __instance.Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (float)(__instance.isPassable() ? __instance.GetBoundingBoxAt(x, y).Top : __instance.GetBoundingBoxAt(x, y).Bottom) / 10000f);
                        return false;
                    }
                    if (__instance.Fragility != 2)
                    {
                        spriteBatch.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + 32, y * 64 + 51 + 4)), Game1.shadowTexture.Bounds, Color.White * alpha, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 4f, SpriteEffects.None, (float)__instance.GetBoundingBoxAt(x, y).Bottom / 15000f);
                    }

                    Color color2 = Color.White * alpha;
                    Vector2 origin2 = new Vector2(8f, 8f);
                    Vector2 position3 = Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + 32 + ((__instance.shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), y * 64 + 32 + ((__instance.shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0)));
                    if (__instance.ParentSheetIndex == 746)
                    {
                        origin2 = Vector2.Zero;
                        position3 = Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, y * 64 - 64));
                    }

                    spriteBatch.Draw(textureModel.GetTexture(textureVariation), position3, new Rectangle(xTileOffset, textureOffset, textureModel.TextureWidth, textureModel.TextureHeight), color2, 0f, origin2, (__instance.scale.Y > 1f) ? __instance.getScale().Y : 4f, __instance.Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None, (float)(__instance.isPassable() ? __instance.GetBoundingBoxAt(x, y).Top : __instance.GetBoundingBoxAt(x, y).Bottom) / 10000f);
                    if (__instance.heldObject.Value != null && __instance.IsSprinkler())
                    {
                        // Unhandled sprinkler attachments
                        return false;
                    }
                }

                // Check if product is ready to display (if applicable)
                if (!__instance.readyForHarvest.Value)
                {
                    return false;
                }

                float base_sort = (float)((y + 1) * 64) / 10000f + __instance.TileLocation.X / 50000f;
                if (__instance.IsTapper() || __instance.QualifiedItemId.Equals("(BC)MushroomLog"))
                {
                    base_sort += 0.02f;
                }
                float yOffset = 4f * (float)Math.Round(Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0), 2);
                spriteBatch.Draw(Game1.mouseCursors, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 - 8, (float)(y * 64 - 96 - 16) + yOffset)), new Rectangle(141, 465, 20, 24), Color.White * 0.75f, 0f, Vector2.Zero, 4f, SpriteEffects.None, base_sort + 1E-06f);
                if (__instance.heldObject.Value == null)
                {
                    return false;
                }

                ParsedItemData heldItemData = ItemRegistry.GetDataOrErrorItem(__instance.heldObject.Value.QualifiedItemId);
                Texture2D texture = heldItemData.GetTexture();
                ColoredObject coloredObj = __instance.heldObject.Value as ColoredObject;
                if (coloredObj != null)
                {
                    coloredObj.drawInMenu(spriteBatch, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, (float)(y * 64) - 96f - 8f + yOffset)), 1f, 0.75f, base_sort + 1.1E-05f);
                    return false;
                }
                spriteBatch.Draw(texture, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64 + 32, (float)(y * 64 - 64 - 8) + yOffset)), heldItemData.GetSourceRect(), Color.White * 0.75f, 0f, new Vector2(8f, 8f), 4f, SpriteEffects.None, base_sort + 1E-05f);
                if (__instance.heldObject.Value.Stack > 1)
                {
                    __instance.heldObject.Value.DrawMenuIcons(spriteBatch, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, (float)(y * 64 - 64 - 32) + yOffset - 4f)), 1f, 1f, base_sort + 1.2E-05f, StackDrawType.Draw, Color.White);
                }
                else if (__instance.heldObject.Value.Quality > 0)
                {
                    __instance.heldObject.Value.DrawMenuIcons(spriteBatch, Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, (float)(y * 64 - 64 - 32) + yOffset - 4f)), 1f, 1f, base_sort + 1.2E-05f, StackDrawType.HideButShowQuality, Color.White);
                }

                return false;
            }

            return true;
        }

        internal static bool DrawPlacementBoundsPrefix(Object __instance, SpriteBatch spriteBatch, GameLocation location)
        {
            if (__instance.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME))
            {
                __instance.modData["AlternativeTextureNameCached"] = __instance.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME];
                __instance.modData.Remove(ModDataKeys.ALTERNATIVE_TEXTURE_NAME);
            }
            return true;
        }

        internal static void DayUpdatePostfix(Object __instance)
        {
            if (__instance.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME))
            {
                var textureModel = AlternativeTextures.textureManager.GetSpecificTextureModel(__instance.modData[ModDataKeys.ALTERNATIVE_TEXTURE_NAME]);
                if (textureModel is null)
                {
                    return;
                }

                var textureVariation = Int32.Parse(__instance.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION]);
                if (textureVariation == -1 || AlternativeTextures.modConfig.IsTextureVariationDisabled(textureModel.GetId(), textureVariation))
                {
                    return;
                }

                if (__instance.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_SHEET_ID) && __instance.ParentSheetIndex != Int32.Parse(__instance.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SHEET_ID]))
                {
                    __instance.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SHEET_ID] = __instance.ParentSheetIndex.ToString();
                }
            }
        }

        internal static void RotPostfix(Object __instance)
        {
            if (__instance.modData.ContainsKey(ModDataKeys.ALTERNATIVE_TEXTURE_NAME))
            {
                __instance.modData[ModDataKeys.ALTERNATIVE_TEXTURE_VARIATION] = "-1";
            }
        }

        internal static void PlacementActionPostfix(Object __instance, bool __result, GameLocation location, int x, int y, Farmer who = null)
        {
            if (!__result)
            {
                return;
            }

            // Used for most objects, except for those whom are converted upon placement (such as Fences)
            var placedObject = PatchTemplate.GetObjectAt(location, x, y);
            if (placedObject is null)
            {
                var terrainFeature = GetTerrainFeatureAt(location, x, y);
                if (terrainFeature is Flooring flooring)
                {
                    flooring.modData[ModDataKeys.ALTERNATIVE_TEXTURE_SHEET_ID] = __instance.ParentSheetIndex.ToString();

                    var flooringName = $"{AlternativeTextureModel.TextureType.Flooring}_{GetFlooringName(flooring)}";
                    var flooringSeasonName = $"{flooringName}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";
                    if (AlternativeTextures.textureManager.DoesObjectHaveAlternativeTexture(flooringName) && AlternativeTextures.textureManager.DoesObjectHaveAlternativeTexture(flooringSeasonName))
                    {
                        var result = Game1.random.Next(2) > 0 ? AssignModData(flooring, flooringSeasonName, true) : AssignModData(flooring, flooringName, false);
                        return;
                    }
                    else
                    {
                        if (AlternativeTextures.textureManager.DoesObjectHaveAlternativeTexture(flooringName))
                        {
                            AssignModData(flooring, flooringName, false);
                            return;
                        }

                        if (AlternativeTextures.textureManager.DoesObjectHaveAlternativeTexture(flooringSeasonName))
                        {
                            AssignModData(flooring, flooringSeasonName, true);
                            return;
                        }
                    }

                    AssignDefaultModData(flooring, flooringSeasonName, true);
                }
                return;
            }

            var modelType = placedObject is Furniture ? AlternativeTextureModel.TextureType.Furniture : AlternativeTextureModel.TextureType.Craftable;
            var instanceName = $"{modelType}_{GetObjectName(placedObject)}";
            var instanceSeasonName = $"{instanceName}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";

            if (HasCachedTextureName(__instance) is true)
            {
                return;
            }
            else if (AlternativeTextures.textureManager.DoesObjectHaveAlternativeTexture(instanceName) && AlternativeTextures.textureManager.DoesObjectHaveAlternativeTexture(instanceSeasonName))
            {
                var result = Game1.random.Next(2) > 0 ? AssignModData(placedObject, instanceSeasonName, true, placedObject.bigCraftable.Value) : AssignModData(placedObject, instanceName, false, placedObject.bigCraftable.Value);
                return;
            }
            else
            {
                if (AlternativeTextures.textureManager.DoesObjectHaveAlternativeTexture(instanceName))
                {
                    AssignModData(placedObject, instanceName, false, placedObject.bigCraftable.Value);
                    return;
                }

                if (AlternativeTextures.textureManager.DoesObjectHaveAlternativeTexture(instanceSeasonName))
                {
                    AssignModData(placedObject, instanceSeasonName, true, placedObject.bigCraftable.Value);
                    return;
                }
            }

            AssignDefaultModData(placedObject, instanceSeasonName, true, placedObject.bigCraftable.Value);
        }

        private static void ObjectPostfix(Object __instance, string itemId, int initialStack, bool isRecipe = false, int price = -1, int quality = 0)
        {
            // Handle only artifact spots
            if (itemId.Equals("590") is false)
            {
                return;
            }

            var instanceName = $"{AlternativeTextureModel.TextureType.ArtifactSpot}_{GetObjectName(__instance)}";
            var instanceSeasonName = $"{instanceName}_{Game1.GetSeasonForLocation(Game1.currentLocation)}";

            if (AlternativeTextures.textureManager.DoesObjectHaveAlternativeTexture(instanceName) && AlternativeTextures.textureManager.DoesObjectHaveAlternativeTexture(instanceSeasonName))
            {
                var result = Game1.random.Next(2) > 0 ? AssignModData(__instance, instanceSeasonName, true, __instance.bigCraftable.Value) : AssignModData(__instance, instanceName, false, __instance.bigCraftable.Value);
                return;
            }
            else
            {
                if (AlternativeTextures.textureManager.DoesObjectHaveAlternativeTexture(instanceName))
                {
                    AssignModData(__instance, instanceName, false, __instance.bigCraftable.Value);
                    return;
                }

                if (AlternativeTextures.textureManager.DoesObjectHaveAlternativeTexture(instanceSeasonName))
                {
                    AssignModData(__instance, instanceSeasonName, true, __instance.bigCraftable.Value);
                    return;
                }
            }

            AssignDefaultModData(__instance, instanceSeasonName, true, __instance.bigCraftable.Value);
        }
    }
}
