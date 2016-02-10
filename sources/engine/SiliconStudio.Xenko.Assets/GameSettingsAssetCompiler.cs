using System.Linq;
using System.Threading.Tasks;
using SiliconStudio.Assets;
using SiliconStudio.Assets.Compiler;
using SiliconStudio.BuildEngine;
using SiliconStudio.Core;
using SiliconStudio.Core.IO;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Core.Serialization.Assets;
using SiliconStudio.Xenko.Data;
using SiliconStudio.Xenko.Engine.Design;

namespace SiliconStudio.Xenko.Assets
{
    public class GameSettingsAssetCompiler : AssetCompilerBase<GameSettingsAsset>
    {
        protected override void Compile(AssetCompilerContext context, string urlInStorage, UFile assetAbsolutePath, GameSettingsAsset asset, AssetCompilerResult result)
        {
            // TODO: We should ignore game settings stored in dependencies
            result.BuildSteps = new AssetBuildStep(AssetItem)
            {
                new GameSettingsCompileCommand(urlInStorage, AssetItem.Package, context.Platform, asset),
            };
        }

        private class GameSettingsCompileCommand : AssetCommand<GameSettingsAsset>
        {
            private readonly Package package;
            private readonly PlatformType platform;

            public GameSettingsCompileCommand(string url, Package package, PlatformType platform, GameSettingsAsset asset)
                : base(url, asset)
            {
                this.package = package;
                this.platform = platform;
            }

            protected override void ComputeParameterHash(BinarySerializationWriter writer)
            {
                base.ComputeParameterHash(writer);

                // Hash used parameters from package
                writer.Write(package.Id);
                writer.Write(package.UserSettings.GetValue(GameUserSettings.Effect.EffectCompilation));
                writer.Write(package.UserSettings.GetValue(GameUserSettings.Effect.RecordUsedEffects));

                // Hash platform
                writer.Write(platform);
            }

            protected override Task<ResultStatus> DoCommandOverride(ICommandContext commandContext)
            {
                var result = new GameSettings
                {
                    PackageId = package.Id,
                    PackageName = package.Meta.Name,
                    DefaultSceneUrl = AssetParameters.DefaultScene != null ? AttachedReferenceManager.GetUrl(AssetParameters.DefaultScene) : null,
                    EffectCompilation = package.UserSettings.GetValue(GameUserSettings.Effect.EffectCompilation),
                    RecordUsedEffects = package.UserSettings.GetValue(GameUserSettings.Effect.RecordUsedEffects),
                    Configurations = new PlatformConfigurations()
                };

                //start from the default platform and go down overriding

                foreach (var configuration in AssetParameters.Defaults.Where(x => !x.OfflineOnly))
                {
                    result.Configurations.Configurations.Add(new ConfigurationOverride
                    {
                        Platforms = ConfigPlatforms.None,
                        SpecificFilter = -1,
                        Configuration = configuration
                    });
                }

                foreach (var configurationOverride in AssetParameters.Overrides.Where(x => x.Configuration != null && !x.Configuration.OfflineOnly))
                {
                    result.Configurations.Configurations.Add(configurationOverride);
                }

                result.Configurations.PlatformFilters = AssetParameters.PlatformFilters;

                // TODO: Platform-specific settings have priority
                //if (platform != PlatformType.Shared)
                //{
                //    var platformProfile = package.Profiles.FirstOrDefault(o => o.Platform == platform);
                //    if (platformProfile != null && platformProfile.Properties.ContainsKey(DefaultGraphicsProfile))
                //    {
                //        var customProfile = platformProfile.Properties.Get(DefaultGraphicsProfile);
                //        result.DefaultGraphicsProfileUsed = customProfile;
                //    }
                //}

                var assetManager = new AssetManager();
                assetManager.Save(Url, result);

                return Task.FromResult(ResultStatus.Successful);
            }
        }
    }
}