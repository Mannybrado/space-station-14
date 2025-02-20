using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Shared.Construction;
using Content.Shared.Examine;
using JetBrains.Annotations;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Server.Construction.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public class ContainerNotEmpty : IGraphCondition
    {
        [DataField("container")] public string Container { get; private set; } = string.Empty;
        [DataField("examineText")] public string? ExamineText { get; }
        [DataField("guideText")] public string? GuideText { get; }
        [DataField("guideIcon")] public SpriteSpecifier? GuideIcon { get; }

        public bool Condition(EntityUid uid, IEntityManager entityManager)
        {
            var containerSystem = entityManager.EntitySysManager.GetEntitySystem<ContainerSystem>();
            if (!containerSystem.TryGetContainer(uid, Container, out var container))
                return false;

            return container.ContainedEntities.Count != 0;
        }

        public bool DoExamine(ExaminedEvent args)
        {
            if (ExamineText == null)
                return false;

            var entity = args.Examined;

            if (!entity.TryGetComponent(out ContainerManagerComponent? containerManager) ||
                !containerManager.TryGetContainer(Container, out var container)) return false;

            if (container.ContainedEntities.Count != 0)
                return false;

            args.PushMarkup(Loc.GetString(ExamineText));
            return true;
        }

        public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
        {
            if (GuideText == null)
                yield break;

            yield return new ConstructionGuideEntry()
            {
                Localization = GuideText,
                Icon = GuideIcon,
            };
        }
    }
}
