﻿// -----------------------------------------------------------------
// <copyright file="Creature.cs" company="2Dudes">
// Copyright (c) | Jose L. Nunez de Caceres et al.
// https://linkedin.com/in/nunezdecaceres
//
// All Rights Reserved.
//
// Licensed under the MIT License. See LICENSE in the project root for license information.
// </copyright>
// -----------------------------------------------------------------

namespace Fibula.Creatures
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Fibula.Common;
    using Fibula.Common.Contracts.Abstractions;
    using Fibula.Common.Contracts.Enumerations;
    using Fibula.Common.Contracts.Structs;
    using Fibula.Common.Utilities;
    using Fibula.Creatures.Contracts.Abstractions;
    using Fibula.Creatures.Contracts.Constants;
    using Fibula.Creatures.Contracts.Enumerations;
    using Fibula.Creatures.Contracts.Structs;
    using Fibula.Data.Entities.Contracts.Abstractions;
    using Fibula.Data.Entities.Contracts.Enumerations;
    using Fibula.Data.Entities.Contracts.Structs;
    using Fibula.Items.Contracts.Abstractions;
    using Fibula.Mechanics.Contracts.Abstractions;
    using Fibula.Mechanics.Contracts.Delegates;

    /// <summary>
    /// Class that represents all creatures in the game.
    /// </summary>
    public abstract class Creature : Thing, ICreatureThatSensesOthers
    {
        /// <summary>
        /// The default index for body containers.
        /// </summary>
        private const byte DefaultBodyContainerIndex = 0;

        /// <summary>
        /// Lock used when assigning creature ids.
        /// </summary>
        private static readonly object IdLock = new object();

        /// <summary>
        /// Counter to assign new ids to new creatures created.
        /// </summary>
        private static uint idCounter = 1;

        /// <summary>
        /// Stores the creatures that this creature is aware of.
        /// </summary>
        private readonly Dictionary<ICreature, AwarenessLevel> creatureAwarenessMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="Creature"/> class.
        /// </summary>
        /// <param name="creationMetadata">The metadata for this player.</param>
        protected Creature(ICreatureEntity creationMetadata)
        {
            creationMetadata.ThrowIfNull(nameof(creationMetadata));
            creationMetadata.Name.ThrowIfNullOrWhiteSpace(nameof(creationMetadata.Name));

            if (creationMetadata.MaxHitpoints == 0)
            {
                throw new ArgumentException($"{nameof(creationMetadata.MaxHitpoints)} must be positive.", nameof(creationMetadata.MaxHitpoints));
            }

            lock (Creature.IdLock)
            {
                this.Id = idCounter++;
            }

            this.Name = creationMetadata.Name;
            this.Article = creationMetadata.Article;
            this.CorpseTypeId = creationMetadata.Corpse;

            this.LastMovementCostModifier = 1;

            this.Outfit = new Outfit
            {
                Id = 0,
                ItemIdLookAlike = 0,
            };

            this.Stats = new Dictionary<CreatureStat, IStat>();

            this.creatureAwarenessMap = new Dictionary<ICreature, AwarenessLevel>();

            this.Stats.Add(CreatureStat.HitPoints, new Stat(CreatureStat.HitPoints, creationMetadata.CurrentHitpoints == default ? creationMetadata.MaxHitpoints : creationMetadata.CurrentHitpoints, creationMetadata.MaxHitpoints));
            this.Stats[CreatureStat.HitPoints].Changed += this.RaiseStatChange;

            this.Stats.Add(CreatureStat.CarryStrength, new Stat(CreatureStat.CarryStrength, 150, CreatureConstants.MaxCreatureCarryStrength));
            this.Stats[CreatureStat.CarryStrength].Changed += this.RaiseStatChange;

            this.Stats.Add(CreatureStat.BaseSpeed, new Stat(CreatureStat.BaseSpeed, 70, CreatureConstants.MaxCreatureSpeed));
            this.Stats[CreatureStat.BaseSpeed].Changed += this.RaiseStatChange;
        }

        /// <summary>
        /// Event triggered when this creature's stat has changed.
        /// </summary>
        public event OnCreatureStatChanged StatChanged;

        /// <summary>
        /// Event called when this creature senses another.
        /// </summary>
        public event OnCreatureSensed CreatureSensed;

        /// <summary>
        /// Event called when this creature sees another.
        /// </summary>
        public event OnCreatureSeen CreatureSeen;

        /// <summary>
        /// Event called when this creature loses track of a sensed creature.
        /// </summary>
        public event OnCreatureLost CreatureLost;

        /// <summary>
        /// Gets the type id of this creature.
        /// </summary>
        public override ushort TypeId => CreatureConstants.CreatureTypeId;

        /// <summary>
        /// Gets the creature's in-game id.
        /// </summary>
        public uint Id { get; }

        /// <summary>
        /// Gets the article in the name of the creature.
        /// </summary>
        public string Article { get; }

        /// <summary>
        /// Gets the name of the creature.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the corpse of the creature.
        /// </summary>
        public ushort CorpseTypeId { get; }

        /// <summary>
        /// Gets the location where this thing is being carried at, which is null for creatures.
        /// </summary>
        public override Location? CarryLocation
        {
            get
            {
                // Creatures cannot be carried, so this should always be null!
                return null;
            }
        }

        /// <summary>
        /// Gets or sets this creature's light level.
        /// </summary>
        public byte EmittedLightLevel { get; protected set; }

        /// <summary>
        /// Gets or sets this creature's light color.
        /// </summary>
        public byte EmittedLightColor { get; protected set; }

        /// <summary>
        /// Gets this creature's speed.
        /// </summary>
        public abstract ushort Speed { get; }

        /// <summary>
        /// Gets or sets this creature's variable speed.
        /// </summary>
        public int VariableSpeed { get; protected set; }

        /// <summary>
        /// Gets this creature's flags.
        /// </summary>
        public uint Flags { get; private set; }

        /// <summary>
        /// Gets or sets this creature's blood type.
        /// </summary>
        public BloodType BloodType { get; protected set; }

        /// <summary>
        /// Gets or sets the inventory for the creature.
        /// </summary>
        public abstract IInventory Inventory { get; protected set; }

        /// <summary>
        /// Gets or sets the outfit of this creature.
        /// </summary>
        public Outfit Outfit { get; set; }

        /// <summary>
        /// Gets or sets the direction that this creature is facing.
        /// </summary>
        public Direction Direction { get; set; }

        /// <summary>
        /// Gets or sets the creature's last move modifier.
        /// </summary>
        public decimal LastMovementCostModifier { get; set; }

        /// <summary>
        /// Gets or sets this creature's walk plan.
        /// </summary>
        public WalkPlan WalkPlan { get; set; }

        /// <summary>
        /// Gets the creatures who are sensed by this creature.
        /// </summary>
        public IEnumerable<ICreature> TrackedCreatures => this.creatureAwarenessMap.Keys;

        /// <summary>
        /// Gets a value indicating whether the creature is considered dead.
        /// </summary>
        public bool IsDead => this.Stats[CreatureStat.HitPoints].Current == 0;

        /// <summary>
        /// Gets a value indicating whether this creature can walk.
        /// </summary>
        public bool CanWalk => this.Speed > 0;

        /// <summary>
        /// Gets the current stats information for the creature.
        /// </summary>
        /// <remarks>
        /// The key is a <see cref="CreatureStat"/>, and the value is an <see cref="IStat"/>.
        /// </remarks>
        public IDictionary<CreatureStat, IStat> Stats { get; private set; }

        /// <summary>
        /// Flags a creature as being sensed.
        /// </summary>
        /// <param name="creature">The creature which is sensed.</param>
        public void StartSensingCreature(ICreature creature)
        {
            creature.ThrowIfNull(nameof(creature));

            // Do not track self.
            if (creature == this)
            {
                return;
            }

            // Add a creature and mark it as sensed only.
            if (this.creatureAwarenessMap.TryAdd(creature, AwarenessLevel.Sensed))
            {
                this.CreatureSensed?.Invoke(this, creature);
            }

            // check if we can actually see the creature, and mark it as such if so.
            if (this.creatureAwarenessMap[creature] == AwarenessLevel.Sensed && this.CanSee(creature))
            {
                this.creatureAwarenessMap[creature] = AwarenessLevel.Seen;

                this.CreatureSeen?.Invoke(this, creature);
            }
        }

        /// <summary>
        /// Flags a creature as no longed being sensed.
        /// </summary>
        /// <param name="creature">The creature that is lost.</param>
        public void StopSensingCreature(ICreature creature)
        {
            creature.ThrowIfNull(nameof(creature));

            // We don't track self anyways.
            if (creature == this)
            {
                return;
            }

            if (this.creatureAwarenessMap.Remove(creature))
            {
                this.CreatureLost?.Invoke(this, creature);
            }
        }

        /// <summary>
        /// Checks if this creature can see a given creature.
        /// </summary>
        /// <param name="otherCreature">The creature to check against.</param>
        /// <returns>True if this creature can see the given creature, false otherwise.</returns>
        public bool CanSee(ICreature otherCreature)
        {
            otherCreature.ThrowIfNull(nameof(otherCreature));

            // (!otherCreature.IsInvisible || this.CanSeeInvisible) &&
            return this.CanSee(otherCreature.Location);
        }

        /// <summary>
        /// Checks if this creature can see a given location.
        /// </summary>
        /// <param name="location">The location to check against.</param>
        /// <returns>True if this creature can see the given location, false otherwise.</returns>
        public bool CanSee(Location location)
        {
            if (this.Location.Z <= 7)
            {
                // we are on ground level or above (7 -> 0)
                // view is from 7 -> 0
                if (location.Z > 7)
                {
                    return false;
                }
            }
            else if (this.Location.Z >= 8)
            {
                // we are underground (8 -> 15)
                // view is +/- 2 from the floor we stand on
                if (Math.Abs(this.Location.Z - location.Z) > 2)
                {
                    return false;
                }
            }

            var offsetZ = this.Location.Z - location.Z;

            if (location.X >= this.Location.X - 8 + offsetZ && location.X <= this.Location.X + 9 + offsetZ &&
                location.Y >= this.Location.Y - 6 + offsetZ && location.Y <= this.Location.Y + 7 + offsetZ)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to add a <see cref="IThing"/> to this container.
        /// </summary>
        /// <param name="thingFactory">A reference to the factory of things to use.</param>
        /// <param name="thing">The <see cref="IThing"/> to add to the container.</param>
        /// <param name="index">Optional. The index at which to add the <see cref="IThing"/>. Defaults to byte.MaxValue, which instructs to add the <see cref="IThing"/> at any free index.</param>
        /// <returns>A tuple with a value indicating whether the attempt was at least partially successful, and false otherwise. If the result was only partially successful, a remainder of the thing may be returned.</returns>
        public (bool result, IThing remainder) AddContent(IThingFactory thingFactory, IThing thing, byte index = byte.MaxValue)
        {
            // TODO: try to add at that specific body container.
            return (false, thing);
        }

        /// <summary>
        /// Attempts to remove a thing from this container.
        /// </summary>
        /// <param name="thingFactory">A reference to the factory of things to use.</param>
        /// <param name="thing">The <see cref="IThing"/> to remove from the container.</param>
        /// <param name="index">Optional. The index from which to remove the <see cref="IThing"/>. Defaults to byte.MaxValue, which instructs to remove the <see cref="IThing"/> if found at any index.</param>
        /// <param name="amount">Optional. The amount of the <paramref name="thing"/> to remove.</param>
        /// <returns>A tuple with a value indicating whether the attempt was at least partially successful, and false otherwise. If the result was only partially successful, a remainder of the thing may be returned.</returns>
        public (bool result, IThing remainder) RemoveContent(IThingFactory thingFactory, ref IThing thing, byte index = byte.MaxValue, byte amount = 1)
        {
            // TODO: try to delete from that specific body container.
            throw new NotImplementedException();
        }

        /// <summary>
        /// Attempts to replace a <see cref="IThing"/> from this container with another.
        /// </summary>
        /// <param name="thingFactory">A reference to the factory of things to use.</param>
        /// <param name="fromThing">The <see cref="IThing"/> to remove from the container.</param>
        /// <param name="toThing">The <see cref="IThing"/> to add to the container.</param>
        /// <param name="index">Optional. The index from which to replace the <see cref="IThing"/>. Defaults to byte.MaxValue, which instructs to replace the <see cref="IThing"/> if found at any index.</param>
        /// <param name="amount">Optional. The amount of the <paramref name="fromThing"/> to replace.</param>
        /// <returns>A tuple with a value indicating whether the attempt was at least partially successful, and false otherwise. If the result was only partially successful, a remainder of the thing may be returned.</returns>
        public (bool result, IThing remainderToChange) ReplaceContent(IThingFactory thingFactory, IThing fromThing, IThing toThing, byte index = byte.MaxValue, byte amount = 1)
        {
            if (this.Inventory[index] is IContainerItem bodyContainer)
            {
                return bodyContainer.ReplaceContent(thingFactory, fromThing, toThing, DefaultBodyContainerIndex, amount);
            }

            return (false, fromThing);
        }

        /// <summary>
        /// Attempts to find an <see cref="IThing"/> whitin this container.
        /// </summary>
        /// <param name="index">The index at which to look for the <see cref="IThing"/>.</param>
        /// <returns>The <see cref="IThing"/> found at the index, if any was found.</returns>
        public IThing FindThingAtIndex(byte index)
        {
            if (this.Inventory[index] is IContainerItem bodyContainer)
            {
                return bodyContainer[DefaultBodyContainerIndex];
            }

            return null;
        }

        /// <summary>
        /// Provides a string describing the current creature for logging purposes.
        /// </summary>
        /// <returns>The string to log.</returns>
        public override string DescribeForLogger()
        {
            return $"{(string.IsNullOrWhiteSpace(this.Article) ? string.Empty : $"{this.Article} ")}{this.Name}";
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">The other object to compare against.</param>
        /// <returns>True if the current object is equal to the other parameter, false otherwise.</returns>
        public bool Equals([AllowNull] ICreature other)
        {
            return this.Id == other?.Id;
        }

        /// <summary>
        /// Raises the <see cref="StatChanged"/> event for this creature on the given stat.
        /// </summary>
        /// <param name="forStat">The stat that changed.</param>
        /// <param name="previousLevel">The previous stat value.</param>
        /// <param name="previousPercent">The previous percent value of the stat.</param>
        protected void RaiseStatChange(CreatureStat forStat, uint previousLevel, byte previousPercent)
        {
            if (!this.Stats.ContainsKey(forStat))
            {
                return;
            }

            this.StatChanged?.Invoke(this, this.Stats[forStat], previousLevel, previousPercent);
        }
    }
}
