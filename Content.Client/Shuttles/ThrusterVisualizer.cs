using Content.Shared.Shuttles.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.Shuttles
{
    public sealed class ThrusterVisualizer : AppearanceVisualizer
    {
        public override void OnChangeData(AppearanceComponent component)
        {
            base.OnChangeData(component);

            if (!component.Owner.TryGetComponent(out SpriteComponent? spriteComponent)) return;

            component.TryGetData(ThrusterVisualState.State, out bool state);

            switch (state)
            {
                case true:
                    spriteComponent.LayerSetVisible(ThrusterVisualLayers.ThrustOn, true);

                    if (component.TryGetData(ThrusterVisualState.Thrusting, out bool thrusting) && thrusting)
                    {
                        if (spriteComponent.LayerMapTryGet(ThrusterVisualLayers.Thrusting, out _))
                        {
                            spriteComponent.LayerSetVisible(ThrusterVisualLayers.Thrusting, true);
                        }

                        if (spriteComponent.LayerMapTryGet(ThrusterVisualLayers.ThrustingUnshaded, out _))
                        {
                            spriteComponent.LayerSetVisible(ThrusterVisualLayers.ThrustingUnshaded, true);
                        }
                    }
                    else
                    {
                        DisableThrusting(component, spriteComponent);
                    }

                    break;
                case false:
                    spriteComponent.LayerSetVisible(ThrusterVisualLayers.ThrustOn, false);
                    DisableThrusting(component, spriteComponent);
                    break;
            }
        }

        private void DisableThrusting(AppearanceComponent component, SpriteComponent spriteComponent)
        {
            if (spriteComponent.LayerMapTryGet(ThrusterVisualLayers.Thrusting, out _))
            {
                spriteComponent.LayerSetVisible(ThrusterVisualLayers.Thrusting, false);
            }

            if (spriteComponent.LayerMapTryGet(ThrusterVisualLayers.ThrustingUnshaded, out _))
            {
                spriteComponent.LayerSetVisible(ThrusterVisualLayers.ThrustingUnshaded, false);
            }
        }
    }

    public enum ThrusterVisualLayers : byte
    {
        Base,
        ThrustOn,
        Thrusting,
        ThrustingUnshaded,
    }
}
