using System;
using Content.Server.Animals.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Nutrition.Components;
using Content.Shared.Nutrition.Components;
using Content.Server.Chemistry.Components.SolutionManager;
using Content.Server.DoAfter;
using Robust.Shared.Localization;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Content.Server.Popups;

namespace Content.Server.Animals.Systems
{
    /// <summary>
    ///     Gives ability to living beings with acceptable hunger level to produce milkable reagents.
    /// </summary>
    internal class UdderSystem : EntitySystem
    {
        [Dependency] private readonly SolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<UdderComponent, GetAlternativeVerbsEvent>(AddMilkVerb);
            SubscribeLocalEvent<UdderComponent, MilkingFinishedEvent>(OnMilkingFinished);
            SubscribeLocalEvent<UdderComponent, MilkingFailEvent>(OnMilkingFailed);
        }

        public override void Update(float frameTime)
        {
            foreach (var udder in EntityManager.EntityQuery<UdderComponent>(false))
            {
                udder.AccumulatedFrameTime += frameTime;

                if (udder.AccumulatedFrameTime < udder.UpdateRate)
                    continue;

                // Actually there is food digestion so no problem with instant reagent generation "OnFeed"
                if (udder.Owner.TryGetComponent<HungerComponent>(out var hunger))
                {
                    hunger.HungerThresholds.TryGetValue(HungerThreshold.Peckish, out var targetThreshold);

                    // Is there enough nutrition to produce reagent?
                    if (hunger.CurrentHunger < targetThreshold)
                        continue;
                }

                if (!_solutionContainerSystem.TryGetSolution(udder.OwnerUid, udder.TargetSolutionName, out var solution))
                    continue;

                //TODO: toxins from bloodstream !?
                _solutionContainerSystem.TryAddReagent(udder.OwnerUid, solution, udder.ReagentId, udder.QuantityPerUpdate, out var accepted);
                udder.AccumulatedFrameTime = 0;
            }
        }

        private void AttemptMilk(EntityUid uid, EntityUid userUid, EntityUid containerUid, UdderComponent? udder = null)
        {
            if (!Resolve(uid, ref udder))
                return;

            if (udder.BeingMilked)
            {
                _popupSystem.PopupEntity(Loc.GetString("udder-system-already-milking"), uid, Filter.Entities(userUid));
                return;
            }

            udder.BeingMilked = true;

            var doargs = new DoAfterEventArgs(userUid, 5, default, uid)
            {
                BreakOnUserMove = true,
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnTargetMove = true,
                MovementThreshold = 1.0f,
                TargetFinishedEvent = new MilkingFinishedEvent(userUid, containerUid),
                TargetCancelledEvent = new MilkingFailEvent()
            };

            _doAfterSystem.DoAfter(doargs);
        }

        private void OnMilkingFinished(EntityUid uid, UdderComponent udder, MilkingFinishedEvent ev)
        {
            udder.BeingMilked = false;

            if (!_solutionContainerSystem.TryGetSolution(uid, udder.TargetSolutionName, out var solution))
                return;

            if (!_solutionContainerSystem.TryGetRefillableSolution(ev.ContainerUid, out var targetSolution))
                return;

            var quantity = solution.TotalVolume;
            if(quantity == 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("udder-system-dry"), uid, Filter.Entities(ev.UserUid));
                return;
            }

            if (quantity > targetSolution.AvailableVolume)
                quantity = targetSolution.AvailableVolume;

            var split = _solutionContainerSystem.SplitSolution(uid, solution, quantity);
            _solutionContainerSystem.TryAddSolution(ev.ContainerUid, targetSolution, split);

            var container = EntityManager.GetEntity(ev.ContainerUid);
            _popupSystem.PopupEntity(Loc.GetString("udder-system-success", ("amount", quantity), ("target", container)), uid, Filter.Entities(ev.UserUid));
        }

        private void OnMilkingFailed(EntityUid uid, UdderComponent component, MilkingFailEvent ev)
        {
            component.BeingMilked = false;
        }

        private void AddMilkVerb(EntityUid uid, UdderComponent component, GetAlternativeVerbsEvent args)
        {
            if (args.Using == null ||
                 !args.CanInteract ||
                 !args.Using.HasComponent<RefillableSolutionComponent>())
                return;

            Verb verb = new();
            verb.Act = () =>
            {
                AttemptMilk(uid, args.User.Uid, args.Using.Uid, component);
            };
            verb.Text = Loc.GetString("udder-system-verb-milk");
            verb.Priority = 2;
            args.Verbs.Add(verb);
        }

        private class MilkingFinishedEvent : EntityEventArgs
        {
            public EntityUid UserUid;
            public EntityUid ContainerUid;

            public MilkingFinishedEvent(EntityUid userUid, EntityUid containerUid)
            {
                UserUid = userUid;
                ContainerUid = containerUid;
            }
        }

        private class MilkingFailEvent : EntityEventArgs
        { }
    }
}
