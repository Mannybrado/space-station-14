using Content.Server.Throwing;
using Content.Shared.Acts;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.Explosion.Components
{
    [RegisterComponent]
    public class ExplosionLaunchedComponent : Component, IExAct
    {
        public override string Name => "ExplosionLaunched";

        void IExAct.OnExplosion(ExplosionEventArgs eventArgs)
        {
            if (Owner.Deleted)
                return;

            var sourceLocation = eventArgs.Source;
            var targetLocation = Owner.EntityManager.GetComponent<TransformComponent>(eventArgs.Target).Coordinates;

            if (sourceLocation.Equals(targetLocation)) return;

            var offset = (targetLocation.ToMapPos(Owner.EntityManager) - sourceLocation.ToMapPos(Owner.EntityManager));

            //Don't throw if the direction is center (0,0)
            if (offset == Vector2.Zero) return;

            var direction = offset.Normalized;

            var throwForce = eventArgs.Severity switch
            {
                ExplosionSeverity.Heavy => 30,
                ExplosionSeverity.Light => 20,
                _ => 0,
            };

            Owner.TryThrow(direction, throwForce);
        }
    }
}
