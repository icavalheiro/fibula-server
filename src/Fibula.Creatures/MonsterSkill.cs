﻿// -----------------------------------------------------------------
// <copyright file="MonsterSkill.cs" company="2Dudes">
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
    using Fibula.Common.Utilities;
    using Fibula.Data.Entities.Contracts.Enumerations;
    using Fibula.Mechanics.Contracts.Abstractions;
    using Fibula.Mechanics.Contracts.Delegates;

    /// <summary>
    /// Class that represents a monster's standard skill.
    /// </summary>
    public class MonsterSkill : ISkill
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MonsterSkill"/> class.
        /// </summary>
        /// <param name="ownerCreature">The creature that owns this skill.</param>
        /// <param name="type">This skill's type.</param>
        /// <param name="defaultLevel">This skill's default level.</param>
        /// <param name="level">This skill's current level.</param>
        /// <param name="maxLevel">This skill's maximum level.</param>
        /// <param name="currentCount">The skill's current count.</param>
        /// <param name="targetForNextLevel">This skill's target count for next level.</param>
        /// <param name="targetIncreaseFactor">This skill's target increase factor for calculating the next level's target.</param>
        /// <param name="increasePerLevel">This skill's value increase level over level.</param>
        public MonsterSkill(ICreatureWithSkills ownerCreature, SkillType type, int defaultLevel, int level, int maxLevel, uint currentCount, uint targetForNextLevel, uint targetIncreaseFactor, byte increasePerLevel)
        {
            ownerCreature.ThrowIfNull(nameof(ownerCreature));

            if (defaultLevel < 0)
            {
                throw new ArgumentException($"{nameof(defaultLevel)} must not be negative.", nameof(defaultLevel));
            }

            if (maxLevel < 1)
            {
                throw new ArgumentException($"{nameof(maxLevel)} must be positive.", nameof(maxLevel));
            }

            if (maxLevel < defaultLevel)
            {
                throw new ArgumentException($"{nameof(maxLevel)} must be at least the same value as {nameof(defaultLevel)}.", nameof(maxLevel));
            }

            this.OwnerCreature = ownerCreature;
            this.Type = type;
            this.DefaultLevel = (uint)Math.Max(0, defaultLevel);
            this.MaximumLevel = (uint)Math.Max(0, maxLevel);
            this.CurrentLevel = (uint)Math.Min(this.MaximumLevel, level == 0 ? defaultLevel : level);
            this.Rate = targetIncreaseFactor / 1000d;
            this.CurrentCount = currentCount;
            this.CountForNextLevel = targetForNextLevel;
            this.PerLevelIncrease = increasePerLevel;
        }

        /// <summary>
        /// Event triggered when this skill changes.
        /// </summary>
        public event OnSkillChanged Changed;

        /// <summary>
        /// Gets the creature that owns this skill.
        /// </summary>
        public ICreatureWithSkills OwnerCreature { get; }

        /// <summary>
        /// Gets this skill's type.
        /// </summary>
        public SkillType Type { get; }

        /// <summary>
        /// Gets this skill's level.
        /// </summary>
        public uint CurrentLevel { get; private set; }

        /// <summary>
        /// Gets this skill's maximum level.
        /// </summary>
        public uint MaximumLevel { get; }

        /// <summary>
        /// Gets this skill's default level.
        /// </summary>
        public uint DefaultLevel { get; }

        /// <summary>
        /// Gets this skill's current count.
        /// </summary>
        public double CurrentCount { get; private set; }

        /// <summary>
        /// Gets the value by which to advance on skill level increase.
        /// </summary>
        public byte PerLevelIncrease { get; }

        /// <summary>
        /// Gets this skill's rate of target count increase.
        /// </summary>
        public double Rate { get; }

        /// <summary>
        /// Gets this skill's target count.
        /// </summary>
        public double CountForNextLevel { get; private set; }

        /// <summary>
        /// Gets the count at which the current level starts.
        /// </summary>
        public double CountAtStartOfLevel { get; private set; }

        /// <summary>
        /// Gets this skill's target base increase level over level.
        /// </summary>
        public double BaseTargetIncrease => throw new NotImplementedException();

        /// <summary>
        /// Gets the current percentual value between current and target counts this skill.
        /// </summary>
        public byte Percent
        {
            get
            {
                var unadjustedPercent = Math.Max(0, Math.Min(this.CurrentCount / this.CountForNextLevel, 100)) * 100;

                return (byte)Math.Floor(unadjustedPercent);
            }
        }

        /// <summary>
        /// Increases this skill's counter.
        /// </summary>
        /// <param name="value">The amount by which to increase this skills counter.</param>
        public void IncreaseCounter(double value)
        {
            var lastLevel = this.CurrentLevel;
            var lastPercentVal = this.Percent;

            this.CurrentCount = Math.Min(this.CountForNextLevel, this.CurrentCount + value);

            // Skill level advance
            if (Math.Abs(this.CurrentCount - this.CountForNextLevel) < 0.001)
            {
                this.CurrentLevel += this.PerLevelIncrease;

                this.CountAtStartOfLevel = this.CountForNextLevel;
                this.CountForNextLevel = Math.Floor(this.CountForNextLevel * this.Rate);
            }

            // Invoke any subscribers to the change event.
            if (this.CurrentLevel != lastLevel || this.Percent != lastPercentVal)
            {
                this.Changed?.Invoke(this.Type, lastLevel, lastPercentVal);
            }
        }
    }
}
