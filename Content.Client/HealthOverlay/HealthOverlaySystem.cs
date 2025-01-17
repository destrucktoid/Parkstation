using System.Collections.Generic;
using Content.Client.HealthOverlay.UI;
using Content.Shared.Damage;
using Content.Shared.GameTicking;
using Content.Shared.Mobs.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Content.Shared.Examine;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Content.Client.Examine;
using Content.Shared.CCVar;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.HealthOverlay
{
    [UsedImplicitly]
    public sealed class HealthOverlaySystem : EntitySystem
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IEntityManager _entities = default!;

        private readonly Dictionary<EntityUid, HealthOverlayGui> _guis = new();
        private EntityUid? _attachedEntity;
        private bool _enabled;

        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value)
                {
                    return;
                }

                _enabled = value;

                foreach (var gui in _guis.Values)
                {
                    gui.SetVisibility(value);
                }
            }
        }

        public bool CheckLOS = false;
        public bool OrganicsOnly = false;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<PlayerAttachSysMessage>(HandlePlayerAttached);
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            foreach (var gui in _guis.Values)
            {
                gui.Dispose();
            }

            _guis.Clear();
            _attachedEntity = default;
        }

        private void HandlePlayerAttached(PlayerAttachSysMessage message)
        {
            _attachedEntity = message.AttachedEntity;
        }

        public override void FrameUpdate(float frameTime)
        {
            base.Update(frameTime);

            if (!_enabled)
            {
                return;
            }

            if (_attachedEntity is not {} ent || Deleted(ent))
            {
                return;
            }

            var viewBox = _eyeManager.GetWorldViewport().Enlarged(2.0f);
            var ourXform = Transform(_attachedEntity.Value);

            foreach (var (mobState, damageable) in EntityManager.EntityQuery<MobStateComponent, DamageableComponent>())
            {
                var entity = mobState.Owner;

                if (OrganicsOnly && damageable.DamageContainerID != "Biological")
                    continue;

                if (Transform(ent).MapID != Transform(entity).MapID ||
                    !viewBox.Contains(Transform(entity).WorldPosition))
                {
                    if (_guis.TryGetValue(entity, out var oldGui))
                    {
                        _guis.Remove(entity);
                        oldGui.Dispose();
                    }

                    continue;
                }

                var distance = (Transform(entity).Coordinates.Position - viewBox.Center).Length;


                if (CheckLOS && !ExamineSystemShared.InRangeUnOccluded(ourXform.MapPosition, Transform(entity).MapPosition, distance, e => e == _attachedEntity.Value, entMan: _entities))
                {
                    if (_guis.TryGetValue(entity, out var oldGui))
                    {
                        _guis.Remove(entity);
                        oldGui.Dispose();
                    }

                    continue;
                }

                if (_guis.ContainsKey(entity))
                {
                    continue;
                }

                var gui = new HealthOverlayGui(entity);
                _guis.Add(entity, gui);
            }
        }
    }
}
